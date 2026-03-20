using UnityEditor;
using UnityEngine;

/// <summary>
/// NPCEntry 自定义 Inspector
/// 大图预览 + 简洁的字段布局
/// </summary>
[CustomEditor(typeof(NPCEntry))]
public class NPCEntryEditor : Editor
{
    private SerializedProperty username;
    private SerializedProperty message;
    private SerializedProperty sprite;

    private void OnEnable()
    {
        username = serializedObject.FindProperty("username");
        message = serializedObject.FindProperty("message");
        sprite = serializedObject.FindProperty("sprite");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        NPCEntry entry = target as NPCEntry;

        // ===== 居中大图预览 =====
        EditorGUILayout.Space(8);

        if (entry.sprite != null)
        {
            const int previewSize = 128;
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            Rect previewRect = GUILayoutUtility.GetRect(previewSize, previewSize, GUILayout.Width(previewSize), GUILayout.Height(previewSize));

            Texture2D tex = AssetPreview.GetAssetPreview(entry.sprite);
            if (tex != null)
            {
                GUI.DrawTexture(previewRect, tex, ScaleMode.ScaleToFit);
            }
            else if (entry.sprite.texture != null)
            {
                GUI.DrawTexture(previewRect, entry.sprite.texture, ScaleMode.ScaleToFit);
                Repaint(); // 等 AssetPreview 准备好后刷新
            }

            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.HelpBox("请拖入 NPC 形象图片（PNG Sprite）", MessageType.None);
        }

        EditorGUILayout.Space(8);

        // ===== 字段 =====
        EditorGUILayout.PropertyField(sprite, new GUIContent("NPC 形象"));
        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(username, new GUIContent("用户名"));
        EditorGUILayout.Space(4);
        EditorGUILayout.PropertyField(message, new GUIContent("说的话"));

        // ===== 信息 =====
        if (entry.sprite != null)
        {
            EditorGUILayout.Space(8);
            var tex = entry.sprite.texture;
            EditorGUILayout.LabelField($"图片尺寸: {tex.width} × {tex.height} px", EditorStyles.miniLabel);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
