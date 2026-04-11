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
///   - 对话期间角色不能移动（通过 GameManager.SetDialogueLock）
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

    // 特殊 NPC 多轮对话引用
    private SpecialNPCController specialDialogueNPC;

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
        ResetDialogueUI();
    }

    private void OnDisable()
    {
        // 对象失活时不能再启动协程，直接同步收尾，避免锁状态残留。
        CloseDialogueImmediate();
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
                // 只有按 E 才能���启对话（不响应鼠标点击，避免误触）
                if (Input.GetKeyDown(interactKey) && closestNPC != null)
                    OpenDialogue(closestNPC);
                break;

            case DialogueState.Typing:
                if (inputTriggered)
                    SkipTypewriter();
                break;

            case DialogueState.Waiting:
                if (inputTriggered)
                {
                    // 特殊 NPC：尝试推进到下一行；普通 NPC：直接关闭
                    if (specialDialogueNPC != null)
                        AdvanceSpecialDialogue();
                    else
                        CloseDialogue();
                }
                break;
        }
    }

    // ==================== 最近 NPC 检测 ====================

    /// 每帧找出最近的 NPC，只对它显示气泡
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
        {
            if (CanRunDialogueCoroutine())
                CloseDialogue();
            else
                CloseDialogueImmediate();
        }

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
        if (npc == null || !CanRunDialogueCoroutine()) return;

        // ── 检测是否为特殊 NPC ──
        var special = npc as SpecialNPCController;
        if (special != null)
        {
            OpenSpecialDialogue(special);
            return;
        }

        // ── 普通 NPC 单条对话（原有逻辑不变）──
        if (npc.Info == null) return;

        dialogueNPC = npc;
        SetDialogueNpcMovementPaused(true);

        // 隐藏气泡（对话中不需要气泡）
        npc.HideBubble();

        // 每次对话开始前先重置 UI，避免上一位 NPC 的内容短暂残留。
        ResetDialogueContent();

        // 设置 UI 内容
        SetPortrait(npc.Info.Sprite);
        SetName(npc.Info.Username);
        PrepareMessage(npc.Info.Message);

        // 锁定角色移动，但不触发暂停菜单
        if (GameManager.Instance != null)
            GameManager.Instance.SetDialogueLock(true);

        // 淡入面板
        StartCoroutine(ShowDialoguePanel(npc.Info.Message));
    }

    // ==================== 特殊 NPC 多轮对话 ====================

    private void OpenSpecialDialogue(SpecialNPCController special)
    {
        if (!special.CanStartDialogue()) return;

        specialDialogueNPC = special;
        dialogueNPC = special; // 基类引用也设置，用于走开时强制关闭
        SetDialogueNpcMovementPaused(true);

        special.HideBubble();
        special.StartDialogue();

        ResetDialogueContent();

        // 使用特殊 NPC 的立绘和名字
        SetPortrait(special.GetPortrait());
        SetName(special.GetName());

        string firstLine = special.GetCurrentLine();
        PrepareMessage(firstLine);

        if (GameManager.Instance != null)
            GameManager.Instance.SetDialogueLock(true);

        StartCoroutine(ShowDialoguePanel(firstLine));
    }

    /// <summary>
    /// 按 E 推进特殊 NPC 对话：有下一行 → 显示下一行；没有 → 关闭对话
    /// </summary>
    private void AdvanceSpecialDialogue()
    {
        if (specialDialogueNPC == null)
        {
            CloseDialogue();
            return;
        }

        if (specialDialogueNPC.AdvanceDialogue())
        {
            // 还有下一行 → 打字机显示
            string nextLine = specialDialogueNPC.GetCurrentLine();
            PrepareMessage(nextLine);

            state = DialogueState.Typing;
            typewriterCoroutine = StartCoroutine(TypewriterRoutine(nextLine));
        }
        else
        {
            // 对话结束
            CloseDialogue();
        }
    }

    // ==================== 对话面板控制 ====================

    private IEnumerator ShowDialoguePanel(string message)
    {
        // 显示NPCPanel（仅对特殊NPC）
        string npcName = dialogueNPC.Info != null ? dialogueNPC.Info.Username : (specialDialogueNPC != null ? specialDialogueNPC.GetName() : dialogueNPC.gameObject.name);
        if (MedalManager.Instance != null && MedalManager.Instance.data != null && MedalManager.Instance.data.IsSpecialNPC(npcName))
        {
            string panelText = MedalManager.Instance.GetPanelText(npcName);
            if (!string.IsNullOrEmpty(panelText) && MedalManager.Instance.npcPanelComponent != null)
            {
                StartCoroutine(MedalManager.Instance.npcPanelComponent.ShowPanelWithFade(panelText));
            }
        }

        // 淡入对话面板
        yield return StartCoroutine(UIAnimationHelper.FadeIn(dialogueCanvasGroup, fadeDuration));

        // 开始打字机
        state = DialogueState.Typing;
        typewriterCoroutine = StartCoroutine(TypewriterRoutine(message));
    }

    public void CloseDialogue()
    {
        SetDialogueNpcMovementPaused(false);

        // 停止打字机
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        // 记录当前NPC
        NPCController tempNPC = dialogueNPC;

        state = DialogueState.Idle;
        specialDialogueNPC = null;
        dialogueNPC = null;

        // 对象或对话框已失活时不能 StartCoroutine，改为立即关闭。
        if (!CanRunDialogueCoroutine())
        {
            CloseDialogueImmediate();
            return;
        }

        // 淡出面板
        StartCoroutine(HideDialoguePanel(tempNPC));
    }

    private void CloseDialogueImmediate()
    {
        SetDialogueNpcMovementPaused(false);

        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }

        // 尝试添加勋章
        if (dialogueNPC != null && dialogueNPC.Info != null && MedalManager.Instance != null)
        {
            MedalManager.Instance.TryAddMedal(dialogueNPC.Info.Username);
        }

        state = DialogueState.Idle;
        dialogueNPC = null;
        specialDialogueNPC = null;

        ResetDialogueUI();

        if (GameManager.Instance != null)
            GameManager.Instance.SetDialogueLock(false);
    }

    private IEnumerator HideDialoguePanel(NPCController tempNPC)
    {
        yield return StartCoroutine(UIAnimationHelper.FadeOut(dialogueCanvasGroup, fadeDuration));

        ResetDialogueUI();

        // 解除对话锁
        if (GameManager.Instance != null)
            GameManager.Instance.SetDialogueLock(false);

        // 尝试添加勋章
        if (tempNPC != null && MedalManager.Instance != null)
        {
            string npcName = tempNPC.Info != null ? tempNPC.Info.Username : tempNPC.gameObject.name;
            Debug.Log("尝试添加勋章 for NPC: " + npcName);
            MedalManager.Instance.TryAddMedal(npcName);
        }
        else
        {
            Debug.Log("无法添加勋章: tempNPC=" + (tempNPC != null) + ", MedalManager.Instance=" + (MedalManager.Instance != null));
        }
    }

    // ==================== 打字机效果 ====================

    /// <summary>
    /// 逐字显示 message。使用 TMP 的 maxVisibleCharacters。
    /// 即使未来切到暂停模式也能工作，因此统一用 Time.unscaledDeltaTime 驱动。
    /// </summary>
    private IEnumerator TypewriterRoutine(string message)
    {
        PrepareMessage(message);

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

    private void PrepareMessage(string message)
    {
        if (npcMessageText == null) return;

        npcMessageText.text = message ?? string.Empty;
        npcMessageText.maxVisibleCharacters = 0;
        npcMessageText.ForceMeshUpdate();
    }

    private void ResetDialogueContent()
    {
        if (npcPortraitImage != null)
        {
            npcPortraitImage.sprite = null;
            npcPortraitImage.gameObject.SetActive(false);
        }

        if (npcNameText != null)
            npcNameText.text = string.Empty;

        if (npcMessageText != null)
        {
            npcMessageText.text = string.Empty;
            npcMessageText.maxVisibleCharacters = 0;
        }
    }

    private void ResetDialogueUI()
    {
        ResetDialogueContent();

        if (dialogueCanvasGroup != null)
        {
            dialogueCanvasGroup.alpha = 0f;
            dialogueCanvasGroup.interactable = false;
            dialogueCanvasGroup.blocksRaycasts = false;
            dialogueCanvasGroup.gameObject.SetActive(false);
        }
    }

    private bool CanRunDialogueCoroutine()
    {
        // 只检查 Player 自身是否活跃，不检查对话框——对话框平时是 SetActive(false) 的，
        // 由 FadeIn 负责在协程里激活，不能把它列为"能否启动协程"的前提条件。
        return isActiveAndEnabled && gameObject.activeInHierarchy && dialogueCanvasGroup != null;
    }

    private void SetDialogueNpcMovementPaused(bool paused)
    {
        if (dialogueNPC == null) return;

        var walk = dialogueNPC.GetComponent<NPCWalk>();
        if (walk != null)
            walk.SetDialoguePaused(paused);
    }
}
