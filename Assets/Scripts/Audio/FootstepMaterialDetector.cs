using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 检测指定世界坐标下的 Tilemap 瓷砖，返回对应的 FMOD material 参数值。
/// 每个 Tilemap 层直接指定材质类型（在 Inspector 中设置），无需依赖瓦片名称。
/// 按数组顺序从索引 0（最高优先级/最上层）开始检测，
/// 第一个在该位置有瓦片的层决定材质。
/// </summary>
public class FootstepMaterialDetector : MonoBehaviour
{
    // FMOD material 参数常量
    public const int MATERIAL_ROAD  = 0;
    public const int MATERIAL_GRASS = 1;
    public const int MATERIAL_WATER = 2;

    [System.Serializable]
    public struct TilemapMaterialEntry
    {
        [Tooltip("Tilemap 层引用")]
        public Tilemap tilemap;

        [Tooltip("该层的材质类型：0=Road, 1=Grass, 2=Water")]
        public int materialType;
    }

    [Tooltip("地面 Tilemap 列表（按优先级从高到低排列，索引 0 = 最上层）。\n每一项指定一个 Tilemap 及其对应的脚步声材质类型。")]
    public TilemapMaterialEntry[] tilemapEntries;

    [Header("调试")]
    [Tooltip("开启后在 Console 输出详细检测日志")]
    public bool debugMode = false;

    private static readonly string[] MaterialNames = { "Road(0)", "Grass(1)", "Water(2)" };

    private string GetMaterialName(int type)
    {
        return type >= 0 && type < MaterialNames.Length ? MaterialNames[type] : $"Unknown({type})";
    }

    private void Awake()
    {
        if (!debugMode) return;

        if (tilemapEntries == null || tilemapEntries.Length == 0)
        {
            Debug.LogWarning("[FootstepDetector] ⚠ tilemapEntries 为空！请在 Inspector 中配置 Tilemap 和材质类型。");
            return;
        }

        Debug.Log($"[FootstepDetector] 已配置 {tilemapEntries.Length} 个 Tilemap 层：");
        for (int i = 0; i < tilemapEntries.Length; i++)
        {
            var entry = tilemapEntries[i];
            string tmName = entry.tilemap != null ? entry.tilemap.gameObject.name : "NULL";
            Debug.Log($"  [{i}] Tilemap=\"{tmName}\", materialType={GetMaterialName(entry.materialType)}");
        }
    }

    /// <summary>
    /// 根据世界坐标获取脚下的材质 ID（对应 FMOD material 参数）。
    /// </summary>
    public int GetMaterialAtPosition(Vector3 worldPos)
    {
        if (tilemapEntries == null || tilemapEntries.Length == 0)
        {
            if (debugMode) Debug.LogWarning("[FootstepDetector] tilemapEntries 为空，返回默认 Road");
            return MATERIAL_ROAD;
        }

        int fallbackMaterial = -1; // 记录第一个非 road 命中

        for (int i = 0; i < tilemapEntries.Length; i++)
        {
            Tilemap tilemap = tilemapEntries[i].tilemap;
            if (tilemap == null) continue;

            Vector3Int cellPos = tilemap.WorldToCell(worldPos);
            TileBase tile = tilemap.GetTile(cellPos);

            if (tile != null)
            {
                int mat = tilemapEntries[i].materialType;
                if (debugMode)
                    Debug.Log($"[FootstepDetector] 位置{worldPos} → Tilemap=\"{tilemap.gameObject.name}\" 格子{cellPos} 瓦片=\"{tile.name}\"(type={tile.GetType().Name}) → 材质={GetMaterialName(mat)}");

                // Road 最高优先级，立即返回
                if (mat == MATERIAL_ROAD)
                    return MATERIAL_ROAD;

                // 记住第一个非 road 命中，继续检查其他层是否有 road
                if (fallbackMaterial < 0)
                    fallbackMaterial = mat;
            }
        }

        if (fallbackMaterial >= 0)
        {
            if (debugMode)
                Debug.Log($"[FootstepDetector] 位置{worldPos} → 无 Road 重叠，使用 {GetMaterialName(fallbackMaterial)}");
            return fallbackMaterial;
        }

        if (debugMode)
            Debug.LogWarning($"[FootstepDetector] 位置{worldPos} → 所有 {tilemapEntries.Length} 层均无瓦片，返回默认 Road");
        return MATERIAL_ROAD;
    }
}



