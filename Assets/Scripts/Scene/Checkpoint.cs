using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 自动存档点
/// 挂载到带有 Collider2D (IsTrigger) 的 GameObject 上。
/// 玩家进入区域时自动保存当前场景和位置。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class Checkpoint : MonoBehaviour
{
    [Tooltip("存档点唯一标识（跨场景不可重复）")]
    [SerializeField] private string checkpointId;

    [Header("状态")]
    [SerializeField] private bool activated;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (SaveManager.Instance == null) return;

        string sceneName = SceneManager.GetActiveScene().name;
        Vector2 position = transform.position;

        SaveManager.Instance.Save(sceneName, position, checkpointId);
        activated = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = activated ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.4f);
    }
}
