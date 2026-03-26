using UnityEngine;

/// <summary>
/// 游戏管理器（单例）
/// 负责全局游戏状态管理，如暂停、恢复等
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏状态")]
    public bool isPaused = false;
    public bool isDialogueLocked = false;

    private void Awake()
    {
        // 实现单例模式：确保全局只有一个 GameManager
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 如果你希望在切场景时不销毁它，取消下面的注释
        // DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // 对话期间不允许 ESC 抢占为暂停菜单
        if (isDialogueLocked)
            return;

        // 示例：按下 ESC 键切换暂停状态
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    /// <summary>
    /// 暂停游戏
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // 停止游戏时间（物理和Invoke都会停止）
        Debug.Log("Game Paused");
        // 这里可以添加显示暂停菜单的代码
    }

    /// <summary>
    /// 恢复游戏
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // 恢复游戏时间
        Debug.Log("Game Resumed");
        // 这里可以添加隐藏暂停菜单的代码
    }

    /// <summary>
    /// 仅锁定/解锁玩家与游戏内交互，不触发暂停菜单，也不修改 timeScale。
    /// 用于 NPC 对话等需要停角色但不想弹出暂停窗口的场景。
    /// </summary>
    public void SetDialogueLock(bool locked)
    {
        isDialogueLocked = locked;
        Debug.Log(locked ? "Dialogue Locked" : "Dialogue Unlocked");
    }
}

