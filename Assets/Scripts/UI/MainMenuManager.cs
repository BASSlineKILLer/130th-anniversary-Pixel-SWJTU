using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWJTUGame.UI
{
    /// <summary>
    /// 主菜单管理器
    /// 挂载在主菜单场景的 Canvas 或空 GameObject 上
    /// 通过 Inspector 配置场景名称，不硬编码任何场景引用
    /// </summary>
    public class MainMenuManager : MonoBehaviour
    {
        [Header("场景配置")]
        [Tooltip("点击'开始游戏'后加载的场景名称")]
        public string gameSceneName = "GameScene";

        [Header("UI 面板引用（可选）")]
        [Tooltip("设置面板 — 留空则跳过设置功能")]
        public GameObject settingsPanel;

        [Tooltip("关于面板 — 留空则跳过关于功能")]
        public GameObject aboutPanel;

        [Header("动画设置")]
        [Tooltip("面板淡入淡出时长（秒）")]
        public float panelFadeDuration = 0.25f;

        private CanvasGroup settingsCanvasGroup;
        private CanvasGroup aboutCanvasGroup;

        private void Awake()
        {
            // 尝试获取面板的 CanvasGroup（用于淡入淡出动画）
            if (settingsPanel != null)
                settingsCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();

            if (aboutPanel != null)
                aboutCanvasGroup = aboutPanel.GetComponent<CanvasGroup>();

            // 确保面板初始状态为隐藏
            HidePanelImmediate(settingsPanel);
            HidePanelImmediate(aboutPanel);
        }

        private void Start()
        {
            // 确保主菜单中时间是正常流动的
            // （防止从暂停的游戏场景返回时 timeScale 仍为 0）
            Time.timeScale = 1f;
        }

        // ===== 按钮回调方法（在 Inspector 中绑定到 Button.OnClick） =====

        /// <summary>
        /// 开始游戏
        /// </summary>
        public void OnStartGame()
        {
            if (string.IsNullOrEmpty(gameSceneName))
            {
                Debug.LogWarning("[MainMenuManager] 未设置游戏场景名称！请在 Inspector 中配置 gameSceneName。");
                return;
            }
            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>
        /// 打开设置面板
        /// </summary>
        public void OnOpenSettings()
        {
            ShowPanel(settingsPanel, settingsCanvasGroup);
        }

        /// <summary>
        /// 关闭设置面板
        /// </summary>
        public void OnCloseSettings()
        {
            HidePanel(settingsPanel, settingsCanvasGroup);
        }

        /// <summary>
        /// 打开关于面板
        /// </summary>
        public void OnOpenAbout()
        {
            ShowPanel(aboutPanel, aboutCanvasGroup);
        }

        /// <summary>
        /// 关闭关于面板
        /// </summary>
        public void OnCloseAbout()
        {
            HidePanel(aboutPanel, aboutCanvasGroup);
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        public void OnQuitGame()
        {
            Debug.Log("[MainMenuManager] 退出游戏");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ===== 内部方法 =====

        private void ShowPanel(GameObject panel, CanvasGroup canvasGroup)
        {
            UIAnimationHelper.ShowPanelWithAudio(this, panel, canvasGroup, panelFadeDuration);
        }

        private void HidePanel(GameObject panel, CanvasGroup canvasGroup)
        {
            UIAnimationHelper.HidePanelWithAudio(this, panel, canvasGroup, panelFadeDuration);
        }

        private void HidePanelImmediate(GameObject panel)
        {
            if (panel != null)
                panel.SetActive(false);
        }
    }
}
