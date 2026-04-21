using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 搜索功能锁的对接 API。
/// 任务系统 / 剧情系统完成特定条件后，调用 <see cref="UnlockByQuest"/> 即可解锁搜索功能。
/// 解锁条件为 OR 关系：勋章数达标 或 任务已解锁 任一满足即解锁。
/// </summary>
public static class SearchLockState
{
    /// <summary>任务系统解锁开关。默认 false，解锁后在同一局游戏内保持 true。</summary>
    public static bool QuestUnlocked { get; private set; }

    /// <summary>任务完成时调用：永久解锁（本局游戏内）。</summary>
    public static void UnlockByQuest()
    {
        if (QuestUnlocked) return;
        QuestUnlocked = true;
        UnityEngine.Debug.Log("[SearchLockState] 搜索功能已由任务系统解锁");
    }

    /// <summary>新游戏 / 存档切换时重置。</summary>
    public static void ResetForNewGame()
    {
        QuestUnlocked = false;
    }
}

public class NpcSearch : MonoBehaviour
{
    [Header("UI 组件")]
    [Tooltip("检索输入框")]
    public TMP_InputField searchInput;

    [Tooltip("检索面板")]
    public GameObject searchPanel;

    [Tooltip("错误面板（未找到NPC时显示）")]
    public GameObject errorPanel;

    [Tooltip("错误文本")]
    public TextMeshProUGUI errorText;

    [Header("功能锁（AND 关系：勋章 + 剧情，两者都满足才解锁）")]
    [Tooltip("解锁搜索功能所需的勋章数量")]
    [Min(0)] public int requiredMedalCount = 5;

    [Tooltip("勋章不足时显示的提示，{0} 会被替换为所需勋章数")]
    [TextArea] public string medalLockedHint = "需要收集 {0} 枚勋章才能解锁搜索功能";

    [Tooltip("关键剧情未触发时显示的提示")]
    [TextArea] public string storyLockedHint = "完成关键剧情后才能解锁搜索功能";

    private bool isInRange = false;
    private bool isSearching = false;
    private Transform playerTransform;

    void Start()
    {
        playerTransform = GameObject.FindWithTag("Player")?.transform;
        if (playerTransform == null)
        {
            Debug.LogError("未找到 Player 对象，请确保 Player 有 'Player' 标签");
        }

        AutoFindReferences();

        if (searchPanel != null) searchPanel.SetActive(false);
        if (errorPanel != null) errorPanel.SetActive(false);

        if (searchInput != null)
            searchInput.onEndEdit.AddListener(OnSearch);
    }

    /// <summary>
    /// 引用为空时自动从 SearchCanvas 中查找子面板
    /// </summary>
    private void AutoFindReferences()
    {
        if (searchPanel != null && errorPanel != null && searchInput != null)
            return;

        var canvas = GameObject.Find("SearchCanvas");
        if (canvas == null) return;

        if (searchPanel == null)
            searchPanel = canvas.transform.Find("SearchPanel")?.gameObject;
        if (errorPanel == null)
            errorPanel = canvas.transform.Find("ErrorPanel")?.gameObject;
        if (searchInput == null)
            searchInput = canvas.GetComponentInChildren<TMP_InputField>(true);
        if (errorText == null && errorPanel != null)
            errorText = errorPanel.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isInRange = true;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isInRange = false;
            // 如果正在搜索，不关闭面板
            if (!isSearching && searchPanel != null)
            {
                searchPanel.SetActive(false);
            }
        }
    }

    void Update()
    {
        if (isSearching && Input.GetKeyDown(KeyCode.Space))
        {
            // 如果正在搜索，按 Space 关闭面板
            ClosePanel();
        }
        else if (isInRange && Input.GetKeyDown(KeyCode.Space))
        {
            // 功能锁：勋章和剧情必须都满足，否则按优先级给对应提示
            string lockedReason = GetLockedReason();
            if (lockedReason != null)
            {
                ShowHint(lockedReason);
                return;
            }

            searchPanel.SetActive(true);
            searchInput.ActivateInputField();
            isSearching = true;
            GameManager.Instance?.SetDialogueLock(true);
        }
    }

    /// <summary>
    /// 返回未解锁的原因提示；已解锁返回 null。
    /// 优先级：勋章不足 > 剧情未触发。
    /// </summary>
    private string GetLockedReason()
    {
        if (!IsMedalUnlocked())
            return string.Format(medalLockedHint, requiredMedalCount);

        if (!SearchLockState.QuestUnlocked)
            return storyLockedHint;

        return null;
    }

    private bool IsMedalUnlocked()
    {
        if (requiredMedalCount <= 0) return true;
        if (MedalManager.Instance == null) return false;
        return MedalManager.Instance.GetMedalCount() >= requiredMedalCount;
    }

    private void ShowHint(string message)
    {
        if (errorPanel == null || errorText == null) return;

        errorPanel.SetActive(true);
        var cg = errorPanel.GetComponent<CanvasGroup>();
        if (cg != null) cg.alpha = 1f;
        errorText.text = message;
        StartCoroutine(HideErrorPanelAfterDelay(1.2f));
    }

    public void OnSearch(string inputText)
    {
        string name = inputText.Trim();
        if (string.IsNullOrEmpty(name)) return;

        // 1. 优先查本场景已生成的 NPC（无需跨场景传送）
        GameObject localNpc = FindNPCByName(name);
        if (localNpc != null)
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TeleportPlayer(localNpc.transform.position);
            else
                playerTransform.position = localNpc.transform.position;

            Debug.Log($"[NpcSearch] 同场景传送到 NPC: {name}");
            ClosePanel();
            return;
        }

        // 2. 本场景没有 → 查全局分配，跨场景传送
        if (NPCDistributor.Instance != null)
        {
            string targetScene = NPCDistributor.Instance.FindNPCSceneByName(name);
            if (!string.IsNullOrEmpty(targetScene) && SceneTransitionManager.Instance != null)
            {
                ClosePanel();
                SceneTransitionManager.Instance.TransitionToSceneAndFindNPC(targetScene, name);
                Debug.Log($"[NpcSearch] 跨场景传送到 '{targetScene}' 的 NPC: {name}");
                return;
            }
        }

        // 3. 全局也没有 → 提示未找到
        ShowHint($"未找到 NPC: {name}");
        searchInput.text = "";
        searchInput.ActivateInputField();
    }

    private void ClosePanel()
    {
        if (searchPanel != null) searchPanel.SetActive(false);
        if (errorPanel != null) errorPanel.SetActive(false);
        isSearching = false;
        GameManager.Instance?.SetDialogueLock(false);
    }

    private GameObject FindNPCByName(string name)
    {
        var npcs = FindObjectsOfType<NPCController>();
        foreach (var npc in npcs)
        {
            if (npc.Info != null && npc.Info.Username == name)
            {
                return npc.gameObject;
            }
        }
        return null;
    }

    private IEnumerator HideErrorPanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        var errorCanvasGroup = errorPanel.GetComponent<CanvasGroup>();
        if (errorCanvasGroup != null)
        {
            // 开始淡出
            while (errorCanvasGroup.alpha > 0)
            {
                errorCanvasGroup.alpha -= Time.deltaTime / delay;
                yield return null;
            }
        }
        errorPanel.SetActive(false);
    }
}
