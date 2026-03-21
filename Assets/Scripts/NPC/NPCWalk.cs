using UnityEngine;

namespace NPC
{
    public class NpcWalk : MonoBehaviour
    {
        [Header("移动设置")]
        public float moveSpeed = 1.5f;
        [Tooltip("决策时间范围（秒），例如在原地等待或走向下一个点之前")]
        public Vector2 decisionTimeRange = new Vector2(2, 5);

        [Header("移动区域")]
        [Tooltip("NPC移动的中心点")]
        public Transform movementAreaCenter;
        [Tooltip("NPC移动区域的大小")]
        public Vector2 movementAreaSize = new Vector2(10, 10);

        private Rigidbody2D rb;
        private Vector2 targetPosition;
        private float decisionTimer;
        private bool isMoving;

        // 决定NPC是否应该移动
        private bool canMove;

        void Start()
        {
            rb = GetComponent<Rigidbody2D>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody2D>();
            }
            
            // 关键：设置为 Kinematic 模式，防止其与主角发生物理挤压或阻挡
            // 同时确保不会受重力影响，也不会被其他物理力推走
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0;
            rb.freezeRotation = true;
            // 禁用通过力进行碰撞反馈，这样它就不会把主角“顶”走
            rb.useFullKinematicContacts = false;

            // 30%的概率让NPC可以移动
            if (Random.value < 0.3f)
            {
                canMove = true;
            }

            if (movementAreaCenter == null)
            {
                // 如果没有设置移动区域中心，则使用NPC自己的初始位置
                var areaCenterObject = new GameObject($"{name}_MovementArea");
                areaCenterObject.transform.position = transform.position;
                movementAreaCenter = areaCenterObject.transform;
            }

            ResetDecisionTimer();
            if (canMove)
            {
                ChooseNewTargetPosition();
            }
        }

        void Update()
        {
            if (!canMove)
            {
                return;
            }

            decisionTimer -= Time.deltaTime;

            if (decisionTimer <= 0)
            {
                ResetDecisionTimer();
                // 随机决定是移动还是站立
                isMoving = Random.value > 0.5f; 
                if (isMoving)
                {
                    ChooseNewTargetPosition();
                }
            }

            if (isMoving)
            {
                // 如果接近目标点，则停下等待下一次决策
                if (Vector2.Distance(transform.position, targetPosition) < 0.1f)
                {
                    isMoving = false;
                }
            }
        }

        private void FixedUpdate()
        {
            if (isMoving && canMove)
            {
                Vector2 currentPos = transform.position;
                Vector2 moveDirection = (targetPosition - currentPos).normalized;
                Vector2 nextPos = currentPos + moveDirection * (moveSpeed * Time.fixedDeltaTime);
                
                // 使用 MovePosition 移动 Kinematic 刚体
                rb.MovePosition(nextPos);

                // 处理翻转：只有在水平移动时才改变缩放 (假设初始朝向向左)
                if (Mathf.Abs(moveDirection.x) > 0.01f)
                {
                    float direction = moveDirection.x > 0 ? -1f : 1f;
                    transform.localScale = new Vector3(direction, 1f, 1f);
                }
            }
        }

        void ResetDecisionTimer()
        {
            decisionTimer = Random.Range(decisionTimeRange.x, decisionTimeRange.y);
        }

        void ChooseNewTargetPosition()
        {
            float randomX = Random.Range(-movementAreaSize.x / 2, movementAreaSize.x / 2);
            float randomY = Random.Range(-movementAreaSize.y / 2, movementAreaSize.y / 2);
            targetPosition = (Vector2)movementAreaCenter.position + new Vector2(randomX, randomY);
        }

        // 在编辑器中绘制出移动范围，方便调试
        private void OnDrawGizmosSelected()
        {
            if (movementAreaCenter != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireCube(movementAreaCenter.position, movementAreaSize);
            }
        }
    }
}

