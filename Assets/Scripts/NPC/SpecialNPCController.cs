using UnityEngine;

/// <summary>
/// 特殊 NPC 控制器（继承 NPCController）
///
/// 与普通 NPCController 的区别：
///   - 直接在场景中手动放置，固定位置不走动
///   - 持有 SpecialNPCEntry 数据，支持多轮对话
///   - 对话进度由此组件维护
///
/// 使用方式：
///   1. 场景中创建 GameObject
///   2. 挂载 SpecialNPCController（自动添加 SpriteRenderer + BoxCollider2D）
///   3. 在 Inspector 中拖入 SpecialNPCEntry 资产
///   4. 确保有 Bubble 子物体（如需气泡提示）
///
/// NPCInteraction 靠近时自动检测到此组件（因为继承自 NPCController），
/// 并根据 IsSpecialNPC 标志切换到多轮对话模式。
/// </summary>
public class SpecialNPCController : NPCController
{
    [Header("特殊 NPC 数据")]
    [Tooltip("拖入 SpecialNPCEntry 资产")]
    public SpecialNPCEntry specialData;

    /// <summary>是否为特殊 NPC（用于 NPCInteraction 区分对话模式）</summary>
    public bool IsSpecialNPC => true;

    /// <summary>当前对话行索引</summary>
    private int dialogueIndex = 0;

    /// <summary>该特殊NPC对话是否已完成（用于不可重复对话）</summary>
    private bool dialogueCompleted = false;

    [Header("NPC 预制体引用")]
    [Tooltip("拖入普通 NPC 预制体（Resources/NPCData/NPC），脚本将自动提取其中的 Bubble 子物体")]
    public GameObject npcPrefab;

    private void Start()
    {
        InitFromSpecialData();
    }

    /// <summary>
    /// 从 SpecialNPCEntry 初始化显示和数据
    /// </summary>
    private void InitFromSpecialData()
    {
        if (specialData == null)
        {
            Debug.LogWarning($"[SpecialNPCController] {gameObject.name} 未配置 SpecialNPCEntry！");
            return;
        }

        // 自动创建 Bubble（如果 Inspector 中未手动拖入 bubbleRoot）
        if (bubbleRoot == null)
        {
            AutoCreateBubble();
        }

        // 构建 NPCInfo 供基类和 NPCInteraction 使用
        var info = new NPCInfo
        {
            Id = GetInstanceID(), // 使用实例 ID 作为唯一标识
            Username = specialData.npcName,
            Message = GetCurrentLine(), // 首行对话
            Sprite = specialData.worldSprite
        };

        // 调用基类的 SetData 初始化显示
        SetData(info);

        gameObject.name = $"SpecialNPC_{specialData.npcName}";

        // 特殊 NPC 不走动：移除 NPCWalk（如果 Prefab 意外挂了的话）
        var walk = GetComponent<NPCWalk>();
        if (walk != null)
        {
            walk.enabled = false;
        }
    }

    /// <summary>
    /// 从 NPC 预制体中提取 Bubble 子物体并实例化
    /// 优先使用 Inspector 中拖入的 npcPrefab，
    /// 若为空则自动从 Resources/NPCData/NPC 加载
    /// </summary>
    private void AutoCreateBubble()
    {
        GameObject prefab = npcPrefab;

        // 如果没手动拖入，尝试从 Resources 自动加载
        if (prefab == null)
        {
            prefab = Resources.Load<GameObject>("NPCData/NPC");
        }

        if (prefab == null)
        {
            Debug.LogWarning("[SpecialNPCController] 找不到 NPC 预制体，无法自动创建 Bubble。" +
                             "请在 Inspector 中拖入 NPC 预制体，或确保 Resources/NPCData/NPC 存在。");
            return;
        }

        // 在预制体中查找名为 "Bubble" 或 "bubble" 的子物体
        Transform bubbleTemplate = null;
        foreach (Transform child in prefab.transform)
        {
            if (child.name.Equals("Bubble", System.StringComparison.OrdinalIgnoreCase))
            {
                bubbleTemplate = child;
                break;
            }
        }

        if (bubbleTemplate == null)
        {
            Debug.LogWarning($"[SpecialNPCController] NPC 预制体中未找到名为 'Bubble' 的子物体");
            return;
        }

        // 实例化 Bubble 为当前特殊 NPC 的子物体
        GameObject bubble = Instantiate(bubbleTemplate.gameObject, transform);
        bubble.name = "Bubble";
        bubble.SetActive(false); // 初始隐藏，与普通 NPC 行为一致
        bubbleRoot = bubble;

        Debug.Log($"[SpecialNPCController] 已从 NPC 预制体中提取 Bubble 并创建到 {gameObject.name}");
    }

    // ═══════════════════════════════════════════════════
    //  多轮对话接口（供 NPCInteraction 调用）
    // ═══════════════════════════════════════════════════

    /// <summary>
    /// 是否可以开始对话
    /// </summary>
    public bool CanStartDialogue()
    {
        if (specialData == null) return false;
        if (specialData.dialogueLines == null || specialData.dialogueLines.Length == 0) return false;
        if (dialogueCompleted && !specialData.repeatable) return false;
        return true;
    }

    /// <summary>
    /// 开始新的对话（重置到第一行）
    /// </summary>
    public void StartDialogue()
    {
        dialogueIndex = 0;
        dialogueCompleted = false;
    }

    /// <summary>
    /// 获取当前对话行内容
    /// </summary>
    public string GetCurrentLine()
    {
        if (specialData == null || specialData.dialogueLines == null) return "";
        if (dialogueIndex >= specialData.dialogueLines.Length) return "";
        return specialData.dialogueLines[dialogueIndex];
    }

    /// <summary>
    /// 推进到下一行对话
    /// </summary>
    /// <returns>true = 还有下一行；false = 对话已结束</returns>
    public bool AdvanceDialogue()
    {
        dialogueIndex++;
        if (dialogueIndex >= specialData.dialogueLines.Length)
        {
            dialogueCompleted = true;
            return false; // 没有更多对话了
        }
        return true; // 还有下一行
    }

    /// <summary>
    /// 获取总对话行数
    /// </summary>
    public int GetTotalLines()
    {
        if (specialData == null || specialData.dialogueLines == null) return 0;
        return specialData.dialogueLines.Length;
    }

    /// <summary>
    /// 获取当前对话行索引（从 0 开始）
    /// </summary>
    public int GetCurrentLineIndex() => dialogueIndex;

    /// <summary>
    /// 对话是否已完成
    /// </summary>
    public bool IsDialogueCompleted => dialogueCompleted;

    /// <summary>
    /// 获取对话立绘
    /// </summary>
    public Sprite GetPortrait()
    {
        return specialData != null ? specialData.GetPortrait() : null;
    }

    /// <summary>
    /// 获取 NPC 名称
    /// </summary>
    public string GetName()
    {
        return specialData != null ? specialData.npcName : "???";
    }
}
