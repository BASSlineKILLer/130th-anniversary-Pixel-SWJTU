using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; //用于 Image 组件
using TMPro; //用于 TextMeshPro 组件

public class NPCInteraction : MonoBehaviour
{
    [Header("UI设置")]
    [Tooltip("对话框的整个面板对象")]
    public GameObject dialoguePanel;
    
    [Tooltip("对话框左边显示NPC立绘的Image")]
    public Image npcPortraitImage;
    
    [Tooltip("对话框右边显示文字的组件 (使用 TextMeshProUGUI)")]
    public TextMeshProUGUI npcDialogueText;

    [Header("交互设置")]
    public KeyCode interactKey = KeyCode.E;

    // 当前范围内可交互的 NPC
    private NPCController currentNPC;
    // 是否正在对话中
    private bool isDialoguing = false;

    void Start()
    {
        // 游戏开始时隐藏对话框
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
    }

    void Update()
    {
        // 检测交互按键
        if (Input.GetKeyDown(interactKey))
        {
            if (isDialoguing)
            {
                CloseDialogue();
            }
            else if (currentNPC != null)
            {
                OpenDialogue();
            }
        }
    }

    /// <summary>
    /// 打开对话框并显示内容
    /// </summary>
    private void OpenDialogue()
    {
        if (currentNPC == null || currentNPC.Info == null) return;

        isDialoguing = true;
        dialoguePanel.SetActive(true);

        // 1. 设置左边的立绘
        if (npcPortraitImage != null)
        {
            if (currentNPC.Info.Sprite != null)
            {
                npcPortraitImage.sprite = currentNPC.Info.Sprite;
                npcPortraitImage.gameObject.SetActive(true);
            }
            else
            {
                // 如果没有立绘，选择隐藏 Image 还是显示默认图片？这里先隐藏
                npcPortraitImage.gameObject.SetActive(false);
            }
        }

        // 2. 设置右边的文字
        if (npcDialogueText != null)
        {
            // 格式： 名字: 消息
            // 或者你可以根据需求只显示消息
            npcDialogueText.text = $"<color=yellow>{currentNPC.Info.Username}</color>:\n{currentNPC.Info.Message}";
        }

        // 3. 暂停游戏（禁止玩家移动）
        if (GameManager.Instance != null)
        {
            GameManager.Instance.PauseGame();
        }
    }

    /// <summary>
    /// 关闭对话框
    /// </summary>
    private void CloseDialogue()
    {
        isDialoguing = false;
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // 恢复游戏
        if (GameManager.Instance != null)
        {
            GameManager.Instance.ResumeGame();
        }
    }

    // 当进入 NPC 的 Trigger 范围时
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检查碰到的物体是否有 NPCController 组件
        NPCController npc = other.GetComponent<NPCController>();
        if (npc != null)
        {
            currentNPC = npc;
            // 这里可以加一些 UI 提示，比如 "按 E 交互"
        }
    }

    // 当离开 NPC 的 Trigger 范围时
    private void OnTriggerExit2D(Collider2D other)
    {
        NPCController npc = other.GetComponent<NPCController>();
        if (npc != null && npc == currentNPC)
        {
            // 如果正在对话中，强制关闭
            if (isDialoguing)
            {
                CloseDialogue();
            }
            currentNPC = null;
        }
    }
}
