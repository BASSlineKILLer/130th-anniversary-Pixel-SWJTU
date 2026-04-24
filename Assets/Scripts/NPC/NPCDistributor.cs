using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 跨场景分配器（单例 + DontDestroyOnLoad）。
/// 在 MainMenu 初始化，一次性加载全部 NPC 数据（手动 + API），
/// 洗牌后按场景名均匀分配。各场景的 NPCManager 通过 GetNPCsForScene 获取自己的子集，
/// 再根据场景内配置的点位容量自行装桶。
/// </summary>
public class NPCDistributor : MonoBehaviour
{
    public static NPCDistributor Instance { get; private set; }

    [Header("NPC 数据库")]
    [Tooltip("拖入 NPCDatabase 资产。留空则自动从 Resources/NPCData/NPCDatabase 加载")]
    public NPCDatabase database;

    [Header("场景配置")]
    [Tooltip("需要分配 NPC 的游戏场景名称（可随时增删）。点位容量在各场景的 NPCManager 中配置")]
    public List<string> gameSceneNames = new List<string>();

    /// <summary>所有 NPC 的总数（手动 + API）</summary>
    public int TotalNPCs => allNPCs.Count;

    /// <summary>数据是否已就绪（手动条目加载 + API 至少返回一次）</summary>
    public bool IsReady { get; private set; }

    private readonly List<NPCInfo> allNPCs = new List<NPCInfo>();
    private readonly Dictionary<string, List<NPCInfo>> sceneAssignment
        = new Dictionary<string, List<NPCInfo>>();

