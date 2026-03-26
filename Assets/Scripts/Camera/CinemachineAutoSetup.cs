using Cinemachine;
using UnityEngine;

/// <summary>
/// Cinemachine 2D 自动配置脚本
/// 挂载到 Main Camera 上，运行时自动创建虚拟相机并配置跟随与边界。
/// 通过代码配置，绕过 Cinemachine 编辑器 Inspector 的已知 bug。
/// </summary>
public class CinemachineAutoSetup : MonoBehaviour
{
    [Header("跟随目标")]
    [SerializeField] private Transform followTarget;

    [Header("相机参数")]
    [SerializeField] private float damping = 1.5f;
    [SerializeField] private float orthographicSize = 5f;
    [SerializeField] private float cameraDistance = 10f;
    [SerializeField] private float deadZoneWidth = 0.05f;
    [SerializeField] private float deadZoneHeight = 0.05f;

    [Header("边界限制（需要一个带 PolygonCollider2D 的 GameObject）")]
    [SerializeField] private Collider2D boundsCollider;
    [SerializeField] private float confinerDamping = 0.3f;

    private void Awake()
    {
        EnsureBrain();
        var vcam = CreateVirtualCamera();
        ConfigureBody(vcam);
        ConfigureConfiner(vcam);
    }

    private void EnsureBrain()
    {
        if (GetComponent<CinemachineBrain>() != null) return;
        gameObject.AddComponent<CinemachineBrain>();
    }

    private CinemachineVirtualCamera CreateVirtualCamera()
    {
        var vcamGO = new GameObject("CM_VirtualCamera_2D");
        var vcam = vcamGO.AddComponent<CinemachineVirtualCamera>();
        vcam.Follow = followTarget;
        vcam.m_Lens.OrthographicSize = orthographicSize;
        return vcam;
    }

    private void ConfigureBody(CinemachineVirtualCamera vcam)
    {
        var body = vcam.AddCinemachineComponent<CinemachineFramingTransposer>();
        body.m_XDamping = damping;
        body.m_YDamping = damping;
        body.m_ZDamping = 0f;
        body.m_CameraDistance = cameraDistance;
        body.m_DeadZoneWidth = deadZoneWidth;
        body.m_DeadZoneHeight = deadZoneHeight;
        body.m_ScreenX = 0.5f;
        body.m_ScreenY = 0.5f;
        body.m_LookaheadTime = 0f;
    }

    private void ConfigureConfiner(CinemachineVirtualCamera vcam)
    {
        if (boundsCollider == null) return;

        var confiner = vcam.gameObject.AddComponent<CinemachineConfiner>();
        confiner.m_BoundingShape2D = boundsCollider;
        confiner.m_Damping = confinerDamping;
        confiner.m_ConfineScreenEdges = true;
    }
}
