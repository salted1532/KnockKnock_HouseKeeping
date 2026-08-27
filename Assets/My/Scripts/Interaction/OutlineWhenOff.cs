using UnityEngine;

// 토글 상호작용이 "off" 상태(IsOn == false)일 때 Outline 을 계속 켜둔다.
// 어두운 방의 조명 스위치처럼, 꺼놓으면 안 보여서 다시 켜기 힘든 오브젝트에 붙인다.
// Interactor 가 시선을 떼면 Outline 을 끄므로, LateUpdate 에서 다시 켠다.
[RequireComponent(typeof(Interactable))]
[RequireComponent(typeof(Outline))]
public class OutlineWhenOff : MonoBehaviour
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
        // 켜져 있으면 간섭 안 함 (Interactor 의 호버 로직에 맡김)
        if (!interactable.IsOn && !outline.enabled)
            outline.enabled = true;
    }
}
