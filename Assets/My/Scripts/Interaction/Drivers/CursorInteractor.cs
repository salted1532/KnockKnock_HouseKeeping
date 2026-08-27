using UnityEngine;
using UnityEngine.InputSystem;

// 마우스 커서 레이 → 호버 아웃라인, 좌클릭 상호작용. UI 모드(UIInteractionMode)에서만 활성화된다.
public class CursorInteractor : Interactor
{
    [SerializeField] private float interactDistance = 5f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private Camera cam;

    private Outline currentOutline;

    private void Reset() => cam = Camera.main;
    private void OnDisable() => ClearOutline();

    private void Update()
    {
        if (Mouse.current == null || cam == null) return;

        Interactable hovered = null;
        Outline hitOutline = null;
        Vector3 point = Vector3.zero;

        Ray ray = cam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            point = hit.point;
            hitOutline = hit.collider.GetComponentInParent<Outline>();
            var candidate = hit.collider.GetComponentInParent<Interactable>();
            if (candidate != null && candidate.CanInteract)
                hovered = candidate;
        }

        if (hitOutline != currentOutline)
        {
            if (currentOutline != null) currentOutline.enabled = false;
            if (hitOutline != null) hitOutline.enabled = true;
            currentOutline = hitOutline;
        }

        if (hovered != null && Mouse.current.leftButton.wasPressedThisFrame)
            hovered.Interact(this, point);
    }

    private void ClearOutline()
    {
        if (currentOutline != null) currentOutline.enabled = false;
        currentOutline = null;
    }
}
