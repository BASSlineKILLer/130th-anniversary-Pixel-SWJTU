using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对话系统（单例）
/// 对话框左边显示NPC头像，右边显示文字
/// </summary>
public class DialogueSystem : MonoBehaviour
{
    public static DialogueSystem Instance { get; private set; }

    [Header("UI引用")]
    public GameObject dialoguePanel;
    public Image npcPortraitImage;
    public Text npcNameText;
    public Text dialogueText;

    private bool isDialogueActive = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);
    }

    private void Update()
    {
        if (isDialogueActive && Input.GetKeyDown(KeyCode.E))
        {
            EndDialogue();
        }
    }

    public void StartDialogue(Sprite portrait, string npcName, string text)
    {
        if (dialoguePanel == null) return;

        isDialogueActive = true;
        dialoguePanel.SetActive(true);

        if (npcPortraitImage != null && portrait != null)
            npcPortraitImage.sprite = portrait;

        if (npcNameText != null)
            npcNameText.text = npcName;

        if (dialogueText != null)
            dialogueText.text = text;

        // 暂停玩家移动
        if (GameManager.Instance != null)
            GameManager.Instance.isPaused = true;
    }

    public void EndDialogue()
    {
        isDialogueActive = false;

        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        if (GameManager.Instance != null)
            GameManager.Instance.isPaused = false;
    }
}
