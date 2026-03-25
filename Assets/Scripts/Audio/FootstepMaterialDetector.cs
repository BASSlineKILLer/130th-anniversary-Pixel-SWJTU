using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// 检测指定世界坐标下的 Tilemap 瓷砖，返回对应的 FMOD material 参数值。
/// 映射规则：草坪→1(grass)，直路/转角/十字路→0(road)，水体→2(water)，默认→0(road)。
/// </summary>
public class FootstepMaterialDetector : MonoBehaviour
{
    // FMOD material 参数常量
    public const int MATERIAL_ROAD  = 0;
    public const int MATERIAL_GRASS = 1;
    public const int MATERIAL_WATER = 2;

    [Tooltip("场景中用于地面的 Tilemap（Layer1）")]
    public Tilemap groundTilemap;

    /// <summary>
    /// 根据世界坐标获取脚下的材质 ID（对应 FMOD material 参数）。
    /// </summary>
    public int GetMaterialAtPosition(Vector3 worldPos)
    {
        if (groundTilemap == null) return MATERIAL_ROAD;

        Vector3Int cellPos = groundTilemap.WorldToCell(worldPos);
        TileBase tile = groundTilemap.GetTile(cellPos);

        if (tile == null) return MATERIAL_ROAD;

        return ClassifyTile(tile.name);
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

        // Gemini_Generated_Image 等未分类瓷砖默认为 road
        return MATERIAL_ROAD;
    }
}
