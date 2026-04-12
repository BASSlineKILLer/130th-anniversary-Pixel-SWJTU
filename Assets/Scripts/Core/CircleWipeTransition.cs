using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 圆形虹膜遮罩转场控制器（Iris Wipe / Circle Wipe）
/// 运行时自动创建全屏遮罩 Canvas + RawImage，通过自定义 Shader 实现圆形遮罩动画。
/// 挂载到 DontDestroyOnLoad 的 GameObject 上（推荐与 SceneTransitionManager 同一物体）。
/// 
/// 使用方式：
///   yield return CircleWipeTransition.Instance.PlayClose(); // 缩小到玩家
///   yield return CircleWipeTransition.Instance.PlayOpen();  // 从玩家展开
/// 
/// 注意：打包时需将 "Custom/CircleWipe" Shader 添加到
///       Edit > Project Settings > Graphics > Always Included Shaders，
///       或在 Inspector 中手动指定 circleWipeShader 引用。
/// </summary>
public class CircleWipeTransition : MonoBehaviour
{
    public static CircleWipeTransition Instance { get; private set; }

    [Header("动画参数")]
    [Tooltip("圆形关闭动画时长（秒）")]
    [SerializeField] private float closeDuration = 0.8f;

    [Tooltip("圆形打开动画时长（秒）")]
    [SerializeField] private float openDuration = 0.6f;

    [Tooltip("全黑停留时长（秒）")]
    [SerializeField] private float holdDuration = 0.2f;

    [Tooltip("圆形边缘柔和度（0 = 完全锐利，像素风建议 ≤ 0.01）")]
    [SerializeField] private float softness = 0.005f;

    [Header("缓动曲线")]
    [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private AnimationCurve openCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Shader 引用（可选，留空自动查找）")]
    [SerializeField] private Shader circleWipeShader;

    [Header("玩家配置")]
    [Tooltip("Player 的 Tag，必须与 SceneTransitionManager 中的一致")]
    [SerializeField] private string playerTag = "Player";

    private RawImage wipeImage;
    private Material wipeMaterial;
    private bool isAnimating;

    // Shader 属性 ID 缓存，避免每帧字符串查找
    private static readonly int CenterProp = Shader.PropertyToID("_Center");
    private static readonly int RadiusProp = Shader.PropertyToID("_Radius");
    private static readonly int SoftnessProp = Shader.PropertyToID("_Softness");
    private static readonly int AspectProp = Shader.PropertyToID("_Aspect");

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeOverlay();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (wipeMaterial != null) Destroy(wipeMaterial);
    }

    /// <summary>
    /// 运行时自动创建覆盖全屏的遮罩 UI
    /// </summary>
    private void InitializeOverlay()
    {
        // 查找 Shader
        if (circleWipeShader == null)
            circleWipeShader = Shader.Find("Custom/CircleWipe");

        if (circleWipeShader == null)
        {
            Debug.LogError("[CircleWipeTransition] 找不到 Custom/CircleWipe Shader！" +
                           "请确保 Assets/Shaders/CircleWipe.shader 存在。");
            return;
        }

        // 创建 Material
        wipeMaterial = new Material(circleWipeShader);

        // 创建 Screen Space Overlay Canvas（最高层级）
        var canvasGo = new GameObject("CircleWipeCanvas");
        canvasGo.transform.SetParent(transform);
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;

        // 创建全屏 RawImage 作为遮罩载体
        var imageGo = new GameObject("WipeImage");
        imageGo.transform.SetParent(canvasGo.transform, false);

        wipeImage = imageGo.AddComponent<RawImage>();
        wipeImage.texture = Texture2D.whiteTexture;
        wipeImage.material = wipeMaterial;
        wipeImage.raycastTarget = false; // 不拦截点击

        // 拉伸到全屏
        var rect = wipeImage.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        // 初始状态：完全透明（不可见）
        SetFullyTransparent();
        wipeImage.gameObject.SetActive(false);
    }

    // ===================== Public API =====================

