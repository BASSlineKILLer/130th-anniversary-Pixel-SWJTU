using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
        [Tooltip("点击'新游戏'后加载的场景名称")]
        public string gameSceneName = "GameScene";
        [Tooltip("新游戏的初始出生点 ID")]
        public string newGameSpawnPointId = "start";

        [Header("按钮引用")]
        [Tooltip("继续游戏按钮 — 没有存档时自动隐藏")]
        public GameObject continueButton;

        [Header("UI 面板引用（可选）")]
        [Tooltip("设置面板 — 留空则跳过设置功能")]
        public GameObject settingsPanel;

        [Tooltip("关于面板 — 留空则跳过关于功能")]
        public GameObject aboutPanel;

        [Tooltip("确认新游戏面板 — 存在存档时点新游戏弹出")]
        public GameObject confirmNewGamePanel;

        [Header("动画设置")]
        [Tooltip("面板淡入淡出时长（秒）")]
        public float panelFadeDuration = 0.25f;

        private CanvasGroup settingsCanvasGroup;
        private CanvasGroup aboutCanvasGroup;
        private CanvasGroup confirmCanvasGroup;

        private void Awake()
        {
            // 尝试获取面板的 CanvasGroup（用于淡入淡出动画）
            if (settingsPanel != null)
                settingsCanvasGroup = settingsPanel.GetComponent<CanvasGroup>();

            if (aboutPanel != null)
                aboutCanvasGroup = aboutPanel.GetComponent<CanvasGroup>();

            if (confirmNewGamePanel != null)
                confirmCanvasGroup = confirmNewGamePanel.GetComponent<CanvasGroup>();

            // 确保面板初始状态为隐藏
            HidePanelImmediate(settingsPanel);
            HidePanelImmediate(aboutPanel);
            HidePanelImmediate(confirmNewGamePanel);
        }

        private void Start()
        {
            // 确保主菜单中时间是正常流动的
            // （防止从暂停的游戏场景返回时 timeScale 仍为 0）
            Time.timeScale = 1f;

            // 根据是否有存档来显示/隐藏"继续游戏"按钮
            UpdateContinueButton();
        }

        // ===== 按钮回调方法（在 Inspector 中绑定到 Button.OnClick） =====

        /// <summary>
        /// 新游戏按钮 — 如有存档弹确认框，没有则直接开始
        /// </summary>
        public void OnStartGame()
        {
            if (string.IsNullOrEmpty(gameSceneName))
            {
                Debug.LogWarning("[MainMenuManager] 未设置游戏场景名称！请在 Inspector 中配置 gameSceneName。");
                return;
            }

            // 如果有存档，弹确认框
            if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
            {
                if (confirmNewGamePanel != null)
                {
                    ShowPanel(confirmNewGamePanel, confirmCanvasGroup);
                }
                else
                {
                    Debug.LogWarning("[MainMenuManager] 未在 Inspector 中配置 confirmNewGamePanel！直接覆盖存档开始新游戏。");
                    StartNewGame();
                }
                return;
            }

            // 没有存档，直接开始新游戏
            StartNewGame();
        }

        /// <summary>
        /// 确认新游戏（确认面板的"确认"按钮调用）
        /// </summary>
        public void OnConfirmNewGame()
        {
            HidePanel(confirmNewGamePanel, confirmCanvasGroup);
            StartNewGame();
        }

        /// <summary>
        /// 取消新游戏（确认面板的"取消"按钮调用）
        /// </summary>
        public void OnCancelNewGame()
        {
            HidePanel(confirmNewGamePanel, confirmCanvasGroup);
        }

        /// <summary>
        /// 继续游戏（从存档恢复）
        /// </summary>
        public void OnContinueGame()
        {
            if (SaveManager.Instance == null || !SaveManager.Instance.HasSave())
            {
                Debug.LogWarning("[MainMenuManager] 没有存档，无法继续游戏");
                return;
            }

            var data = SaveManager.Instance.Load();
            if (data == null)
            {
                Debug.LogWarning("[MainMenuManager] 存档数据无效");
                return;
            }

            // 恢复勋章进度到 MedalManager
            SaveManager.Instance.RestoreMedalData(data);

            // 重置 GameManager 状态（清除上一局可能残留的暂停/锁定）
            if (GameManager.Instance != null)
                GameManager.Instance.ResetForNewGame();

            SceneTransitionManager.Instance.LoadFromSave(data);
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

        /// <summary>
        /// 执行新游戏：清除存档 + 重置单例状态 + 加载场景
        /// </summary>
        private void StartNewGame()
        {
            // 清除存档文件
            if (SaveManager.Instance != null)
                SaveManager.Instance.DeleteSave();

            // 重置所有持久化单例的内存状态，确保真正的新游戏
            if (GameManager.Instance != null)
                GameManager.Instance.ResetForNewGame();
            if (MedalManager.Instance != null)
                MedalManager.Instance.ResetForNewGame();

            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TransitionToScene(gameSceneName, newGameSpawnPointId);
            else
                SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>
        /// 根据是否有存档来决定"继续游戏"按钮的显隐
        /// </summary>
        private void UpdateContinueButton()
        {
            if (continueButton != null)
            {
                bool hasSave = SaveManager.Instance != null && SaveManager.Instance.HasSave();
                continueButton.SetActive(hasSave);
            }
        }

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

