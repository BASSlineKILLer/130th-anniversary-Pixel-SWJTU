using UnityEngine;

/// <summary>
/// 挂在"图书馆传送门"触发器上。传送门始终激活。
/// 玩家进入时，通过 <see cref="MedalManager.IsNodeUnlocked"/> 查询勋章是否达标：
///   - 已解锁 → <see cref="SceneTransitionManager.TransitionToScene"/> 传送
///   - 未解锁 → <see cref="MedalManager.ShowLockedHint"/> 弹提示
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneShift : MonoBehaviour
{
    [Tooltip("目标场景名（Build Settings 中的场景名）")]
    public string targetSceneName = "Library";

    [Tooltip("可选：传送到目标场景后的出生点 ID")]
    public string targetSpawnPointId;

    [Tooltip("节点 ID，需与 MedalProgressConfig 中对应节点的 nodeId 一致")]
    public string nodeId = "Library";

    [Tooltip("玩家 Tag")]
    public string playerTag = "Player";

    private bool triggered;

    private void Reset()
    {
        // 自动把 Collider 设为 trigger
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag(playerTag)) return;
        if (triggered) return; // 单次触发，防止传送中抖动
        if (MedalManager.Instance == null)
        {
            Debug.LogWarning("[SceneShift] MedalManager.Instance 为空，无法判定解锁状态");
            return;
        }

        if (MedalManager.Instance.IsNodeUnlocked(nodeId))
        {
            triggered = true;
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionToScene(targetSceneName, targetSpawnPointId);
            else
                Debug.LogError("[SceneShift] SceneTransitionManager.Instance 为空，无法传送");
        }
        else
        {
            MedalManager.Instance.ShowLockedHint(nodeId);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag(playerTag)) triggered = false;
    }
}
