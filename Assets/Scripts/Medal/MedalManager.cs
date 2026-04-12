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

    public UnityEvent onMedalPanelHidden; // 勋章面板隐藏时的事件

    public SpecialNPCData data; // 特殊NPC数据

    [Header("出生点设置")] public Transform spawnPoint; // 出生点位置

    // 数据
    private int totalMedal = 0;
    private HashSet<string> talkedNPCs = new HashSet<string>(); // 已对话的NPC记录
    private List<string> talkedSpecialNPCs = new List<string>(); // 已对话的特殊NPC顺序记录
    
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
    /// 重置所有内存数据，用于开始新游戏时清空状态。
    /// （DontDestroyOnLoad 的单例不会被销毁，需手动重置）
    /// </summary>
    public void ResetForNewGame()
    {
        totalMedal = 0;
        talkedNPCs.Clear();
        talkedSpecialNPCs.Clear();
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
        Debug.Log($"[MedalManager] 从存档恢复: 勋章={totalMedal}, 已对话NPC={talkedNPCs.Count}, 图书馆={libraryUnlocked}");
    }
}
