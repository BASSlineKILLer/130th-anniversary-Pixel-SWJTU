using TMPro;
using UnityEngine;
using UnityEngine.UI;

public static class SceneLoadingOverlay
{
    private const int SORTING_ORDER = 10000;
    private const float PANEL_ALPHA = 0.65f;
    private const float BAR_WIDTH = 520f;
    private const float BAR_HEIGHT = 22f;
    private const float TEXT_Y = -24f;
    private const float BAR_Y = -74f;
    private const string ROOT_NAME = "SceneLoadingOverlay";
    private const string DEFAULT_MESSAGE = "Loading...";

    private const float SHIMMER_WIDTH = 20f;
    private const float SHIMMER_SPEED = 100f;

    private static GameObject root;
    private static TextMeshProUGUI messageText;
    private static Image progressFill;
    private static RectTransform shimmerRect;
    private static RectTransform barBackgroundRect;

    public static void Show(string message)
    {
        EnsureCreated();
        root.SetActive(true);
        SetMessage(message);
        SetProgress(0f);
    }

    public static void SetProgress(float progress)
    {
        EnsureCreated();
        progressFill.fillAmount = Mathf.Clamp01(progress);
    }

    public static void Hide()
    {
        if (root == null) return;
        root.SetActive(false);
    }

    private static void SetMessage(string message)
    {
        messageText.text = string.IsNullOrEmpty(message) ? DEFAULT_MESSAGE : message;
    }

    private static void EnsureCreated()
    {
        if (root != null) return;

        root = new GameObject(ROOT_NAME);
        Object.DontDestroyOnLoad(root);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = SORTING_ORDER;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        root.AddComponent<GraphicRaycaster>();
        CreatePanel(root.transform);
        messageText = CreateText(root.transform);
        progressFill = CreateProgressBar(root.transform);

        var runner = root.AddComponent<ShimmerRunner>();
        runner.Init(shimmerRect, barBackgroundRect, progressFill);

        root.SetActive(false);
    }

    private static void CreatePanel(Transform parent)
    {
        var panel = new GameObject("Panel");
        panel.transform.SetParent(parent, false);

        var image = panel.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, PANEL_ALPHA);
        image.raycastTarget = false;

        var rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TextMeshProUGUI CreateText(Transform parent)
    {
        var textObject = new GameObject("MessageText");
        textObject.transform.SetParent(parent, false);

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 32f;
        text.color = Color.white;
        text.raycastTarget = false;

        var rect = text.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(BAR_WIDTH, 60f);
        rect.anchoredPosition = new Vector2(0f, TEXT_Y);
        return text;
    }

    private static Image CreateProgressBar(Transform parent)
    {
        var background = new GameObject("ProgressBackground");
        background.transform.SetParent(parent, false);

        var backgroundImage = background.AddComponent<Image>();
        backgroundImage.color = new Color(1f, 1f, 1f, 0.25f);
        backgroundImage.raycastTarget = false;

        barBackgroundRect = backgroundImage.rectTransform;
        barBackgroundRect.anchorMin = new Vector2(0.5f, 0.5f);
        barBackgroundRect.anchorMax = new Vector2(0.5f, 0.5f);
        barBackgroundRect.sizeDelta = new Vector2(BAR_WIDTH, BAR_HEIGHT);
        barBackgroundRect.anchoredPosition = new Vector2(0f, BAR_Y);

        var fill = new GameObject("ProgressFill");
        fill.transform.SetParent(background.transform, false);

        var fillImage = fill.AddComponent<Image>();
        fillImage.color = Color.white;
        fillImage.type = Image.Type.Filled;
        fillImage.fillMethod = Image.FillMethod.Horizontal;
        fillImage.fillOrigin = (int)Image.OriginHorizontal.Left;
        fillImage.raycastTarget = false;

        var fillRect = fillImage.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        shimmerRect = CreateShimmer(background.transform);

        return fillImage;
    }

    private static RectTransform CreateShimmer(Transform parent)
    {
        var shimmer = new GameObject("Shimmer");
        shimmer.transform.SetParent(parent, false);

        var img = shimmer.AddComponent<Image>();
        img.raycastTarget = false;

        var gradient = new Texture2D(4, 1);
        gradient.SetPixels(new[] {
            new Color(1f, 1f, 1f, 0f),
            new Color(1f, 1f, 1f, 0.55f),
            new Color(1f, 1f, 1f, 0.55f),
            new Color(1f, 1f, 1f, 0f),
        });
        gradient.Apply();
        img.sprite = Sprite.Create(gradient, new Rect(0, 0, 4, 1), new Vector2(0.5f, 0.5f));

        var rect = img.rectTransform;
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.sizeDelta = new Vector2(SHIMMER_WIDTH, 0f);
        rect.anchoredPosition = new Vector2(-SHIMMER_WIDTH, 0f);

        return rect;
    }

    private class ShimmerRunner : MonoBehaviour
    {
        private RectTransform shimmer;
        private RectTransform barRect;
        private Image fill;
        private float shimmerX;

        public void Init(RectTransform shimmerRect, RectTransform barBackground, Image progressFill)
        {
            shimmer = shimmerRect;
            barRect = barBackground;
            fill = progressFill;
            shimmerX = -SHIMMER_WIDTH;
        }

        private void Update()
        {
            if (shimmer == null || fill == null) return;

            float fillWidth = barRect.sizeDelta.x * fill.fillAmount;
            if (fillWidth < 1f)
            {
                shimmer.anchoredPosition = new Vector2(-SHIMMER_WIDTH, 0f);
                return;
            }

            shimmerX += SHIMMER_SPEED * Time.unscaledDeltaTime;
            if (shimmerX > fillWidth + SHIMMER_WIDTH)
                shimmerX = -SHIMMER_WIDTH;

            shimmer.anchoredPosition = new Vector2(shimmerX - SHIMMER_WIDTH * 0.5f, 0f);
        }
    }
}
