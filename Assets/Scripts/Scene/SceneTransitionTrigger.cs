using UnityEngine;

/// <summary>
/// 场景传送点
/// 玩家进入触发区域后切换到对应地标场景
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneTransitionTrigger : MonoBehaviour
{
    [Header("目标场景")]
    public string targetSceneName;
    public Vector2 playerSpawnPosition;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameSceneManager.Instance?.LoadScene(targetSceneName);
        }
    }
}
