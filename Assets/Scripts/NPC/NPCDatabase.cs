using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 数据库（ScriptableObject）
/// 
/// 集中管理所有 NPC 数据来源：
///   - 手动条目：在编辑器中配置 username / message / sprite
///   - API 远程：运行时从后端接口拉取
///
/// 创建方式：Project 窗口右键 → Create → NPC → NPC Database
/// 建议放在 Assets/Resources/NPCData/NPCDatabase.asset
/// </summary>
[CreateAssetMenu(fileName = "NPCDatabase", menuName = "NPC/NPC Database")]
public class NPCDatabase : ScriptableObject
{
    [Header("手动 NPC 条目")]
    [Tooltip("手动创建的 NPC，在编辑器中配置 username / message / sprite")]
    public List<NPCEntry> manualEntries = new List<NPCEntry>();

    [Header("API 远程获取")]
    [Tooltip("是否在运行时从 API 加载额外的 NPC")]
    public bool enableApiFetch = true;

    [Tooltip("后端 API 地址")]
    public string apiUrl = "http://devshowcase.site/api/approved";

    [Tooltip("调试用：勾选后，下次 Play 启动时会先删除本地 NPCCache（JSON + 图片），强制从网络重新拉取。测试完记得取消勾选")]
    public bool clearCacheOnStart = false;

    /// <summary>
    /// 将手动条目转换为运行时 NPCInfo 列表。
    /// 手动条目使用负数 ID（从 -1 递减），与 API 正数 ID 互不冲突。
    /// </summary>
    public List<NPCInfo> GetManualNPCInfos()
    {
        var list = new List<NPCInfo>();
        for (int i = 0; i < manualEntries.Count; i++)
        {
            var entry = manualEntries[i];
            if (entry == null) continue;

            list.Add(new NPCInfo
            {
                Id = -(i + 1),
                Username = entry.username,
                Message = entry.message,
                Sprite = entry.sprite
            });
        }
        return list;
    }
}
