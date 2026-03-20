using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using SWJTUGame.UI;

/// <summary>
/// NPC 对话交互系统（挂在 Player 上）
///
/// 功能：
///   - 自动检测附近 NPC，只对最近的一个显示气泡
///   - 按 E 打开对话框：左侧立绘、右上 username、右下 message（打字机逐字显示）
///   - 打字中按 E / 点击屏幕 → 立即显示全文
///   - 全文显示后按 E / 点击屏幕 → 关闭对话
///   - 对话期间角色不能移动（通过 GameManager.PauseGame）
///
/// 【对话框 Prefab 结构】
/// DialoguePanel (Image - 对话框.png 背景, CanvasGroup)
///   ├─ PortraitImage   (Image - 左侧 NPC 立绘, preserveAspect)
///   ├─ NameText        (TextMeshProUGUI - 右上角 username)
///   └─ MessageText     (TextMeshProUGUI - 右下方 message)
/// </summary>
public class NPCInteraction : MonoBehaviour
{
    [Header("对话框 UI")]
    [Tooltip("对话框面板（需要 CanvasGroup 组件用于淡入淡出）")]
    public CanvasGroup dialogueCanvasGroup;

    [Tooltip("左侧 NPC 立绘")]
    public Image npcPortraitImage;

    [Tooltip("右上角 NPC 名字")]
    public TextMeshProUGUI npcNameText;

    [Tooltip("右下方 NPC 说的话")]
    public TextMeshProUGUI npcMessageText;

    [Header("打字机设置")]
    [Tooltip("每个字符显示的间隔（秒）")]
    public float typeSpeed = 0.05f;

    [Header("动画设置")]
    [Tooltip("对话框淡入淡出时长")]
    public float fadeDuration = 0.15f;

    [Header("交互设置")]
    public KeyCode interactKey = KeyCode.E;

    // ===== 状态 =====
    private enum DialogueState { Idle, Typing, Waiting }
    private DialogueState state = DialogueState.Idle;

    // 附近的 NPC 集合
    private HashSet<NPCController> nearbyNPCs = new HashSet<NPCController>();
    // 当前最近的 NPC（显示气泡的那个）
    private NPCController closestNPC;
    // 当前正在对话的 NPC
    private NPCController dialogueNPC;

    // 打字机协程引用
    private Coroutine typewriterCoroutine;

    private void Start()
    {
        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
            dialogueCanvasGroup.interactable = false;
            dialogueCanvasGroup.blocksRaycasts = false;
            dialogueCanvasGroup.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 1. 每帧更新最近 NPC 气泡（对话中不切换气泡）
        if (state == DialogueState.Idle)
            UpdateClosestNPC();

        // 2. 处理交互输入
        bool inputTriggered = Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0);

