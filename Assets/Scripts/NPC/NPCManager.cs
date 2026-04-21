using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NPC 场景生成器（每个游戏场景一个）。
/// 不再自行加载数据，而是从 NPCDistributor 获取本场景分配到的 NPC 子集，
/// 在本场景的 spawnPoints 上生成 GameObject。
/// </summary>
public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    [Header("NPC 生成配置")]
    [Tooltip("NPC Prefab 在 Resources 中的路径（不含扩展名）")]
    public string npcPrefabPath = "NPCData/NPC";
    [Tooltip("预定义生成点位，NPC 随机分配到各点位")]
    public List<Transform> spawnPoints = new List<Transform>();
    [Tooltip("每个出生点最多容纳的 NPC 数量")]
    [Min(1)] public int maxNPCsPerSpawnPoint = 1;

    [Header("状态（只读）")]
    [SerializeField] private int spawnedCount;

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

        npcPrefab = Resources.Load<GameObject>(npcPrefabPath);
        if (npcPrefab == null)
            Debug.LogError($"[NPCManager] 找不到 Prefab: Resources/{npcPrefabPath}");

        npcParent = new GameObject("NPCs").transform;
        npcParent.SetParent(transform);
    }

    private void Start()
    {
        InitSpawnPointUsage();
        SpawnFromDistributor();
    }

    /// <summary>
    /// 从 NPCDistributor 获取本场景的 NPC 子集并生成
    /// </summary>
    private void SpawnFromDistributor()
    {
        if (npcPrefab == null) return;

        if (NPCDistributor.Instance == null)
        {
            Debug.LogWarning("[NPCManager] NPCDistributor 不存在，无法获取 NPC 数据");
            return;
        }

        string sceneName = SceneManager.GetActiveScene().name;
        var npcList = NPCDistributor.Instance.GetNPCsForScene(sceneName);

        foreach (var info in npcList)
        {
            if (!spawnedNPCs.ContainsKey(info.Id))
                SpawnNPC(info);
        }

        spawnedCount = spawnedNPCs.Count;
        Debug.Log($"[NPCManager] 场景 '{sceneName}' 生成了 {spawnedCount} 个 NPC");
    }

    private bool SpawnNPC(NPCInfo info)
    {
        Vector3 pos = PickRandomSpawnPosition();

        GameObject go = Instantiate(npcPrefab, pos, Quaternion.identity, npcParent);
        var controller = go.GetComponent<NPCController>();
        if (controller != null)
            controller.SetData(info);
        else
            Debug.LogError("[NPCManager] Prefab 缺少 NPCController！");

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
        spawnedCount = 0;
    }

    /// <summary>
    /// 当前场景中已生成的 NPC 数量
    /// </summary>
    public int TotalNPCs => spawnedNPCs.Count;
}
