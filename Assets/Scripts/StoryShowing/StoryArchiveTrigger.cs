using UnityEngine;

public class StoryArchiveTrigger : MonoBehaviour
{
    [Header("面板引用")]
    [Tooltip("拖入场景中的 ShowStoryCanvas（挂有 StoryArchivePanel）")]
    public StoryArchivePanel archivePanel;

    [Header("提示")]
    [Tooltip("可选：靠近时显示的提示文字 GameObject")]
    public GameObject hintUI;

    private bool playerInTrigger = false;
    private bool panelOpen = false;

    private void Start()
    {
        if (archivePanel != null)
        {
            archivePanel.onPanelHidden.AddListener(OnPanelClosed);
            archivePanel.gameObject.SetActive(false);
        }
        if (hintUI != null)
            hintUI.SetActive(false);
    }

    private void OnDestroy()
    {
        if (archivePanel != null)
            archivePanel.onPanelHidden.RemoveListener(OnPanelClosed);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = true;
            if (hintUI != null && !panelOpen)
                hintUI.SetActive(true);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInTrigger = false;
            if (hintUI != null)
                hintUI.SetActive(false);
        }
    }

    private void Update()
    {
        if (!playerInTrigger) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!panelOpen)
                OpenArchive();
            else
                CloseArchive();
        }
    }

    private void OpenArchive()
    {
        if (archivePanel == null) return;

        LockMovement();
        archivePanel.Show();
        panelOpen = true;

        if (hintUI != null)
            hintUI.SetActive(false);
    }

    private void CloseArchive()
    {
        if (archivePanel != null)
            archivePanel.Hide();
    }

    private void OnPanelClosed()
    {
        panelOpen = false;
        UnlockMovement();

        if (hintUI != null && playerInTrigger)
            hintUI.SetActive(true);
    }

    private void LockMovement()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetDialogueLock(true);
    }

    private void UnlockMovement()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.SetDialogueLock(false);
    }
}
