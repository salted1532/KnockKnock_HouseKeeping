using UnityEngine;

public class InteractionOutline : MonoBehaviour
{
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private Camera playerCamera;

    private Outline currentOutline;

    private void Update()
    {
        Outline hitOutline = null;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            hitOutline = hit.collider.GetComponentInParent<Outline>();
        }

        if (hitOutline == currentOutline)
            return;

        if (currentOutline != null)
            currentOutline.enabled = false;
        if (hitOutline != null)
            hitOutline.enabled = true;

        currentOutline = hitOutline;
    }
}
