using UnityEngine;

/// <summary>
/// NPC状态机
/// 状态：Idle（不动，上下抖动）/ Wander（四处走动）
/// 约30%的NPC走动，70%静止
/// </summary>
public class NPCStateMachine : MonoBehaviour
{
    public enum NPCState
    {
        Idle,
        Wander
    }

    [Header("状态设置")]
    public NPCState currentState = NPCState.Idle;

    [Header("走动设置")]
    public float wanderSpeed = 1.5f;
    public float wanderRadius = 3f;
    public float wanderInterval = 3f;

    [Header("抖动设置")]
    public float bobAmplitude = 0.05f;
    public float bobFrequency = 2f;

    private Vector3 startPosition;
    private Vector3 wanderTarget;
    private float wanderTimer;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        startPosition = transform.position;
    }

    private void Start()
    {
        // 随机决定30%走动，70%静止
        NPCController npcController = GetComponent<NPCController>();
        if (npcController != null && npcController.canWander)
        {
            currentState = Random.value < 0.3f ? NPCState.Wander : NPCState.Idle;
        }

        wanderTarget = startPosition;
    }

    private void Update()
    {
        switch (currentState)
        {
            case NPCState.Idle:
                IdleBob();
                break;
            case NPCState.Wander:
                Wander();
                break;
        }
    }

    private void IdleBob()
    {
        // 上下抖动效果
        float offsetY = Mathf.Sin(Time.time * bobFrequency) * bobAmplitude;
        transform.position = startPosition + new Vector3(0, offsetY, 0);
    }

    private void Wander()
    {
        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0f)
        {
            // 选择新的随机目标点
            Vector2 randomDir = Random.insideUnitCircle * wanderRadius;
            wanderTarget = startPosition + new Vector3(randomDir.x, randomDir.y, 0);
            wanderTimer = wanderInterval;
        }

        // 移动向目标点
        transform.position = Vector3.MoveTowards(transform.position, wanderTarget, wanderSpeed * Time.deltaTime);

        // 到达后上下抖动
        if (Vector3.Distance(transform.position, wanderTarget) < 0.1f)
        {
            IdleBob();
        }
    }
}
