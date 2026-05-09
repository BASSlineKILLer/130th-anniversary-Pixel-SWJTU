using System.Collections.Generic;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

/// <summary>
/// 一键将 SpecialNPC 相关资产移出 Resources 文件夹并注册到 Addressables。
/// 菜单：Tools → SpecialNPC → Setup Addressables
/// </summary>
public static class SpecialNPCAddressableSetup
{
    private const string SPECIAL_ENTRIES_SRC  = "Assets/Resources/NPCData/SpecialEntries";
    private const string SPECIAL_ENTRIES_DST  = "Assets/Resources_moved/NPCData/SpecialEntries";
    private const string SPECIAL_NPC_DATA_SRC = "Assets/Resources/SpecialNPC/SpecialNPCData.asset";
    private const string SPECIAL_NPC_DATA_DST = "Assets/Resources_moved/SpecialNPC/SpecialNPCData.asset";
    private const string DIALOGUE_SRC         = "Assets/Resources/NPCData/SpecialEntries/dialogue";
    private const string DIALOGUE_DST         = "Assets/Resources_moved/NPCData/SpecialEntries/dialogue";
    private const string GROUP_NAME           = "SpecialNPC";

    [MenuItem("Tools/SpecialNPC/Setup Addressables")]
    public static void Run()
    {
        var settings = AddressableAssetSettingsDefaultObject.Settings;
        if (settings == null)
        {
            Debug.LogError("[SpecialNPCSetup] 找不到 AddressableAssetSettings，请先初始化 Addressables。");
            return;
        }

        EnsureDirectory("Assets/Resources_moved/NPCData/SpecialEntries/dialogue");
        EnsureDirectory("Assets/Resources_moved/SpecialNPC");

        var group = GetOrCreateGroup(settings, GROUP_NAME);

        // 1. 移动 dialogue 子目录里的资产
        MoveAndRegister(DIALOGUE_SRC, DIALOGUE_DST, settings, group, "*.asset");

        // 2. 移动 SpecialEntries 根目录里的资产
        MoveAndRegister(SPECIAL_ENTRIES_SRC, SPECIAL_ENTRIES_DST, settings, group, "*.asset");

        // 3. 移动 SpecialNPCData.asset
        MoveSingleAndRegister(SPECIAL_NPC_DATA_SRC, SPECIAL_NPC_DATA_DST, settings, group);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[SpecialNPCSetup] 完成！已将资产移至 Resources_moved 并注册到 Addressables Group: {GROUP_NAME}");
    }

    // ── 移动一个目录下的所有 .asset 并注册 ──────────────────────────────
    private static void MoveAndRegister(
        string srcDir, string dstDir,
        AddressableAssetSettings settings, AddressableAssetGroup group,
        string filter)
    {
        string[] guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { srcDir });
        foreach (string guid in guids)
        {
            string srcPath = AssetDatabase.GUIDToAssetPath(guid);
            // 只处理直接在该目录下的文件（不递归到子目录，子目录单独处理）
            string relative = srcPath.Substring(srcDir.Length).TrimStart('/');
            if (relative.Contains("/")) continue; // 是子目录的文件，跳过

            string fileName = System.IO.Path.GetFileName(srcPath);
            string dstPath  = dstDir + "/" + fileName;

            string error = AssetDatabase.MoveAsset(srcPath, dstPath);
            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogWarning($"[SpecialNPCSetup] 移动失败 {srcPath} → {dstPath}: {error}");
                continue;
            }

            RegisterToGroup(settings, group, guid, AddressFromPath(dstPath));
            Debug.Log($"[SpecialNPCSetup] 已移动并注册: {dstPath}");
        }
    }

    // ── 移动单个文件并注册 ──────────────────────────────────────────────
    private static void MoveSingleAndRegister(
        string srcPath, string dstPath,
        AddressableAssetSettings settings, AddressableAssetGroup group)
    {
        if (!AssetDatabase.LoadAssetAtPath<Object>(srcPath))
        {
            Debug.LogWarning($"[SpecialNPCSetup] 文件不存在，跳过: {srcPath}");
            return;
        }

        string guid  = AssetDatabase.AssetPathToGUID(srcPath);
        string error = AssetDatabase.MoveAsset(srcPath, dstPath);
        if (!string.IsNullOrEmpty(error))
        {
            Debug.LogWarning($"[SpecialNPCSetup] 移动失败 {srcPath} → {dstPath}: {error}");
            return;
        }

        RegisterToGroup(settings, group, guid, AddressFromPath(dstPath));
        Debug.Log($"[SpecialNPCSetup] 已移动并注册: {dstPath}");
    }

    // ── 注册到 Group（已存在则跳过）───────────────────────────────────────
    private static void RegisterToGroup(
        AddressableAssetSettings settings, AddressableAssetGroup group,
        string guid, string address)
    {
        var entry = settings.FindAssetEntry(guid);
        if (entry != null) return; // 已注册

        entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
        entry.address = address;
    }

    // ── 确保目录存在（用 AssetDatabase 创建）──────────────────────────────
    private static void EnsureDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string parent = System.IO.Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = System.IO.Path.GetFileName(path);
        EnsureDirectory(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    // ── Group 不存在则创建 ────────────────────────────────────────────────
    private static AddressableAssetGroup GetOrCreateGroup(
        AddressableAssetSettings settings, string groupName)
    {
        var group = settings.FindGroup(groupName);
        if (group != null) return group;

        var template = settings.GetGroupTemplateObject(0) as AddressableAssetGroupTemplate;
        group = settings.CreateGroup(groupName, setAsDefaultGroup: false,
                                     readOnly: false, postEvent: false,
                                     template != null ? new List<AddressableAssetGroupSchema>(template.SchemaObjects) : null);
        return group;
    }

    // ── 从路径生成可读 address（去掉前缀和扩展名）────────────────────────
    private static string AddressFromPath(string assetPath)
    {
        string name = System.IO.Path.GetFileNameWithoutExtension(assetPath);
        return "SpecialNPC/" + name;
    }
}
