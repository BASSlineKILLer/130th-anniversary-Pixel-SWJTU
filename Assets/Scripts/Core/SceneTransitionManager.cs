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
    private float teleportImmunityEndTime;

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
    /// 跨场景传送：加载目标场景并将玩家放到指定出生点
    /// </summary>
    public void TransitionToScene(string sceneName, string spawnPointId)
    {
        pendingSpawnPointId = spawnPointId;
        GrantTeleportImmunity();
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// 同场景内传送：直接移动玩家到目标位置
    /// </summary>
    public void TeleportPlayer(Vector2 targetPosition)
    {
        var player = FindPlayer();
        if (player == null) return;

        player.position = new Vector3(targetPosition.x, targetPosition.y, player.position.z);
        GrantTeleportImmunity();
        SnapCamera(player);
        Debug.Log($"[SceneTransition] 同场景传送到 ({targetPosition.x:F1},{targetPosition.y:F1})");
    }

    /// <summary>
    /// 从存档恢复：加载存档场景并定位到存档位置
    /// </summary>
    public void LoadFromSave(SaveData data)
    {
        if (data == null) return;

        pendingSpawnPointId = data.checkpointId;
        SceneManager.LoadScene(data.sceneName);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (string.IsNullOrEmpty(pendingSpawnPointId)) return;

        PlacePlayerAtSpawnPoint(pendingSpawnPointId);
        pendingSpawnPointId = null;
        GrantTeleportImmunity();

        var player = FindPlayer();
        if (player != null)
            SnapCamera(player);
    }

    private void PlacePlayerAtSpawnPoint(string spawnPointId)
    {
        var spawnPoint = SpawnPoint.Find(spawnPointId);
        if (spawnPoint == null)
        {
            Debug.LogWarning($"[SceneTransition] 未找到出生点: {spawnPointId}");
            TryPlacePlayerFromSave();
            return;
        }

        var player = FindPlayer();
        if (player == null) return;

        player.position = new Vector3(spawnPoint.position.x, spawnPoint.position.y, player.position.z);
        Debug.Log($"[SceneTransition] 玩家已放置到出生点: {spawnPointId}");
    }

    private void TryPlacePlayerFromSave()
    {
        if (SaveManager.Instance == null) return;

        var data = SaveManager.Instance.Load();
        if (data == null) return;

        var player = FindPlayer();
        if (player == null) return;

        player.position = new Vector3(data.positionX, data.positionY, player.position.z);
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
