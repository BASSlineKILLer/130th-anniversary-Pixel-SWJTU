using UnityEngine;

public class WebGLRuntimeBridge : MonoBehaviour
{
    private const int TargetFrameRate = 60;
    private static WebGLRuntimeBridge instance;
    private bool pausedByBrowser;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureInstance()
    {
        if (instance != null) return;

        var go = new GameObject(nameof(WebGLRuntimeBridge));
        instance = go.AddComponent<WebGLRuntimeBridge>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        Application.runInBackground = true;
        Application.targetFrameRate = TargetFrameRate;
    }

    public void OnBrowserVisibilityChanged(string visibleFlag)
    {
        bool isVisible = visibleFlag == "1";
        if (isVisible)
            ResumeAudio();
        else
            PauseAudio();
    }

    private void PauseAudio()
    {
        if (pausedByBrowser) return;

        AudioListener.pause = true;
        pausedByBrowser = true;
        BGMManager.Instance?.SetPausedByBrowser(true);
    }

    private void ResumeAudio()
    {
        if (!pausedByBrowser) return;

        AudioListener.pause = false;
        pausedByBrowser = false;
        BGMManager.Instance?.SetPausedByBrowser(false);
    }
}
