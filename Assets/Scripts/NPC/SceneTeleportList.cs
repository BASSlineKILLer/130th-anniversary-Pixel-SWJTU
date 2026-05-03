using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SceneTeleportList : MonoBehaviour
{
    public SceneTeleportListConfig config;
    public Transform itemParent;
    public Button itemPrefab;
    public GameObject panel;

    private void OnEnable()
    {
        AutoBindReferences();

        if (config == null)
        {
            Debug.LogError("[SceneTeleportList] 未配置 SceneTeleportListConfig 资产！");
            return;
        }
        if (itemParent == null || itemPrefab == null)
        {
            Debug.LogError("[SceneTeleportList] UI引用未完全配置 (itemParent 或 itemPrefab 为空)！");
            return;
        }

        // 清空现有的按钮
        foreach (Transform child in itemParent)
        {
            Destroy(child.gameObject);
        }

        // 生成配置的传送列表项
        if (config.entries != null)
        {
            foreach (var entry in config.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.sceneName)) continue;
                Button btn = Instantiate(itemPrefab, itemParent);
                
                // 更新图标（如果有）
                if (entry.icon != null)
                {
                    Image iconImage = btn.transform.Find("Icon")?.GetComponent<Image>();
                    if (iconImage == null)
                    {
                        // 尝试直接获取Image组件，如果没有Icon子物体
                        var images = btn.GetComponentsInChildren<Image>();
                        foreach (var img in images)
                        {
                            if (img.gameObject != btn.gameObject) // 假设子物体是Icon
                            {
                                iconImage = img;
                                break;
                            }
                        }
                    }
                    
                    if (iconImage != null)
                    {
                        iconImage.sprite = entry.icon;
                        iconImage.gameObject.SetActive(true);
                    }
                }
                
                // 更新显示文字
                TextMeshProUGUI label = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = !string.IsNullOrEmpty(entry.displayName) ? entry.displayName : entry.sceneName;
                }
                else
                {
                    // 兼容旧版 Text
                    Text oldText = btn.GetComponentInChildren<Text>();
                    if (oldText != null)
                    {
                        oldText.text = !string.IsNullOrEmpty(entry.displayName) ? entry.displayName : entry.sceneName;
                    }
                }
                
                // 绑定点击事件
                btn.onClick.AddListener(() => Teleport(entry));
            }
        }
        Debug.Log($"[SceneTeleportList] 已生成传送列表：{itemParent.childCount} 项");
    }

    private void AutoBindReferences()
    {
        if (panel == null) panel = gameObject;
        if (itemParent == null)
        {
            var content = transform.Find("Content");
            itemParent = content != null ? content : transform;
        }
    }

    private void Teleport(SceneTeleportListConfig.Entry entry)
    {
        // 关面板
        if (panel != null)
        {
            panel.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false); // 如果没配panel，关闭自身
        }
            
        // 传送到目标场景
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(entry.sceneName, entry.spawnPointId);
        }
        else
        {
            Debug.LogError("[SceneTeleportList] SceneTransitionManager.Instance 为空，无法传送");
        }
    }
}