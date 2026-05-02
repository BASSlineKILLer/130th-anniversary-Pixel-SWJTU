using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SceneTeleportListConfig", menuName = "Medal/SceneTeleportListConfig")]
public class SceneTeleportListConfig : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        [Tooltip("必填，Build Settings 中的场景名")]
        public string sceneName;

        [Tooltip("UI 显示")]
        public string displayName;

        [Tooltip("可选，图标")]
        public Sprite icon;

        [Tooltip("可选，传给 SceneTransitionManager")]
        public string spawnPointId;
    }

    public List<Entry> entries;
}