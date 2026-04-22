using UnityEngine;

/// <summary>
/// 平台入口触发器：穿越式开关，支持任意方向。
/// 
/// 判断逻辑：
/// - Enter 时记录玩家相对触发器的位置（哪一侧）
/// - Exit 时比较进入/离开位置：
///   * 从"远离平台侧"进、从"平台侧"出 → 上平台
///   * 从"平台侧"进、从"远离平台侧"出 → 下平台
///   * 同侧进出（碰一下回去）→ 不切换
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlatformEntryTrigger : MonoBehaviour
{
    /// <summary>平台相对于触发器的方向</summary>
    public enum PlatformSide { Up, Down, Left, Right }

    [Header("穿越方向")]
    [Tooltip("平台位于触发器的哪个方向。例如楼梯通向上方平台则选 Up，左右穿越则选 Left/Right")]
    public PlatformSide platformSide = PlatformSide.Up;

    private PlatformCollisionController platform;
    private bool enteredFromPlatformSide;
    private bool hasEntered;

    void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
    }

    /// <summary>
    /// 由 PlatformCollisionController 初始化时调用
    /// </summary>
    public void Initialize(PlatformCollisionController controller)
    {
        platform = controller;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
            return;

        enteredFromPlatformSide = IsOnPlatformSide(other.transform.position);
        hasEntered = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player") || !hasEntered)
            return;

        bool exitedFromPlatformSide = IsOnPlatformSide(other.transform.position);

        // 只有真正穿越（进出异侧）才触发
        if (!enteredFromPlatformSide && exitedFromPlatformSide)
        {
            platform?.OnPlayerEnteredEntry(true);  // 向平台方向穿越 → 上平台
        }
        else if (enteredFromPlatformSide && !exitedFromPlatformSide)
        {
            platform?.OnPlayerEnteredEntry(false); // 远离平台方向穿越 → 下平台
        }
        // 同侧进出：不做任何处理

        hasEntered = false;
    }

    /// <summary>
    /// 判断给定位置是否位于"平台那一侧"
    /// </summary>
    bool IsOnPlatformSide(Vector3 position)
    {
        Vector2 rel = (Vector2)(position - transform.position);
        switch (platformSide)
        {
            case PlatformSide.Up:    return rel.y > 0;
            case PlatformSide.Down:  return rel.y < 0;
            case PlatformSide.Left:  return rel.x < 0;
            case PlatformSide.Right: return rel.x > 0;
            default: return false;
        }
    }
}
