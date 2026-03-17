using UnityEngine;

/// <summary>
/// NPC像素图片数据导入
/// 与Web端对接，将用户生成的像素小人数据导入Unity
/// </summary>
public class NPCDataImporter : MonoBehaviour
{
    [Header("NPC数据路径")]
    public string npcDataFolder = "NPCData";

    /// <summary>
    /// 从Resources文件夹加载NPC精灵图
    /// </summary>
    public Sprite LoadNPCSprite(string npcId)
    {
        string path = $"{npcDataFolder}/{npcId}";
        Sprite sprite = Resources.Load<Sprite>(path);

        if (sprite == null)
        {
            Debug.LogWarning($"未找到NPC精灵图: {path}");
        }

        return sprite;
    }

    /// <summary>
    /// 加载所有NPC精灵图
    /// </summary>
    public Sprite[] LoadAllNPCSprites()
    {
        return Resources.LoadAll<Sprite>(npcDataFolder);
    }
}
