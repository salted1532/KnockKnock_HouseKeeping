using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionOutline : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject interactionText;

    private Outline currentOutline;
    private Interactable currentInteractable;

    private void Update()
    {
        Outline hitOutline = null;
        Interactable hitInteractable = null;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            hitOutline = hit.collider.GetComponentInParent<Outline>();
            hitInteractable = hit.collider.GetComponentInParent<Interactable>();
        }

        if (hitOutline != currentOutline)
        {
            if (currentOutline != null)
                currentOutline.enabled = false;
            if (hitOutline != null)
                hitOutline.enabled = true;
            currentOutline = hitOutline;
        }

        if (hitInteractable != currentInteractable)
        {
            currentInteractable = hitInteractable;
            if (interactionText != null)
                interactionText.SetActive(currentInteractable != null);
        }

        if (currentInteractable != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            currentInteractable.Interact(hit.point);

            if (currentOutline != null)
                currentOutline.enabled = false;
            currentOutline = null;
            currentInteractable = null;

            if (interactionText != null)
                interactionText.SetActive(false);
        }
    }
}
