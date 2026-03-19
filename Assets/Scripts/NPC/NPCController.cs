using UnityEngine;

/// <summary>
/// 单个 NPC 行为控制。
///
/// 当前职责：
///   - 接收 NPCInfo 数据，将 Sprite 显示在 SpriteRenderer 上
///   - 将数据保存供对话系统后续读取（Info 属性）
///   - 玩家靠近时显示提示气泡（仅提示，不显示 username/message）
///   - 通过 BoxCollider2D(Trigger) 感知玩家进出范围，预留对话系统接入点
///
/// 【Prefab 结构】
/// NPC
///   ├─ SpriteRenderer   ← 显示角色 Sprite
///   ├─ BoxCollider2D    ← Trigger，检测玩家靠近/离开（由脚本自动配置）
///   └─ BubbleRoot       ← 可选，提示气泡子物体（建议 SpriteRenderer）
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class NPCController : MonoBehaviour
{
    [Header("提示气泡")]
    [Tooltip("提示气泡根节点（可选）。玩家靠近显示，离开隐藏")]
    public GameObject bubbleRoot;
    [Tooltip("气泡相对于 NPC 的偏移位置")]
    public Vector3 bubbleOffset = new Vector3(0f, 1.2f, 0f);

    [Header("检测范围")]
    [Tooltip("玩家进入多少单位范围内视为靠近，影响 BoxCollider2D 大小")]
    public float triggerRadius = 2f;

    /// <summary>
    /// NPC 数据（id、username、message 等），供对话系统读取
    /// </summary>
    public NPCInfo Info { get; private set; }

    private SpriteRenderer spriteRenderer;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        var col = GetComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(triggerRadius * 2f, triggerRadius * 2f);

        if (bubbleRoot != null)
        {
            bubbleRoot.transform.localPosition = bubbleOffset;
            bubbleRoot.SetActive(false);
        }
    }

    /// <summary>
    /// 由 NPCManager 在生成时调用，注入数据并更新 Sprite
    /// </summary>
    public void SetData(NPCInfo info)
    {
        Info = info;

        if (info.Sprite != null)
            spriteRenderer.sprite = info.Sprite;

        gameObject.name = $"NPC_{info.Id}_{info.Username}";
    }

    // 玩家走进范围 —— 预留：对话系统可在此调用 DialogueManager.Instance.StartDialogue(Info)
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (bubbleRoot != null)
            bubbleRoot.SetActive(true);
    }

    // 玩家离开范围 —— 预留：对话系统可在此调用 DialogueManager.Instance.EndDialogue()
    private void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;

        if (bubbleRoot != null)
            bubbleRoot.SetActive(false);
    }
}
