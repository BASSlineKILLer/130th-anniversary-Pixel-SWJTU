using UnityEngine;

/// <summary>
/// 存档管理器（单例 + 跨场景持久）
/// 使用 PlayerPrefs + JsonUtility 保存玩家位置和场景信息。
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

    public void Save(string sceneName, Vector2 position, string checkpointId)
    {
        var data = new SaveData
        {
            sceneName = sceneName,
            positionX = position.x,
            positionY = position.y,
            checkpointId = checkpointId
        };
        string json = JsonUtility.ToJson(data);
        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();
        Debug.Log($"[SaveManager] 已保存: 场景={sceneName}, 位置=({position.x:F1},{position.y:F1}), 存档点={checkpointId}");
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

    public Vector2 Position => new Vector2(positionX, positionY);
}
