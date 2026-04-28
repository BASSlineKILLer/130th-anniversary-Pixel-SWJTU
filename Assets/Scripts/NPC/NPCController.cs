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
[RequireComponent(typeof(Rigidbody2D))]
public class NPCController : MonoBehaviour
{
    // 脚底实体碰撞体的默认尺寸：半径小、贴脚底，避免上半身阻挡视觉
    private const float DEFAULT_SOLID_RADIUS = 0.15f;
    private const float DEFAULT_SOLID_FOOT_OFFSET_Y = 0.1f;

    [Header("提示气泡")]
    [Tooltip("气泡子物体")]
    public GameObject bubbleRoot;
    [Tooltip("气泡相对于 Sprite 顶部的额外偏移")]
    public Vector3 bubbleOffset = new Vector3(0f, 0.3f, 0f);

    [Header("检测范围")]
    [Tooltip("触发区域比 Sprite 实际大小多出的额外范围")]
    public float triggerPadding = 1.5f;

    [Header("实体碰撞（脚底）")]
    [Tooltip("脚底实体碰撞圈半径，用于阻挡墙体和玩家")]
    public float solidColliderRadius = DEFAULT_SOLID_RADIUS;
    [Tooltip("脚底实体碰撞圈相对 pivot（脚底）的 Y 偏移")]
    public float solidColliderFootOffsetY = DEFAULT_SOLID_FOOT_OFFSET_Y;

    /// <summary>
    /// NPC 数据，供对话系统读取
    /// </summary>
    public NPCInfo Info { get; private set; }

    /// <summary>脚底实体碰撞体，NPCWalk 用 Rigidbody2D.Cast 走这个</summary>
    public CircleCollider2D SolidCollider { get; private set; }

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        SetupTriggerCollider();
        SetupKinematicRigidbody();
        SolidCollider = SetupSolidCollider();

        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);

        // 自动挂载 Y 轴排序
        if (GetComponent<YSortRenderer>() == null)
            gameObject.AddComponent<YSortRenderer>();

        // 自动挂载遮挡轮廓
        if (GetComponent<OcclusionSilhouette>() == null)
            gameObject.AddComponent<OcclusionSilhouette>();
    }

    /// <summary>玩家交互检测用的 trigger collider（覆盖整个 Sprite）</summary>
    private void SetupTriggerCollider()
    {
        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
    }

    /// <summary>
    /// Kinematic Rigidbody2D：让 NPC 参与物理但不被外力推动。
    /// Player 的 Dynamic Rigidbody 撞上来不会撞飞 NPC。
    /// </summary>
    private void SetupKinematicRigidbody()
    {
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.useFullKinematicContacts = true;
    }

    /// <summary>
    /// 脚底实体 CircleCollider2D：阻挡墙体和玩家。
    /// 复用已有的实体（非 trigger）CircleCollider2D；没有就新建一个并配置默认尺寸。
    /// </summary>
    private CircleCollider2D SetupSolidCollider()
    {
        foreach (var existing in GetComponents<CircleCollider2D>())
        {
            if (!existing.isTrigger) return existing;
        }

        var solid = gameObject.AddComponent<CircleCollider2D>();
        solid.isTrigger = false;
        solid.radius = solidColliderRadius;
        solid.offset = new Vector2(0f, solidColliderFootOffsetY);
        return solid;
    }

    /// <summary>
    /// 由 NPCManager 生成后调用
    /// </summary>
    public void SetData(NPCInfo info)
    {
        Info = info;

        // 如果在 Awake 之前（例如父物体隐藏导致 Instantiate 不会立即触发 Awake）调用了 SetData，
        // 我们需要手动获取依赖组件，避免 NullReferenceException。
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (info.Sprite != null && spriteRenderer != null)
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
