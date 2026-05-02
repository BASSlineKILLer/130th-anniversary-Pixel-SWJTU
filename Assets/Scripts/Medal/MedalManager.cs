using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // Add this for HashSet
using UnityEngine.Events;

public class MedalManager : MonoBehaviour
{
    public static MedalManager Instance { get; private set; }

    [Header("UI组件")] public TextMeshProUGUI medalText; // 拖入显示数字的Text组件
    public MedalPanel medalPanelComponent; // MedalPanel组件
    public NPCPanel npcPanelComponent; // NPCPanel组件

    [Header("节点解锁通知")]
    [Tooltip("勋章进度配置：节点阈值、描述、解锁回调都在里面")]
    public MedalProgressConfig progressConfig;
    [Tooltip("节点解锁弹窗（文本 = \"{unlockDescription}已解锁\"）")]
    public SceneUnlockPanel sceneUnlockPanel;

    public UnityEvent onMedalPanelHidden; // 勋章面板隐藏时的事件
    public UnityEvent onMedalCountChanged; // 勋章数量变化时的事件
    public UnityEvent onMedalAddedForNPC; // 添加勋章时的事件

    public SpecialNPCData data; // 特殊NPC数据

    [Header("出生点设置")] public Transform spawnPoint; // 出生点位置

    // 数据
    private int totalMedal = 0;
    private HashSet<string> talkedNPCs = new HashSet<string>(); // 已对话的NPC记录
    private List<string> talkedSpecialNPCs = new List<string>(); // 已对话的特殊NPC顺序记录
    private readonly HashSet<int> reachedThresholds = new HashSet<int>(); // 已解锁过的节点阈值（避免重复触发 onUnlock）
    
    // 状态存档：记录图书馆是否已开启，防止过场动画和弹窗重复触发
    public bool IsLibraryUnlocked { get; set; } = false;

