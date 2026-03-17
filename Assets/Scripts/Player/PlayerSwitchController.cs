using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 切换主控模块
/// 在出生点（饮水思源碑）交互输入昵称，可操控对应NPC
/// </summary>
public class PlayerSwitchController : MonoBehaviour, IInteractable
{
    [Header("UI引用")]
    public GameObject inputPanel;
    public InputField nicknameInput;
    public Button confirmButton;

    private void Start()
    {
        if (inputPanel != null)
            inputPanel.SetActive(false);

        confirmButton?.onClick.AddListener(OnConfirmNickname);
    }

    public void Interact()
    {
        // 显示输入昵称面板
        if (inputPanel != null)
        {
            inputPanel.SetActive(true);
            GameManager.Instance?.PauseGame();
        }
    }

    private void OnConfirmNickname()
    {
        if (nicknameInput == null) return;

        string nickname = nicknameInput.text.Trim();
        if (string.IsNullOrEmpty(nickname)) return;

        // 根据昵称查找并切换到对应NPC
        SwitchToNPC(nickname);

        inputPanel?.SetActive(false);
        GameManager.Instance?.ResumeGame();
    }

    private void SwitchToNPC(string nickname)
    {
        // 查找名字匹配的NPC
        NPCController[] npcs = FindObjectsOfType<NPCController>();
        foreach (var npc in npcs)
        {
            if (npc.npcName == nickname)
            {
                // 将相机目标切换到该NPC
                CameraFollow cam = UnityEngine.Camera.main?.GetComponent<CameraFollow>();
                if (cam != null)
                {
                    cam.target = npc.transform;
                }
                break;
            }
        }
    }
}
