using UnityEngine;

/// <summary>
/// 多 NPC 群体剧情脚本（ScriptableObject）
///
/// 使用方式：
///   1. Project 窗口右键 → Create → NPC → Dialogue Script
///   2. 填写 scriptId、dialogueLines
///   3. 将此资产拖入主 NPC 的 SpecialNPCEntry.groupScript 字段
///
/// 说话者查找规则：
///   - speakerName 为空 → 旁白，沿用主 NPC 的立绘和名字
///   - speakerName 不为空 → 由 SpecialNPCManager 按名称查找立绘，找不到则只换名字
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueScript", menuName = "NPC/Dialogue Script")]
public class DialogueScript : ScriptableObject
{
    [Header("脚本标识")]
    [Tooltip("唯一 ID，用于存档记录已完成状态（建议使用英文，如 dialogue_roommate_intro）")]
    public string scriptId;

    [Header("台词序列")]
    public DialogueLine[] dialogueLines;

    [Header("对话行为")]
    [Tooltip("完成后是否可重复触发")]
    public bool repeatable = false;

    [Header("勋章奖励")]
    [Tooltip("对话完成后向 MedalManager 提交的 NPC ID 列表（可以是多个参与者）")]
    public string[] medalNpcIds;
}
