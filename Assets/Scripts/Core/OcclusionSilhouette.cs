using UnityEngine;

/// <summary>
/// 遮挡轮廓组件。
/// 挂到角色（Player、NPC）上，被遮挡时自动显示被遮挡部分的 1 像素边缘轮廓。
///
/// 工作原理（Stencil）：
///   ① Player 层：角色正常渲染（不改 Shader）
///   ② Buildings / Flower&amp;Tree 层：遮挡物用 SpriteOccluder Shader 渲染，写 Stencil bit7
///   ③ UI 层：轮廓子对象用 SpriteOutline Shader，只在 Stencil bit7==1 处渲染边缘
///   → 只有角色被遮挡的区域才会显示轮廓，不需要脚本检测。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class OcclusionSilhouette : MonoBehaviour
{
    private const string OUTLINE_SHADER_NAME = "Custom/SpriteOutline";
    private const string OUTLINE_SORTING_LAYER = "UI";
    private const int OUTLINE_SORTING_ORDER = -1000;

    private static readonly int ID_OUTLINE_COLOR = Shader.PropertyToID("_OutlineColor");
    private static readonly int ID_OUTLINE_WIDTH = Shader.PropertyToID("_OutlineWidth");

    [Header("轮廓设置")]
    [Tooltip("轮廓颜色")]
    [SerializeField] private Color outlineColor = new Color(1f, 1f, 1f, 0.7f);

    [Tooltip("轮廓宽度（像素单位）")]
    [SerializeField] private float outlineWidth = 1f;

    private SpriteRenderer parentRenderer;
    private SpriteRenderer outlineRenderer;

    private void Awake()
    {
        parentRenderer = GetComponent<SpriteRenderer>();
        if (parentRenderer == null) return;

        CreateOutlineChild();
    }

    private void CreateOutlineChild()
    {
        var shader = Shader.Find(OUTLINE_SHADER_NAME);
        if (shader == null)
        {
            Debug.LogError($"[OcclusionSilhouette] Shader '{OUTLINE_SHADER_NAME}' 未找到！");
            return;
        }

        var go = new GameObject("_OutlineSilhouette");
        go.transform.SetParent(transform, false);

        outlineRenderer = go.AddComponent<SpriteRenderer>();

        var mat = new Material(shader);
        mat.SetColor(ID_OUTLINE_COLOR, outlineColor);
        mat.SetFloat(ID_OUTLINE_WIDTH, outlineWidth);

        outlineRenderer.material = mat;
        outlineRenderer.sortingLayerName = OUTLINE_SORTING_LAYER;
        outlineRenderer.sortingOrder = OUTLINE_SORTING_ORDER;
    }

    private void LateUpdate()
    {
        if (parentRenderer == null || outlineRenderer == null) return;

        outlineRenderer.sprite = parentRenderer.sprite;
        outlineRenderer.flipX = parentRenderer.flipX;
        outlineRenderer.flipY = parentRenderer.flipY;
    }
}
