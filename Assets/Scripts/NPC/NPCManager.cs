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
    [Tooltip("预定义生成点位，NPC 按分配列表的索引确定性分配到点位")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("状态（只读）")]
    [SerializeField] private int spawnedCount;

    private GameObject npcPrefab;
    private Transform npcParent;
    private Dictionary<int, GameObject> spawnedNPCs = new Dictionary<int, GameObject>();

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
        SpawnFromDistributor();
    }

    /// <summary>
    /// 从 NPCDistributor 获取本场景的 NPC 子集并生成。
    /// 点位按列表索引确定性分配：NPC i → spawnPoints[i % count]，保证同一局游戏内进出场景位置稳定。
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

        for (int i = 0; i < npcList.Count; i++)
        {
            var info = npcList[i];
            if (!spawnedNPCs.ContainsKey(info.Id))
                SpawnNPC(info, i);
        }

        spawnedCount = spawnedNPCs.Count;
        Debug.Log($"[NPCManager] 场景 '{sceneName}' 生成了 {spawnedCount} 个 NPC");
    }

    private void SpawnNPC(NPCInfo info, int listIndex)
    {
        Vector3 pos = PickSpawnPositionByIndex(listIndex);

        GameObject go = Instantiate(npcPrefab, pos, Quaternion.identity, npcParent);
        var controller = go.GetComponent<NPCController>();
        if (controller != null)
            controller.SetData(info);
        else
            Debug.LogError("[NPCManager] Prefab 缺少 NPCController！");

        spawnedNPCs[info.Id] = go;
    }

    private Vector3 PickSpawnPositionByIndex(int npcIndex)
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("[NPCManager] 未配置 spawnPoints，NPC 生成在原点");
            return Vector3.zero;
        }

        int idx = npcIndex % spawnPoints.Count;
        var point = spawnPoints[idx];
        if (point == null)
        {
            Debug.LogWarning($"[NPCManager] spawnPoints[{idx}] 为空，NPC 生成在原点");
            return Vector3.zero;
        }
        return point.position;
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
        spawnedCount = 0;
    }

    /// <summary>
    /// 当前场景中已生成的 NPC 数量
    /// </summary>
    public int TotalNPCs => spawnedNPCs.Count;
}
