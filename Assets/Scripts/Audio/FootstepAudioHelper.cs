using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// 脚步声播放工具类
/// 封装 FMOD 脚步声实例的创建、参数设置和播放，供 FootstepAudioPlayer 和 NPCWalk 共用。
/// </summary>
public static class FootstepAudioHelper
{
    /// <summary>
    /// 播放一次脚步声
    /// </summary>
    /// <param name="footstepEvent">FMOD 事件引用</param>
    /// <param name="materialParamName">FMOD material 参数名</param>
    /// <param name="materialValue">材质类型值</param>
    /// <param name="worldPosition">播放位置</param>
    public static void Play(EventReference footstepEvent, string materialParamName, int materialValue, Vector3 worldPosition)
    {
        if (footstepEvent.IsNull) return;

        EventInstance instance = RuntimeManager.CreateInstance(footstepEvent);
        instance.setParameterByName(materialParamName, materialValue);
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(worldPosition));
        instance.start();
        instance.release();
    }
}
