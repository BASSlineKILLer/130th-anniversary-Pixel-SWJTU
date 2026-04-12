using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 存档管理器（单例 + 跨场景持久）
/// 使用 PlayerPrefs + JsonUtility 保存玩家位置、场景信息和勋章进度。
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SAVE_KEY = "PlayerSaveData";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 保存当前状态（位置 + 勋章进度）
    /// </summary>
    public void Save(string sceneName, Vector2 position, string checkpointId)
    {
        var data = new SaveData
        {
            sceneName = sceneName,
            positionX = position.x,
            positionY = position.y,
            checkpointId = checkpointId,
        };

        // 同步保存勋章进度
        if (MedalManager.Instance != null)
        {
            data.medalCount = MedalManager.Instance.GetMedalCount();
            data.talkedNPCs = new List<string>(MedalManager.Instance.GetTalkedNPCs());
            data.talkedSpecialNPCs = new List<string>(MedalManager.Instance.GetTalkedSpecialNPCs());
            data.isLibraryUnlocked = MedalManager.Instance.IsLibraryUnlocked;
        }

        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        Debug.Log($"[SaveManager] 已保存: 场景={sceneName}, 位置=({position.x:F1},{position.y:F1}), 存档点={checkpointId}, 勋章={data.medalCount}");
    }

    public SaveData Load()
    {
        if (!HasSave()) return null;

        string json = PlayerPrefs.GetString(SAVE_KEY);
        try
        {
            return JsonUtility.FromJson<SaveData>(json);
        }
        catch
        {
            Debug.LogWarning("[SaveManager] 存档数据损坏，已忽略");
            return null;
        }
    }

    public bool HasSave()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }

    public void DeleteSave()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();
        Debug.Log("[SaveManager] 存档已删除");
    }

    /// <summary>
    /// 从存档数据恢复勋章进度到 MedalManager
    /// </summary>
    public void RestoreMedalData(SaveData data)
    {
        if (data == null || MedalManager.Instance == null) return;
        MedalManager.Instance.RestoreFromSave(data.medalCount, data.talkedNPCs, data.talkedSpecialNPCs, data.isLibraryUnlocked);
    }
}

/// <summary>
/// 存档数据结构（可序列化）
/// </summary>
[System.Serializable]
public class SaveData
{
    public string sceneName;
    public float positionX;
    public float positionY;
    public string checkpointId;

    // 勋章进度
    public int medalCount;
    public List<string> talkedNPCs = new List<string>();
    public List<string> talkedSpecialNPCs = new List<string>();
    public bool isLibraryUnlocked;

    public Vector2 Position => new Vector2(positionX, positionY);
}

