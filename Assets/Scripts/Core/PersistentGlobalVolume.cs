using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// 挂载到 Global Volume 对象上，确保跨场景不被销毁。
/// 每次场景加载后自动为 Main Camera 启用 Post Processing。
/// </summary>
public class PersistentGlobalVolume : MonoBehaviour
{
    private static PersistentGlobalVolume instance;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnablePostProcessingOnMainCamera();
    }

    private static void EnablePostProcessingOnMainCamera()
    {
        var cam = Camera.main;
        if (cam == null) return;

        var urpData = cam.GetComponent<UniversalAdditionalCameraData>();
        if (urpData == null) return;

        if (!urpData.renderPostProcessing)
        {
            urpData.renderPostProcessing = true;
            Debug.Log($"[PersistentGlobalVolume] 已为 {cam.name} 启用 Post Processing");
        }
    }
}
