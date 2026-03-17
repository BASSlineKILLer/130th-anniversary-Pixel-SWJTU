using UnityEngine;

/// <summary>
/// NPC控制器 - 管理单个NPC的行为
/// </summary>
public class NPCController : MonoBehaviour, IInteractable
{
    [Header("NPC数据")]
    public string npcName;
    public Sprite npcPortrait;
    [TextArea(3, 5)]
    public string dialogueText;

    [Header("NPC设置")]
    public bool canWander = false;

    private NPCStateMachine stateMachine;

    private void Awake()
    {
        stateMachine = GetComponent<NPCStateMachine>();
    }

    public void Interact()
    {
        if (DialogueSystem.Instance != null)
        {
            DialogueSystem.Instance.StartDialogue(npcPortrait, npcName, dialogueText);
        }
    }

    public void SetNPCData(string name, Sprite portrait, string dialogue, bool wander)
    {
        npcName = name;
        npcPortrait = portrait;
        dialogueText = dialogue;
        canWander = wander;
    }
}
