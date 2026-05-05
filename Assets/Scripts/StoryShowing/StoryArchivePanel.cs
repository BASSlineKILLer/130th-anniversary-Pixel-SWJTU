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

    private static readonly Color LOCKED_COLOR = new Color(0.35f, 0.35f, 0.35f, 1f);
    private const string LOCKED_STORY = "???";

    private void Start()
    {
        AutoBindUI();
    }

    private void AutoBindUI()
    {
        Transform root = canvasRoot != null ? canvasRoot.transform : transform;

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

        if (Input.GetKeyDown(KeyCode.A))
            PrevPage();
        else if (Input.GetKeyDown(KeyCode.D))
            NextPage();
        else if (Input.GetKeyDown(KeyCode.Space))
            Hide();
    }

    public void Show()
    {
        if (data == null || data.entries == null || data.entries.Count == 0)
        {
            Debug.LogWarning("[StoryArchivePanel] SpecialNPCData 为空");
            return;
        }

        entries = data.entries;
        currentIndex = 0;
        isVisible = true;

        if (canvasRoot != null)
        {
            canvasRoot.SetActive(true);
            canvasRoot.transform.SetAsLastSibling();
        }

        RenderCurrent();
    }

    public void Hide()
    {
        isVisible = false;

        if (canvasRoot != null)
            canvasRoot.SetActive(false);

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
            string displayText = isUnlocked ? entry.storyText : LOCKED_STORY;
            storyText.text = $"<b>{npc.npcName}</b>\n\n{displayText}";
        }

        if (pageIndicator != null)
        {
            pageIndicator.text = $"{currentIndex + 1}/{entries.Count}";
        }
    }
}
