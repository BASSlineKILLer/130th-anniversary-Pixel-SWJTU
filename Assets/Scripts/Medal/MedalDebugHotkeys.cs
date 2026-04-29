using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 任务系统 Debug 快捷键（仅编辑器/开发版本生效）
///
/// F1：一次性给 5 枚勋章（用 5 个虚拟 NPC ID 走 TryAddMedal）
/// F2：解锁 SpecialNPCData 里所有 NPC 的故事（不增加勋章数）
/// F3：重置勋章 / 已对话 / 解锁状态（等同于"开始新游戏"）
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
#endif
}
