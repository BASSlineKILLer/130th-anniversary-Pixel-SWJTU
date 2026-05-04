using UnityEngine;
using TMPro;

/// <summary>
/// SearchCanvas 预制体内部 UI 引用聚合。
///
/// 【为什么需要这个脚本】
/// Unity 的 prefab 不支持在 Inspector 里跨 prefab 引用子物体
/// （例如 NpcSearchTrigger.prefab 无法拖拽 SearchCanvas.prefab 内部的
/// CandidateContainer）。解决办法是把引用集中到 SearchCanvas 内部的
/// 这个脚本上（同 prefab 内引用合法），使用方通过 FindObjectOfType 拿到。
///
/// 【使用方式】
/// 1. 把本脚本挂到 SearchCanvas 根节点上
/// 2. 在 Inspector 把 SearchPanel / InputField / CandidateContainer 等拖到下列字段
/// 3. NpcSearch 运行时通过 FindObjectOfType&lt;SearchCanvasRefs&gt;() 拿引用
/// </summary>
public class SearchCanvasRefs : MonoBehaviour
{
    [Header("搜索面板")]
    [Tooltip("SearchPanel 根节点")]
    public GameObject searchPanel;

    [Tooltip("搜索输入框 InputField (TMP)")]
    public TMP_InputField searchInput;

    [Header("候选卡片")]
    [Tooltip("候选卡片容器（推荐：SearchPanel/card/CandidateContainer）")]
    public Transform candidateContainer;

    [Header("错误提示")]
    [Tooltip("错误面板 ErrorPanel")]
    public GameObject errorPanel;

    [Tooltip("错误文本")]
    public TextMeshProUGUI errorText;

    [Header("场景传送列表（可选）")]
    [Tooltip("场景传送列表面板")]
    public SceneTeleportList teleportListPanel;
}
