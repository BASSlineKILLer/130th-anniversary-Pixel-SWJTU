using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using FMODUnity;

namespace SWJTUGame.UI
{
    /// <summary>
    /// 全局 UI 音效管理器（单例）
    /// 
    /// 功能：
    ///   1. 自动为场景中所有 Button 注册点击音效，无需逐个挂脚本
    ///   2. 提供静态 Play 方法供其他脚本调用（暂停、对话框弹出等）
    ///   3. 所有 FMOD 事件路径集中配置，统一管理
    ///
    /// 用法：
    ///   - 在任意一个场景中放置此脚本并勾选 DontDestroyOnLoad（推荐放在第一个加载的场景）
    ///   - 或在每个需要 UI 音效的场景中各放一个（单例会自动去重）
    ///   - 其他脚本调用: UIAudioManager.Play(UIAudioManager.SoundType.Click);
    /// </summary>
    [AddComponentMenu("SWJTUGame/UI/UI Audio Manager")]
    public class UIAudioManager : MonoBehaviour
    {
        // ══════════════════════════════════════════════════════════
        //  单例
        // ══════════════════════════════════════════════════════════
        public static UIAudioManager Instance { get; private set; }

        // ══════════════════════════════════════════════════════════
        //  音效类型枚举 — 新增音效类型只需在这里加一行
        // ══════════════════════════════════════════════════════════
        public enum SoundType
        {
            Click,          // 按钮点击
            PanelOpen,      // 面板/菜单打开（包括暂停菜单）
            PanelClose,     // 面板/菜单关闭
            DialogueOpen,   // 对话框弹出
            DialogueClose,  // 对话框关闭
        }

        // ══════════════════════════════════════════════════════════
        //  FMOD 事件路径配置 — 全部集中在 Inspector 中管理
        // ══════════════════════════════════════════════════════════
        [Header("FMOD 事件路径")]
        [Tooltip("按钮点击音效")]
        public string clickEvent = "event:/UI/Click";

        [Tooltip("面板打开音效（暂停菜单、设置面板等）")]
        public string panelOpenEvent = "";

        [Tooltip("面板关闭音效")]
        public string panelCloseEvent = "";

        [Tooltip("对话框弹出音效")]
        public string dialogueOpenEvent = "";

        [Tooltip("对话框关闭音效")]
        public string dialogueCloseEvent = "";

        [Header("全局设置")]
        [Tooltip("是否自动给场景中所有 Button 注册点击音效")]
        public bool autoHookButtons = true;

        [Tooltip("跨场景保留（推荐开启）")]
        public bool persistent = true;

        // ══════════════════════════════════════════════════════════
        //  生命周期
        // ══════════════════════════════════════════════════════════
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (persistent)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        private void OnEnable()
        {
            // 监听场景加载事件，每次新场景加载后自动注册按钮
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void Start()
        {
            // 首次启动时也注册一次当前场景的按钮
            if (autoHookButtons)
            {
                HookAllButtons();
            }
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (autoHookButtons)
            {
                // 延迟一帧，确保新场景的 UI 已完全初始化
                StartCoroutine(HookButtonsNextFrame());
            }
        }

        private System.Collections.IEnumerator HookButtonsNextFrame()
        {
            yield return null; // 等待一帧
            HookAllButtons();
        }

        // ══════════════════════════════════════════════════════════
        //  自动注册所有按钮
        // ══════════════════════════════════════════════════════════
        private void HookAllButtons()
        {
            // FindObjectsOfType(true) 包括未激活的按钮（如暂停面板中的按钮）
            Button[] allButtons = Resources.FindObjectsOfTypeAll<Button>();
            int count = 0;

            foreach (Button btn in allButtons)
            {
                // 跳过预制体中的按钮（只处理场景中实际存在的）
                if (btn.gameObject.scene.name == null || !btn.gameObject.scene.isLoaded)
                    continue;

                // 避免重复注册：用一个标记组件检测
                if (btn.GetComponent<UIAudioHooked>() != null)
                    continue;

                btn.onClick.AddListener(() => Play(SoundType.Click));
                btn.gameObject.AddComponent<UIAudioHooked>(); // 打标记
                count++;
            }

            if (count > 0)
            {
                Debug.Log($"[UIAudioManager] 已为 {count} 个按钮注册点击音效");
            }
        }

        // ══════════════════════════════════════════════════════════
        //  公共播放方法 — 其他脚本调用入口
        // ══════════════════════════════════════════════════════════

        /// <summary>
        /// 播放指定类型的 UI 音效
        /// 用法: UIAudioManager.Play(UIAudioManager.SoundType.Click);
        /// </summary>
        public static void Play(SoundType type)
        {
            if (Instance == null)
            {
                Debug.LogWarning("[UIAudioManager] 实例不存在，无法播放音效。请确保场景中有 UIAudioManager。");
                return;
            }
            Instance.PlayInternal(type);
        }

        private void PlayInternal(SoundType type)
        {
            string path = GetEventPath(type);
            if (string.IsNullOrEmpty(path))
                return;

            RuntimeManager.PlayOneShot(path);
        }

        private string GetEventPath(SoundType type)
        {
            switch (type)
            {
                case SoundType.Click:         return clickEvent;
                case SoundType.PanelOpen:      return panelOpenEvent;
                case SoundType.PanelClose:     return panelCloseEvent;
                case SoundType.DialogueOpen:   return dialogueOpenEvent;
                case SoundType.DialogueClose:  return dialogueCloseEvent;
                default:                       return null;
            }
        }

        // ══════════════════════════════════════════════════════════
        //  便捷静态方法 — 一行调用
        // ══════════════════════════════════════════════════════════

        /// <summary>播放按钮点击音效</summary>
        public static void PlayClick() => Play(SoundType.Click);

        /// <summary>播放面板打开音效（暂停菜单、设置面板等）</summary>
        public static void PlayPanelOpen() => Play(SoundType.PanelOpen);

        /// <summary>播放面板关闭音效</summary>
        public static void PlayPanelClose() => Play(SoundType.PanelClose);

        /// <summary>播放对话框弹出音效</summary>
        public static void PlayDialogueOpen() => Play(SoundType.DialogueOpen);

        /// <summary>播放对话框关闭音效</summary>
        public static void PlayDialogueClose() => Play(SoundType.DialogueClose);
    }

    /// <summary>
    /// 内部标记组件，用于防止重复注册按钮的点击音效
    /// 自动隐藏在 Inspector 中，无需关注
    /// </summary>
    [HideInInspector]
    internal class UIAudioHooked : MonoBehaviour { }
}
