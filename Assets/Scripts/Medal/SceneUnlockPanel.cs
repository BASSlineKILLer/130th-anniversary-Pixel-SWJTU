using UnityEngine;
using TMPro;
using SWJTUGame.UI; // Add this for UIAnimationHelper

public class SceneUnlockPanel : MonoBehaviour
{
    public TextMeshProUGUI panelText; // 拖入面板上的TextMeshProUGUI组件
    public CanvasGroup canvasGroup; // 拖入CanvasGroup组件
    public float fadeDuration = 0.15f; // 淡入淡出时长

    private bool isVisible = false; // 是否正在显示

    private void Start()
    {
        if (!isVisible)
        {
            gameObject.SetActive(false); // 确保一开始隐藏
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }
    }



    /// 显示面板并设置文字（带淡入效果）
    /// <param name="message">要显示的消息</param>
    public System.Collections.IEnumerator ShowPanelWithFade(string message)
    {
        isVisible = true;

        // 1. 必须先激活对象，否则无法运行后续逻辑
        gameObject.SetActive(true);
        transform.SetAsLastSibling(); // 确保在最上层

        if (panelText != null) {
            panelText.text = message;
            Debug.Log("Panel text set to: " + message);
        } else {
            Debug.Log("panelText is null");
        }

        if (canvasGroup != null) {
            Debug.Log("CanvasGroup found, starting fade in");
            yield return StartCoroutine(FadeIn(canvasGroup, fadeDuration));
        } else {
            Debug.Log("CanvasGroup is null, panel shown without fade");
        }

        Debug.Log("ShowPanelWithFade completed for SceneUnlockPanel");

        // 等待0.7秒后自动消失
        yield return new WaitForSeconds(0.7f);
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
    }

    private System.Collections.IEnumerator FadeIn(CanvasGroup panel, float duration)
    {
        if (panel == null)
        {
            // 如果没有 CanvasGroup，直接激活并等待
            Debug.Log("No CanvasGroup found, showing panel immediately");
            yield break;
        }

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

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            panel.alpha = Mathf.Clamp01(1f - elapsed / duration);
            yield return null;
        }

        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
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

    /// 显示面板并设置文字（同步）
    /// <param name="message">要显示的消息</param>
    public void ShowPanel(string message)
    {
        if (panelText != null)
        {
            panelText.text = message;
        }
        isVisible = true;
        gameObject.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }
        Debug.Log("Panel shown synchronously: " + message);
    }
}
