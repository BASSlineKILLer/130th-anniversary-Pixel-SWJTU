using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 主控角色移动（俯视角Top-Down），参考星露谷物语
/// 支持 WASD 上下左右移动
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    public float moveSpeed = 5f;

    private Rigidbody2D rb;
    private Vector2 moveInput;
    private Animator animator;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>(); // 可选：如果没有动画也可以运行

        // 安全检查：确保 Rigidbody2D 配置适合 Top-down 游戏
        if (rb != null)
        {
            rb.gravityScale = 0; // 无重力
            rb.freezeRotation = true; // 不旋转
        }
    }

    private void Update()
    {
        // 检查游戏是否暂停 (需确保项目中存在GameManager)
        if (GameManager.Instance != null && GameManager.Instance.isPaused)
            return;

        // 获取输入: Horizontal (A/D/Left/Right), Vertical (W/S/Up/Down)
        moveInput.x = Input.GetAxisRaw("Horizontal");
        moveInput.y = Input.GetAxisRaw("Vertical");

        // 归一化：防止斜向移动速度变快
        if (moveInput.magnitude > 1)
        {
            moveInput.Normalize();
        }

        // 更新动画参数（如果挂载了Animator）
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        // 物理移动放在 FixedUpdate 中
        if (rb != null)
        {
            // MovePosition 会平滑移动并在碰到碰撞体时停止
            rb.MovePosition(rb.position + moveInput * moveSpeed * Time.fixedDeltaTime);
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null) return;

        // 设置混合树(Blend Tree)参数
        // 假设 Animator 中有 MoveX, MoveY, IsMoving 参数
        if (moveInput.sqrMagnitude > 0.01f)
        {
            animator.SetFloat("MoveX", moveInput.x);
            animator.SetFloat("MoveY", moveInput.y);
            animator.SetBool("IsMoving", true);

            // 记录最后的朝向，用于Idle状态保持面向
            // 需在Animator中也添加 LastMoveX, LastMoveY 参数，如果没有可忽略
            animator.SetFloat("LastMoveX", moveInput.x);
            animator.SetFloat("LastMoveY", moveInput.y);
        }
        else
        {
            animator.SetBool("IsMoving", false);
        }
    }
}
