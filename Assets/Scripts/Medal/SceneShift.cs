using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneShift : MonoBehaviour
{
    public SceneUnlockPanel sceneUnlockPanel; // 拖入场景解锁面板
    public GameObject libraryPortal; // 拖入图书馆传送物体
    public int requiredMedalCount = 5; // 需要的勋章数量，可以在Unity Inspector中修改

    private bool libraryUnlocked = false;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("SceneShift Start, MedalManager: " + (MedalManager.Instance != null));
        if (MedalManager.Instance != null)
        {
            MedalManager.Instance.onMedalPanelHidden.AddListener(CheckForSceneUnlock);
            Debug.Log("Subscribed to onMedalPanelHidden");
        }
        if (libraryPortal != null)
        {
            libraryPortal.SetActive(false);
        }
        // 移除对sceneUnlockPanel.gameObject.SetActive(false)的调用，因为SceneUnlockPanel自己会在Start中处理初始隐藏
    }

    // Update is called once per frame

    private void CheckForSceneUnlock()
    {
        Debug.Log("CheckForSceneUnlock called, medal count: " + MedalManager.Instance.GetMedalCount() + ", unlocked: " + libraryUnlocked);
        if (MedalManager.Instance.GetMedalCount() >= requiredMedalCount && !libraryUnlocked)
        {
            libraryUnlocked = true;
            Debug.Log("Unlocking library");
            if (libraryPortal != null)
            {
                libraryPortal.SetActive(true);
            }

            if (sceneUnlockPanel != null)
            {
                Debug.Log("Starting ShowPanelWithFade for SceneUnlockPanel");
                StartCoroutine(sceneUnlockPanel.ShowPanelWithFade("###图书馆已解锁###"));
            }
        }
    }
}
