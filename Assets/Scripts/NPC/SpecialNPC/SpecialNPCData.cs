using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpecialNPCData", menuName = "Data/SpecialNPCData")]
public class SpecialNPCData : ScriptableObject
{
    [System.Serializable]
    public class Entry
    {
        public SpecialNPCEntry specialNPCEntry; // 拖入SpecialNPCEntry资产
        public string panelText;
        public string storyText;
    }

    public List<Entry> entries = new List<Entry>(); // 在Unity中添加NPC条目并输入文本

    /// <summary>
    /// 检查是否为特殊NPC（名字在entries中的NPC）
    /// </summary>
    public bool IsSpecialNPC(string npcName)
    {
        return entries.Exists(e => e.specialNPCEntry != null && e.specialNPCEntry.npcName == npcName);
    }

    /// <summary>
    /// 获取NPC的面板文字
    /// </summary>
    public string GetPanelText(string npcName)
    {
        var entry = entries.Find(e => e.specialNPCEntry != null && e.specialNPCEntry.npcName == npcName);
        return entry?.panelText ?? "";
    }

    /// <summary>
    /// 获取NPC的故事文本
    /// </summary>
    public string GetStoryText(string npcName)
    {
        var entry = entries.Find(e => e.specialNPCEntry != null && e.specialNPCEntry.npcName == npcName);
        return entry?.storyText ?? "";
    }
}
