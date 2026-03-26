using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 出生点标记组件
/// 挂载到场景中的空 GameObject 上，用于标记玩家可能出现的位置。
/// SceneTransitionManager 通过 spawnPointId 查找对应的出生点。
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    private static readonly Dictionary<string, SpawnPoint> registry = new Dictionary<string, SpawnPoint>();

    [Tooltip("出生点唯一标识（同场景内不可重复）")]
    public string spawnPointId;

    public Vector3 position => transform.position;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(spawnPointId)) return;

        if (registry.ContainsKey(spawnPointId))
            Debug.LogWarning($"[SpawnPoint] 重复的 spawnPointId: {spawnPointId}");

        registry[spawnPointId] = this;
    }

    private void OnDisable()
    {
        if (registry.ContainsKey(spawnPointId) && registry[spawnPointId] == this)
            registry.Remove(spawnPointId);
    }

    /// <summary>
    /// 根据 ID 查找当前场景中的出生点
    /// </summary>
    public static SpawnPoint Find(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        registry.TryGetValue(id, out var point);
        return point;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, 0.3f);
        Gizmos.DrawIcon(transform.position, "d_Favorite Icon", true);
    }
}
