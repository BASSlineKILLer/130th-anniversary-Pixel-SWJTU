using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 特殊 NPC 全局注册管理器（单例）
///
/// 功能：
///   - 自动收集场景中所有 SpecialNPCController
///   - 提供查询接口供其他系统使用
///   - Inspector 中可查看所有已注册的特殊 NPC
///
/// 使用方式：
///   在游戏场景中放置一个空 GameObject，挂载 SpecialNPCManager。
///   它会在 Start 时自动扫描并注册所有特殊 NPC。
/// </summary>
public class SpecialNPCManager : MonoBehaviour
{
    public static SpecialNPCManager Instance { get; private set; }

    [Header("状态（只读）")]
    [SerializeField] private int registeredCount;

    private Dictionary<string, SpecialNPCController> npcByName = new Dictionary<string, SpecialNPCController>();
    private List<SpecialNPCController> allSpecialNPCs = new List<SpecialNPCController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        ScanAndRegisterAll();
    }

    /// <summary>
    /// 扫描场景中所有 SpecialNPCController 并注册
    /// </summary>
    public void ScanAndRegisterAll()
    {
        npcByName.Clear();
        allSpecialNPCs.Clear();

        var found = FindObjectsOfType<SpecialNPCController>();
        foreach (var npc in found)
        {
            Register(npc);
        }

        registeredCount = allSpecialNPCs.Count;
        Debug.Log($"[SpecialNPCManager] 已注册 {registeredCount} 个特殊 NPC");
    }

    /// <summary>
    /// 注册一个特殊 NPC
    /// </summary>
    public void Register(SpecialNPCController npc)
    {
        if (npc == null || npc.specialData == null) return;

        string key = npc.specialData.npcName;
        if (!npcByName.ContainsKey(key))
        {
            npcByName[key] = npc;
            allSpecialNPCs.Add(npc);
        }
    }

    /// <summary>
    /// 根据名称获取特殊 NPC
    /// </summary>
    public SpecialNPCController GetByName(string npcName)
    {
        npcByName.TryGetValue(npcName, out var npc);
        return npc;
    }

    /// <summary>
    /// 获取所有已注册的特殊 NPC
    /// </summary>
    public List<SpecialNPCController> GetAll()
    {
        return allSpecialNPCs;
    }

    /// <summary>
    /// 重置所有特殊 NPC 的对话进度
    /// </summary>
    public void ResetAllDialogues()
    {
        foreach (var npc in allSpecialNPCs)
        {
            if (npc != null)
                npc.StartDialogue();
        }
        Debug.Log("[SpecialNPCManager] 所有特殊 NPC 对话已重置");
    }
}
