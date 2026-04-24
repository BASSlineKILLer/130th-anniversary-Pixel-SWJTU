using UnityEditor;
using UnityEngine;
using System.IO;

/// <summary>
/// NPCDatabase 自定义 Inspector
/// 提供可视化 NPC 数据库管理：条目卡片预览、快速新建 / 删除、API 设置
/// </summary>
[CustomEditor(typeof(NPCDatabase))]
public class NPCDatabaseEditor : Editor
{
    private const long KB = 1024L;
    private const long MB = KB * 1024L;

    private SerializedProperty manualEntries;
    private SerializedProperty enableApiFetch;
    private SerializedProperty apiUrl;
    private SerializedProperty clearCacheOnStart;

    private Vector2 scrollPos;

    private void OnEnable()
    {
        manualEntries = serializedObject.FindProperty("manualEntries");
        enableApiFetch = serializedObject.FindProperty("enableApiFetch");
        apiUrl = serializedObject.FindProperty("apiUrl");
        clearCacheOnStart = serializedObject.FindProperty("clearCacheOnStart");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ===== 标题 =====
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("NPC 数据库", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        int total = manualEntries.arraySize;
        int validCount = 0;
        for (int i = 0; i < total; i++)
        {
            if (manualEntries.GetArrayElementAtIndex(i).objectReferenceValue != null)
                validCount++;
        }
        EditorGUILayout.HelpBox($"手动条目: {validCount} 个  |  API: {(enableApiFetch.boolValue ? "已启用" : "已关闭")}", MessageType.Info);

        // ===== 手动条目列表 =====
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("── 手动 NPC 条目 ──", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.MaxHeight(600));

        for (int i = 0; i < manualEntries.arraySize; i++)
        {
            if (DrawEntryCard(i))
            {
                // Entry was deleted, re-check bounds
                break;
            }
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndScrollView();

        // ===== 添加按钮 =====
        EditorGUILayout.Space(8);
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("+ 添加现有条目", GUILayout.Height(30)))
        {
            manualEntries.InsertArrayElementAtIndex(manualEntries.arraySize);
            var newElement = manualEntries.GetArrayElementAtIndex(manualEntries.arraySize - 1);
            newElement.objectReferenceValue = null;
        }

        if (GUILayout.Button("+ 新建 NPC 条目", GUILayout.Height(30)))
        {
            CreateNewEntry();
        }

        EditorGUILayout.EndHorizontal();

        // ===== API 设置 =====
        EditorGUILayout.Space(16);
        DrawSeparator();
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("── API 远程获取 ──", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(enableApiFetch, new GUIContent("启用 API 获取"));

        if (enableApiFetch.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(apiUrl, new GUIContent("API 地址"));
            EditorGUI.indentLevel--;
        }

        // ===== 缓存调试 =====
        EditorGUILayout.Space(16);
        DrawSeparator();
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("── 缓存调试 ──", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        DrawCacheDebugSection();

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>
    /// 缓存调试区域：启动时清缓存勾选 + 当前大小 + 立即清除按钮
    /// </summary>
    private void DrawCacheDebugSection()
    {
        EditorGUILayout.PropertyField(clearCacheOnStart,
            new GUIContent("启动时清除缓存", "勾选后下次 Play 会强制从网络重新拉取。测试完记得取消勾选"));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField($"当前缓存大小：{FormatCacheSize(NPCApiService.GetCacheSize())}");

        if (GUILayout.Button("立即清除", GUILayout.Width(80)))
        {
            if (EditorUtility.DisplayDialog("确认清除缓存",
                "删除 NPCCache 下的所有 JSON 和图片缓存？", "清除", "取消"))
            {
                NPCApiService.ClearCache();
                Repaint();
            }
        }
        EditorGUILayout.EndHorizontal();
    }

    private static string FormatCacheSize(long bytes)
    {
        if (bytes < KB) return $"{bytes} B";
        if (bytes < MB) return $"{bytes / (float)KB:F1} KB";
        return $"{bytes / (float)MB:F2} MB";
    }

    /// <summary>
    /// 绘制单个 NPC 条目卡片。返回 true 表示该条目被删除。
    /// </summary>
    private bool DrawEntryCard(int index)
    {
        var prop = manualEntries.GetArrayElementAtIndex(index);
        NPCEntry entry = prop.objectReferenceValue as NPCEntry;

        // 卡片背景
        EditorGUILayout.BeginVertical("helpBox");
        EditorGUILayout.BeginHorizontal();

        // 左侧：Sprite 预览
        DrawSpritePreview(entry, 64);

        // 中间：信息
        EditorGUILayout.BeginVertical();

        if (entry != null)
        {
            EditorGUILayout.LabelField(string.IsNullOrEmpty(entry.username) ? "(未命名)" : entry.username, EditorStyles.boldLabel);

            // 消息预览（截断显示）
            string msgPreview = string.IsNullOrEmpty(entry.message) ? "(无消息)" : entry.message;
            if (msgPreview.Length > 40)
                msgPreview = msgPreview.Substring(0, 40) + "...";
            EditorGUILayout.LabelField(msgPreview, EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.Space(2);

            // 条目引用字段（可替换为其他 NPCEntry）
            EditorGUILayout.PropertyField(prop, new GUIContent("条目资产"));
        }
        else
        {
            EditorGUILayout.LabelField("(空条目 - 请拖入 NPCEntry)", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(prop, new GUIContent("拖入条目"));
        }

        EditorGUILayout.EndVertical();

        // 右侧：操作按钮
        EditorGUILayout.BeginVertical(GUILayout.Width(60));

        if (entry != null && GUILayout.Button("编辑", GUILayout.Width(56)))
        {
            Selection.activeObject = entry;
            EditorGUIUtility.PingObject(entry);
        }

        if (GUILayout.Button("删除", GUILayout.Width(56)))
        {
            if (EditorUtility.DisplayDialog("确认删除",
                $"确定要从数据库中移除 \"{(entry != null ? entry.username : "空条目")}\" 吗？\n（不会删除资产文件）",
                "移除", "取消"))
            {
                // SerializedProperty 删除 object ref 需要两步：先清空引用，再删除元素
                if (prop.objectReferenceValue != null)
                    prop.objectReferenceValue = null;
                manualEntries.DeleteArrayElementAtIndex(index);
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                return true;
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        return false;
    }

    /// <summary>
    /// 绘制 Sprite 缩略图预览
    /// </summary>
    private void DrawSpritePreview(NPCEntry entry, int size)
    {
        Rect previewRect = GUILayoutUtility.GetRect(size, size, GUILayout.Width(size), GUILayout.Height(size));

        if (entry != null && entry.sprite != null)
        {
            Texture2D tex = AssetPreview.GetAssetPreview(entry.sprite);
            if (tex != null)
            {
                GUI.DrawTexture(previewRect, tex, ScaleMode.ScaleToFit);
            }
            else
            {
                // AssetPreview might not be ready yet, request and repaint
                Texture2D spriteTexture = entry.sprite.texture;
                if (spriteTexture != null)
                    GUI.DrawTexture(previewRect, spriteTexture, ScaleMode.ScaleToFit);
                else
                    EditorGUI.LabelField(previewRect, "加载中...", EditorStyles.centeredGreyMiniLabel);
                Repaint();
            }
        }
        else
        {
            EditorGUI.DrawRect(previewRect, new Color(0.2f, 0.2f, 0.2f, 0.5f));
            EditorGUI.LabelField(previewRect, "无图片", EditorStyles.centeredGreyMiniLabel);
        }
    }

    /// <summary>
    /// 在 NPCDatabase 同级目录下的 Entries 子文件夹中创建新的 NPCEntry 资产
    /// </summary>
    private void CreateNewEntry()
    {
        string dbPath = AssetDatabase.GetAssetPath(target);
        string dir = Path.GetDirectoryName(dbPath);
        string entriesDir = Path.Combine(dir, "Entries").Replace("\\", "/");

        if (!AssetDatabase.IsValidFolder(entriesDir))
        {
            string parentFolder = dir.Replace("\\", "/");
            AssetDatabase.CreateFolder(parentFolder, "Entries");
        }

        string path = AssetDatabase.GenerateUniqueAssetPath(entriesDir + "/NewNPC.asset");

        NPCEntry entry = ScriptableObject.CreateInstance<NPCEntry>();
        entry.username = "新NPC";
        entry.message = "";

        AssetDatabase.CreateAsset(entry, path);
        AssetDatabase.SaveAssets();

        // 添加到数据库列表
        serializedObject.Update();
        manualEntries.InsertArrayElementAtIndex(manualEntries.arraySize);
        manualEntries.GetArrayElementAtIndex(manualEntries.arraySize - 1).objectReferenceValue = entry;
        serializedObject.ApplyModifiedProperties();

        // 高亮并选中新资产
        EditorGUIUtility.PingObject(entry);
        Selection.activeObject = entry;
    }

    private void DrawSeparator()
    {
        var rect = EditorGUILayout.GetControlRect(false, 1);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
    }
}
