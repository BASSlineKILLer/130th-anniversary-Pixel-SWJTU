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

    public UnityEvent onMedalPanelHidden; // 勋章面板隐藏时的事件

    // 数据
    private int totalMedal = 0;
    private HashSet<string> talkedNPCs = new HashSet<string>(); // 已对话的NPC记录

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


    /// <summary>
    /// 尝试添加勋章（在对话结束时调用）
    /// </summary>
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
        totalMedal++;
        UpdateMedalUI();


        // 显示MedalPanel
        if (medalPanelComponent != null)
        {
            medalPanelComponent.SetMedalCount(totalMedal);
            Debug.Log("Starting ShowPanelWithFade for MedalPanel");
            StartCoroutine(medalPanelComponent.ShowPanelWithFade("交大勋章 +1！"));
        }
        else
        {
            Debug.Log("medalPanelComponent is null");
        }

        Debug.Log($"获得勋章！当前总数：{totalMedal} (NPC: {npcUniqueID})");
        return true;
    }

    /// <summary>
    /// 更新UI显示
    /// </summary>
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
    
    /// <summary>
    /// 获取当前勋章总数（供其他系统使用）
    /// </summary>
    public int GetMedalCount()
    {
        return totalMedal;
    }
    
    /// <summary>
    /// 检查某个NPC是否已经对话过
    /// </summary>
    public bool HasTalkedToNPC(string npcUniqueID)
    {
        return talkedNPCs.Contains(npcUniqueID);
    }

    private void MedalPanelHidden()
    {
        Debug.Log("MedalPanelHidden called, invoking onMedalPanelHidden");
        onMedalPanelHidden?.Invoke();
    }
}
