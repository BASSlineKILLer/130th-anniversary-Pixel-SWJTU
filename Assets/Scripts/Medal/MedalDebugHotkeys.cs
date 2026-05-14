using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 任务系统 Debug 快捷键（仅编辑器/开发版本生效）
///
/// F1：一次性给 5 枚勋章（用 5 个虚拟 NPC ID 走 TryAddMedal）
/// F2：解锁 SpecialNPCData 里所有 NPC 的故事（不增加勋章数）
/// F3：重置勋章 / 已对话 / 解锁状态（等同于"开始新游戏"）
/// F4：打印收集进度统计（普通/特殊/总数/还差多少）
///
/// 挂在场景里任意常驻 GameObject 上。指定 specialNPCData 给 F2 用。
/// 发布后自动失效（#if UNITY_EDITOR || DEVELOPMENT_BUILD）
/// </summary>
public class MedalDebugHotkeys : MonoBehaviour
{
    private const int F1_MEDAL_BATCH = 5;
    private const string DEBUG_NPC_ID_PREFIX = "debug_npc_";

    [Tooltip("F2 解锁所有特殊 NPC 用的数据源；留空则尝试从 MedalManager.data 读")]
    public SpecialNPCData specialNPCData;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private int debugMedalCounter;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1)) AddDebugMedals();
        else if (Input.GetKeyDown(KeyCode.F2)) UnlockAllSpecial();
        else if (Input.GetKeyDown(KeyCode.F3)) ResetAll();
        else if (Input.GetKeyDown(KeyCode.F4)) PrintCollectionStats();
    }

    private void AddDebugMedals()
    {
        if (MedalManager.Instance == null)
        {
            Debug.LogWarning("[MedalDebug] MedalManager 不存在");
            return;
        }
        for (int i = 0; i < F1_MEDAL_BATCH; i++)
        {
            string id = DEBUG_NPC_ID_PREFIX + (debugMedalCounter++);
            MedalManager.Instance.TryAddMedal(id);
        }
        Debug.Log($"[MedalDebug] F1 +{F1_MEDAL_BATCH} 勋章 → 当前 {MedalManager.Instance.GetMedalCount()}");
    }

    private void UnlockAllSpecial()
    {
        if (MedalManager.Instance == null)
        {
            Debug.LogWarning("[MedalDebug] MedalManager 不存在");
            return;
        }
        var data = specialNPCData != null ? specialNPCData : MedalManager.Instance.data;
        if (data == null || data.entries == null)
        {
            Debug.LogWarning("[MedalDebug] 找不到 SpecialNPCData，无法解锁特殊 NPC");
            return;
        }

        var names = new List<string>();
        foreach (var entry in data.entries)
        {
            if (entry?.specialNPCEntry != null && !string.IsNullOrEmpty(entry.specialNPCEntry.npcName))
                names.Add(entry.specialNPCEntry.npcName);
        }
        MedalManager.Instance.DebugUnlockAllSpecial(names);
    }

    private void ResetAll()
    {
        if (MedalManager.Instance == null)
        {
            Debug.LogWarning("[MedalDebug] MedalManager 不存在");
            return;
        }
        MedalManager.Instance.ResetForNewGame();
        debugMedalCounter = 0;
        Debug.Log("[MedalDebug] F3 已重置勋章 / 已对话 / 解锁状态");
    }

    private void PrintCollectionStats()
    {
        if (MedalManager.Instance == null)
        {
            Debug.LogWarning("[MedalDebug] MedalManager 不存在");
            return;
        }

        // 已收集数量
        int totalCollected = MedalManager.Instance.GetMedalCount();
        int specialCollected = MedalManager.Instance.GetTalkedSpecialNPCs().Count;
        int normalCollected = totalCollected - specialCollected;

        // 总数量 - 优先从实时数据源获取
        int totalNormal = 0;
        int totalSpecial = 0;
        
        // 普通NPC总数从 NPCDistributor 获取
        if (NPCDistributor.Instance != null)
            totalNormal = NPCDistributor.Instance.TotalNPCs;
        // 回退：从配置读取
        if (totalNormal == 0 && MedalProgress.Instance != null && MedalProgress.Instance.config != null)
            totalNormal = MedalProgress.Instance.config.totalNPCs;

        // 特殊NPC总数从 SpecialNPCData 获取
        if (MedalManager.Instance.data != null && MedalManager.Instance.data.entries != null)
            totalSpecial = MedalManager.Instance.data.entries.Count;
        // 回退：从配置读取
        else if (MedalProgress.Instance != null && MedalProgress.Instance.config != null)
            totalSpecial = MedalProgress.Instance.config.totalSpecialNPCs;

        int grandTotal = totalNormal + totalSpecial;
        int remaining = Mathf.Max(0, grandTotal - totalCollected);

        // 打印统计
        Debug.Log("╔══════════════════════════════════════════════════╗");
        Debug.Log("║          📊 勋章收集进度统计 (F4)               ║");
        Debug.Log("╠══════════════════════════════════════════════════╣");
        Debug.Log($"║  普通NPC: {normalCollected,3} / {totalNormal,3}                              ║");
        Debug.Log($"║  特殊NPC: {specialCollected,3} / {totalSpecial,3}                              ║");
        Debug.Log($"║  ─────────────────────────────────────────────  ║");
        Debug.Log($"║  总收集:  {totalCollected,3} / {grandTotal,3}  (还差 {remaining} 个)           ║");
        Debug.Log($"║  进度:    {(totalCollected * 100f / Mathf.Max(1, grandTotal)),5:F1}%                              ║");
        Debug.Log("╚══════════════════════════════════════════════════╝");

        // 检查是否满足全收集条件
        if (totalCollected >= grandTotal && grandTotal > 0)
            Debug.Log("🎉 [MedalDebug] 已满足全收集条件！应该触发庆祝弹窗");
    }
#endif
}
