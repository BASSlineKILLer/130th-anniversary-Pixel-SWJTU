using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class StoryArchivePanel : MonoBehaviour
{
    [Header("Canvas 引用")]
    [Tooltip("拖入场景中的 ShowStoryCanvas GameObject")]
    public GameObject canvasRoot;

    [Header("数据")]
    public SpecialNPCData data;

    [Header("UI 引用（自动从 canvasRoot 查找）")]
    public Image portraitImage;
    public TextMeshProUGUI storyText;
    public TextMeshProUGUI pageIndicator;

    public UnityEvent onPanelHidden;

    private List<SpecialNPCData.Entry> entries;
    private int currentIndex = 0;
    private bool isVisible = false;
    private Transform innerPanel; // ShowStoryPanel 子物体，真正承载内容
    private int shownFrame = -1; // 记录 Show() 的帧号，当帧不响应关闭输入

    private static readonly Color LOCKED_COLOR = new Color(0.35f, 0.35f, 0.35f, 1f);
    private const string LOCKED_STORY = "???";

    private void Start()
    {
        AutoBindUI();
    }

    private void AutoBindUI()
    {
        Transform root = canvasRoot != null ? canvasRoot.transform : transform;

        if (innerPanel == null)
            innerPanel = root.Find("ShowStoryPanel");

        if (portraitImage == null)
        {
            var imgTrans = root.Find("ShowStoryPanel/Image/PortraitImage ");
            if (imgTrans != null) portraitImage = imgTrans.GetComponent<Image>();
        }
        if (storyText == null)
        {
            var txtTrans = root.Find("ShowStoryPanel/Image/Text (TMP)");
            if (txtTrans != null) storyText = txtTrans.GetComponent<TextMeshProUGUI>();
        }
    }

    private void Update()
    {
        if (!isVisible) return;
        // 避免与 StoryArchiveTrigger 同帧抢 Space 输入导致立即关闭
        if (Time.frameCount == shownFrame) return;

        if (Input.GetKeyDown(KeyCode.A))
            PrevPage();
        else if (Input.GetKeyDown(KeyCode.D))
            NextPage();
        else if (Input.GetKeyDown(KeyCode.Space))
            Hide();
    }

    public void Show()
    {
        AutoBindUI();

        if (data == null || data.entries == null || data.entries.Count == 0)
        {
            Debug.LogWarning("[StoryArchivePanel] SpecialNPCData 为空");
            return;
        }

        entries = data.entries;
        currentIndex = 0;
        isVisible = true;
        shownFrame = Time.frameCount;

        if (canvasRoot != null)
        {
            canvasRoot.SetActive(true);
            FixCanvasRootTransform();
            canvasRoot.transform.SetAsLastSibling();
        }

        if (innerPanel != null)
            innerPanel.gameObject.SetActive(true);
        else if (canvasRoot == null)
            gameObject.SetActive(true);

        RenderCurrent();
    }

    /// <summary>
    /// 修正 ShowStoryCanvas prefab 上 RectTransform 的异常配置：
    /// prefab 中 m_LocalScale = (0,0,0) 会导致整张 UI 不可见；
    /// SizeDelta/Anchors 也可能被设为 0，这里强制还原为占满屏幕。
    /// </summary>
    private void FixCanvasRootTransform()
    {
        if (canvasRoot == null) return;
        var rt = canvasRoot.GetComponent<RectTransform>();
        if (rt == null) return;

        if (rt.localScale.sqrMagnitude < 0.0001f)
            rt.localScale = Vector3.one;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    public void HideCanvasOnly()
    {
        AutoBindUI();
        isVisible = false;

        // 只隐藏内部 ShowStoryPanel，保持 ShowStoryCanvas 自身 active，
        // 这样挂在 ShowStoryCanvas 下的 HintPanel 等子物体仍可独立控制。
        if (innerPanel != null)
            innerPanel.gameObject.SetActive(false);
    }

    public void Hide()
    {
        AutoBindUI();
        isVisible = false;

        if (innerPanel != null)
            innerPanel.gameObject.SetActive(false);
        else if (canvasRoot == null)
            gameObject.SetActive(false);

        onPanelHidden?.Invoke();
    }

    private void NextPage()
    {
        if (entries == null || entries.Count == 0) return;
        currentIndex = (currentIndex + 1) % entries.Count;
        RenderCurrent();
    }

    private void PrevPage()
    {
        if (entries == null || entries.Count == 0) return;
        currentIndex = (currentIndex - 1 + entries.Count) % entries.Count;
        RenderCurrent();
    }

    private void RenderCurrent()
    {
        if (entries == null || currentIndex >= entries.Count) return;

        var entry = entries[currentIndex];
        if (entry == null || entry.specialNPCEntry == null) return;

        var npc = entry.specialNPCEntry;
        bool isUnlocked = MedalManager.Instance != null
            && MedalManager.Instance.GetTalkedSpecialNPCs().Contains(npc.npcName);

        if (portraitImage != null)
        {
            portraitImage.sprite = isUnlocked ? npc.GetPortrait() : npc.worldSprite;
            portraitImage.color = isUnlocked ? Color.white : LOCKED_COLOR;
        }

        if (storyText != null)
        {
            if (isUnlocked)
            {
                storyText.text = $"<align=center>{entry.panelText}</align>\n<align=right>{npc.npcName}</align>\n\n{entry.storyText}";
            }
            else
            {
                storyText.text = $"<align=center>{LOCKED_STORY}</align>\n<align=right>{npc.npcName}</align>";
            }
        }

        if (pageIndicator != null)
        {
            pageIndicator.text = $"{currentIndex + 1}/{entries.Count}";
        }
    }
}
