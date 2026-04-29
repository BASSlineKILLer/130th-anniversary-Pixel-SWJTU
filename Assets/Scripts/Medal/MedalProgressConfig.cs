using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "MedalProgressConfig", menuName = "Medal/MedalProgressConfig")]
public class MedalProgressConfig : ScriptableObject
{
    [Tooltip("全部普通 NPC 数量（从 NPCManager 获取，如果需要固定值可手动设置）")]
    public int totalNPCs;

    [System.Serializable]
    public class Node
    {
        [Tooltip("解锁进度（枚勋章）")]
        public int threshold;
        [Tooltip("解锁描述")]
        public string unlockDescription;
        [Tooltip("节点图标（未达灰调，已达原色）")]
        public Sprite icon;
    }

    [Tooltip("进度条节点配置")]
    public List<Node> nodes;
}
