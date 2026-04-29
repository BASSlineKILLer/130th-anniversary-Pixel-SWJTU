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

    private void DisplayCurrentPage()
    {
        // Get talked special NPCs
        var talkedSpecialNPCs = MedalManager.Instance != null ? MedalManager.Instance.GetTalkedSpecialNPCs() : new List<string>();

        int startIndex = currentPage * NPCsPerPage;
        int endIndex = Mathf.Min(startIndex + NPCsPerPage, allEntries.Count);

        // Set up position
        if (endIndex > startIndex)
        {
            var entry = allEntries[startIndex];
            if (entry.specialNPCEntry != null)
            {
                // Set name up
                var nameTextUp = cardContainer.Find("NameTextUp")?.GetComponent<TextMeshProUGUI>();
                if (nameTextUp != null)
                {
                    nameTextUp.text = entry.specialNPCEntry.npcName;
                }

                // Set image up
                var portraitImageUp = cardContainer.Find("PortraitImageUp")?.GetComponent<Image>();
                if (portraitImageUp != null)
                {
                    bool isUnlocked = talkedSpecialNPCs.Contains(entry.specialNPCEntry.npcName);
                    Sprite portrait = isUnlocked ? entry.specialNPCEntry.GetPortrait() : entry.specialNPCEntry.worldSprite;
                    portraitImageUp.sprite = portrait;
                    portraitImageUp.color = isUnlocked ? Color.white : Color.gray;
                }

                // Set story text up
                var storyTextUp = cardContainer.Find("StoryTextUp")?.GetComponent<TextMeshProUGUI>();
                if (storyTextUp != null)
                {
                    bool isUnlocked = talkedSpecialNPCs.Contains(entry.specialNPCEntry.npcName);
                    storyTextUp.text = isUnlocked ? entry.storyText : "???";
                }

                // Set index text up
                var indexTextUp = cardContainer.Find("IndexTextUp")?.GetComponent<TextMeshProUGUI>();
                if (indexTextUp != null)
                {
                    indexTextUp.text = $"第{startIndex + 1}个特殊NPC";
                }
            }
        }

        // Set down position
        if (endIndex > startIndex + 1)
        {
            var entry = allEntries[startIndex + 1];
            if (entry.specialNPCEntry != null)
            {
                // Set name down
                var nameTextDown = cardContainer.Find("NameTextDown")?.GetComponent<TextMeshProUGUI>();
                if (nameTextDown != null)
                {
                    nameTextDown.text = entry.specialNPCEntry.npcName;
                }

                // Set image down
                var portraitImageDown = cardContainer.Find("PortraitImageDown")?.GetComponent<Image>();
                if (portraitImageDown != null)
                {
                    bool isUnlocked = talkedSpecialNPCs.Contains(entry.specialNPCEntry.npcName);
                    Sprite portrait = isUnlocked ? entry.specialNPCEntry.GetPortrait() : entry.specialNPCEntry.worldSprite;
                    portraitImageDown.sprite = portrait;
                    portraitImageDown.color = isUnlocked ? Color.white : Color.gray;
                }

                // Set story text down
                var storyTextDown = cardContainer.Find("StoryTextDown")?.GetComponent<TextMeshProUGUI>();
                if (storyTextDown != null)
                {
                    bool isUnlocked = talkedSpecialNPCs.Contains(entry.specialNPCEntry.npcName);
                    storyTextDown.text = isUnlocked ? entry.storyText : "???";
                }

                // Set index text down
                var indexTextDown = cardContainer.Find("IndexTextDown")?.GetComponent<TextMeshProUGUI>();
                if (indexTextDown != null)
                {
                    indexTextDown.text = $"第{startIndex + 2}个特殊NPC";
                }
            }
        }

        // Hide down if only one
        bool hasDown = endIndex > startIndex + 1;
        var downImage = cardContainer.Find("PortraitImageDown")?.gameObject;
        if (downImage != null) downImage.SetActive(hasDown);
        var downName = cardContainer.Find("NameTextDown")?.gameObject;
        if (downName != null) downName.SetActive(hasDown);
        var downText = cardContainer.Find("StoryTextDown")?.gameObject;
        if (downText != null) downText.SetActive(hasDown);
        var downIndex = cardContainer.Find("IndexTextDown")?.gameObject;
        if (downIndex != null) downIndex.SetActive(hasDown);
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