        switch (state)
        {
            case DialogueState.Idle:
                // 只有按 E 才能开启对话（不响应鼠标点击，避免误触）
                if (Input.GetKeyDown(interactKey) && closestNPC != null)
                    OpenDialogue(closestNPC);
                break;

            case DialogueState.Typing:
                if (inputTriggered)
                    SkipTypewriter();
                break;

            case DialogueState.Waiting:
                if (inputTriggered)
                    CloseDialogue();
                break;
        }
    }

    // ==================== 最近 NPC 检测 ====================

    /// <summary>
    /// 每帧找出最近的 NPC，只对它显示气泡
    /// </summary>
    private void UpdateClosestNPC()
    {
        NPCController nearest = null;
        float minDist = float.MaxValue;
        Vector3 myPos = transform.position;

        // 清理已销毁的 NPC
        nearbyNPCs.RemoveWhere(n => n == null);

        foreach (var npc in nearbyNPCs)
        {
            float dist = Vector2.Distance(myPos, npc.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                nearest = npc;
            }
        }

        if (nearest != closestNPC)
        {
            // 旧的隐藏气泡
            if (closestNPC != null)
                closestNPC.HideBubble();

            closestNPC = nearest;

            // 新的显示气泡
            if (closestNPC != null)
                closestNPC.ShowBubble();
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        var npc = other.GetComponent<NPCController>();
        if (npc != null)
            nearbyNPCs.Add(npc);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        var npc = other.GetComponent<NPCController>();
        if (npc == null) return;

        nearbyNPCs.Remove(npc);

        // 如果离开的是正在对话的 NPC，强制关闭
        if (npc == dialogueNPC && state != DialogueState.Idle)
            CloseDialogue();

        // 如果离开的是显示气泡的 NPC，立刻切换
        if (npc == closestNPC)
        {
            npc.HideBubble();
            closestNPC = null;
        }
    }

    // ==================== 对话控制 ====================

    private void OpenDialogue(NPCController npc)
    {
        if (npc == null || npc.Info == null) return;

        dialogueNPC = npc;

        // 隐藏气泡（对话中不需要气泡）
        npc.HideBubble();

        // 设置 UI 内容
        SetPortrait(npc.Info.Sprite);
        SetName(npc.Info.Username);

        // 暂停游戏（角色不能移动）
        if (GameManager.Instance != null)
            GameManager.Instance.PauseGame();

        // 淡入面板
        StartCoroutine(ShowDialoguePanel(npc.Info.Message));
    }

    private IEnumerator ShowDialoguePanel(string message)
    {
        // 淡入
        yield return StartCoroutine(UIAnimationHelper.FadeIn(dialogueCanvasGroup, fadeDuration));

        // 开始打字机
        state = DialogueState.Typing;
        typewriterCoroutine = StartCoroutine(TypewriterRoutine(message));
    }

    private void CloseDialogue()
    {
        // 停止打字机
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        state = DialogueState.Idle;
        dialogueNPC = null;

        // 淡出面板
        StartCoroutine(HideDialoguePanel());
    }

    private IEnumerator HideDialoguePanel()
    {
        yield return StartCoroutine(UIAnimationHelper.FadeOut(dialogueCanvasGroup, fadeDuration));

        // 恢复游戏
        if (GameManager.Instance != null)
            GameManager.Instance.ResumeGame();
    }

    // ==================== 打字机效果 ====================

    /// <summary>
    /// 逐字显示 message。使用 TMP 的 maxVisibleCharacters。
    /// 因为对话时 timeScale=0，用 Time.unscaledDeltaTime 驱动。
    /// </summary>
    private IEnumerator TypewriterRoutine(string message)
    {
        npcMessageText.text = message;
        npcMessageText.ForceMeshUpdate();

        int totalChars = npcMessageText.textInfo.characterCount;
        npcMessageText.maxVisibleCharacters = 0;

        for (int i = 0; i < totalChars; i++)
        {
            npcMessageText.maxVisibleCharacters = i + 1;

            // 等待 typeSpeed 秒（unscaled）
            float elapsed = 0f;
            while (elapsed < typeSpeed)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
        }

        // 打字完成
        typewriterCoroutine = null;
        state = DialogueState.Waiting;
    }

    /// <summary>
    /// 跳过打字，立即显示全文
    /// </summary>
    private void SkipTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        // 显示全部文字
        if (npcMessageText != null)
        {
            npcMessageText.ForceMeshUpdate();
            npcMessageText.maxVisibleCharacters = npcMessageText.textInfo.characterCount;
        }

        state = DialogueState.Waiting;
    }

    // ==================== UI 设置 ====================

    private void SetPortrait(Sprite sprite)
    {
        if (npcPortraitImage == null) return;

        if (sprite != null)
        {
            npcPortraitImage.sprite = sprite;
            npcPortraitImage.preserveAspect = true;
            npcPortraitImage.gameObject.SetActive(true);
        }
        else
        {
            npcPortraitImage.gameObject.SetActive(false);
        }
    }

    private void SetName(string username)
    {
        if (npcNameText != null)
            npcNameText.text = string.IsNullOrEmpty(username) ? "???" : username;
    }
}
