using UnityEngine;

/// <summary>
/// 传送点触发器
/// 挂载到带有 Collider2D (IsTrigger) 的 GameObject 上。
/// 支持同场景传送和跨场景传送两种模式。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class TeleportTrigger : MonoBehaviour
{
    public enum TeleportMode { SameScene, CrossScene }

    [Header("传送模式")]
    [SerializeField] private TeleportMode mode = TeleportMode.CrossScene;

    [Header("同场景传送（SameScene 模式）")]
    [Tooltip("目标位置的 Transform")]
    [SerializeField] private Transform targetPosition;

    [Header("跨场景传送（CrossScene 模式）")]
    [Tooltip("目标场景名称（需在 Build Settings 中注册）")]
    [SerializeField] private string targetSceneName;
    [Tooltip("目标场景中的出生点 ID")]
    [SerializeField] private string targetSpawnPointId;

    [Header("防重复触发")]
    [SerializeField] private float cooldown = 1f;

    private float lastTriggerTime = float.NegativeInfinity;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (Time.time - lastTriggerTime < cooldown) return;
        if (SceneTransitionManager.Instance != null && SceneTransitionManager.Instance.IsTeleportImmune) return;

        lastTriggerTime = Time.time;
        ExecuteTeleport();
    }

    private void ExecuteTeleport()
    {
        if (SceneTransitionManager.Instance == null)
        {
            Debug.LogError("[TeleportTrigger] SceneTransitionManager 不存在，请在场景中放置");
            return;
        }

        switch (mode)
        {
            case TeleportMode.SameScene:
                TeleportSameScene();
                break;
            case TeleportMode.CrossScene:
                TeleportCrossScene();
                break;
        }
    }

    private void TeleportSameScene()
    {
        if (targetPosition == null)
        {
            Debug.LogWarning("[TeleportTrigger] 同场景传送缺少 targetPosition");
            return;
        }
        SceneTransitionManager.Instance.TeleportPlayer(targetPosition.position);
    }

    private void TeleportCrossScene()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[TeleportTrigger] 跨场景传送缺少 targetSceneName");
            return;
        }
        SceneTransitionManager.Instance.TransitionToScene(targetSceneName, targetSpawnPointId);
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = mode == TeleportMode.SameScene ? Color.cyan : Color.magenta;
        Gizmos.DrawWireCube(transform.position, GetComponent<Collider2D>()?.bounds.size ?? Vector3.one * 0.5f);

        if (mode == TeleportMode.SameScene && targetPosition != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(transform.position, targetPosition.position);
        }
    }
}
