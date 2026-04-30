using UnityEngine;

/// <summary>
/// 图书馆传送门的"解锁副作用"承载者。
/// 解锁判定与弹窗已统一搬到 MedalManager + MedalProgressConfig：
/// 在 config 对应节点的 onUnlock 中拖入本 GameObject 的 <see cref="UnlockLibrary"/>。
/// </summary>
public class SceneShift : MonoBehaviour
{
    [Tooltip("图书馆传送物体；解锁时会被激活")]
    public GameObject libraryPortal;

    void Start()
    {
        // 根据持久化状态初始化传送门可见性
        if (MedalManager.Instance != null && libraryPortal != null)
        {
            libraryPortal.SetActive(MedalManager.Instance.IsLibraryUnlocked);
        }
    }

    /// <summary>
    /// 配置入口：在 MedalProgressConfig 对应节点的 onUnlock 拖入本方法即可。
    /// 弹窗文本由 MedalManager 根据 config.unlockDescription 自动生成，无需在此处理。
    /// </summary>
    public void UnlockLibrary()
    {
        if (libraryPortal != null) libraryPortal.SetActive(true);
        if (MedalManager.Instance != null) MedalManager.Instance.IsLibraryUnlocked = true;
        Debug.Log("[SceneShift] 图书馆已解锁（由 MedalProgressConfig.onUnlock 触发）");
    }
}
