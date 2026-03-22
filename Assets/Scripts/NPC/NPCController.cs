using UnityEngine;

/// <summary>
/// 单个 NPC 行为控制。
///
/// 职责：
///   - 显示解码后的 Sprite
///   - 根据 Sprite 实际大小自动调整 Collider 和气泡位置
///   - 提供 ShowBubble / HideBubble 供 NPCInteraction 统一管理
///   - 保存 NPCInfo 供对话系统读取
///
/// 【Prefab 结构】放在 Resources/NPCData/NPC.prefab
/// NPC
///   ├─ SpriteRenderer
///   ├─ BoxCollider2D (Trigger，脚本自动调整大小)
///   └─ Bubble  ← 子物体，提示气泡
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class NPCController : MonoBehaviour
{
    [Header("提示气泡")]
    [Tooltip("气泡子物体")]
    public GameObject bubbleRoot;
    [Tooltip("气泡相对于 Sprite 顶部的额外偏移")]
    public Vector3 bubbleOffset = new Vector3(0f, 0.3f, 0f);

    [Header("检测范围")]
    [Tooltip("触发区域比 Sprite 实际大小多出的额外范围")]
    public float triggerPadding = 1.5f;

    /// <summary>
    /// NPC 数据，供对话系统读取
    /// </summary>
    public NPCInfo Info { get; private set; }

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;

        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);
    }

    /// <summary>
    /// 由 NPCManager 生成后调用
    /// </summary>
    public void SetData(NPCInfo info)
    {
        Info = info;

        if (info.Sprite != null)
        {
            spriteRenderer.sprite = info.Sprite;
            AdjustColliderAndBubble();
        }

        gameObject.name = $"NPC_{info.Id}_{info.Username}";

        // 初始化走动组件（如果 Prefab 上挂了 NPCWalk）
        var walk = GetComponent<NPCWalk>();
        if (walk != null)
            walk.Init(transform.position);
    }

    /// <summary>
    /// 显示气泡（由 NPCInteraction 调用）
    /// </summary>
    public void ShowBubble()
    {
        if (bubbleRoot != null && !bubbleRoot.activeSelf)
            bubbleRoot.SetActive(true);
    }

    /// <summary>
    /// 隐藏气泡（由 NPCInteraction 调用）
    /// </summary>
    public void HideBubble()
    {
        if (bubbleRoot != null && bubbleRoot.activeSelf)
            bubbleRoot.SetActive(false);
    }

    /// <summary>
    /// 根据 Sprite 实际世界尺寸自动调整 Collider 和气泡位置
    /// </summary>
    private void AdjustColliderAndBubble()
    {
        var col = GetComponent<BoxCollider2D>();
        Bounds bounds = spriteRenderer.bounds;
        float spriteW = bounds.size.x;
        float spriteH = bounds.size.y;

        // Collider 比 Sprite 大一圈 triggerPadding
        col.size = new Vector2(spriteW + triggerPadding * 2f, spriteH + triggerPadding * 2f);
        // pivot 在脚底 (0.5, 0)，Sprite 中心在 (0, spriteH/2)
        col.offset = new Vector2(0f, spriteH / 2f);

        // 气泡放在 Sprite 头顶
        if (bubbleRoot != null)
        {
            bubbleRoot.transform.localPosition = new Vector3(0f, spriteH, 0f) + bubbleOffset;
        }
    }
}
