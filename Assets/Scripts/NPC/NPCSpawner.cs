using UnityEngine;

/// <summary>
/// NPC生成逻辑
/// 在地图上的固定点位生成若干NPC
/// </summary>
public class NPCSpawner : MonoBehaviour
{
    [Header("生成设置")]
    public GameObject npcPrefab;
    public Transform[] spawnPoints;
    public int maxNPCCount = 50;

    [Header("NPC数据")]
    public NPCDataImporter dataImporter;

    private void Start()
    {
        SpawnNPCs();
    }

    public void SpawnNPCs()
    {
        if (npcPrefab == null || spawnPoints == null || spawnPoints.Length == 0)
            return;

        int spawnCount = Mathf.Min(maxNPCCount, spawnPoints.Length);

        for (int i = 0; i < spawnCount; i++)
        {
            Vector3 spawnPos = spawnPoints[i % spawnPoints.Length].position;
            // 加一点随机偏移
            spawnPos += new Vector3(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f), 0);

            GameObject npcObj = Instantiate(npcPrefab, spawnPos, Quaternion.identity, transform);
            npcObj.name = $"NPC_{i}";

            NPCController npc = npcObj.GetComponent<NPCController>();
            if (npc != null)
            {
                npc.canWander = Random.value < 0.3f;
            }
        }
    }
}
