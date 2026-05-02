using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

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

    [Header("场景传送列表（可选）")]
    [Tooltip("场景传送面板；由独立的 SceneTeleportList 脚本管理。打开入口由 OpenTeleportList 提供")]
    public SceneTeleportList teleportListPanel;

    [Header("功能锁节点 ID（对应 MedalProgressConfig.nodes[i].nodeId）")]
    [Tooltip("解锁搜索功能的节点 ID")]
    public string searchNodeId = "NpcSearch";

    [Tooltip("解锁场景传送列表的节点 ID")]
    public string teleportListNodeId = "TeleportList";

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
        if (teleportListPanel != null) teleportListPanel.gameObject.SetActive(false);

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
            
        if (teleportListPanel == null)
        {
            teleportListPanel = canvas.GetComponentInChildren<SceneTeleportList>(true);
        }
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
            if (MedalManager.Instance != null)
            {
                // 先检查较小阈值的传送功能是否解锁
                if (!MedalManager.Instance.IsNodeUnlocked(teleportListNodeId))
                {
                    MedalManager.Instance.ShowLockedHint(teleportListNodeId);
                    return;
                }
                
                // 再检查较高阈值的搜索功能是否解锁
                if (!MedalManager.Instance.IsNodeUnlocked(searchNodeId))
                {
                    MedalManager.Instance.ShowLockedHint(searchNodeId);
                    return;
                }
            }

            searchPanel.SetActive(true);
            searchInput.ActivateInputField();
            isSearching = true;
            GameManager.Instance?.SetDialogueLock(true);
        }
    }

    /// <summary>
    /// 打开场景传送列表面板（供 UI 按钮调用）
    /// </summary>
    public void OpenTeleportList()
    {
        if (MedalManager.Instance != null && !MedalManager.Instance.IsNodeUnlocked(teleportListNodeId))
        {
            MedalManager.Instance.ShowLockedHint(teleportListNodeId);
            return;
        }

        if (teleportListPanel == null)
        {
            // 尝试再次查找
            var canvas = GameObject.Find("SearchCanvas");
            if (canvas != null)
            {
                teleportListPanel = canvas.GetComponentInChildren<SceneTeleportList>(true);
            }
        }

        if (teleportListPanel != null)
        {
            teleportListPanel.gameObject.SetActive(true);
        }
        else
        {
            Debug.LogError("[NpcSearch] TeleportListPanel 未找到，无法打开传送列表。");
        }
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
