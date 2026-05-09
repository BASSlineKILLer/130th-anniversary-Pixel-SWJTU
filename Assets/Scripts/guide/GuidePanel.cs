using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GuidePanel : MonoBehaviour
{
    private const string GUIDE_COMPLETED_KEY = "GuideCompleted";

    public enum AdvanceMode
    {
        MovementKey,
        KeyDown,
        MedalAdded,
        MedalPanelHidden,
        DelayComplete,
        Sprint
    }

    [System.Serializable]
    public class GuideStep
    {
        public GameObject panel;
        public AdvanceMode advanceMode;
        public KeyCode keyCode;
        public float delaySeconds = 2f;
    }

    [Header("Panels")]
    public GameObject panel1;
    public GameObject panel2;
    public GameObject panel3;
    public GameObject panel4;
    public GameObject panel5;

    [Header("Steps")]
    public List<GuideStep> steps = new List<GuideStep>();

    [Header("Other Panels for Detection")]
    // public GameObject dialoguePanel; // 对话面板

    private int currentStepIndex = -1;
    private bool isFirstTime = true;
    private bool isCompleted;
    private bool wasPausedLastFrame;
    private GraphicRaycaster guideRaycaster;
    // private bool dialoguePanelWasActive = false;

    private void Start()
    {
        guideRaycaster = GetComponentInParent<GraphicRaycaster>();
        BuildLegacyStepsIfNeeded();
        HideAllPanels();

        // 检查是否第一次进入场景
        if (PlayerPrefs.GetInt(GUIDE_COMPLETED_KEY, 0) == 1)
        {
            isFirstTime = false;
            return;
        }

        ShowStep(0);

        // 添加事件监听
        if (MedalManager.Instance != null)
        {
            MedalManager.Instance.onMedalAddedForNPC.AddListener(OnMedalAdded);
            MedalManager.Instance.onMedalPanelHidden.AddListener(OnMedalPanelHidden);
        }
    }

    private void Update()
    {
        SyncRaycasterWithPauseState();

        if (!isFirstTime) return;
        if (isCompleted) return;
        if (GameManager.Instance != null && GameManager.Instance.isPaused) return;

        var step = GetCurrentStep();
        if (step == null) return;
        if (CanAdvanceByInput(step)) AdvanceStep();
    }

    private void SyncRaycasterWithPauseState()
    {
        if (guideRaycaster == null) return;
        bool isPaused = GameManager.Instance != null && GameManager.Instance.isPaused;
        if (isPaused == wasPausedLastFrame) return;

        wasPausedLastFrame = isPaused;
        guideRaycaster.enabled = !isPaused;
    }

    private void OnDestroy()
    {
        if (MedalManager.Instance == null) return;
        MedalManager.Instance.onMedalAddedForNPC.RemoveListener(OnMedalAdded);
        MedalManager.Instance.onMedalPanelHidden.RemoveListener(OnMedalPanelHidden);
    }

    private void CompleteGuide()
    {
        isCompleted = true;
        HideAllPanels();
        // 标记引导完成
        PlayerPrefs.SetInt(GUIDE_COMPLETED_KEY, 1);
        PlayerPrefs.Save();
    }

    public static void ResetGuideForNewGame()
    {
        PlayerPrefs.DeleteKey(GUIDE_COMPLETED_KEY);
        PlayerPrefs.Save();
    }

    public static void SkipGuideForContinueGame()
    {
        PlayerPrefs.SetInt(GUIDE_COMPLETED_KEY, 1);
        PlayerPrefs.Save();
    }

    private void HideAllPanels()
    {
        foreach (var step in steps)
        {
            if (step == null) continue;
            HidePanel(step.panel);
        }
    }

    private void ShowPanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(true);
        }
    }

    private void HidePanel(GameObject panel)
    {
        if (panel != null)
        {
            panel.SetActive(false);
        }
    }

    private void BuildLegacyStepsIfNeeded()
    {
        if (steps.Count > 0) return;
        steps.Add(CreateStep(panel1, AdvanceMode.MovementKey, KeyCode.None));
        steps.Add(CreateStep(panel2, AdvanceMode.MedalAdded, KeyCode.None));
        steps.Add(CreateStep(null, AdvanceMode.MedalPanelHidden, KeyCode.None));
        steps.Add(CreateStep(panel3, AdvanceMode.KeyDown, KeyCode.Tab));
        steps.Add(CreateStep(null, AdvanceMode.KeyDown, KeyCode.Tab));
        steps.Add(CreateStep(panel4, AdvanceMode.KeyDown, KeyCode.Escape));
        steps.Add(CreateStep(null, AdvanceMode.KeyDown, KeyCode.Escape));
        steps.Add(CreateStep(panel5, AdvanceMode.DelayComplete, KeyCode.None));
    }

    private GuideStep CreateStep(GameObject panel, AdvanceMode advanceMode, KeyCode keyCode)
    {
        return new GuideStep { panel = panel, advanceMode = advanceMode, keyCode = keyCode };
    }

    private GuideStep GetCurrentStep()
    {
        if (currentStepIndex < 0) return null;
        if (currentStepIndex >= steps.Count) return null;
        return steps[currentStepIndex];
    }

    private bool CanAdvanceByInput(GuideStep step)
    {
        if (step.advanceMode == AdvanceMode.MovementKey) return IsMovementKeyDown();
        if (step.advanceMode == AdvanceMode.KeyDown) return Input.GetKeyDown(step.keyCode);
        if (step.advanceMode == AdvanceMode.Sprint) return IsSprinting();
        return false;
    }

    private bool IsMovementKeyDown()
    {
        return Input.GetKeyDown(KeyCode.W)
            || Input.GetKeyDown(KeyCode.A)
            || Input.GetKeyDown(KeyCode.S)
            || Input.GetKeyDown(KeyCode.D);
    }

    private bool IsSprinting()
    {
        return Input.GetKey(KeyCode.LeftShift) && IsMovementKeyHeld();
    }

    private bool IsMovementKeyHeld()
    {
        return Input.GetKey(KeyCode.W)
            || Input.GetKey(KeyCode.A)
            || Input.GetKey(KeyCode.S)
            || Input.GetKey(KeyCode.D);
    }

    private void OnMedalAdded()
    {
        AdvanceByMode(AdvanceMode.MedalAdded);
    }

    private void OnMedalPanelHidden()
    {
        AdvanceByMode(AdvanceMode.MedalPanelHidden);
    }

    private void AdvanceByMode(AdvanceMode advanceMode)
    {
        var step = GetCurrentStep();
        if (step == null) return;
        if (step.advanceMode != advanceMode) return;
        AdvanceStep();
    }

    private void ShowStep(int stepIndex)
    {
        currentStepIndex = stepIndex;
        if (currentStepIndex >= steps.Count)
        {
            CompleteGuide();
            return;
        }

        var step = GetCurrentStep();
        if (step == null)
        {
            ShowStep(currentStepIndex + 1);
            return;
        }

        ShowPanel(step.panel);
        if (step.advanceMode == AdvanceMode.DelayComplete)
        {
            Invoke(nameof(CompleteGuide), step.delaySeconds);
        }
    }

    private void AdvanceStep()
    {
        var step = GetCurrentStep();
        if (step == null) return;
        HidePanel(step.panel);
        ShowStep(currentStepIndex + 1);
    }
}
