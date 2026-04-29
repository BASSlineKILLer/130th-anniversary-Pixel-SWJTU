using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public TaskPanel taskPanel;
    public SpecialNPCData data;

    private bool wasSpacePressed = false;
    private bool playerInTrigger = false;
    private bool isPanelShown = false;

    private void Start()
    {
        if (taskPanel != null)
        {
            taskPanel.onPanelHidden.AddListener(UnlockMovement);
            if (taskPanel.panel != null)
            {
                taskPanel.panel.SetActive(false);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
            Debug.Log("Player entered TaskManager trigger");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            Debug.Log("Player exited TaskManager trigger");
        }
    }

    private void Update()
    {
        if (!playerInTrigger) return;

        bool isSpaceDown = Input.GetKey(KeyCode.Space);
        if (isSpaceDown && !wasSpacePressed)
        {
            if (!isPanelShown)
            {
                ShowTaskPanel();
                isPanelShown = true;
            }
            else
            {
                HideTaskPanel();
                isPanelShown = false;
            }
        }
        wasSpacePressed = isSpaceDown;
    }

    private void ShowTaskPanel()
    {
        Debug.Log("ShowTaskPanel called");
        if (data == null || taskPanel == null)
        {
            Debug.Log("ShowTaskPanel: data=" + data + ", taskPanel=" + taskPanel);
            return;
        }

        LockMovement();
        taskPanel.Show(data.entries);
    }

    private void HideTaskPanel()
    {
        if (taskPanel != null)
        {
            taskPanel.Hide();
        }
        UnlockMovement();
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
