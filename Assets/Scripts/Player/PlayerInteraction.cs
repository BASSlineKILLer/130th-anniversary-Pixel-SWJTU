using UnityEngine;

/// <summary>
/// 玩家与NPC/物体交互
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    public float interactionRange = 1.5f;
    public LayerMask interactableLayer;
    public KeyCode interactKey = KeyCode.E;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.isPaused)
            return;

        if (Input.GetKeyDown(interactKey))
        {
            TryInteract();
        }
    }

    private void TryInteract()
    {
        Collider2D hit = Physics2D.OverlapCircle(transform.position, interactionRange, interactableLayer);
        if (hit != null)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();
            interactable?.Interact();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }
}
