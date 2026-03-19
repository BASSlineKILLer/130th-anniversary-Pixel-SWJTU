using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 管理器（单例）
/// 负责从 API 拉取数据 → 批量生成 NPC → 增量更新
/// </summary>
public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    [Header("API 配置")]
    [Tooltip("后端接口地址")]
    public string apiUrl = "http://devshowcase.site/api/approved";

    [Header("NPC 生成配置")]
    [Tooltip("NPC 预制体，需挂载 NPCController")]
    public GameObject npcPrefab;
    [Tooltip("NPC 父容器（保持 Hierarchy 整洁）")]
    public Transform npcParent;
    [Tooltip("预定义的 NPC 生成点位，按顺序分配")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("状态（只读）")]
    [SerializeField] private int loadedCount;
    [SerializeField] private bool isLoading;

    // 已生成的 NPC，按 id 跟踪，防止重复
    private Dictionary<int, GameObject> spawnedNPCs = new Dictionary<int, GameObject>();
    // 下一个可用的点位索引
    private int nextSpawnIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // 计算已被占用的点位数量（场景可能有预置 NPC）
        nextSpawnIndex = 0;
        FetchAndSpawnNPCs();
    }

    /// <summary>
    /// 触发 API 请求 → 解码 → 批量生成  (可由外部调用刷新)
    /// </summary>
    public void FetchAndSpawnNPCs()
    {
        if (isLoading)
        {
            Debug.LogWarning("[NPCManager] 正在加载中，忽略重复请求");
            return;
        }

        if (npcPrefab == null)
        {
            Debug.LogError("[NPCManager] npcPrefab 未设置！请在 Inspector 中拖入 NPC 预制体");
            return;
        }

        isLoading = true;
        StartCoroutine(NPCApiService.FetchNPCs(apiUrl, OnFetchSuccess, OnFetchError));
    }

    private void OnFetchSuccess(List<NPCInfo> npcList)
    {
        isLoading = false;
        Debug.Log($"[NPCManager] 获取到 {npcList.Count} 个 NPC");

        int newCount = 0;
        foreach (var info in npcList)
        {
            // 增量更新：跳过已存在的 NPC
            if (spawnedNPCs.ContainsKey(info.Id))
                continue;

            if (SpawnNPC(info))
                newCount++;
        }

        loadedCount = spawnedNPCs.Count;
        Debug.Log($"[NPCManager] 本次新增 {newCount} 个 NPC，总计 {loadedCount} 个");
    }

    private void OnFetchError(string error)
    {
        isLoading = false;
        Debug.LogError($"[NPCManager] {error}");
    }

    /// <summary>
    /// 在下一个可用点位生成单个 NPC
    /// </summary>
    private bool SpawnNPC(NPCInfo info)
    {
        Vector3 pos;

        if (spawnPoints.Count > 0 && nextSpawnIndex < spawnPoints.Count)
        {
            // 使用预定义点位
            Transform point = spawnPoints[nextSpawnIndex];
            pos = point != null ? point.position : Vector3.zero;
        }
        else
        {
            // 点位用完了，给出警告
            Debug.LogWarning($"[NPCManager] 预定义点位已用完 (已用 {nextSpawnIndex}/{spawnPoints.Count})，NPC(id={info.Id}) 将生成在原点");
            pos = Vector3.zero;
        }

        GameObject go = Instantiate(npcPrefab, pos, Quaternion.identity, npcParent);
        NPCController controller = go.GetComponent<NPCController>();
        if (controller != null)
        {
            controller.SetData(info);
        }
        else
        {
            Debug.LogError("[NPCManager] NPC 预制体上缺少 NPCController 组件！");
        }

        spawnedNPCs[info.Id] = go;
        nextSpawnIndex++;
        return true;
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
        nextSpawnIndex = 0;
        loadedCount = 0;
        Debug.Log("[NPCManager] 所有 NPC 已清除");
    }
}
