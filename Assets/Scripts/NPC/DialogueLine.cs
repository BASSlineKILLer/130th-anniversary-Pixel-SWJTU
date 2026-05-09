using UnityEngine;

/// <summary>
/// 一条多 NPC 剧情台词条目
/// </summary>
[System.Serializable]
public class DialogueLine
{
    [Tooltip("说话者名称（对应 SpecialNPCEntry.npcName；为空 = 旁白，使用当前主 NPC 立绘）")]
    public string speakerName;

    [TextArea(2, 5)]
    [Tooltip("台词内容")]
    public string text;

    [Tooltip("覆盖立绘（为空则使用该说话者 NPC 的默认立绘）")]
    public Sprite portraitOverride;
}
