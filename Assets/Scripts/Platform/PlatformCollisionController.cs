using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 平台碰撞控制器：支持 Sorting Layer 切换、多碰撞箱、自动收集子触发器。
/// 
/// 工作流程：
/// 1. 初始：Renderer 在 Buildings 排序层（遮挡角色），保护碰撞箱激活（防穿出）
/// 2. 玩家从触发器下方进入 → 上平台：保护关闭，平台碰撞开启，排序层切 Platform
/// 3. 玩家从触发器上方进入 → 下平台：恢复初始状态
/// 
/// 重要：initialSortingLayer / activeSortingLayer 是 Sorting Layer（渲染排序层），不是物理 Layer！
/// </summary>
public class PlatformCollisionController : MonoBehaviour
{
    [Header("=== 入口触发器 ===")]
    [Tooltip("楼梯顶部的入口触发器。留空则自动收集子物体中的 PlatformEntryTrigger")]
    public List<PlatformEntryTrigger> entryTriggers = new List<PlatformEntryTrigger>();

    [Header("=== 平台碰撞箱（支持多个）===")]
    [Tooltip("平台主体碰撞箱，进入平台后激活。支持多个（主体 + 内部障碍物）")]
    public List<Collider2D> platformColliders = new List<Collider2D>();

    [Header("=== 保护碰撞箱（支持多个）===")]
    [Tooltip("预平台保护碰撞箱，进入平台前激活防止穿出，进入后关闭")]
    public List<Collider2D> protectionColliders = new List<Collider2D>();

    [Header("=== Sorting Layer 排序层设置 ===")]
    [Tooltip("初始排序层名称（如 Buildings，在角色之上遮挡）")]
    public string initialSortingLayer = "Buildings";

    [Tooltip("激活后排序层名称（如 Platform，在角色之下，角色可站在上面）")]
    public string activeSortingLayer = "Platform";

    [Tooltip("要同步切换排序层的 Renderer 列表（SpriteRenderer / TilemapRenderer 等）。留空则自动收集自身和子物体")]
    public List<Renderer> affectedRenderers = new List<Renderer>();

    [Header("=== 调试 ===")]
    public bool showDebugLog = true;

    private bool isPlayerOnPlatform = false;

    void Awake()
    {
        AutoCollectChildren();
        SetInitialState();
    }

    void OnEnable()
    {
        // 注册所有入口触发器
        foreach (var trigger in entryTriggers)
        {
            if (trigger != null)
                trigger.Initialize(this);
        }
    }

    /// <summary>
    /// 自动收集子物体的触发器和 SpriteRenderer（兼容用户忘记拖入的情况）
    /// </summary>
    void AutoCollectChildren()
    {
        // 自动收集子物体中的入口触发器
        if (entryTriggers.Count == 0)
        {
            var found = GetComponentsInChildren<PlatformEntryTrigger>(true);
            entryTriggers.AddRange(found);

            if (showDebugLog && found.Length > 0)
                Debug.Log($"[PlatformController] {gameObject.name} 自动收集 {found.Length} 个入口触发器");
        }

        // 自动收集自身和子物体的所有 Renderer（SpriteRenderer、TilemapRenderer 等）
        if (affectedRenderers.Count == 0)
        {
            var found = GetComponentsInChildren<Renderer>(true);
            affectedRenderers.AddRange(found);

            if (showDebugLog && found.Length > 0)
                Debug.Log($"[PlatformController] {gameObject.name} 自动收集 {found.Length} 个 Renderer");
        }
    }

    /// <summary>
    /// 设置初始状态：保护碰撞开启，平台碰撞关闭，排序层为 initialSortingLayer
    /// </summary>
    void SetInitialState()
    {
        SetCollidersEnabled(protectionColliders, true);
        SetCollidersEnabled(platformColliders, false);
        SetSortingLayer(initialSortingLayer);

        if (showDebugLog)
            Debug.Log($"[PlatformController] {gameObject.name} 初始状态：保护开启 ({protectionColliders.Count}个)，平台关闭 ({platformColliders.Count}个)，排序层={initialSortingLayer}");
    }

    /// <summary>
    /// 玩家进入入口触发器时调用：作为一次性开关，根据进入方向切换平台状态。
    /// - 从下方进入 → 上平台
    /// - 从上方进入 → 下平台
    /// 状态判断本身防止重复切换：已在平台上时忽略上平台操作，反之同理。
    /// </summary>
    /// <param name="fromBelow">玩家是否从下方进入触发器</param>
    public void OnPlayerEnteredEntry(bool fromBelow)
    {
        // 从下方进入 → 上平台；从上方进入 → 下平台
        if (fromBelow && !isPlayerOnPlatform)
        {
            EnterPlatform();
        }
        else if (!fromBelow && isPlayerOnPlatform)
        {
            ExitPlatform();
        }
    }

    /// <summary>
    /// 玩家离开入口触发器时调用：不做任何处理，触发器仅是开关。
    /// </summary>
    public void OnPlayerExitedEntry()
    {
        // 开关式触发器不需要处理离开事件
    }

    void EnterPlatform()
    {
        isPlayerOnPlatform = true;

        SetCollidersEnabled(protectionColliders, false);
        SetCollidersEnabled(platformColliders, true);
        SetSortingLayer(activeSortingLayer);

        if (showDebugLog)
            Debug.Log($"[PlatformController] {gameObject.name} 上平台，排序层={activeSortingLayer}");
    }

    void ExitPlatform()
    {
        isPlayerOnPlatform = false;
        SetInitialState();

        if (showDebugLog)
            Debug.Log($"[PlatformController] {gameObject.name} 下平台，排序层={initialSortingLayer}");
    }

    /// <summary>
    /// 外部调用：强制重置到初始状态（如传送、剧情触发）
    /// </summary>
    public void ForceReset()
    {
        isPlayerOnPlatform = false;
        SetInitialState();
    }

    // ========== 辅助方法 ==========

    void SetCollidersEnabled(List<Collider2D> colliders, bool enabled)
    {
        foreach (var col in colliders)
        {
            if (col != null) col.enabled = enabled;
        }
    }

    void SetSortingLayer(string layerName)
    {
        if (string.IsNullOrEmpty(layerName)) return;

        // 检测 Sorting Layer 是否存在
        if (!SortingLayerExists(layerName))
        {
            Debug.LogWarning($"[PlatformController] Sorting Layer '{layerName}' 不存在，请在 Project Settings > Tags and Layers 中添加");
            return;
        }

        foreach (var sr in affectedRenderers)
        {
            if (sr != null) sr.sortingLayerName = layerName;
        }
    }

    bool SortingLayerExists(string layerName)
    {
        foreach (var layer in SortingLayer.layers)
        {
            if (layer.name == layerName) return true;
        }
        return false;
    }
}
