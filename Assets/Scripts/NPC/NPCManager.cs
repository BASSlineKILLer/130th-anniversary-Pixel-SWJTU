using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 管理器（单例）
///
/// 双数据源架构：
///   1. 手动条目 —— 从 NPCDatabase 读取，编辑器中可视化配置
///   2. API 远程  —— 运行时从后端拉取（可在 NPCDatabase 中开关）
///
/// NPC Prefab 从 Resources 加载，场景中只需挂载此脚本 + 配置 spawnPoints。
/// </summary>
public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    [Header("NPC 数据库")]
    [Tooltip("拖入 NPCDatabase 资产。留空则自动从 Resources/NPCData/NPCDatabase 加载")]
    public NPCDatabase database;

    [Header("NPC 生成配置")]
    [Tooltip("NPC Prefab 在 Resources 中的路径（不含扩展名）")]
    public string npcPrefabPath = "NPCData/NPC";
    [Tooltip("预定义生成点位，NPC 随机分配到各点位")]
    public List<Transform> spawnPoints = new List<Transform>();
    [Tooltip("每个出生点最多容纳的 NPC 数量")]
    [Min(1)] public int maxNPCsPerSpawnPoint = 1;

    [Header("状态（只读）")]
    [SerializeField] private int manualCount;
    [SerializeField] private int apiCount;
    [SerializeField] private bool isLoading;

    private GameObject npcPrefab;
    private Transform npcParent;
    private Dictionary<int, GameObject> spawnedNPCs = new Dictionary<int, GameObject>();
    private Dictionary<int, int> spawnPointUsage = new Dictionary<int, int>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 自动加载数据库
        if (database == null)
            database = Resources.Load<NPCDatabase>("NPCData/NPCDatabase");

        if (database == null)
            Debug.LogWarning("[NPCManager] 未找到 NPCDatabase。请创建：右键 → Create → NPC → NPC Database，放到 Resources/NPCData/");

        // 加载 Prefab
        npcPrefab = Resources.Load<GameObject>(npcPrefabPath);
        if (npcPrefab == null)
            Debug.LogError($"[NPCManager] 找不到 Prefab: Resources/{npcPrefabPath}");

        // 自动创建容器
        npcParent = new GameObject("NPCs").transform;
        npcParent.SetParent(transform);
    }

    private void Start()
    {
        InitSpawnPointUsage();
        LoadAllNPCs();
    }

    /// <summary>
    /// 加载所有 NPC：先手动条目，再 API（如果启用）。可由外部调用刷新。
    /// </summary>
    public void LoadAllNPCs()
    {
        if (npcPrefab == null) return;

        // ===== 阶段 1：手动 NPC =====
        if (database != null)
        {
            var manualList = database.GetManualNPCInfos();
            int newManual = 0;
            foreach (var info in manualList)
            {
                if (!spawnedNPCs.ContainsKey(info.Id) && SpawnNPC(info))
                    newManual++;
            }
            manualCount = newManual;
            if (newManual > 0)
                Debug.Log($"[NPCManager] 手动 NPC 已加载: {newManual} 个");
        }

        // ===== 阶段 2：API NPC =====
        if (database != null && database.enableApiFetch)
        {
            FetchApiNPCs();
        }
    }

    /// <summary>
    /// 仅触发 API 获取（手动条目不重新加载）
    /// </summary>
    public void FetchApiNPCs()
    {
        if (isLoading)
        {
            Debug.LogWarning("[NPCManager] 正在加载，忽略重复请求");
            return;
        }
        if (npcPrefab == null) return;

        string url = database != null ? database.apiUrl : "http://devshowcase.site/api/approved";
        isLoading = true;
        StartCoroutine(NPCApiService.FetchNPCs(url, OnFetchSuccess, OnFetchError));
    }

    private void OnFetchSuccess(List<NPCInfo> npcList)
    {
        isLoading = false;
        Debug.Log($"[NPCManager] API 返回 {npcList.Count} 个 NPC");

        int newCount = 0;
        foreach (var info in npcList)
        {
            if (spawnedNPCs.ContainsKey(info.Id))
            {
                // 已存在：更新 Sprite（缓存→网络可能有新图）
                var existing = spawnedNPCs[info.Id];
                if (existing != null)
                {
                    var ctrl = existing.GetComponent<NPCController>();
                    if (ctrl != null) ctrl.SetData(info);
                }
                continue;
            }

            if (SpawnNPC(info))
                newCount++;
        }

        apiCount = newCount;
        if (newCount > 0)
            Debug.Log($"[NPCManager] 新增 {newCount} 个 API NPC, 总计 {spawnedNPCs.Count} 个");

        RemoveRevokedNPCs(npcList);
    }

    /// <summary>
    /// 对比最新 API 列表，移除场景中已被撤销审核的 NPC（仅检查 API NPC，即 ID > 0）
    /// </summary>
    private void RemoveRevokedNPCs(List<NPCInfo> validList)
    {
        var validIds = new HashSet<int>();
        foreach (var info in validList)
            validIds.Add(info.Id);

        var toRemove = new List<int>();
        foreach (var kvp in spawnedNPCs)
        {
            if (kvp.Key > 0 && !validIds.Contains(kvp.Key))
                toRemove.Add(kvp.Key);
        }

        foreach (int id in toRemove)
        {
            if (spawnedNPCs[id] != null)
                Destroy(spawnedNPCs[id]);
            spawnedNPCs.Remove(id);
            NPCApiService.RemoveCachedImage(id);
        }

        if (toRemove.Count > 0)
            Debug.Log($"[NPCManager] 已移除 {toRemove.Count} 个被撤销审核的 NPC");
    }

    private void OnFetchError(string error)
    {
        isLoading = false;
        Debug.LogError($"[NPCManager] {error}");
    }

    private bool SpawnNPC(NPCInfo info)
    {
        Vector3 pos = PickRandomSpawnPosition();

        GameObject go = Instantiate(npcPrefab, pos, Quaternion.identity, npcParent);
        var controller = go.GetComponent<NPCController>();
        if (controller != null)
        {
            controller.SetData(info);
        }
        else
        {
            Debug.LogError("[NPCManager] Prefab 缺少 NPCController！");
        }

        spawnedNPCs[info.Id] = go;
        return true;
    }

    private void InitSpawnPointUsage()
    {
        spawnPointUsage.Clear();
        for (int i = 0; i < spawnPoints.Count; i++)
            spawnPointUsage[i] = 0;
    }

    private Vector3 PickRandomSpawnPosition()
    {
        var available = new List<int>();
        for (int i = 0; i < spawnPoints.Count; i++)
        {
            if (spawnPoints[i] != null && spawnPointUsage[i] < maxNPCsPerSpawnPoint)
                available.Add(i);
        }

        if (available.Count == 0)
        {
            Debug.LogWarning("[NPCManager] 所有出生点已满，NPC 生成在原点");
            return Vector3.zero;
        }

        int chosen = available[Random.Range(0, available.Count)];
        spawnPointUsage[chosen]++;
        return spawnPoints[chosen].position;
    }

    /// <summary>
    /// 清除场景中所有已生成的 NPC
    /// </summary>
    public void ClearAllNPCs()
    {
        foreach (var kvp in spawnedNPCs)
        {
            if (kvp.Value != null)
                Destroy(kvp.Value);
        }
        spawnedNPCs.Clear();
        InitSpawnPointUsage();
        manualCount = 0;
        apiCount = 0;
        Debug.Log("[NPCManager] 所有 NPC 已清除");
    }
}
