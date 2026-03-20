using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 管理器（单例）
///
/// NPC Prefab 从 Resources/NPCData/NPC 加载，不需要在场景中预先放置 Prefab 实例。
/// 场景中只需一个挂载此脚本的空物体，配好 spawnPoints 即可。
/// </summary>
public class NPCManager : MonoBehaviour
{
    public static NPCManager Instance { get; private set; }

    [Header("API 配置")]
    [Tooltip("后端接口地址")]
    public string apiUrl = "http://devshowcase.site/api/approved";

    [Header("NPC 生成配置")]
    [Tooltip("NPC Prefab 在 Resources 中的路径（不含扩展名）")]
    public string npcPrefabPath = "NPCData/NPC";
    [Tooltip("预定义生成点位")]
    public List<Transform> spawnPoints = new List<Transform>();

    [Header("状态（只读）")]
    [SerializeField] private int loadedCount;
    [SerializeField] private bool isLoading;

    private GameObject npcPrefab;
    private Transform npcParent;
    private Dictionary<int, GameObject> spawnedNPCs = new Dictionary<int, GameObject>();
    private int nextSpawnIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // 从 Resources 加载 NPC Prefab，无需场景中拖引用
        npcPrefab = Resources.Load<GameObject>(npcPrefabPath);
        if (npcPrefab == null)
            Debug.LogError($"[NPCManager] 找不到 Prefab: Resources/{npcPrefabPath}。请把 NPC.prefab 放到 Assets/Resources/NPCData/ 下");

        // 自动创建容器
        npcParent = new GameObject("NPCs").transform;
        npcParent.SetParent(transform);
    }

    private void Start()
    {
        nextSpawnIndex = 0;
        FetchAndSpawnNPCs();
    }

    /// <summary>
    /// 触发加载/刷新。可由外部调用。
    /// </summary>
    public void FetchAndSpawnNPCs()
    {
        if (isLoading)
        {
            Debug.LogWarning("[NPCManager] 正在加载，忽略重复请求");
            return;
        }

        if (npcPrefab == null)
        {
            Debug.LogError("[NPCManager] NPC Prefab 未加载！请检查 Resources 路径");
            return;
        }

        isLoading = true;
        StartCoroutine(NPCApiService.FetchNPCs(apiUrl, OnFetchSuccess, OnFetchError));
    }

    private void OnFetchSuccess(List<NPCInfo> npcList)
    {
        isLoading = false;
        Debug.Log($"[NPCManager] 收到 {npcList.Count} 个 NPC 数据");

        int newCount = 0;
        foreach (var info in npcList)
        {
            if (spawnedNPCs.ContainsKey(info.Id))
            {
                // 已存在：更新数据（网络可能返回了更新的 Sprite）
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

        loadedCount = spawnedNPCs.Count;
        if (newCount > 0)
            Debug.Log($"[NPCManager] 新增 {newCount} 个 NPC, 总计 {loadedCount} 个");
    }

    private void OnFetchError(string error)
    {
        isLoading = false;
        Debug.LogError($"[NPCManager] {error}");
    }

    private bool SpawnNPC(NPCInfo info)
    {
        Vector3 pos = Vector3.zero;

        if (nextSpawnIndex < spawnPoints.Count)
        {
            Transform point = spawnPoints[nextSpawnIndex];
            if (point != null)
                pos = point.position;
        }
        else
        {
            Debug.LogWarning($"[NPCManager] 点位用完 ({nextSpawnIndex}/{spawnPoints.Count}), NPC(id={info.Id}) 生成在原点");
        }

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
        nextSpawnIndex++;
        return true;
    }

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
