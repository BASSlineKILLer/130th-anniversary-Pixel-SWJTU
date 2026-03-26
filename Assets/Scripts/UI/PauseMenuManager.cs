using UnityEngine;
using UnityEngine.SceneManagement;

namespace SWJTUGame.UI
{
    /// <summary>
    /// 暂停菜单管理器
    /// 被动监听 GameManager.isPaused 状态变化，不抢占 ESC 输入。
    /// 只响应真正的暂停，不响应对话锁。
    /// 挂载在游戏场景中的 Canvas 或空 GameObject 上
    /// </summary>
    public class PauseMenuManager : MonoBehaviour
    {
        [Header("UI 引用")]
        [Tooltip("暂停菜单面板（需包含 CanvasGroup 以支持淡入淡出）")]
        public GameObject pauseMenuPanel;

        [Header("场景配置")]
        [Tooltip("返回主菜单时加载的场景名称")]
        public string mainMenuSceneName = "MainMenu";

        [Header("动画设置")]
        [Tooltip("面板淡入淡出时长（秒）")]
        public float fadeDuration = 0.2f;

        private CanvasGroup panelCanvasGroup;
        private bool wasPausedLastFrame = false;

        private void Awake()
        {
            if (pauseMenuPanel != null)
            {
                panelCanvasGroup = pauseMenuPanel.GetComponent<CanvasGroup>();
                // 初始隐藏
                pauseMenuPanel.SetActive(false);
            }
        }

        private void Update()
        {
            // === 核心设计：被动监听，不主动处理输入 ===
            // GameManager 在自己的 Update 中处理 ESC 键并修改 isPaused
            // 我们只观测 isPaused 的变化并反应
            if (GameManager.Instance == null) return;

            bool isPausedNow = GameManager.Instance.isPaused;

            if (isPausedNow && !wasPausedLastFrame)
            {
                // 状态从「运行」变为「暂停」→ 显示暂停菜单
                ShowPauseMenu();
            }
            else if (!isPausedNow && wasPausedLastFrame)
            {
                // 状态从「暂停」变为「运行」→ 隐藏暂停菜单
                HidePauseMenu();
            }

            wasPausedLastFrame = isPausedNow;
        }

        // ===== 按钮回调方法（在 Inspector 中绑定到 Button.OnClick） =====

        /// <summary>
        /// 继续游戏按钮
        /// </summary>
        public void OnResumeGame()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.ResumeGame();
            }
            // 不需要手动隐藏面板 — Update 中的状态监听会自动处理
        }

        /// <summary>
        /// 返回主菜单按钮
        /// </summary>
        public void OnReturnToMainMenu()
        {
            // 恢复时间（防止主菜单场景中时间仍暂停）
            Time.timeScale = 1f;

            if (string.IsNullOrEmpty(mainMenuSceneName))
            {
                Debug.LogWarning("[PauseMenuManager] 未设置主菜单场景名称！请在 Inspector 中配置。");
                return;
            }

            SceneManager.LoadScene(mainMenuSceneName);
        }

        // ===== 内部方法 =====

        private void ShowPauseMenu()
        {
            UIAnimationHelper.ShowPanelWithAudio(this, pauseMenuPanel, panelCanvasGroup, fadeDuration);
        }

        private void HidePauseMenu()
        {
            UIAnimationHelper.HidePanelWithAudio(this, pauseMenuPanel, panelCanvasGroup, fadeDuration);
        }
    }
}
