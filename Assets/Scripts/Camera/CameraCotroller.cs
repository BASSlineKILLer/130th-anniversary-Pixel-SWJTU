using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 警告：类名 "Camera" 与 UnityEngine.Camera 冲突。建议在 Unity 中将文件名和类名都改为 "CameraController"。
public class CameraCotroller : MonoBehaviour
{
    [Header("跟随目标")]
    public Transform target;        // 要跟随的主角
    public float smoothing = 5f;    // 移动平滑度

    [Header("移动范围限制")]
    public Vector2 minPosition;     // 允许移动到的左下角极限坐标
    public Vector2 maxPosition;     // 允许移动到的右上角极限坐标


    // Start is called before the first frame update
    void Start()
    {
        // 自动修正：如果范围都是0（用户忘记设置），则给一个很大的默认范围，防止相机卡死不动
        if (minPosition == Vector2.zero && maxPosition == Vector2.zero)
        {
            minPosition = new Vector2(-100, -100);
            maxPosition = new Vector2(100, 100);
        }

        // 自动修正：如果相机和物体在同一个Z平面（比如都是0），正交相机可能拍不到。
        // 强制把相机拉远一点（Z = -10 是2D游戏的标准值）
        if (Mathf.Abs(transform.position.z) < 1f)
        {
            Debug.LogWarning("CameraCotroller: 检测到相机Z轴太靠近0，自动调整为 -10 以便能看到物体。");
            transform.position = new Vector3(transform.position.x, transform.position.y, -10f);
        }
    }

    // 使用 LateUpdate 确保在目标移动之后再跟随，防止抖动
    void LateUpdate()
    {
        if (target != null)
        {
            // 目标位置（默认保持相机自己的 Z 轴）
            Vector3 targetPos = target.position;
            targetPos.z = transform.position.z;

            // 限制 X 和 Y 轴的移动范围
            targetPos.x = Mathf.Clamp(targetPos.x, minPosition.x, maxPosition.x);
            targetPos.y = Mathf.Clamp(targetPos.y, minPosition.y, maxPosition.y);

            // 平滑移动到目标位置
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothing * Time.deltaTime);
        }
    }
}
