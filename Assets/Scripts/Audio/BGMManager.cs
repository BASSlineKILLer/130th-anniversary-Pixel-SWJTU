using UnityEngine;
using FMODUnity;
using FMOD.Studio;

/// <summary>
/// BGM 全局管理器（单例 + 跨场景持久）
/// 多 Event 架构：每首 BGM 是独立的 FMOD Event。
/// 选择 BGM 时停止当前播放，重新开始新的 BGM（类似车载音乐切换）。
/// 玩家选择和音量通过 PlayerPrefs 持久化。
/// </summary>
public class BGMManager : MonoBehaviour
{
    public static BGMManager Instance { get; private set; }

    [System.Serializable]
    public class BGMTrack
    {
        [Tooltip("BGM 显示名称")]
        public string displayName = "BGM";
        
        [Tooltip("FMOD Event 路径")]
        public string eventPath = "event:/BGM/Track1";
    }

    [Header("FMOD 配置")]
    [Tooltip("VCA 路径（用于音量控制）")]
    [SerializeField] private string vcaPath = "vca:/Music";

    [Header("BGM 列表")]
    [Tooltip("所有可选的 BGM 曲目")]
    [SerializeField] private BGMTrack[] bgmTracks = new BGMTrack[]
    {
        new BGMTrack { displayName = "默认 BGM", eventPath = "event:/BGM/Track1" }
    };

    private const string PREF_VOLUME = "BGM_Volume";
    private const string PREF_BGM_INDEX = "BGM_Index";
    private const float DEFAULT_VOLUME = 0.8f;

    private EventInstance currentBGMInstance;
    private VCA musicVCA;
    private int currentBGMIndex = -1;
    private float currentVolume;
    private bool vcaValid;
    private bool playbackStarted; // 是否已经开始过播放（WebGL 下需要等用户手势）

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadPrefs();
        InitVCA();

#if UNITY_WEBGL && !UNITY_EDITOR
        // WebGL: 浏览器 autoplay policy 要求必须有用户手势后才能启动 AudioContext。
        // 此处不直接播放，改为 Update 中监听首次输入。
#else
        if (currentBGMIndex >= 0 && currentBGMIndex < bgmTracks.Length)
        {
            PlayBGM(currentBGMIndex);
            playbackStarted = true;
        }
#endif
    }

#if UNITY_WEBGL && !UNITY_EDITOR
    private void Update()
    {
        if (playbackStarted) return;
        if (!HasUserGesture()) return;

        if (currentBGMIndex >= 0 && currentBGMIndex < bgmTracks.Length)
            PlayBGM(currentBGMIndex);
        playbackStarted = true;
    }

    /// <summary>
    /// 检测是否存在用户手势（鼠标/键盘/触摸）。
    /// 用 try/catch 兜底：若项目关闭了老 Input Manager 也不会崩，
    /// 这种情况下 BGM 会在首个 Button 点击后由 SelectBGM/UI 触发间接启动。
    /// </summary>
    private bool HasUserGesture()
    {
        try
        {
            if (Input.anyKeyDown) return true;
            if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) return true;
            if (Input.touchCount > 0) return true;
        }
        catch (System.Exception)
        {
            // 老 Input Manager 被禁用，依赖 UI 点击 → SelectBGM 兜底启动。
        }
        return false;
    }
#endif

    private void OnDestroy()
    {
        if (Instance == this)
            StopAndRelease();
    }

    // ══════════════════════════════════════════════════════════
    //  公共 API
    // ══════════════════════════════════════════════════════════

    public string[] GetBGMNames()
    {
        string[] names = new string[bgmTracks.Length];
        for (int i = 0; i < bgmTracks.Length; i++)
            names[i] = bgmTracks[i].displayName;
        return names;
    }
    public int GetCurrentBGMIndex() => currentBGMIndex;
    public float GetVolume() => currentVolume;
    public int BGMCount => bgmTracks.Length;

    /// <summary>
    /// 切换到指定索引的 BGM（停止当前播放，重新开始新的 BGM）
    /// </summary>
    public void SelectBGM(int index)
    {
        if (index < 0 || index >= bgmTracks.Length) return;
        if (index == currentBGMIndex && playbackStarted) return;

        currentBGMIndex = index;
        PlayBGM(index);
        playbackStarted = true;
        SavePrefs();
    }

    /// <summary>
    /// 设置 BGM 音量 (0~1)
    /// </summary>
    public void SetVolume(float volume)
    {
        currentVolume = Mathf.Clamp01(volume);
        ApplyVolume();
        SavePrefs();
    }

    // ══════════════════════════════════════════════════════════
    //  内部实现
    // ══════════════════════════════════════════════════════════

    private void LoadPrefs()
    {
        currentVolume = PlayerPrefs.GetFloat(PREF_VOLUME, DEFAULT_VOLUME);
        currentBGMIndex = PlayerPrefs.GetInt(PREF_BGM_INDEX, 0);

        if (currentBGMIndex >= bgmTracks.Length)
            currentBGMIndex = 0;
    }

    private void SavePrefs()
    {
        PlayerPrefs.SetFloat(PREF_VOLUME, currentVolume);
        PlayerPrefs.SetInt(PREF_BGM_INDEX, currentBGMIndex);
        PlayerPrefs.Save();
    }

    private void InitVCA()
    {
        if (string.IsNullOrEmpty(vcaPath) || vcaValid) return;

        try
        {
            musicVCA = RuntimeManager.GetVCA(vcaPath);
            vcaValid = musicVCA.isValid();

            if (vcaValid)
            {
                Debug.Log($"[BGMManager] VCA 初始化成功: {vcaPath}");
                ApplyVolume();
            }
            else
            {
                Debug.LogWarning($"[BGMManager] VCA 暂未就绪: {vcaPath}（bank 可能还在加载）");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[BGMManager] 获取 VCA 失败（可能 bank 未就绪）: {e.Message}");
            vcaValid = false;
        }
    }

    private void PlayBGM(int index)
    {
        if (index < 0 || index >= bgmTracks.Length) return;

        StopCurrentBGM();

        string eventPath = bgmTracks[index].eventPath;
        if (string.IsNullOrEmpty(eventPath))
        {
            Debug.LogWarning($"[BGMManager] BGM {index} 的 Event 路径为空");
            return;
        }

        currentBGMInstance = RuntimeManager.CreateInstance(eventPath);
        if (!currentBGMInstance.isValid())
        {
            Debug.LogError($"[BGMManager] 无法创建 FMOD Event: {eventPath}");
            return;
        }

        currentBGMInstance.start();
        Debug.Log($"[BGMManager] 开始播放 BGM: {bgmTracks[index].displayName}");

        // Event 创建成功说明 bank 已加载完毕，此时可以安全初始化 VCA
        if (!vcaValid) InitVCA();
    }

    private void StopCurrentBGM()
    {
        if (!currentBGMInstance.isValid()) return;
        
        currentBGMInstance.stop(FMOD.Studio.STOP_MODE.ALLOWFADEOUT);
        currentBGMInstance.release();
    }

    private void ApplyVolume()
    {
        if (vcaValid)
        {
            musicVCA.setVolume(currentVolume);
            Debug.Log($"[BGMManager] 设置音量: {currentVolume:F2}");
        }
        else
        {
            Debug.LogWarning("[BGMManager] VCA 无效，无法设置音量");
        }
    }

    private void StopAndRelease()
    {
        StopCurrentBGM();
    }
}
