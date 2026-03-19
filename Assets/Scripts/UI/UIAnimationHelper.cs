using System.Collections;
using UnityEngine;

namespace SWJTUGame.UI
{
    /// <summary>
    /// UI 动画工具类
    /// 提供可复用的面板淡入淡出、缩放动画等
    /// 使用 unscaledDeltaTime，保证暂停时动画仍可播放
    /// </summary>
    public static class UIAnimationHelper
    {
        /// <summary>
        /// 淡入面板（alpha 从 0 → 1），同时激活 GameObject
        /// 需要目标物体上挂有 CanvasGroup 组件
        /// </summary>
        /// <param name="panel">要淡入的面板</param>
        /// <param name="duration">动画时长（秒）</param>
        /// <returns>协程</returns>
        public static IEnumerator FadeIn(CanvasGroup panel, float duration = 0.2f)
        {
            if (panel == null) yield break;

            panel.gameObject.SetActive(true);
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

        /// <summary>
        /// 淡出面板（alpha 从 1 → 0），完成后关闭 GameObject
        /// 需要目标物体上挂有 CanvasGroup 组件
        /// </summary>
        /// <param name="panel">要淡出的面板</param>
        /// <param name="duration">动画时长（秒）</param>
        /// <returns>协程</returns>
        public static IEnumerator FadeOut(CanvasGroup panel, float duration = 0.2f)
        {
            if (panel == null) yield break;

            panel.interactable = false;
            panel.blocksRaycasts = false;

            float startAlpha = panel.alpha;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                panel.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                yield return null;
            }

            panel.alpha = 0f;
            panel.gameObject.SetActive(false);
        }

        /// <summary>
        /// 按钮点击缩放反馈（先缩小再弹回）
        /// </summary>
        /// <param name="target">按钮的 RectTransform</param>
        /// <param name="punchScale">缩小比例（默认 0.9）</param>
        /// <param name="duration">动画总时长</param>
        /// <returns>协程</returns>
        public static IEnumerator ButtonPunchScale(Transform target, float punchScale = 0.9f, float duration = 0.15f)
        {
            if (target == null) yield break;

            Vector3 originalScale = target.localScale;
            Vector3 smallScale = originalScale * punchScale;

            float half = duration * 0.5f;

            // 缩小
            float elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(originalScale, smallScale, elapsed / half);
                yield return null;
            }

            // 弹回
            elapsed = 0f;
            while (elapsed < half)
            {
                elapsed += Time.unscaledDeltaTime;
                target.localScale = Vector3.Lerp(smallScale, originalScale, elapsed / half);
                yield return null;
            }

            target.localScale = originalScale;
        }
    }
}
