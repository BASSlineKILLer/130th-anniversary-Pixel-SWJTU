using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "MedalProgressConfig", menuName = "Medal/MedalProgressConfig")]
public class MedalProgressConfig : ScriptableObject
{
    [Tooltip("全部普通 NPC 数量（从 NPCManager 获取，如果需要固定值可手动设置）")]
    public int totalNPCs;

    [Serializable]
    public class Node
    {
        [Tooltip("业务标识，用于 MedalManager.IsNodeUnlocked(nodeId) 查询。例：\"Library\"、\"TeleportList\"、\"NpcSearch\"")]
        public string nodeId;

        [Tooltip("解锁所需勋章数")]
        public int threshold;

        [Tooltip("解锁描述：会拼到弹窗里成 \"{unlockDescription}已解锁\"。如：\"图书馆\"、\"场景传送功能\"、\"NPC 搜索\"")]
        public string unlockDescription;

        [Tooltip("未达阈值时玩家进入触发点的提示；支持 {0} 占位为 threshold。例：\"需要 {0} 枚勋章才能前往图书馆\"")]
        [TextArea] public string lockedHint;

        [Tooltip("节点图标（未达灰调，已达原色）")]
        public Sprite icon;
    }

    [Tooltip("进度条节点配置")]
    public List<Node> nodes;

    /// <summary>
    /// 按 nodeId 查找节点；不存在返回 null。
    /// </summary>
    public Node FindNode(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId) || nodes == null) return null;
        for (int i = 0; i < nodes.Count; i++)
        {
            var n = nodes[i];
            if (n != null && n.nodeId == nodeId) return n;
        }
        return null;
    }
}
