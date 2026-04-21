using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MedalProgress : MonoBehaviour
{
    [Header("配置")]
    [Tooltip("拖入 MedalProgressConfig 资产")]
    public MedalProgressConfig config;

    [Header("UI 组件")]
    [Tooltip("进度条 Slider")]
    public Slider progressSlider;
    [Tooltip("进度百分比文本")]
    public TextMeshProUGUI progressText;

    void Start()
    {
        UpdateProgress();
        // 禁用 Slider 的用户交互
        if (progressSlider != null)
            progressSlider.interactable = false;
        // 默认隐藏面板（使用 scale）
        transform.localScale = Vector3.zero;
    }

    void Update()
    {
        UpdateProgress();
        // 按 Tab 键切换面板显示/隐藏
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool willShow = transform.localScale == Vector3.zero;
            if (willShow)
            {
                LockMovement();
            }
            else
            {
                UnlockMovement();
            }
            transform.localScale = willShow ? Vector3.one : Vector3.zero;
        }
    }

    void UpdateProgress()
    {
        if (config == null || progressSlider == null || MedalManager.Instance == null || NPCManager.Instance == null)
            return;

        // 获取总数（优先从 NPCManager，否则用配置）
        int total = NPCManager.Instance.TotalNPCs;
        if (total == 0 && config.totalNPCs > 0)
            total = config.totalNPCs;

        progressSlider.maxValue = total;

        // 当前值
        int current = MedalManager.Instance.GetMedalCount();
        progressSlider.value = current;

        // 更新进度百分比文本
        if (progressText != null && total > 0)
        {
            float percentage = (float)current / total * 100;
            progressText.text = $"进度：{percentage:F1}%";
        }

        // 节点图标已移除，不再更新
    }

    private void LockMovement()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDialogueLock(true);
        }
    }

    private void UnlockMovement()
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetDialogueLock(false);
        }
    }
}