    private bool distributed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (database == null)
            database = Resources.Load<NPCDatabase>("NPCData/NPCDatabase");
    }

    private void Start()
    {
        LoadAndDistribute();
    }

    /// <summary>
    /// 加载所有 NPC 数据并分配到各场景。
    /// 先用手动条目 + API 缓存秒加载，后台再拉取最新 API 数据。
    /// </summary>
    public void LoadAndDistribute()
    {
        allNPCs.Clear();
        sceneAssignment.Clear();
        distributed = false;
        IsReady = false;

        if (database != null && database.clearCacheOnStart)
        {
            NPCApiService.ClearCache();
            Debug.Log("[NPCDistributor] 已按 clearCacheOnStart 标记清除本地 NPC 缓存");
        }

        if (database != null)
        {
            allNPCs.AddRange(database.GetManualNPCInfos());
        }

        if (database != null && database.enableApiFetch)
        {
            StartCoroutine(FetchAndMerge());
        }
        else
        {
            EnsureDistributed();
        }
    }

    private IEnumerator FetchAndMerge()
    {
        string url = database != null ? database.apiUrl : "http://devshowcase.site/api/approved";

        yield return NPCApiService.FetchNPCs(
            url,
            onSuccess: apiList =>
            {
                MergeApiNPCs(apiList);
                EnsureDistributed();
                Debug.Log($"[NPCDistributor] 数据刷新完成，共 {allNPCs.Count} 个 NPC");
            },
            onError: error =>
            {
                Debug.LogWarning($"[NPCDistributor] API 获取失败: {error}");
                EnsureDistributed();
            }
        );
    }

    /// <summary>
    /// 确保已完成初次分配；若已分配，则把新加入的 NPC 追加到负载最少的场景，避免重新洗牌。
    /// </summary>
    private void EnsureDistributed()
    {
        if (!distributed)
            Distribute();
        else
            AppendUnassignedNPCs();
        IsReady = true;
    }

    private void AppendUnassignedNPCs()
    {
        if (sceneAssignment.Count == 0) return;

        var assignedIds = new HashSet<int>();
        foreach (var list in sceneAssignment.Values)
            foreach (var npc in list)
                assignedIds.Add(npc.Id);

        foreach (var npc in allNPCs)
        {
            if (assignedIds.Contains(npc.Id)) continue;
            string targetScene = FindLeastLoadedScene();
            if (targetScene != null)
                sceneAssignment[targetScene].Add(npc);
        }
    }

    private string FindLeastLoadedScene()
    {
        string minScene = null;
        int minCount = int.MaxValue;
        foreach (var kvp in sceneAssignment)
        {
            if (kvp.Value.Count < minCount)
            {
                minCount = kvp.Value.Count;
                minScene = kvp.Key;
            }
        }
        return minScene;
    }

    /// <summary>
    /// 合并 API NPC 到全局池（去重：同 ID 覆盖）
    /// </summary>
    private void MergeApiNPCs(List<NPCInfo> apiList)
    {
        var existingIds = new HashSet<int>();
        foreach (var npc in allNPCs)
            existingIds.Add(npc.Id);

        foreach (var npc in apiList)
        {
            if (existingIds.Contains(npc.Id))
            {
                int idx = allNPCs.FindIndex(n => n.Id == npc.Id);
                if (idx >= 0) allNPCs[idx] = npc;
            }
            else
            {
                allNPCs.Add(npc);
                existingIds.Add(npc.Id);
            }
        }
    }

    /// <summary>
    /// 重新洗牌并分配（复用已加载的 NPC 池，不重新拉取 API）。
    /// 新游戏 / 继续游戏时调用，用于让本局拿到不同的随机分布。
    /// 若首次加载尚未完成，本调用被忽略，沉用进行中的加载结果。
    /// </summary>
    public void Redistribute()
    {
        if (!IsReady)
        {
            Debug.Log("[NPCDistributor] Redistribute 跳过：首次加载尚未就绪，沉用已有流程");
            return;
        }
        distributed = false;
        Distribute();
    }

    private void Distribute()
    {
        sceneAssignment.Clear();

        if (gameSceneNames.Count == 0)
        {
            Debug.LogWarning("[NPCDistributor] gameSceneNames 为空，没有场景可分配 NPC！");
            distributed = true;
            return;
        }

        foreach (var sceneName in gameSceneNames)
            sceneAssignment[sceneName] = new List<NPCInfo>();

        var shuffled = ShuffleCopy(allNPCs);
        for (int i = 0; i < shuffled.Count; i++)
        {
            string sceneName = gameSceneNames[i % gameSceneNames.Count];
            sceneAssignment[sceneName].Add(shuffled[i]);
        }

        distributed = true;
        LogDistribution();
    }

    private static List<NPCInfo> ShuffleCopy(List<NPCInfo> source)
    {
        var shuffled = new List<NPCInfo>(source);
        for (int i = shuffled.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (shuffled[i], shuffled[j]) = (shuffled[j], shuffled[i]);
        }
        return shuffled;
    }

    private void LogDistribution()
    {
        Debug.Log($"[NPCDistributor] 已分配 {allNPCs.Count} 个 NPC 到 {gameSceneNames.Count} 个场景");
        foreach (var kvp in sceneAssignment)
            Debug.Log($"  {kvp.Key}: {kvp.Value.Count} 个 NPC");
    }

    /// <summary>
    /// 获取指定场景的 NPC 子集。NPCManager 在 Start 中调用，随后按点位容量自行装桶。
    /// </summary>
    public List<NPCInfo> GetNPCsForScene(string sceneName)
    {
        if (sceneAssignment.TryGetValue(sceneName, out var list))
            return list;

        Debug.LogWarning($"[NPCDistributor] 场景 '{sceneName}' 未在 gameSceneNames 中配置");
        return new List<NPCInfo>();
    }

    /// <summary>
    /// 跨场景按用户名查找 NPC 所在的场景名。未找到返回 null。
    /// </summary>
    public string FindNPCSceneByName(string username)
    {
        if (string.IsNullOrEmpty(username)) return null;

        foreach (var kvp in sceneAssignment)
            foreach (var npc in kvp.Value)
                if (npc.Username == username) return kvp.Key;

        return null;
    }

    /// <summary>
    /// 重置（新游戏时由 MainMenuManager 调用）
    /// </summary>
    public void ResetForNewGame()
    {
        LoadAndDistribute();
    }
}
