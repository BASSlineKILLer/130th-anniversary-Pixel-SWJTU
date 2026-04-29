using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MedalProgress : MonoBehaviour
{
    // 节点图标的颜色：未达 = 灰调半透明；已达 = 原色全亮
    private static readonly Color NODE_LOCKED_COLOR = new Color(0.4f, 0.4f, 0.4f, 0.6f);
    private static readonly Color NODE_UNLOCKED_COLOR = Color.white;

    // 在这些场景里禁用 Tab 监听并保持隐藏（菜单/过场不需要进度条）
    private static readonly HashSet<string> SCENES_WITHOUT_PROGRESS = new HashSet<string>
    {
        "MainMenu",
    };

    public static MedalProgress Instance { get; private set; }

    [Header("配置")]
    [Tooltip("拖入 MedalProgressConfig 资产")]
    public MedalProgressConfig config;

    [Header("UI 组件")]
    [Tooltip("进度条 Slider")]
    public Slider progressSlider;
    [Tooltip("进度百分比文本")]
    public TextMeshProUGUI progressText;

    [Header("节点图标")]
    [Tooltip("节点图标的父容器（需与进度条横向对齐，用 RectTransform）")]
    public RectTransform nodeIconParent;
    [Tooltip("节点图标 Prefab：根节点需挂 Image，可选挂 TextMeshProUGUI 显示阈值/描述")]
    public GameObject nodeIconPrefab;

    private readonly List<Image> nodeIcons = new List<Image>();
    private bool nodesBuilt;
    private bool tabEnabled;   // 当前场景是否允许 Tab 切换进度条
    private Canvas rootCanvas; // 根 Canvas，用 enabled 控制可见性（兼容 Overlay 模式）

    void Awake()
    {
        // 单例：跨场景常驻，避免重复实例
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        rootCanvas = GetComponent<Canvas>();

        // 立即隐藏 + 屏蔽 Tab，必须在第一帧渲染前完成
        SetVisible(false);
        tabEnabled = false;

        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 启动时立刻判断当前场景（GetActiveScene 在 Awake 中已可用）
        ApplySceneVisibility(SceneManager.GetActiveScene());
    }

    /// <summary>
    /// 统一控制可见性：优先用 Canvas.enabled，退后为 SetActive。
    /// Canvas.enabled=false 会立即停止本 Canvas 的渲染与输入，远比 transform.scale 可靠。
    /// </summary>
    private void SetVisible(bool visible)
    {
        if (rootCanvas != null)
        {
            rootCanvas.enabled = visible;
            return;
        }
        // 没有根 Canvas 时才退后用 scale
        transform.localScale = visible ? Vector3.one : Vector3.zero;
    }

    private bool IsVisible()
    {
        return rootCanvas != null ? rootCanvas.enabled : transform.localScale != Vector3.zero;
    }

    void Start()
    {
        if (progressSlider != null)
            progressSlider.interactable = false;

        if (MedalManager.Instance != null)
            MedalManager.Instance.onMedalCountChanged.AddListener(UpdateProgress);
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (MedalManager.Instance != null)
            MedalManager.Instance.onMedalCountChanged.RemoveListener(UpdateProgress);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneVisibility(scene);
    }

    /// <summary>
    /// 进入新场景时：在菜单/过场场景禁用 Tab 监听并强制隐藏；
    /// 在游戏场景启用 Tab 监听，但默认仍隐藏（玩家自行按 Tab 打开）。
    /// </summary>
    private void ApplySceneVisibility(Scene scene)
    {
        bool sceneAllowsProgress = !SCENES_WITHOUT_PROGRESS.Contains(scene.name);
        tabEnabled = sceneAllowsProgress;
        SetVisible(false); // 进入新场景默认隐藏，由玩家按 Tab 开启
        if (!sceneAllowsProgress)
            UnlockMovement(); // 切场景前可能仍处展开态，确保解锁玩家移动
        Debug.Log($"[MedalProgress] 场景={scene.name}, tabEnabled={tabEnabled}, hasCanvas={rootCanvas != null}");
    }

    void Update()
    {
        if (!tabEnabled) return;
        if (!Input.GetKeyDown(KeyCode.Tab)) return;

        bool willShow = !IsVisible();
        if (willShow)
            LockMovement();
        else
            UnlockMovement();

        SetVisible(willShow);

        if (willShow)
            UpdateProgress();
    }

    void UpdateProgress()
    {
        if (config == null || progressSlider == null || MedalManager.Instance == null)
            return;

        // 获取全局 NPC 总数（优先从 NPCDistributor，回退到配置值）
        int total = NPCDistributor.Instance != null ? NPCDistributor.Instance.TotalNPCs : 0;
        if (total == 0 && config.totalNPCs > 0)
            total = config.totalNPCs;

        progressSlider.maxValue = total;

        // 当前值
        int current = MedalManager.Instance.GetMedalCount();
        progressSlider.value = current;

        // 更新进度百分比文本
        if (progressText != null && total > 0)
        {
            float percentage = (float)current / total * 100;
            progressText.text = $"进度：{percentage:F1}%";
        }

        if (!nodesBuilt) RebuildNodes(total);
        UpdateNodeStates(current);
    }

    /// <summary>
    /// 根据 config.nodes 在 nodeIconParent 上排放节点图标。
    /// X 坐标按 threshold/total 等比映射到 nodeIconParent 宽度上，与 slider 横向对齐。
    /// </summary>
    private void RebuildNodes(int total)
    {
        if (nodeIconParent == null || nodeIconPrefab == null || config == null || config.nodes == null) return;
        if (total <= 0) return;

        ClearNodeIcons();

        float width = nodeIconParent.rect.width;
        foreach (var node in config.nodes)
        {
            if (node == null) continue;
            var iconGO = Instantiate(nodeIconPrefab, nodeIconParent);
            var rt = iconGO.transform as RectTransform;
            if (rt != null)
            {
                float ratio = Mathf.Clamp01((float)node.threshold / total);
                // 以容器左边为 0，右边为 width；锚点用左下方便定位
                rt.anchorMin = new Vector2(0f, 0.5f);
                rt.anchorMax = new Vector2(0f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = new Vector2(width * ratio, 0f);
            }

            var img = iconGO.GetComponent<Image>();
            if (img != null && node.icon != null)
                img.sprite = node.icon;

            // 如果 prefab 内有 TextMeshProUGUI，自动塞入阈值文本作为副标
            var label = iconGO.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = node.threshold.ToString();

            nodeIcons.Add(img);
        }

        nodesBuilt = true;
    }

    private void UpdateNodeStates(int current)
    {
        if (config == null || config.nodes == null) return;
        for (int i = 0; i < nodeIcons.Count && i < config.nodes.Count; i++)
        {
            if (nodeIcons[i] == null || config.nodes[i] == null) continue;
            bool reached = current >= config.nodes[i].threshold;
            nodeIcons[i].color = reached ? NODE_UNLOCKED_COLOR : NODE_LOCKED_COLOR;
        }
    }

    private void ClearNodeIcons()
    {
        foreach (var icon in nodeIcons)
        {
            if (icon != null) Destroy(icon.gameObject);
        }
        nodeIcons.Clear();
    }

    private void LockMovement()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDialogueLock(true);
        }
    }

    private void UnlockMovement()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDialogueLock(false);
        }
    }
}
