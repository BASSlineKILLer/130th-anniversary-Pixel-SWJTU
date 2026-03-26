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

    public void Init(Vector3 origin)
    {
        spawnOrigin = origin;
        logicalPosition = origin;
        spriteRenderer = GetComponent<SpriteRenderer>();

        canMove = Random.value < moveProbability;

        if (canMove)
        {
            ResetIdleTimer();
            ChooseNewTarget();
        }
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

