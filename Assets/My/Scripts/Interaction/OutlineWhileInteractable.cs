using UnityEngine;

// 상호작용이 가능해진 동안(Interactable.CanInteract == true) Outline 을 계속 켜둔다 — 유도용 표식.
// HUD ObjectiveMarker(나침반 역할)와 함께 씀: 마커가 방향, 이 외곽선이 "이 물건" 을 벽 너머로 보여줌.
// Interactor 가 시선을 떼면 Outline 을 끄므로 LateUpdate 에서 다시 켠다. doc/0144.
// 벽 너머 표시는 Outline 컴포넌트의 Outline Mode = OutlineAll (ZTest Always). 색은 흰색이면 충분(PxlCrush 무관).
[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Outline))]
public class OutlineWhileInteractable : MonoBehaviour
{
    private Interactable interactable;
    private Outline outline;

    private void Awake()
    {
        interactable = GetComponent<Interactable>();
        outline = GetComponent<Outline>();
    }

    private void LateUpdate()
    {
        bool want = interactable.CanInteract;
        if (want != outline.enabled) outline.enabled = want;
    }
}
