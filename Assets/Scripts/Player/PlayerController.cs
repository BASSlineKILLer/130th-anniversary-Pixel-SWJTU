using UnityEngine;
using UnityEngine.Serialization;

namespace Player
{
    /// <summary>
    /// 主控角色移动（俯视角Top-Down），参考星露谷物语
    /// 支持 WASD 上下左右移动
    /// </summary>
    public class PlayerController : MonoBehaviour
    {
        [Header("移动设置")]
        public float moveSpeed = 5f;

        public Rigidbody2D rb;
        public Vector2 moveInput;
        public SpriteRenderer spriteRenderer;
        public Animator playerAnim;

        private void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            playerAnim = GetComponent<Animator>();

            // 安全检查：确保 Rigidbody2D 配置适合 Top-down 游戏
            if (rb != null)
            {
                rb.gravityScale = 0; // 无重力
                rb.freezeRotation = true; // 不旋转
            }
        }

        private void Update()
        {
            // 检查游戏是否暂停或被对话锁定
            if (GameManager.Instance != null &&
                (GameManager.Instance.isPaused || GameManager.Instance.isDialogueLocked))
            {
                rb.velocity = Vector2.zero; // 暂停时重置速度
                return;
            }

            // 1. 获取输入
            moveInput.x = Input.GetAxisRaw("Horizontal");
            moveInput.y = Input.GetAxisRaw("Vertical");

            // 2. 归一化：防止斜向移动速度变快
            if (moveInput.magnitude > 1)
            {
                moveInput.Normalize();
            }

            // 3. 执行移动和动画逻辑
            PlayerMove();
        }

        void PlayerMove()
        {
            // 使用归一化后的 moveInput 来设置速度
            rb.velocity = moveInput * moveSpeed;

            // 只要有任何移动输入，isMoving 就为 true
            bool isMoving = moveInput.sqrMagnitude > 0.001f;
            
            if (playerAnim != null)
            {
                playerAnim.SetBool("IsMoving", isMoving);
            }

            // 处理翻转：只有在水平移动时才改变缩放
            if (Mathf.Abs(moveInput.x) > 0.001f)
            {
                // 如果 moveInput.x > 0 (向右)，localScale.x 为 -1 (假设素材默认向左)
                // 或者根据你的素材朝向调整：1 : -1
                float direction = moveInput.x > 0 ? -1f : 1f;
                transform.localScale = new Vector3(direction, transform.localScale.y, transform.localScale.z);
            }
        }
        
    }
}
