using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NPC 场景生成器（每个游戏场景一个）。
/// 从 NPCDistributor 获取本场景分配的 NPC 子集，再按点位容量轮询装桶生成。
/// 超出点位总容量的 NPC 会被丢弃并报警。
/// </summary>
public class NPCManager : MonoBehaviour
{
    /// <summary>未配置 spawnPointCapacities 时的默认点位容量</summary>
    private const int DEFAULT_CAPACITY = 1;

    public static NPCManager Instance { get; private set; }

    [Header("NPC 生成配置")]
    [Tooltip("NPC Prefab 在 Resources 中的路径（不含扩展名）")]
    public string npcPrefabPath = "NPCData/NPC";

    [Header("点位配置")]
    [Tooltip("预定义生成点位")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Tooltip("每个点位的最大 NPC 容量。索引对应 spawnPoints；缺少的项按默认容量 1 处理")]
    [Min(0)]
    public List<int> spawnPointCapacities = new List<int>();

    [Header("散布")]
    [Tooltip("同点位的多个 NPC 以点位为中心的随机散布半径（Unity 单位）。0 = 完全重叠")]
    [Min(0f)]
    public float spawnSpreadRadius = 0.5f;

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
        StartCoroutine(SpawnWhenDistributorReady());
    }

    /// <summary>
    /// 等 Distributor 完成首次加载再生成 NPC。
    /// 首次冷启动玩家很快点进场景时，数据可能仍在后台拉取；避免空列表导致 NPC 消失。
    /// </summary>
    private IEnumerator SpawnWhenDistributorReady()
    {
        while (NPCDistributor.Instance == null || !NPCDistributor.Instance.IsReady)
            yield return null;

        SpawnFromDistributor();
    }

    /// <summary>
    /// 从 NPCDistributor 获取本场景的 NPC 扁平列表，再按本场景的点位容量轮询装桶生成。
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

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning($"[NPCManager] 场景 '{sceneName}' 未配置任何点位，{npcList.Count} 个 NPC 未生成");
            return;
        }

        int placed = DistributeIntoPoints(npcList);
        spawnedCount = spawnedNPCs.Count;

        if (placed < npcList.Count)
        {
            int dropped = npcList.Count - placed;
            Debug.LogWarning($"[NPCManager] 场景 '{sceneName}' 点位总容量不足，丢弃 {dropped} 个 NPC");
        }
        Debug.Log($"[NPCManager] 场景 '{sceneName}' 生成了 {spawnedCount} 个 NPC");
    }

    /// <summary>轮询装桶：每轮遍历所有点位各放 1 个，该点位满则跳过。返回成功放入的 NPC 数</summary>
    private int DistributeIntoPoints(List<NPCInfo> npcList)
    {
        var current = new int[spawnPoints.Count];
        int npcIdx = 0;
        while (npcIdx < npcList.Count)
        {
            bool placedThisRound = false;
            for (int i = 0; i < spawnPoints.Count && npcIdx < npcList.Count; i++)
            {
                if (!TryPlaceAtPoint(i, current, npcList[npcIdx])) continue;
                npcIdx++;
                placedThisRound = true;
            }
            if (!placedThisRound) break;
        }
        return npcIdx;
    }

    private bool TryPlaceAtPoint(int pointIdx, int[] current, NPCInfo info)
    {
        var point = spawnPoints[pointIdx];
        if (point == null) return false;
        if (current[pointIdx] >= GetCapacity(pointIdx)) return false;
        if (spawnedNPCs.ContainsKey(info.Id)) return false;

        Vector3 pos = point.position + RandomSpreadOffset(spawnSpreadRadius);
        SpawnNPC(info, pos);
        current[pointIdx]++;
        return true;
    }

    /// <summary>取指定点位的容量；超出 spawnPointCapacities 长度的索引返回默认值</summary>
    private int GetCapacity(int pointIdx)
    {
        return pointIdx < spawnPointCapacities.Count ? spawnPointCapacities[pointIdx] : DEFAULT_CAPACITY;
    }

    private void SpawnNPC(NPCInfo info, Vector3 pos)
    {
        GameObject go = Instantiate(npcPrefab, pos, Quaternion.identity, npcParent);
        var controller = go.GetComponent<NPCController>();
        if (controller != null)
            controller.SetData(info);
        else
            Debug.LogError("[NPCManager] Prefab 缺少 NPCController！");

        spawnedNPCs[info.Id] = go;
    }

    private static Vector3 RandomSpreadOffset(float radius)
    {
        if (radius <= 0f) return Vector3.zero;
        Vector2 offset = Random.insideUnitCircle * radius;
        return new Vector3(offset.x, offset.y, 0f);
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
