using UnityEngine;

/// <summary>
/// 特殊 NPC 数据条目（ScriptableObject）
///
/// 与普通 NPCEntry 的区别：
///   - 支持多轮对话（dialogueLines[]）
///   - 有独立的世界 Sprite 和对话立绘
///   - 可配置是否可重复对话
///
/// 创建方式：Project 窗口右键 → Create → NPC → Special NPC Entry
/// </summary>
[CreateAssetMenu(fileName = "NewSpecialNPC", menuName = "NPC/Special NPC Entry")]
public class SpecialNPCEntry : ScriptableObject
{
    [Header("基本信息")]
    [Tooltip("NPC 名称（显示在对话框中）")]
    public string npcName;

    [Tooltip("NPC 在场景中的 Sprite")]
    public Sprite worldSprite;

    [Tooltip("对话立绘（显示在对话框左侧，为空则使用 worldSprite）")]
    public Sprite portrait;

    [Header("对话内容")]
    [TextArea(2, 5)]
    [Tooltip("多轮对话内容，按数组顺序逐条显示，按 E 推进")]
    public string[] dialogueLines;

    [Header("对话行为")]
    [Tooltip("对话结束后是否可以重新触发（从头开始）")]
    public bool repeatable = true;

    [Header("分类")]
    [Tooltip("是否为彩蛋 NPC（在 TaskPanel 卡片上加彩蛋标记和金色边框）")]
    public bool isEasterEgg = false;

    /// <summary>
    /// 获取对话立绘（优先使用 portrait，为空则回退到 worldSprite）
    /// </summary>
    public Sprite GetPortrait()
    {
        return portrait != null ? portrait : worldSprite;
    }
}
