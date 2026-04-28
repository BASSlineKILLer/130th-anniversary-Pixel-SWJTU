using UnityEngine;
using FMODUnity;

/// <summary>
/// NPC 自由走动 + 走路抖动效果。
/// 由 NPCController.SetData() 自动初始化。
/// 纯 Transform 移动，不使用 Rigidbody。
/// </summary>
public class NPCWalk : MonoBehaviour
{
    [Header("移动概率")]
    [Range(0f, 1f)]
    [Tooltip("该 NPC 会走动的概率")]
    public float moveProbability = 0.3f;

    [Header("移动设置")]
    public float moveSpeed = 1.0f;
    [Tooltip("每次停下后等待的随机时间范围（秒）")]
    public Vector2 idleTimeRange = new Vector2(2f, 5f);
    [Tooltip("以出生点为中心的漫步半径")]
    public float roamRadius = 3f;

    [Header("走路抖动")]
    [Tooltip("抖动幅度（世界单位）")]
    public float bobAmplitude = 0.06f;
    [Tooltip("抖动频率（越大越快）")]
    public float bobFrequency = 8f;

    [Header("障碍检测")]
    [Tooltip("视为障碍的层（默认 Everything；建议剔除 NPC 自身所在层避免互相阻挡）")]
    public LayerMask obstacleLayers = ~0;
    [Tooltip("Cast 检测距离的额外冗余（避免贴墙抖动）")]
    public float obstacleSkin = 0.02f;
    [Tooltip("连续碰壁多少次后放弃挣扎、进入 idle 等待，避免死角内反复 cast")]
    [Min(1)]
    public int maxBlockedAttempts = 3;
    [Tooltip("反弹目标相对于障碍反方向的随机夹角上限（度）")]
    [Range(0f, 180f)]
    public float reboundSpreadDegrees = 60f;

    [Header("脚步声")]
    [Tooltip("FMOD 脚步声事件")]
    public EventReference footstepEvent;
    [Tooltip("材质检测组件")]
    public FootstepMaterialDetector materialDetector;
    [Tooltip("FMOD material 参数名")]
    public string materialParamName = "material";

    private float prevBobSin;

    private bool canMove;
    private bool isMoving;
    private Vector2 spawnOrigin;
    private Vector2 targetPosition;
    private float idleTimer;

    /// <summary>
    /// 逻辑位置（不含 bob 偏移），用于游戏逻辑计算。
    /// transform.position = logicalPosition + bob偏移
    /// </summary>
    private Vector2 logicalPosition;
    private SpriteRenderer spriteRenderer;
    private bool isDialoguePaused;
    private Rigidbody2D rb;
    private ContactFilter2D obstacleFilter;
    // 多到 4 是为了在出生点附近有多个 NPC 重叠时，能把它们都过滤掉，看到真正的墙
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[4];
    private int consecutiveBlockedAttempts;

    public void Init(Vector3 origin)
    {
        spawnOrigin = origin;
        logicalPosition = origin;
        spriteRenderer = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        obstacleFilter = new ContactFilter2D
        {
            useTriggers = false,
            useLayerMask = true,
            layerMask = obstacleLayers,
        };

        canMove = Random.value < moveProbability;

        if (canMove)
        {
            ResetIdleTimer();
            ChooseNewTarget();
        }
    }

    /// <summary>
    /// 沿 moveDir 方向 step 距离上是否有墙体/玩家阻挡。
    /// 使用 Rigidbody2D.Cast 自动排除自身 collider；命中其它 NPC 也忽略，避免互相锁死。
    /// </summary>
    private bool IsBlocked(Vector2 moveDir, float step)
    {
        if (rb == null) return false;
        int hitCount = rb.Cast(moveDir, obstacleFilter, castHits, step + obstacleSkin);
        for (int i = 0; i < hitCount; i++)
        {
            var hitCollider = castHits[i].collider;
            if (hitCollider == null) continue;
            // 同类 NPC 不视为障碍：他们之间允许互相穿过
            if (hitCollider.GetComponentInParent<NPCController>() != null) continue;
            return true;
        }
        return false;
    }

