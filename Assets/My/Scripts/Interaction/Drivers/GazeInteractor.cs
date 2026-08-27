using UnityEngine;
using UnityEngine.InputSystem;

// 화면 중앙 레이 → 아웃라인 + 프롬프트, E키로 상호작용. (구 InteractionOutline)
public class GazeInteractor : Interactor
{
    [SerializeField] private float interactDistance = 3f;
    [SerializeField] private LayerMask interactMask = ~0;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private GameObject interactionText;

    private Outline currentOutline;
    private Interactable currentInteractable;

    public bool Suspended { get; set; }   // UI 모드 등에서 잠시 끔

    private void OnDisable() => Clear();

    private void Update()
    {
        if (Suspended) { Clear(); return; }

        Outline hitOutline = null;
        Interactable hitInteractable = null;
        Vector3 point = Vector3.zero;

        Ray ray = playerCamera.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask, QueryTriggerInteraction.Ignore))
        {
            // 벽 너머 상호작용 차단: 대상 앞에 막는 콜라이더(Interaction / Ignore Raycast 제외)가 있으면 무시
            const int ignoreRaycastLayer = 2;
            int occlusionMask = ~interactMask.value & ~(1 << ignoreRaycastLayer);
            if (!Physics.Raycast(ray, hit.distance - 0.01f, occlusionMask, QueryTriggerInteraction.Ignore))
            {
                point = hit.point;
                hitOutline = hit.collider.GetComponentInParent<Outline>();
                var candidate = hit.collider.GetComponentInParent<Interactable>();
                if (candidate != null && candidate.CanInteract)
                    hitInteractable = candidate;
            }
        }

        if (hitOutline != currentOutline)
        {
            if (currentOutline != null) currentOutline.enabled = false;
            if (hitOutline != null) hitOutline.enabled = true;
            currentOutline = hitOutline;
        }

        if (hitInteractable != currentInteractable)
        {
            currentInteractable = hitInteractable;
            if (interactionText != null) interactionText.SetActive(currentInteractable != null);
        }

        if (currentInteractable != null && Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            currentInteractable.Interact(this, point);
            Clear();
        }
    }

    private void Clear()
    {
        if (currentOutline != null) currentOutline.enabled = false;
        currentOutline = null;
        currentInteractable = null;
        if (interactionText != null) interactionText.SetActive(false);
    }
}
