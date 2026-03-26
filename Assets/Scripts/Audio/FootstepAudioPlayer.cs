using UnityEngine;
using FMODUnity;

namespace Player
{
    /// <summary>
    /// 播放玩家脚步声。
    /// 根据脚下 Tilemap 瓷砖类型设置 FMOD material 参数，播放对应音效。
    /// 
    /// 触发方式（二选一，在 Inspector 中设置 useAnimationEvent）：
    ///   true  → 由 Walk 动画的 Animation Event 调用 PlayFootstep()
    ///   false → 按移动距离自动触发（每走 stepDistance 单位播放一次）
    /// </summary>
    public class FootstepAudioPlayer : MonoBehaviour
    {
        [Header("FMOD 设置")]
        [Tooltip("FMOD 脚步声事件路径")]
        public EventReference footstepEvent;

        [Tooltip("FMOD 参数名（与 FMOD Studio 中的 material 参数一致）")]
        public string materialParamName = "material";

        [Header("材质检测")]
        [Tooltip("材质检测组件（场景中挂载 FootstepMaterialDetector 的对象）")]
        public FootstepMaterialDetector materialDetector;

        [Header("触发模式")]
        [Tooltip("true = 由动画事件手动调用 PlayFootstep()；false = 按距离自动触发")]
        public bool useAnimationEvent = false;

        [Tooltip("自动触发模式下，每走多远播放一次脚步声（世界单位）")]
        public float stepDistance = 0.5f;

        [Tooltip("两次脚步声之间的最短间隔（秒），防止过密")]
        public float minInterval = 0.2f;

        private PlayerController playerController;
        private Vector3 lastStepPosition;
        private float lastStepTime;

        private void Awake()
        {
            playerController = GetComponent<PlayerController>();
        }

        private void Start()
        {
            lastStepPosition = transform.position;
        }

        private void Update()
        {
            if (useAnimationEvent) return;

            // 自动触发模式：仅在移动中且游戏未暂停时检测
            if (!IsPlayerMoving()) return;

            float dist = Vector3.Distance(transform.position, lastStepPosition);
            if (dist >= stepDistance)
            {
                PlayFootstep();
                lastStepPosition = transform.position;
            }
        }

        /// <summary>
        /// 播放一次脚步声。可由 Animation Event 或自动触发调用。
        /// </summary>
        public void PlayFootstep()
        {
            if (footstepEvent.IsNull) return;

            // 暂停 / 对话锁定 时静音
            if (GameManager.Instance != null &&
                (GameManager.Instance.isPaused || GameManager.Instance.isDialogueLocked))
                return;

            // 最短间隔保护
            if (Time.time - lastStepTime < minInterval) return;
            lastStepTime = Time.time;

            // 获取脚下材质
            int material = FootstepMaterialDetector.MATERIAL_ROAD;
            if (materialDetector != null)
                material = materialDetector.GetMaterialAtPosition(transform.position);

            FootstepAudioHelper.Play(footstepEvent, materialParamName, material, transform.position);
        }

        private bool IsPlayerMoving()
        {
            if (playerController == null) return false;
            return playerController.MoveInput.sqrMagnitude > 0.001f;
        }
    }
}