    /// <summary>
    /// 播放关闭动画：圆圈从全屏缩小到玩家位置，最终全黑。
    /// 用法：yield return CircleWipeTransition.Instance.PlayClose();
    /// </summary>
    public IEnumerator PlayClose()
    {
        if (wipeMaterial == null || isAnimating) yield break;

        isAnimating = true;
        wipeImage.gameObject.SetActive(true);

        bool hasPlayer = HasPlayer();
        Vector2 center = GetPlayerViewportCenter();
        float aspect = GetScreenAspect();
        float maxRadius = CalculateMaxRadius(center, aspect);

        Debug.Log($"[CircleWipe] PlayClose: hasPlayer={hasPlayer}, center={center}");

        wipeMaterial.SetVector(CenterProp, new Vector4(center.x, center.y, 0, 0));
        wipeMaterial.SetFloat(SoftnessProp, softness);
        wipeMaterial.SetFloat(AspectProp, aspect);

        if (hasPlayer)
        {
            // 有玩家：圆形虹膜缩小到玩家位置
            float elapsed = 0f;
            while (elapsed < closeDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / closeDuration);
                float curveValue = closeCurve.Evaluate(t);
                float radius = Mathf.Lerp(maxRadius, -0.1f, curveValue);
                wipeMaterial.SetFloat(RadiusProp, radius);
                yield return null;
            }
        }
        else
        {
            // 无玩家（如主菜单）：直接全屏渐黑
            float elapsed = 0f;
            while (elapsed < closeDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / (closeDuration * 0.5f));
                float radius = Mathf.Lerp(maxRadius, -0.1f, t);
                wipeMaterial.SetFloat(RadiusProp, radius);
                yield return null;
            }
        }

        // 确保最终状态是完全黑屏
        wipeMaterial.SetFloat(RadiusProp, -0.1f);

        // 全黑停留
        if (holdDuration > 0f)
            yield return new WaitForSecondsRealtime(holdDuration);

        isAnimating = false;
    }

    /// <summary>
    /// 播放打开动画：圆圈从玩家位置扩大到全屏，最终完全透明。
    /// 用法：yield return CircleWipeTransition.Instance.PlayOpen();
    /// </summary>
    public IEnumerator PlayOpen()
    {
        if (wipeMaterial == null || isAnimating) yield break;

        isAnimating = true;

        // 等到帧末尾，确保 Cinemachine LateUpdate 已执行完毕后再读取相机位置
        yield return new WaitForEndOfFrame();

        bool hasPlayer = HasPlayer();
        Vector2 center = GetPlayerViewportCenter();
        float aspect = GetScreenAspect();
        float maxRadius = CalculateMaxRadius(center, aspect);

        Debug.Log($"[CircleWipe] PlayOpen: hasPlayer={hasPlayer}, center={center}");

        wipeMaterial.SetVector(CenterProp, new Vector4(center.x, center.y, 0, 0));
        wipeMaterial.SetFloat(SoftnessProp, softness);
        wipeMaterial.SetFloat(AspectProp, aspect);

        if (hasPlayer)
        {
            // 有玩家：从玩家位置扩大圆形虹膜
            float elapsed = 0f;
            while (elapsed < openDuration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / openDuration);
                float curveValue = openCurve.Evaluate(t);
                float radius = Mathf.Lerp(-0.1f, maxRadius, curveValue);
                wipeMaterial.SetFloat(RadiusProp, radius);
                yield return null;
            }
        }
        else
        {
            // 无玩家（如主菜单）：直接全屏渐亮
            float elapsed = 0f;
            while (elapsed < openDuration * 0.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / (openDuration * 0.5f));
                float radius = Mathf.Lerp(-0.1f, maxRadius, t);
                wipeMaterial.SetFloat(RadiusProp, radius);
                yield return null;
            }
        }

        // 完成：完全透明并隐藏遮罩 Image
        SetFullyTransparent();
        wipeImage.gameObject.SetActive(false);
        isAnimating = false;
    }

    // ===================== Internal Helpers =====================

    /// <summary>
    /// 检测场景中是否存在 Player（决定使用虹膜聚焦还是全屏渐变）
    /// </summary>
    private bool HasPlayer()
    {
        return GameObject.FindGameObjectWithTag(playerTag) != null;
    }

    /// <summary>
    /// 获取玩家在屏幕上的 Viewport 坐标（0-1 范围），作为圆心。
    /// 找不到玩家或相机时，回退到屏幕中心。
    /// </summary>
    private Vector2 GetPlayerViewportCenter()
    {
        var cam = Camera.main;
        if (cam == null) return new Vector2(0.5f, 0.5f);

        var playerGo = GameObject.FindGameObjectWithTag(playerTag);
        if (playerGo == null) return new Vector2(0.5f, 0.5f);

        Vector3 vp = cam.WorldToViewportPoint(playerGo.transform.position);
        return new Vector2(vp.x, vp.y);
    }

    /// <summary>
    /// 计算从圆心到屏幕四个角的最大距离（经宽高比校正），
    /// 确保圆形在 maxRadius 时能完全覆盖全屏。
    /// </summary>
    private float CalculateMaxRadius(Vector2 center, float aspect)
    {
        float maxDist = 0f;

        // 检查四个角：(0,0)  (1,0)  (0,1)  (1,1)
        for (int x = 0; x <= 1; x++)
        {
            for (int y = 0; y <= 1; y++)
            {
                Vector2 diff = new Vector2(x - center.x, y - center.y);
                diff.x *= aspect;
                float dist = diff.magnitude;
                if (dist > maxDist) maxDist = dist;
            }
        }

        return maxDist + 0.1f; // 额外留一点余量
    }

    private float GetScreenAspect()
    {
        return (float)Screen.width / Screen.height;
    }

    private void SetFullyTransparent()
    {
        if (wipeMaterial != null)
            wipeMaterial.SetFloat(RadiusProp, 10f);
    }
}
