using UnityEngine;

/// <summary>
/// 基于 Y 坐标的动态层级排序。
/// 挂载到需要按 Y 位置决定前后关系的对象上（玩家、NPC、树木、建筑等）。
/// Y 值越小（越靠近屏幕下方）→ sortingOrder 越大 → 渲染越靠前。
///
/// 双重保障：
///   1. 运行时强制设置 Sorting Layer，确保所有 Y 排序对象在同一层
///   2. 根据 Y 坐标动态设置 sortingOrder
///   3. 全局 Transparency Sort Mode = Custom Axis (0,1,0) 作为同 Order 的兜底
/// </summary>
public class YSortRenderer : MonoBehaviour
{
    /// <summary>所有 Y 排序对象共用的 Sorting Layer 名称</summary>
    public const string SORTING_LAYER = "Player";

    [Tooltip("排序精度，值越大 Y 差异越敏感（默认 100 = 0.01 单位精度）")]
    [SerializeField] private int precision = 100;

    [Tooltip("是否每帧更新（移动对象设 true，静止对象设 false 仅初始化一次）")]
    [SerializeField] private bool updateEveryFrame = true;

    [Tooltip("Y 轴偏移量（用于调整排序锚点，例如角色脚底）")]
    [SerializeField] private float yOffset = 0f;

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        // 强制统一 Sorting Layer — 这是 Y 排序生效的前提
        if (spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = SORTING_LAYER;
        }

        UpdateSortOrder();
    }

    private void LateUpdate()
    {
        if (updateEveryFrame)
            UpdateSortOrder();
    }

    private void UpdateSortOrder()
    {
        if (spriteRenderer == null) return;
        spriteRenderer.sortingOrder = -(int)((transform.position.y + yOffset) * precision);
    }
}