    private void Awake()
    {
        // 单例模式，确保全局只有一个实例
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("MedalManager Instance created");
            
            // 自动加载 progressConfig
            if (progressConfig == null)
            {
                progressConfig = Resources.Load<MedalProgressConfig>("NPCData/MedalProgressConfig");
                if (progressConfig == null)
                {
                    Debug.LogWarning("[MedalManager] 未能在 Resources/NPCData 目录下找到 MedalProgressConfig，请在 Inspector 中手动分配。");
                }
            }
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        UpdateMedalUI();
        if (medalPanelComponent != null)
        {
            medalPanelComponent.gameObject.SetActive(false); // 一开始隐藏MedalPanel
            medalPanelComponent.onPanelHidden.AddListener(MedalPanelHidden);
        }
        
        // 尝试自动寻找场景中的 SceneUnlockPanel
        if (sceneUnlockPanel == null)
        {
            sceneUnlockPanel = FindObjectOfType<SceneUnlockPanel>(true);
            if (sceneUnlockPanel != null)
            {
                Debug.Log("[MedalManager] 自动找到并绑定了 SceneUnlockPanel");
            }
            else 
            {
                Debug.LogWarning("[MedalManager] 未能自动找到 SceneUnlockPanel，请确保场景中存在挂载该脚本的面板");
            }
        }
        
        SyncReachedThresholds(); // 启动时将已达阈值全部标记为“已触发过”，避免重启/读档后重播弹窗
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            TeleportToSpawn();
        }
    }

    private void TeleportToSpawn()
    {
        if (spawnPoint != null)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
            {
                player.transform.position = spawnPoint.position;
                Debug.Log("Player teleported to spawn point");
            }
            else
            {
                Debug.LogError("Player not found");
            }
        }
        else
        {
            Debug.LogError("Spawn point not set");
        }
    }

    /// 尝试添加勋章（在对话结束时调用）
    /// <param name="npcUniqueID">NPC的唯一标识，如"npc_01"</param>
    /// <returns>是否成功获得勋章</returns>
    public bool TryAddMedal(string npcUniqueID)
    {
         Debug.Log("TryAddMedal called for NPC: " + npcUniqueID);

        // 已经对话过这个NPC了
        if (talkedNPCs.Contains(npcUniqueID))
        {
            Debug.Log($"NPC {npcUniqueID} 已经对话过了，不再给勋章");
            return false;
        }

        // 首次对话，添加勋章
        talkedNPCs.Add(npcUniqueID);
        talkedSpecialNPCs.Add(npcUniqueID);
        totalMedal++;
        UpdateMedalUI();
        onMedalCountChanged?.Invoke();

        // 显示MedalPanel
        string message = "交大勋章 +1！";
        if (medalPanelComponent != null)
        {
            medalPanelComponent.SetMedalCount(totalMedal);
            Debug.Log("Starting ShowPanelWithFade for MedalPanel");
            StartCoroutine(medalPanelComponent.ShowPanelWithFade(message));
        }
        else
        {
            Debug.Log("medalPanelComponent is null");
        }

        Debug.Log($"获得勋章！当前总数：{totalMedal} (NPC: {npcUniqueID})");

        // 触发添加勋章的事件
        onMedalAddedForNPC?.Invoke();

        return true;
    }

    /// 更新UI显示
    private void UpdateMedalUI()
    {
        if (medalText != null)
        {
            medalText.text = totalMedal.ToString();
        }
    }

    private void OnMedalGained()
    {
        if (this != null && gameObject.activeInHierarchy)
        {
            StartCoroutine(MedalTextBlink());
        }
    }

    private System.Collections.IEnumerator MedalTextBlink()
    {
        if (medalText == null) yield break;
        
        Color originalColor = medalText.color;
        for (int i = 0; i < 3; i++)
        {
            medalText.color = Color.yellow;
            yield return new WaitForSeconds(0.1f);
            medalText.color = originalColor;
            yield return new WaitForSeconds(0.1f);
        }
    }

    /// 获取当前勋章总数（供其他系统使用）
    public int GetMedalCount()
    {
        return totalMedal;
    }

    /// 检查某个NPC是否已经对话过
    public bool HasTalkedToNPC(string npcUniqueID)
    {
        return talkedNPCs.Contains(npcUniqueID);
    }

    private void MedalPanelHidden()
    {
        Debug.Log("MedalPanelHidden called, invoking onMedalPanelHidden");
        onMedalPanelHidden?.Invoke();
        // MedalPanel 弹窗隐藏后才检查节点解锁，避免两个弹窗同时出现
        CheckNodeUnlocks();
    }

    /// <summary>
    /// 起始化/读档后调用：将当前 totalMedal 已达到的阈值都标记为“已触发”，
    /// 以免重启/读档时重复弹窗。仅报同步状态，不调用 onUnlock。
    /// </summary>
    private void SyncReachedThresholds()
    {
        reachedThresholds.Clear();
        if (progressConfig == null || progressConfig.nodes == null) return;
        foreach (var node in progressConfig.nodes)
        {
            if (node != null && totalMedal >= node.threshold)
                reachedThresholds.Add(node.threshold);
        }
    }

    /// <summary>
    /// 检查是否有节点被本次 +1 跨过：是则弹“{desc}已解锁”。
    /// 业务副作用由各模块主动查询 <see cref="IsNodeUnlocked"/>，不再集中 Invoke。
    /// </summary>
    private void CheckNodeUnlocks()
    {
        if (progressConfig == null || progressConfig.nodes == null) return;
        foreach (var node in progressConfig.nodes)
        {
            if (node == null) continue;
            if (totalMedal < node.threshold) continue;
            if (reachedThresholds.Contains(node.threshold)) continue;

            reachedThresholds.Add(node.threshold);
            Debug.Log($"[MedalManager] 节点解锁：id={node.nodeId}, threshold={node.threshold}, desc={node.unlockDescription}");

            if (sceneUnlockPanel != null && !string.IsNullOrEmpty(node.unlockDescription))
            {
                string msg = $"{node.unlockDescription}已解锁";
                sceneUnlockPanel.Show(msg);
            }
        }
    }

    // ══════════════════════════════════════════════════════════
    //  节点解锁查询 API（业务方门禁使用）
    // ══════════════════════════════════════════════════════════

    /// <summary>按 nodeId 查询节点配置。未配置返回 null。</summary>
    public MedalProgressConfig.Node GetNode(string nodeId)
    {
        if (progressConfig == null)
        {
            Debug.LogError($"[MedalManager] progressConfig 丢失！请在 Inspector 中为 MedalManager 赋值 MedalProgressConfig 资产！(查询节点: {nodeId})");
            return null;
        }
        return progressConfig.FindNode(nodeId);
    }

    /// <summary>
    /// 判断某能力节点是否已解锁（当前勋章数是否达到 threshold）。
    /// 节点不存在视为未解锁（安全失败）。
    /// </summary>
    public bool IsNodeUnlocked(string nodeId)
    {
        var node = GetNode(nodeId);
        if (node == null) return false;
        return totalMedal >= node.threshold;
    }

    /// <summary>
    /// 未达阈值时统一弹出提示（复用 SceneUnlockPanel）。
    /// 提示文本来自 config 的 lockedHint，支持 {0} 占位为 threshold。
    /// </summary>
    public void ShowLockedHint(string nodeId)
    {
        var node = GetNode(nodeId);
        if (node == null)
        {
            Debug.LogWarning($"[MedalManager] ShowLockedHint 找不到节点 id={nodeId}，或者 progressConfig 未配置");
            return;
        }
        if (sceneUnlockPanel == null)
        {
            // Try to find it one more time dynamically when needed
            sceneUnlockPanel = FindObjectOfType<SceneUnlockPanel>(true);
            if (sceneUnlockPanel == null)
            {
                Debug.LogError("[MedalManager] sceneUnlockPanel 丢失！请在场景中确保存在 SceneUnlockPanel 组件！");
                return;
            }
        }
        string hint = string.IsNullOrEmpty(node.lockedHint)
            ? $"需要 {node.threshold} 枚勋章才能解锁"
            : string.Format(node.lockedHint, node.threshold);
            
        sceneUnlockPanel.Show(hint);
    }

    /// 获取已对话的特殊NPC顺序列表
    public List<string> GetTalkedSpecialNPCs()
    {
        return talkedSpecialNPCs;
    }

    /// 获取NPC的面板文字
    public string GetPanelText(string npcName)
    {
        if (data != null)
        {
            return data.GetPanelText(npcName);
        }
        return "";
    }

    /// <summary>
    /// [Debug 专用] 把传入的 NPC 名字全部塞进 talkedSpecialNPCs，绕过对话流程。
    /// 用于 MedalDebugHotkeys 的 F2 一键解锁所有特殊 NPC 故事。
    /// 不增加勋章数，仅影响 TaskPanel 的解锁态。
    /// </summary>
    public void DebugUnlockAllSpecial(IEnumerable<string> npcNames)
    {
        if (npcNames == null) return;
        foreach (var name in npcNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            if (talkedSpecialNPCs.Contains(name)) continue;
            talkedSpecialNPCs.Add(name);
        }
        Debug.Log($"[MedalManager][DEBUG] 已解锁特殊 NPC，当前数量：{talkedSpecialNPCs.Count}");
    }

    /// <summary>
    /// 重置所有内存数据，用于开始新游戏时清空状态。
    /// （DontDestroyOnLoad 的单例不会被销毁，需手动重置）
    /// </summary>
    public void ResetForNewGame()
    {
        totalMedal = 0;
        talkedNPCs.Clear();
        talkedSpecialNPCs.Clear();
        reachedThresholds.Clear();
        IsLibraryUnlocked = false;
        UpdateMedalUI();
        Debug.Log("[MedalManager] 状态已重置（新游戏）");
    }

    /// <summary>
    /// 获取已对话的所有NPC列表（供 SaveManager 序列化用）
    /// </summary>
    public HashSet<string> GetTalkedNPCs()
    {
        return talkedNPCs;
    }

    /// <summary>
    /// 从存档恢复勋章进度
    /// </summary>
    public void RestoreFromSave(int medals, List<string> talked, List<string> talkedSpecial, bool libraryUnlocked)
    {
        totalMedal = medals;
        talkedNPCs.Clear();
        if (talked != null)
            foreach (var id in talked)
                talkedNPCs.Add(id);

        talkedSpecialNPCs.Clear();
        if (talkedSpecial != null)
            talkedSpecialNPCs.AddRange(talkedSpecial);

        IsLibraryUnlocked = libraryUnlocked;
        UpdateMedalUI();
        onMedalCountChanged?.Invoke();
        SyncReachedThresholds(); // 读档后同步节点解锁状态，避免重复弹窗
        Debug.Log($"[MedalManager] 从存档恢复: 勋章={totalMedal}, 已对话 NPC={talkedNPCs.Count}, 图书馆={libraryUnlocked}");
    }
}
