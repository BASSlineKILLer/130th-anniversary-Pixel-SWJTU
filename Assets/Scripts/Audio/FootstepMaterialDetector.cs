using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 检测指定世界坐标下的 Tilemap 瓷砖，返回对应的 FMOD material 参数值。
/// 支持多层 Tilemap：按数组顺序从索引 0（最高优先级/最上层）开始检测，
/// 第一个在该位置有瓦片的层决定材质。
/// 映射规则：草坪→1(grass)，直路/转角/十字路→0(road)，水体→2(water)，默认→0(road)。
/// </summary>
public class FootstepMaterialDetector : MonoBehaviour
{
    // FMOD material 参数常量
    public const int MATERIAL_ROAD  = 0;
    public const int MATERIAL_GRASS = 1;
    public const int MATERIAL_WATER = 2;

    [Tooltip("场景中用于地面的 Tilemap 列表（按优先级从高到低排列，索引 0 = 最上层）")]
    public Tilemap[] groundTilemaps;

    // 向后兼容：保留旧字段，场景中已序列化的旧数据会自动迁移到数组
    [HideInInspector] [SerializeField] private Tilemap groundTilemap;

    private void Awake()
    {
        MigrateOldField();
    }

    /// <summary>
    /// 自动迁移：如果旧的 groundTilemap 字段有值且新数组为空，将其迁移到数组中。
    /// </summary>
    private void MigrateOldField()
    {
        if (groundTilemap != null && (groundTilemaps == null || groundTilemaps.Length == 0))
        {
            groundTilemaps = new Tilemap[] { groundTilemap };
            groundTilemap = null; // 清除旧引用
            Debug.Log("[FootstepMaterialDetector] 已自动将旧的 groundTilemap 迁移到 groundTilemaps 数组。");
        }
    }

    /// <summary>
    /// 根据世界坐标获取脚下的材质 ID（对应 FMOD material 参数）。
    /// 从最高优先级的 Tilemap（索引 0）开始检测，第一个有瓦片的层决定材质。
    /// </summary>
    public int GetMaterialAtPosition(Vector3 worldPos)
    {
        if (groundTilemaps == null || groundTilemaps.Length == 0)
            return MATERIAL_ROAD;

        // 从最高优先级（索引 0 = 最上层）向下遍历
        for (int i = 0; i < groundTilemaps.Length; i++)
        {
            Tilemap tilemap = groundTilemaps[i];
            if (tilemap == null) continue;

            Vector3Int cellPos = tilemap.WorldToCell(worldPos);
            TileBase tile = tilemap.GetTile(cellPos);

            if (tile != null)
            {
                return ClassifyTile(tile.name);
            }
        }

        // 所有层都没有瓦片，返回默认
        return MATERIAL_ROAD;
    }

    /// <summary>
    /// 根据瓷砖名称前缀判断材质类型。
    /// </summary>
    private int ClassifyTile(string tileName)
    {
        if (tileName.StartsWith("草坪")) return MATERIAL_GRASS;
        if (tileName.StartsWith("直路")) return MATERIAL_ROAD;
        if (tileName.StartsWith("转角")) return MATERIAL_ROAD;
        if (tileName.StartsWith("十字路")) return MATERIAL_ROAD;
        if (tileName.StartsWith("水体")) return MATERIAL_WATER;

        // 未分类瓷砖默认为 road
        return MATERIAL_ROAD;
    }
}