    private void Update()
    {
        if (!canMove) return;

        if (isDialoguePaused)
        {
            SyncPosition();
            return;
        }

        if (isMoving)
        {
            // --- 移动 ---
            Vector2 direction = targetPosition - logicalPosition;
            float distRemaining = direction.magnitude;

            if (distRemaining < 0.05f)
            {
                isMoving = false;
                ResetIdleTimer();
                SyncPosition();
                return;
            }

            Vector2 moveDir = direction.normalized;
            float step = moveSpeed * Time.deltaTime;
            if (step > distRemaining) step = distRemaining;

            // 障碍预检：撞墙 / 撞玩家则朝反方向重挑目标继续走；连续碰壁超过上限才 idle
            if (IsBlocked(moveDir, step))
            {
                HandleBlocked(moveDir);
                return;
            }
            consecutiveBlockedAttempts = 0;

            logicalPosition += moveDir * step;

            // --- 翻转（仅翻转 Sprite，不影响子物体如气泡） ---
            if (spriteRenderer != null && Mathf.Abs(moveDir.x) > 0.01f)
                spriteRenderer.flipX = moveDir.x > 0;

            // --- 视觉位置 = 逻辑位置 + bob ---
            float curSin = Mathf.Sin(Time.time * bobFrequency);
            float bob = Mathf.Abs(curSin) * bobAmplitude;
            transform.position = new Vector3(logicalPosition.x, logicalPosition.y + bob, transform.position.z);

            // bob 正弦波过零点 = 一步落地，触发脚步声
            if (prevBobSin < 0f && curSin >= 0f)
                PlayFootstep();
            prevBobSin = curSin;
        }
        else
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                ChooseNewTarget();
                isMoving = true;
            }
        }
    }

    /// <summary>
    /// 停止移动时，把 transform 同步到逻辑位置（去掉 bob）
    /// </summary>
    private void SyncPosition()
    {
        transform.position = new Vector3(logicalPosition.x, logicalPosition.y, transform.position.z);
    }

    public void SetDialoguePaused(bool paused)
    {
        isDialoguePaused = paused;
        if (paused)
        {
            isMoving = false;
            SyncPosition();
        }
    }

    private void ChooseNewTarget()
    {
        Vector2 randomOffset = Random.insideUnitCircle * roamRadius;
        targetPosition = spawnOrigin + randomOffset;
    }

    /// <summary>
    /// 碰到障碍的处理：
    /// - 未超上限：朝障碍反方向的半平面随机取点，立刻继续走（看起来像转身绕开）
    /// - 超出上限：放弃挣扎，停下 idle，等下一轮计时器重新从 spawnOrigin 周围挑目标
    /// </summary>
    private void HandleBlocked(Vector2 blockedDir)
    {
        consecutiveBlockedAttempts++;
        if (consecutiveBlockedAttempts >= maxBlockedAttempts)
        {
            consecutiveBlockedAttempts = 0;
            isMoving = false;
            ResetIdleTimer();
            SyncPosition();
            return;
        }

        targetPosition = logicalPosition + RandomDirectionAwayFrom(blockedDir) * Random.Range(roamRadius * 0.3f, roamRadius);
    }

    /// <summary>以 -blockedDir 为中心，在 ±reboundSpreadDegrees 扇形内随机返回一个单位方向</summary>
    private Vector2 RandomDirectionAwayFrom(Vector2 blockedDir)
    {
        Vector2 away = -blockedDir.normalized;
        float angleRad = Random.Range(-reboundSpreadDegrees, reboundSpreadDegrees) * Mathf.Deg2Rad;
        float cos = Mathf.Cos(angleRad);
        float sin = Mathf.Sin(angleRad);
        return new Vector2(away.x * cos - away.y * sin, away.x * sin + away.y * cos);
    }

    private void ResetIdleTimer()
    {
        idleTimer = Random.Range(idleTimeRange.x, idleTimeRange.y);
    }

    private void PlayFootstep()
    {
        int material = FootstepMaterialDetector.MATERIAL_ROAD;
        if (materialDetector != null)
            material = materialDetector.GetMaterialAtPosition(transform.position);

        FootstepAudioHelper.Play(footstepEvent, materialParamName, material, transform.position);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Vector3 center = Application.isPlaying ? (Vector3)spawnOrigin : transform.position;
        Gizmos.DrawWireSphere(center, roamRadius);
    }
}

