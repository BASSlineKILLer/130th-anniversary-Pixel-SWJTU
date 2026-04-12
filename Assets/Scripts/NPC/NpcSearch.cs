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

        searchPanel.SetActive(false);
        errorPanel.SetActive(false);

        // 添加输入结束监听，按 Enter 键触发搜索
        searchInput.onEndEdit.AddListener(OnSearch);
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
            // 如果在范围内且未搜索，按 Space 显示面板
            searchPanel.SetActive(true);
            searchInput.ActivateInputField();
            isSearching = true;
            GameManager.Instance?.SetDialogueLock(true);
        }
    }

    public void OnSearch(string inputText)
    {
        string name = inputText.Trim();
        if (string.IsNullOrEmpty(name)) return;

        GameObject npc = FindNPCByName(name);
        if (npc != null)
        {
            // 使用 SceneTransitionManager 进行传送，确保相机跟随和免疫期
            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TeleportPlayer(npc.transform.position);
            }
            else
            {
                playerTransform.position = npc.transform.position;
            }
            Debug.Log("传送到 NPC: " + name);
            ClosePanel();
        }
        else
        {
            Debug.Log("未找到 NPC: " + name);
            // 显示错误面板
            errorPanel.SetActive(true);
            var errorCanvasGroup = errorPanel.GetComponent<CanvasGroup>();
            if (errorCanvasGroup != null)
            {
                errorCanvasGroup.alpha = 1f;
            }
            errorText.text = "未找到 NPC: " + name;

            // 启动协程，0.5秒后淡出
            StartCoroutine(HideErrorPanelAfterDelay(0.7f));

            // 不关闭面板，让用户重新输入
            searchInput.text = "";
            searchInput.ActivateInputField();
        }
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
