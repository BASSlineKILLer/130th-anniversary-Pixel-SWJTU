using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
// 注意：点击音效由同对象上的 UIClickSound 组件负责（如果存在）
// ButtonHoverEffect 仅负责视觉反馈，不重复播放音效

namespace SWJTUGame.UI
{
    /// <summary>
    /// 按钮悬停效果组件
    /// 挂载在任意 UI 元素上即可获得悬停缩放 + 颜色高亮效果
    /// 完全自包含，不依赖任何外部脚本
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        [Header("缩放设置")]
        [Tooltip("悬停时的缩放倍数")]
        public float hoverScale = 1.1f;

        [Tooltip("点击时的缩放倍数")]
        public float pressScale = 0.95f;

        [Tooltip("缩放过渡速度")]
        public float scaleSpeed = 10f;

        private Vector3 originalScale;
        private float targetScale = 1f;

        private void Awake()
        {
            originalScale = transform.localScale;
        }

        private void Update()
        {
            // 平滑过渡到目标缩放值（使用 unscaledDeltaTime 以支持暂停时仍能播放）
            Vector3 target = originalScale * targetScale;
            transform.localScale = Vector3.Lerp(transform.localScale, target, scaleSpeed * Time.unscaledDeltaTime);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            targetScale = hoverScale;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            targetScale = 1f;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = pressScale;
            // 音效由 UIClickSound（同对象）负责，此处只做缩放视觉反馈
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // 如果鼠标仍在按钮上，恢复到悬停状态；否则恢复原始状态
            targetScale = eventData.hovered.Contains(gameObject) ? hoverScale : 1f;
        }

        private void OnDisable()
        {
            // 重置状态，防止禁用时卡在缩放中间态
            transform.localScale = originalScale;
            targetScale = 1f;
        }
    }
}
