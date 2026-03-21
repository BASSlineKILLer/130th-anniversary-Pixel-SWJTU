using UnityEngine;

/// <summary>
/// 单个 NPC 数据条目（ScriptableObject）
/// 
/// 创建方式：
///   1. Project 窗口右键 → Create → NPC → NPC Entry
///   2. 或在 NPCDatabase Inspector 中点击「新建 NPC 条目」
///
/// 将 PNG 图片导入 Unity 后（Texture Type 设为 Sprite），拖到 sprite 字段即可。
/// </summary>
[CreateAssetMenu(fileName = "NewNPC", menuName = "NPC/NPC Entry")]
public class NPCEntry : ScriptableObject
{
    [Tooltip("NPC 用户名")]
    public string username;

    [TextArea(2, 5)]
    [Tooltip("NPC 消息 / 想说的话")]
    public string message;

    [Tooltip("NPC 形象图片（PNG 格式 Sprite）")]
    public Sprite sprite;
}
