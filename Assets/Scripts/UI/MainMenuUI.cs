using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 主菜单UI逻辑
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("UI面板")]
    public GameObject mainMenuPanel;
    public Button startButton;
    public Button settingsButton;
    public Button quitButton;

    private void Start()
    {
        startButton?.onClick.AddListener(OnStartGame);
        settingsButton?.onClick.AddListener(OnOpenSettings);
        quitButton?.onClick.AddListener(OnQuitGame);
    }

    private void OnStartGame()
    {
        GameSceneManager.Instance?.LoadScene("GameScene");
    }

    private void OnOpenSettings()
    {
        // 打开设置面板
    }

    private void OnQuitGame()
    {
        GameManager.Instance?.QuitGame();
    }
}
