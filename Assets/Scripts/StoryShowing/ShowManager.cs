using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShowManager : MonoBehaviour
{
    public ShowPanel showPanel;
    public SpecialNPCData data;

    private bool wasSpacePressed = false;
    private bool playerInTrigger = false;

    private void Start()
    {
        if (showPanel != null)
        {
            showPanel.onPanelHidden.AddListener(UnlockMovement);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
            Debug.Log("Player entered ShowManager trigger");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            Debug.Log("Player exited ShowManager trigger");
        }
    }

    private void Update()
    {
        if (!playerInTrigger) return;

        bool isSpaceDown = Input.GetKey(KeyCode.Space);
        if (isSpaceDown && !wasSpacePressed)
            ShowStory();
        wasSpacePressed = isSpaceDown;
    }

    private void ShowStory()
    {
        Debug.Log("ShowStory called");
        if (data == null || showPanel == null)
        {
            Debug.Log("ShowStory: data=" + data + ", showPanel=" + showPanel);
            return;
        }

        List<string> storyTexts = new List<string>();

        if (MedalManager.Instance != null)
        {
            var talkedNPCs = MedalManager.Instance.GetTalkedSpecialNPCs();
            Debug.Log("Talked special NPCs: " + string.Join(", ", talkedNPCs));

            foreach (var npc in talkedNPCs)
            {
                string storyText = data.GetStoryText(npc);
                Debug.Log($"Story text for {npc}: '{storyText}'");
                if (!string.IsNullOrEmpty(storyText))
                {
                    storyTexts.Add(storyText);
                }
            }
        }

        Debug.Log("Total story texts: " + storyTexts.Count);

        if (storyTexts.Count > 0)
        {
            LockMovement();
            showPanel.Show(storyTexts);
        }
        else
        {
            LockMovement();
            showPanel.Show(new List<string> { "未收集到故事" });
        }
    }

    private void LockMovement()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDialogueLock(true);
        }
    }

    private void UnlockMovement()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDialogueLock(false);
        }
    }
}
