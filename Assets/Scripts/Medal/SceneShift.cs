using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneShift : MonoBehaviour
{
    public SceneUnlockPanel sceneUnlockPanel; // 拖入场景解锁面板
    public GameObject libraryPortal; // 拖入图书馆传送物体
    public int requiredMedalCount = 5; // 需要的勋章数量，可以在Unity Inspector中修改



    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("SceneShift Start, MedalManager: " + (MedalManager.Instance != null));
        if (MedalManager.Instance != null)
        {
            MedalManager.Instance.onMedalPanelHidden.AddListener(CheckForSceneUnlock);
            Debug.Log("Subscribed to onMedalPanelHidden");

            // 根据持久化状态初始化传送门
            if (libraryPortal != null)
            {
                libraryPortal.SetActive(MedalManager.Instance.IsLibraryUnlocked);
            }
        }
    }

    // Update is called once per frame

    private void CheckForSceneUnlock()
    {
        if (MedalManager.Instance == null) return;

        int currentMedals = MedalManager.Instance.GetMedalCount();
        bool alreadyUnlocked = MedalManager.Instance.IsLibraryUnlocked;

        Debug.Log($"CheckForSceneUnlock: medals={currentMedals}, required={requiredMedalCount}, alreadyUnlocked={alreadyUnlocked}");

        if (currentMedals >= requiredMedalCount && !alreadyUnlocked)
        {
            MedalManager.Instance.IsLibraryUnlocked = true;
            Debug.Log("Unlocking library for the first time");

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
