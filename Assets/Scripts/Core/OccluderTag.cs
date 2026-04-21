using UnityEngine;

/// <summary>
/// 遮挡物标记组件。
/// 挂到建筑、树木等需要遮挡角色的对象上（支持 SpriteRenderer 和 TilemapRenderer）。
/// 自动将 Renderer 材质替换为 SpriteOccluder Shader，使其在渲染时写入 Stencil。
/// </summary>
public class OccluderTag : MonoBehaviour
{
    private const string OCCLUDER_SHADER_NAME = "Custom/SpriteOccluder";

    private static Material sharedOccluderMaterial;

    private void Awake()
    {
        if (sharedOccluderMaterial == null)
        {
            var shader = Shader.Find(OCCLUDER_SHADER_NAME);
            if (shader == null)
            {
                Debug.LogError($"[OccluderTag] Shader '{OCCLUDER_SHADER_NAME}' 未找到！");
                return;
            }
            sharedOccluderMaterial = new Material(shader);
        }

        var r = GetComponent<Renderer>();
        if (r != null)
            r.sharedMaterial = sharedOccluderMaterial;
        else
            Debug.LogWarning($"[OccluderTag] {gameObject.name} 没有 Renderer！");
    }
}
