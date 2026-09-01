using UnityEngine;
using UnityEngine.EventSystems;

// UI 그래픽(모니터 화면 배경 등)을 클릭하면 지정/상위 Interactable 을 실행한다.
// 그 위의 버튼 등 다른 클릭 요소는 각자 처리 — 이건 "빈 배경 클릭 = 오브젝트 상호작용" 용.
// 대상 Graphic 은 raycastTarget = true 여야 EventSystem 이 클릭을 전달한다.
public class InteractableProxyClick : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("실행할 Interactable. 비우면 부모에서 탐색")]
    [SerializeField] private Interactable target;

    private void Awake()
    {
        if (target == null) target = GetComponentInParent<Interactable>();
        if (target == null)
            Debug.LogWarning($"[InteractableProxyClick] '{name}' 대상 Interactable 없음", this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (target != null && target.CanInteract)
            target.Interact(null, target.transform.position);
    }
}
