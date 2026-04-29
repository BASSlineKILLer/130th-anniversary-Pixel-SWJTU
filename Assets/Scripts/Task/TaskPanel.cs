using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class TaskPanel : MonoBehaviour
{
    public UnityEvent onPanelHidden;

    [Header("UI Components")]
    public GameObject panel; // The main panel
    public Transform cardContainer; // Container with GridLayoutGroup
    public GameObject cardPrefab; // Prefab for each card, should have Image for portrait, TextMeshProUGUI for name and text

    private List<GameObject> cardInstances = new List<GameObject>();
    private List<SpecialNPCData.Entry> allEntries;
    private int currentPage = 0;
    private const int NPCsPerPage = 2;

    private void Start()
    {
        
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                PreviousPage();
            }
            else if (Input.GetKeyDown(KeyCode.D))
            {
                NextPage();
            }
        }
    }

    public void Show(List<SpecialNPCData.Entry> entries)
    {
        allEntries = entries;
        currentPage = 0;
        if (panel != null) panel.SetActive(true);
        DisplayCurrentPage();
    }

    // 卡片上的子物体名后缀，按"上/下"两槽位区分
    private const string SLOT_UP = "Up";
    private const string SLOT_DOWN = "Down";

    // 卡片边框颜色：彩蛋金色 / 普通白色 / 未解锁灰色
    private static readonly Color FRAME_EASTER_EGG = new Color(1f, 0.84f, 0f, 1f); // #FFD700
    private static readonly Color FRAME_NORMAL = Color.white;
    private static readonly Color FRAME_LOCKED = new Color(0.4f, 0.4f, 0.4f, 1f);

    private void DisplayCurrentPage()
    {
        var talkedSpecialNPCs = MedalManager.Instance != null
            ? MedalManager.Instance.GetTalkedSpecialNPCs()
            : new List<string>();

        int startIndex = currentPage * NPCsPerPage;
        int endIndex = Mathf.Min(startIndex + NPCsPerPage, allEntries.Count);

        bool hasUp = endIndex > startIndex;
        bool hasDown = endIndex > startIndex + 1;

        if (hasUp) RenderCard(SLOT_UP, allEntries[startIndex], startIndex + 1, talkedSpecialNPCs);
        if (hasDown) RenderCard(SLOT_DOWN, allEntries[startIndex + 1], startIndex + 2, talkedSpecialNPCs);

        // 只有一个 NPC 时隐藏下方槽位
        SetSlotActive(SLOT_DOWN, hasDown);
    }

    /// <summary>
    /// 渲染单个卡片槽位。slotSuffix=Up/Down，对应 cardContainer 下的子物体命名约定：
    /// NameText{suffix} / PortraitImage{suffix} / StoryText{suffix} / IndexText{suffix}
    /// 可选：CardFrame{suffix}（彩蛋金色边框）/ EasterEggBadge{suffix}（彩蛋标签）
    /// </summary>
    private void RenderCard(string slotSuffix, SpecialNPCData.Entry entry, int displayIndex, List<string> talkedSpecialNPCs)
    {
        if (entry == null || entry.specialNPCEntry == null) return;

        var npc = entry.specialNPCEntry;
        bool isUnlocked = talkedSpecialNPCs.Contains(npc.npcName);
        bool isEasterEgg = npc.isEasterEgg;

        SetText($"NameText{slotSuffix}", npc.npcName);
        SetText($"StoryText{slotSuffix}", isUnlocked ? entry.storyText : "???");
        SetText($"IndexText{slotSuffix}", $"第{displayIndex}个特殊NPC");

        // 立绘：未解锁灰色剪影 + worldSprite，已解锁原色 + portrait
        var portrait = cardContainer.Find($"PortraitImage{slotSuffix}")?.GetComponent<Image>();
        if (portrait != null)
        {
            portrait.sprite = isUnlocked ? npc.GetPortrait() : npc.worldSprite;
            portrait.color = isUnlocked ? Color.white : Color.gray;
        }

        // 边框：彩蛋金色，普通白色，未解锁灰色
        var frame = cardContainer.Find($"CardFrame{slotSuffix}")?.GetComponent<Image>();
        if (frame != null)
        {
            if (!isUnlocked) frame.color = FRAME_LOCKED;
            else if (isEasterEgg) frame.color = FRAME_EASTER_EGG;
            else frame.color = FRAME_NORMAL;
        }

        // 彩蛋标签：仅"已解锁 + 是彩蛋"时显示，避免剧透
        var badge = cardContainer.Find($"EasterEggBadge{slotSuffix}")?.gameObject;
        if (badge != null) badge.SetActive(isUnlocked && isEasterEgg);
    }

    private void SetText(string childName, string content)
    {
        var tmp = cardContainer.Find(childName)?.GetComponent<TextMeshProUGUI>();
        if (tmp != null) tmp.text = content;
    }

    private void SetSlotActive(string slotSuffix, bool active)
    {
        string[] childNames = {
            $"PortraitImage{slotSuffix}",
            $"NameText{slotSuffix}",
            $"StoryText{slotSuffix}",
            $"IndexText{slotSuffix}",
            $"CardFrame{slotSuffix}",
            $"EasterEggBadge{slotSuffix}",
        };
        foreach (var name in childNames)
        {
            var go = cardContainer.Find(name)?.gameObject;
            if (go != null) go.SetActive(active);
        }
    }

    private void NextPage()
    {
        int totalPages = Mathf.CeilToInt((float)allEntries.Count / NPCsPerPage);
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            DisplayCurrentPage();
        }
    }

    private void PreviousPage()
    {
        if (currentPage > 0)
        {
            currentPage--;
            DisplayCurrentPage();
        }
    }

    public void Hide()
    {
        if (panel != null) panel.SetActive(false);
        onPanelHidden?.Invoke();
    }
}
