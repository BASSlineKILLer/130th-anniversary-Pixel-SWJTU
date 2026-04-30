using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;

public class GuidePanel : MonoBehaviour
{
    [Header("Panels")]
    public GameObject panel1;
    public GameObject panel2;
    public GameObject panel3;
    public GameObject panel4;
    public GameObject panel5;

    [Header("Other Panels for Detection")]
    // public GameObject dialoguePanel; // 对话面板

    private int currentStep = 0;
    private bool isFirstTime = true;
    // private bool dialoguePanelWasActive = false;

    private void Start()
    {
        // 检查是否第一次进入场景
        if (PlayerPrefs.GetInt("GuideCompleted", 0) == 1)
        {
            isFirstTime = false;
            return;
        }

        // 隐藏所有面板
        HideAllPanels();

        // 显示panel1
        if (panel1 != null)
        {
            panel1.SetActive(true);
        }
        currentStep = 1;

        // 添加事件监听
        if (MedalManager.Instance != null)
        {
            MedalManager.Instance.onMedalAddedForNPC.AddListener(() => {
                if (currentStep == 2) {
                    HidePanel(panel2);
                    currentStep = 3;
                }
            });
            MedalManager.Instance.onMedalPanelHidden.AddListener(() => {
                if (currentStep == 3) {
                    ShowPanel(panel3);
                    currentStep = 4;
                }
            });
        }
    }

    private void Update()
    {
        if (!isFirstTime) return;

        switch (currentStep)
        {
            case 1:
                // 等待按下WASD
                if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.R))
                {
                    HidePanel(panel1);
                    ShowPanel(panel2);
                    currentStep = 2;
                }
                break;

            case 2:
                // 等待对话面板弹出
                /*
                if (dialoguePanel != null)
                {
                    if (!dialoguePanelWasActive && dialoguePanel.activeSelf)
                    {
                        HidePanel(panel2);
                        ShowPanel(panel3);
                        currentStep = 3;
                    }
                    dialoguePanelWasActive = dialoguePanel.activeSelf;
                }
                */
                break;

            case 3:
                // 等待对话面板隐藏
                /*
                if (dialoguePanel != null)
                {
                    if (dialoguePanelWasActive && !dialoguePanel.activeSelf)
                    {
                        HidePanel(panel3);
                        currentStep = 4;
                    }
                    dialoguePanelWasActive = dialoguePanel.activeSelf;
                }
                */
                break;

            case 4:
                // 等待按Tab键
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    HidePanel(panel3);
                    currentStep = 5;
                }
                break;

            case 5:
                // 等待再按Tab键
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    ShowPanel(panel4);
                    currentStep = 6;
                }
                break;

            case 6:
                // 等待按Esc键
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    HidePanel(panel4);
                    currentStep = 7;
                }
                break;

            case 7:
                // 等待再按Esc键
                if (Input.GetKeyDown(KeyCode.Escape))
                {
                    ShowPanel(panel5);
                    currentStep = 8;
                    // 2秒后隐藏panel5并标记完成
                    Invoke("CompleteGuide", 2f);
                }
                break;
        }
    }

    private void CompleteGuide()
    {
        HidePanel(panel5);
        // 标记引导完成
        PlayerPrefs.SetInt("GuideCompleted", 1);
        PlayerPrefs.Save();
    }

    private void HideAllPanels()
    {
        if (panel1 != null) panel1.SetActive(false);
        if (panel2 != null) panel2.SetActive(false);
        if (panel3 != null) panel3.SetActive(false);
        if (panel4 != null) panel4.SetActive(false);
        if (panel5 != null) panel5.SetActive(false);
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
}
