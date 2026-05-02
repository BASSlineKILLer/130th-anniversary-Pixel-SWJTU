using UnityEngine;

namespace SWJTUGame.UI
{
    /// <summary>
    /// UIAudioManager 给已注册过点击音效的 Button 打的标记组件，避免重复 AddListener。
    /// 必须放在独立 .cs 文件里，Unity 才能给它分配独立 GUID 并序列化到 prefab，
    /// 否则一旦被加到 prefab 上保存会报 "missing script"。
    /// 玩家无需关心：在 Inspector 中隐藏。
    /// </summary>
    [AddComponentMenu("")] // 不在 Add Component 菜单中显示
    [DisallowMultipleComponent]
    public class UIAudioHooked : MonoBehaviour { }
}
