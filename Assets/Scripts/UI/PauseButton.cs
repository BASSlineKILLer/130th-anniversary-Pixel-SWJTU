using UnityEngine;

/// <summary>
/// 暂停按钮组件
/// 挂载到 UI 按钮上，点击效果和按 ESC 键一样（切换暂停/恢复）
/// </summary>
public class PauseButton : MonoBehaviour
{
    /// <summary>
    /// 按钮点击回调 — 在 Inspector 中绑定到 Button.OnClick
    /// </summary>
    public void OnClick()
    {
        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[PauseButton] GameManager 不存在");
            return;
        }

        // 对话锁定期间不允许暂停
        if (GameManager.Instance.isDialogueLocked)
            return;

        // 切换暂停状态（和 ESC 键逻辑一致）
        if (GameManager.Instance.isPaused)
            GameManager.Instance.ResumeGame();
        else
            GameManager.Instance.PauseGame();
    }
}
