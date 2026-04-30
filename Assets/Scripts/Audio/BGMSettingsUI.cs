using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// BGM 设置 UI 组件
/// 挂载到包含 Slider 和 Dropdown 的面板 GameObject 上。
/// 可复用：主菜单设置面板和暂停菜单各放一个。
/// </summary>
public class BGMSettingsUI : MonoBehaviour
{
    [Header("UI 引用")]
    [Tooltip("音量滑块")]
    [SerializeField] private Slider volumeSlider;

    [Tooltip("BGM 选择下拉列表（TMP 版本）")]
    [SerializeField] private TMP_Dropdown bgmDropdown;

    [Tooltip("（备用）BGM 选择下拉列表（旧版 UI Dropdown）")]
    [SerializeField] private Dropdown bgmDropdownLegacy;

    private void OnEnable()
    {
        if (BGMManager.Instance == null) return;

        InitSlider();
        InitDropdown();
    }

    private void OnDisable()
    {
        RemoveListeners();
        // 设置PlayerPrefs标记Setting面板已关闭，用于跨场景引导检测
        PlayerPrefs.SetInt("SettingPanelClosed", 1);
        PlayerPrefs.Save();
    }

    private void InitSlider()
    {
        if (volumeSlider == null) return;

        volumeSlider.minValue = 0f;
        volumeSlider.maxValue = 1f;
        volumeSlider.SetValueWithoutNotify(BGMManager.Instance.GetVolume());
        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
    }

    private void InitDropdown()
    {
        string[] names = BGMManager.Instance.GetBGMNames();
        int currentIndex = BGMManager.Instance.GetCurrentBGMIndex();

        if (bgmDropdown != null)
        {
            bgmDropdown.ClearOptions();
            bgmDropdown.AddOptions(new List<string>(names));
            bgmDropdown.SetValueWithoutNotify(currentIndex);
            bgmDropdown.onValueChanged.AddListener(OnBGMSelected);
            return;
        }

        if (bgmDropdownLegacy != null)
        {
            bgmDropdownLegacy.ClearOptions();
            bgmDropdownLegacy.AddOptions(new List<string>(names));
            bgmDropdownLegacy.SetValueWithoutNotify(currentIndex);
            bgmDropdownLegacy.onValueChanged.AddListener(OnBGMSelected);
        }
    }

    private void RemoveListeners()
    {
        if (volumeSlider != null)
            volumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);

        if (bgmDropdown != null)
            bgmDropdown.onValueChanged.RemoveListener(OnBGMSelected);

        if (bgmDropdownLegacy != null)
            bgmDropdownLegacy.onValueChanged.RemoveListener(OnBGMSelected);
    }

    private void OnVolumeChanged(float value)
    {
        if (BGMManager.Instance != null)
            BGMManager.Instance.SetVolume(value);
    }

    private void OnBGMSelected(int index)
    {
        if (BGMManager.Instance != null)
            BGMManager.Instance.SelectBGM(index);
    }
}
