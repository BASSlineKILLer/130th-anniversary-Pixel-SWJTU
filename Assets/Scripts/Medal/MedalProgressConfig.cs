using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "MedalProgressConfig", menuName = "Medal/MedalProgressConfig")]
public class MedalProgressConfig : ScriptableObject
{
    [Tooltip("全部普通 NPC 数量（从 NPCManager 获取，如果需要固定值可手动设置）")]
    public int totalNPCs;

    [System.Serializable]
    public class Node
    {
        [Tooltip("解锁所需勋章数")]
        public int threshold;
        [Tooltip("解锁描述：会拼到弹窗里成 \"{unlockDescription}已解锁\"。如：\"图书馆\"、\"传送功能\"、\"搜索NPC\"")]
        public string unlockDescription;
        [Tooltip("节点图标（未达灰调，已达原色）")]
        public Sprite icon;
        [Tooltip("达成阈值时触发的回调。拖入场景内 GameObject 上的方法（如 SceneShift.UnlockLibrary、NpcSearch.UnlockByQuest 等）。MedalManager 会在勋章 +1 弹窗消失后自动调用本事件，并随即弹出解锁通知。")]
        public UnityEvent onUnlock;
    }

    [Tooltip("进度条节点配置")]
    public List<Node> nodes;
}
