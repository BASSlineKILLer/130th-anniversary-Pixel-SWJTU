using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 暂停菜单逻辑
/// </summary>
public class PauseMenuUI : MonoBehaviour
{
    [Header("UI引用")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button mainMenuButton;
    public KeyCode pauseKey = KeyCode.Escape;

    private void Start()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);

        resumeButton?.onClick.AddListener(OnResume);
        mainMenuButton?.onClick.AddListener(OnReturnToMainMenu);
    }

    private void Update()
    {
        if (Input.GetKeyDown(pauseKey))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        if (GameManager.Instance == null) return;

        if (GameManager.Instance.isPaused)
            OnResume();
        else
            OnPause();
    }

    private void OnPause()
    {
        if (pausePanel != null)
            pausePanel.SetActive(true);
        GameManager.Instance?.PauseGame();
    }

    private void OnResume()
    {
        if (pausePanel != null)
            pausePanel.SetActive(false);
        GameManager.Instance?.ResumeGame();
    }

    private void OnReturnToMainMenu()
    {
        GameManager.Instance?.ResumeGame();
        GameSceneManager.Instance?.LoadScene("MainMenu");
    }
}
