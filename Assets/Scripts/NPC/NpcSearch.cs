using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class NpcSearch : MonoBehaviour
{
    private const int MAX_CANDIDATES = 10;

#if UNITY_WEBGL && !UNITY_EDITOR
    [DllImport("__Internal")]
    private static extern void WebGLInputBridge_Show(string objectName, string methodName, string initialText);

    [DllImport("__Internal")]
    private static extern void WebGLInputBridge_Hide();
#endif

    [Header("UI 组件")]
    [Tooltip("检索输入框")]
    public TMP_InputField searchInput;

    [Tooltip("检索面板")]
    public GameObject searchPanel;

    [Tooltip("错误面板（未找到NPC时显示）")]
    public GameObject errorPanel;

    [Tooltip("错误文本")]
    public TextMeshProUGUI errorText;

    [Header("候选卡片列表")]
    [Tooltip("候选卡片容器（建议挂在搜索面板右侧的 Content 上）")]
    public Transform candidateContainer;

    [Tooltip("NPC 卡片预制体；需包含 PortraitImage(Image) 和 NameText(TMP) 两个子物体；自身为 Button")]
    public GameObject npcCardPrefab;

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
    private readonly List<GameObject> candidateCardInstances = new List<GameObject>();

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
        {
            searchInput.onValueChanged.AddListener(OnSearchInputChanged);
        }

        ClearCandidateCards();
    }

    /// <summary>
    /// 引用为空时自动从 SearchCanvas 中查找子面板。
    /// 优先读取 SearchCanvasRefs 聚合脚本（最可靠），找不到再降级到名字查找。
    /// </summary>
    private void AutoFindReferences()
    {
        var refs = FindObjectOfType<SearchCanvasRefs>(true);
        if (refs != null)
        {
            if (searchPanel == null) searchPanel = refs.searchPanel;
            if (searchInput == null) searchInput = refs.searchInput;
            if (errorPanel == null) errorPanel = refs.errorPanel;
            if (errorText == null) errorText = refs.errorText;
            if (candidateContainer == null) candidateContainer = refs.candidateContainer;
            if (teleportListPanel == null) teleportListPanel = refs.teleportListPanel;

            if (candidateContainer != null)
                Debug.Log($"[NpcSearch] 从 SearchCanvasRefs 获取 CandidateContainer: {GetFullPath(candidateContainer)}");
        }

        var canvas = GameObject.Find("SearchCanvas");
        if (canvas == null)
        {
            Debug.LogWarning("[NpcSearch] 未找到 SearchCanvas，无法自动绑定搜索 UI");
            return;
        }

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
            if (teleportListPanel == null)
                Debug.LogWarning("[NpcSearch] 未在 SearchCanvas 下找到 SceneTeleportList / TeleportListPanel");
        }

        if (candidateContainer == null)
        {
            candidateContainer = FindDescendantByNamePrefix(canvas.transform, "CandidateContainer");
            if (candidateContainer == null)
                Debug.LogWarning("[NpcSearch] 未在 SearchCanvas 下找到 CandidateContainer，候选卡片无法显示");
            else
                Debug.Log($"[NpcSearch] 自动绑定 CandidateContainer: {GetFullPath(candidateContainer)}");
        }
    }

    private static string GetFullPath(Transform t)
    {
        if (t == null) return "(null)";
        var path = t.name;
        var p = t.parent;
        while (p != null)
        {
            path = p.name + "/" + path;
            p = p.parent;
        }
        return path;
    }

    /// <summary>
    /// 递归在子树中查找名字以 prefix 开头的 Transform（宽松匹配，容忍尾部空格）。
    /// </summary>
    private static Transform FindDescendantByNamePrefix(Transform root, string prefix)
    {
        if (root == null || string.IsNullOrEmpty(prefix)) return null;
        foreach (Transform child in root)
        {
            if (child.name.Trim().StartsWith(prefix, StringComparison.Ordinal))
                return child;
            var nested = FindDescendantByNamePrefix(child, prefix);
            if (nested != null) return nested;
        }
        return null;
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
            if (searchInput != null && searchInput.isFocused) return;
            // 如果正在搜索，按 Space 关闭面板
            ClosePanel();
        }
        else if (isInRange && Input.GetKeyDown(KeyCode.Space))
        {
            if (MedalManager.Instance != null)
            {
                if (!MedalManager.Instance.IsNodeUnlocked(teleportListNodeId))
                {
                    MedalManager.Instance.ShowLockedHint(teleportListNodeId);
                    return;
                }

                if (!MedalManager.Instance.IsNodeUnlocked(searchNodeId))
                {
                    OpenTeleportList();
                    return;
                }
            }

            OpenSearchPanel();
        }
    }

    private void OpenSearchPanel()
    {
        if (searchPanel != null) searchPanel.SetActive(true);
        OpenTeleportList();
        isSearching = true;
        if (searchInput != null)
        {
            searchInput.text = string.Empty;
#if !UNITY_WEBGL || UNITY_EDITOR
            searchInput.ActivateInputField();
#endif
            ShowWebGLInputBridge();
        }
        ClearCandidateCards();
        GameManager.Instance?.SetDialogueLock(true);
    }

    public void OnWebGLSearchInputChanged(string value)
    {
        if (searchInput == null) return;
        searchInput.SetTextWithoutNotify(value);
        OnSearchInputChanged(value);
    }

    private void ShowWebGLInputBridge()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInput.captureAllKeyboardInput = false;
        WebGLInputBridge_Show(gameObject.name, nameof(OnWebGLSearchInputChanged), searchInput != null ? searchInput.text : string.Empty);
