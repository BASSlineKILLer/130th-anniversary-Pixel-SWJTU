using System.Collections;
using Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景切换管理器（单例 + 跨场景持久）
/// 负责跨场景传送、同场景传送、加载后玩家定位。
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance { get; private set; }

    [Header("玩家配置")]
    [Tooltip("Player 的 Tag，用于场景加载后查找玩家")]
    [SerializeField] private string playerTag = "Player";

    private const float TELEPORT_IMMUNITY_DURATION = 0.5f;

    private string pendingSpawnPointId;
    private SaveData pendingSaveData; // 增加用于读档失败或manual_save时的降级定位
    private float teleportImmunityEndTime;
    private bool shouldPlayOpenOnLoad;
    private bool isTransitioning;
    private float? pendingPlayerScaleX; // 用于跨场景保持朝向

    /// <summary>
    /// 传送免疫期：传送后短暂禁止再次触发传送点，防止反复横跳
    /// </summary>
    public bool IsTeleportImmune => Time.time < teleportImmunityEndTime;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    /// <summary>
    /// 跨场景传送：加载目标场景并将玩家放到指定出生点（带圆形转场）
    /// </summary>
    public void TransitionToScene(string sceneName, string spawnPointId)
    {
        StartCoroutine(TransitionCoroutine(sceneName, spawnPointId));
    }

    private IEnumerator TransitionCoroutine(string sceneName, string spawnPointId)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        if (CircleWipeTransition.Instance != null)
            yield return CircleWipeTransition.Instance.PlayClose();

        // 在离开场景前，记录主角当前的朝向（localScale.x）
        var player = FindPlayer();
        if (player != null)
        {
            pendingPlayerScaleX = player.localScale.x;
        }

        pendingSpawnPointId = spawnPointId;
        pendingSaveData = null; // 正常跨场景传送不使用存档数据
        shouldPlayOpenOnLoad = true;
        GrantTeleportImmunity();
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 同场景内传送：直接移动玩家到目标位置（带圆形转场）
    /// </summary>
    public void TeleportPlayer(Vector2 targetPosition)
    {
        StartCoroutine(TeleportCoroutine(targetPosition));
    }

    private IEnumerator TeleportCoroutine(Vector2 targetPosition)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        if (CircleWipeTransition.Instance != null)
            yield return CircleWipeTransition.Instance.PlayClose();

        var player = FindPlayer();
        if (player != null)
        {
            player.position = new Vector3(targetPosition.x, targetPosition.y, player.position.z);
            GrantTeleportImmunity();
            SnapCamera(player);
        }

        if (CircleWipeTransition.Instance != null)
            yield return CircleWipeTransition.Instance.PlayOpen();

        isTransitioning = false;
        Debug.Log($"[SceneTransition] 同场景传送到 ({targetPosition.x:F1},{targetPosition.y:F1})");
    }

    /// <summary>
    /// 从存档恢复：加载存档场景并定位到存档位置（带圆形转场）
    /// </summary>
    public void LoadFromSave(SaveData data)
    {
        if (data == null) return;
        StartCoroutine(LoadFromSaveCoroutine(data));
    }

    private IEnumerator LoadFromSaveCoroutine(SaveData data)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        if (CircleWipeTransition.Instance != null)
            yield return CircleWipeTransition.Instance.PlayClose();

        pendingSpawnPointId = data.checkpointId;
        pendingSaveData = data; // 记录整个 SaveData 供稍后定位使用
        shouldPlayOpenOnLoad = true;
        SceneManager.LoadScene(data.sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 处理玩家定位
        if (!string.IsNullOrEmpty(pendingSpawnPointId))
        {
            PlacePlayerAtSpawnPoint(pendingSpawnPointId);
            pendingSpawnPointId = null;
            pendingSaveData = null; // 用完后清空
            GrantTeleportImmunity();


            var player = FindPlayer();
            if (player != null)
                SnapCamera(player);
        }

        // 跨场景转场：播放打开动画
        if (shouldPlayOpenOnLoad)
        {
            shouldPlayOpenOnLoad = false;
            StartCoroutine(PlayOpenAndFinishTransition());
        }
    }

    private IEnumerator PlayOpenAndFinishTransition()
    {
        // 等两帧让 Cinemachine 的 LateUpdate 完全就位
        yield return null;
        yield return null;

        // 再次强制对齐相机到玩家，防止 Cinemachine damping 导致偏移
        var player = FindPlayer();
        if (player != null)
            SnapCamera(player);

        // 再等一帧让 SnapCamera 的效果生效
        yield return null;

        if (CircleWipeTransition.Instance != null)
            yield return CircleWipeTransition.Instance.PlayOpen();
        isTransitioning = false;
    }

    private void PlacePlayerAtSpawnPoint(string spawnPointId)
    {
        var player = FindPlayer();
        if (player == null) return;

        var spawnPoint = SpawnPoint.Find(spawnPointId);
        if (spawnPoint == null)
        {
            Debug.LogWarning($"[SceneTransition] 未在当前场景 '{SceneManager.GetActiveScene().name}' 找到名为 '{spawnPointId}' 的出生点！");
            
            // 如果是读档或者 manual_save，允许降级到坐标定位
            if (pendingSaveData != null)
            {
                player.position = new Vector3(pendingSaveData.positionX, pendingSaveData.positionY, player.position.z);
                Debug.Log($"[SceneTransition] 降级：使用存档坐标定位到 ({pendingSaveData.positionX:F1}, {pendingSaveData.positionY:F1})");
            }
            else
            {
                Debug.LogError($"[SceneTransition] 严重错误：场景切换后缺少出生点 '{spawnPointId}'，并且没有存档数据！已停止定位主角。");
                return; // 不做任何移动，避免传送到上个房间缓存的坐标
            }
        }
        else
        {
            player.position = new Vector3(spawnPoint.position.x, spawnPoint.position.y, player.position.z);
            Debug.Log($"[SceneTransition] 玩家已放置到出生点: {spawnPointId}");
        }
        
        // 恢复跨场景前的朝向
        if (pendingPlayerScaleX.HasValue)
        {
            player.localScale = new Vector3(pendingPlayerScaleX.Value, player.localScale.y, player.localScale.z);
            pendingPlayerScaleX = null;
        }
    }

    private void GrantTeleportImmunity()
    {
        teleportImmunityEndTime = Time.time + TELEPORT_IMMUNITY_DURATION;
    }

    /// <summary>
    /// 强制相机立即 snap 到玩家位置，消除 damping 导致的偏移
    /// </summary>
    private void SnapCamera(Transform player)
    {
        var mainCam = Camera.main;
        if (mainCam != null)
        {
            var pos = mainCam.transform.position;
            mainCam.transform.position = new Vector3(player.position.x, player.position.y, pos.z);
        }

        foreach (var vcam in FindObjectsOfType<CinemachineVirtualCamera>())
            vcam.PreviousStateIsValid = false;
    }

    private Transform FindPlayer()
    {
        var player = GameObject.FindGameObjectWithTag(playerTag);
        if (player == null)
        {
            Debug.LogWarning("[SceneTransition] 未找到 Player（请确认 Player 的 Tag 设置为 'Player'）");
            return null;
        }
        return player.transform;
    }
}
