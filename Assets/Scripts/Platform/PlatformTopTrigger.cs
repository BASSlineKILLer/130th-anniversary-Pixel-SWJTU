using UnityEngine;

/// <summary>
/// 【已弃用】请使用 PlatformEntryTrigger。
/// 保留此文件是为了兼容旧场景，新功能请迁移到 PlatformEntryTrigger。
/// </summary>
[System.Obsolete("请使用 PlatformEntryTrigger 替代")]
[RequireComponent(typeof(Collider2D))]
public class PlatformTopTrigger : MonoBehaviour
{
    // 空实现，提醒用户迁移到新组件
#if UNITY_EDITOR
    void OnEnable()
    {
        Debug.LogWarning($"[PlatformTopTrigger] {gameObject.name} 使用了已弃用的组件，请替换为 PlatformEntryTrigger");
    }
#endif
}
