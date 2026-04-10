using UnityEngine;
using TMPro;
using UnityEngine.Events;

public class MedalPanel : MonoBehaviour
{
    public TextMeshProUGUI panelText; // 面板上的文字组件
    public TextMeshProUGUI medalCountText; // 显示勋章个数的文字组件
    public CanvasGroup canvasGroup; // 用于淡入淡出
    public float fadeDuration = 0.15f; // 淡入淡出时长

    public UnityEvent onPanelHidden;

    private bool isVisible = false; // 是否正在显示

    private void Start()
    {
        gameObject.SetActive(false); // 确保一开始隐藏
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }
    }

    /// 显示面板并设置文字（同步）
    /// <param name="message">要显示的消息</param>
    public void ShowPanel(string message)
    {
        if (panelText != null)
        {
            panelText.text = message;
        }
        gameObject.SetActive(true);
        isVisible = true;
    }

    /// 隐藏面板（同步）
    public void HidePanel()
    {
        gameObject.SetActive(false);
        isVisible = false;
    }

    /// 显示面板并设置文字（带淡入效果）
    /// <param name="message">要显示的消息</param>
    public System.Collections.IEnumerator ShowPanelWithFade(string message)
    {
        Debug.Log("ShowPanelWithFade started, canvasGroup=" + (canvasGroup != null));

        // 设置文字
        if (panelText != null)
        {
            panelText.text = message;
        }

        // 激活对象
        gameObject.SetActive(true);

        // 淡入
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeIn(canvasGroup, fadeDuration));
        }

        isVisible = true;
        Debug.Log("ShowPanelWithFade completed");

        // 等待1秒后自动消失
        yield return new WaitForSeconds(0.5f);
        yield return StartCoroutine(HidePanelWithFade());
    }

    /// 隐藏面板（带淡出效果）
    public System.Collections.IEnumerator HidePanelWithFade()
    {
        isVisible = false; // 立即设置为不可见，防止重复调用

        // 淡出
        if (canvasGroup != null)
        {
            yield return StartCoroutine(FadeOut(canvasGroup, fadeDuration));
        }

        // 隐藏对象
        gameObject.SetActive(false);
        onPanelHidden?.Invoke();
    }

    private System.Collections.IEnumerator FadeIn(CanvasGroup panel, float duration)
    {
        if (panel == null) yield break;

        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            panel.alpha = Mathf.Clamp01(elapsed / duration);
            yield return null;
        }

        panel.alpha = 1f;
        panel.interactable = true;
        panel.blocksRaycasts = true;
    }

    private System.Collections.IEnumerator FadeOut(CanvasGroup panel, float duration)
    {
        if (panel == null) yield break;

        panel.alpha = 1f;
        panel.interactable = false;
        panel.blocksRaycasts = false;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            panel.alpha = Mathf.Clamp01(1f - elapsed / duration);
            yield return null;
        }

        panel.alpha = 0f;
    }

    /// 检查面板是否可见
    public bool IsVisible => isVisible;

    /// 设置面板文字
    /// <param name="message">要显示的消息</param>
    public void SetMessage(string message)
    {
        if (panelText != null)
        {
            panelText.text = message;
        }
    }

    /// 设置勋章个数
    /// <param name="count">勋章个数</param>
    public void SetMedalCount(int count)
    {
        if (medalCountText != null)
        {
            medalCountText.text = "当前交大勋章总数：" + count.ToString();
        }
    }
}