#endif
    }

    private static void HideWebGLInputBridge()
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        WebGLInputBridge_Hide();
        WebGLInput.captureAllKeyboardInput = true;
#endif
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
            isSearching = true;
            GameManager.Instance?.SetDialogueLock(true);
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

    private void OnSearchInputChanged(string inputText)
    {
        if (!isSearching) return;
        if (MedalManager.Instance != null && !MedalManager.Instance.IsNodeUnlocked(searchNodeId)) return;
        RefreshCandidateCards(inputText);
    }

    private void RefreshCandidateCards(string query)
    {
        ClearCandidateCards();
        if (candidateContainer == null || npcCardPrefab == null) return;

        string trimmed = query == null ? string.Empty : query.Trim();
        if (trimmed.Length == 0) return;

        var matches = CollectFuzzyMatches(trimmed, MAX_CANDIDATES);
        foreach (var pair in matches)
        {
            CreateCandidateCard(pair.Key, pair.Value);
        }
    }

    private List<KeyValuePair<NPCInfo, string>> CollectFuzzyMatches(string query, int maxCount)
    {
        var results = new List<KeyValuePair<NPCInfo, string>>();
        var seenKeys = new HashSet<string>();

        CollectAssignedMatches(query, maxCount, results, seenKeys);
        if (results.Count < maxCount)
            CollectSceneMatches(query, maxCount, results, seenKeys);
        return results;
    }

    private void CollectAssignedMatches(string query, int maxCount, List<KeyValuePair<NPCInfo, string>> results, HashSet<string> seenKeys)
    {
        if (NPCDistributor.Instance == null || !NPCDistributor.Instance.IsReady) return;

        foreach (var pair in NPCDistributor.Instance.GetAllAssignedNPCs())
        {
            var npc = pair.Key;
            if (!CanAddMatch(npc, query, seenKeys)) continue;

            results.Add(pair);
            if (results.Count >= maxCount) break;
        }
    }

    private void CollectSceneMatches(string query, int maxCount, List<KeyValuePair<NPCInfo, string>> results, HashSet<string> seenKeys)
    {
        string sceneName = SceneManager.GetActiveScene().name;
        foreach (var controller in FindObjectsOfType<NPCController>())
        {
            var npc = controller.Info;
            if (!CanAddMatch(npc, query, seenKeys)) continue;

            results.Add(new KeyValuePair<NPCInfo, string>(npc, sceneName));
            if (results.Count >= maxCount) break;
        }
    }

    private static bool CanAddMatch(NPCInfo npc, string query, HashSet<string> seenKeys)
    {
        if (npc == null || string.IsNullOrEmpty(npc.Username)) return false;
        if (!IsFuzzyMatch(npc.Username, query)) return false;
        return seenKeys.Add(GetNpcMatchKey(npc));
    }

    private static string GetNpcMatchKey(NPCInfo npc)
    {
        if (npc.Id != 0) return npc.Id.ToString();
        return npc.Username;
    }

    private static bool IsFuzzyMatch(string source, string query)
    {
        return source.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void CreateCandidateCard(NPCInfo npc, string sceneName)
    {
        var card = Instantiate(npcCardPrefab, candidateContainer);
        candidateCardInstances.Add(card);

        var portrait = FindCardPortrait(card.transform);
        if (portrait != null && npc.Sprite != null)
        {
            portrait.sprite = npc.Sprite;
            portrait.preserveAspect = true;
        }

        var nameText = FindCardNameText(card.transform);
        if (nameText != null) nameText.text = npc.Username;

        var btn = card.GetComponent<Button>();
        if (btn != null)
        {
            var captured = npc;
            var capturedScene = sceneName;
            btn.onClick.AddListener(() => TeleportToNPC(captured, capturedScene));
        }
    }

    /// <summary>
    /// 找卡片头像：优先按名称前缀“Portrait”，其次取第一个非根节点的 Image。
    /// </summary>
    private static Image FindCardPortrait(Transform card)
    {
        var byName = FindDescendantByNamePrefix(card, "Portrait");
        if (byName != null)
        {
            var img = byName.GetComponent<Image>();
            if (img != null) return img;
        }
        foreach (var img in card.GetComponentsInChildren<Image>(true))
        {
            if (img.transform != card) return img;
        }
        return null;
    }

    /// <summary>
    /// 找卡片名字：优先按名称前缀“Name”，其次取第一个 TMP。
    /// </summary>
    private static TextMeshProUGUI FindCardNameText(Transform card)
    {
        var byName = FindDescendantByNamePrefix(card, "Name");
        if (byName != null)
        {
            var tmp = byName.GetComponent<TextMeshProUGUI>();
            if (tmp != null) return tmp;
        }
        return card.GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void ClearCandidateCards()
    {
        foreach (var card in candidateCardInstances)
        {
            if (card != null) Destroy(card);
        }
        candidateCardInstances.Clear();
    }

    private void TeleportToNPC(NPCInfo npc, string targetScene)
    {
        if (npc == null) return;

        var localNpc = FindNPCByName(npc.Username);
        if (localNpc != null)
        {
            if (SceneTransitionManager.Instance != null)
                SceneTransitionManager.Instance.TeleportPlayer(localNpc.transform.position);
            else if (playerTransform != null)
                playerTransform.position = localNpc.transform.position;

            Debug.Log($"[NpcSearch] 同场景传送到 NPC: {npc.Username}");
            ClosePanel();
            return;
        }

        if (!string.IsNullOrEmpty(targetScene) && SceneTransitionManager.Instance != null)
        {
            ClosePanel();
            SceneTransitionManager.Instance.TransitionToSceneAndFindNPC(targetScene, npc.Username);
            Debug.Log($"[NpcSearch] 跨场景传送到 '{targetScene}' 的 NPC: {npc.Username}");
            return;
        }

        ShowHint($"无法定位 NPC: {npc.Username}");
    }

    private void ClosePanel()
    {
        if (searchPanel != null) searchPanel.SetActive(false);
        if (errorPanel != null) errorPanel.SetActive(false);
        if (teleportListPanel != null) teleportListPanel.gameObject.SetActive(false);
        HideWebGLInputBridge();
        ClearCandidateCards();
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
