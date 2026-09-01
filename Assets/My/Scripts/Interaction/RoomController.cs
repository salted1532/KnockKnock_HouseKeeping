using UnityEngine;

// 객실 1개의 관제. 각 방에 컴포넌트로 붙이고 방 번호(101~110)와 가구들을 인스펙터로 연결한다.
// 새벽에 이 방 배정 손님이 있으면: 정문을 잠그고(여닫기 차단) 노크 상호작용을 노출,
// 내부문·침대 등 sealedInteractables 도 함께 비활성. 노크하면 KnockEffect 가 아래 앵커/스폰포인트를 읽어
// 화면고정 + 지정 손님 스프라이트 스폰 + 새벽 대화. doc/0118.
public class RoomController : MonoBehaviour
{
    [SerializeField] private int roomNumber = 101;   // 101~110

    [Header("정문 (새벽에 노크로 전환)")]
    [Tooltip("정문 — Interactable + HingeEffect + SfxEffect")]
    [SerializeField] private Interactable frontDoor;
    [Tooltip("정문의 노크 상호작용 자식 오브젝트 (Interactable(Knock) + KnockEffect + Collider). 시작 비활성")]
    [SerializeField] private GameObject knockTarget;
    [Tooltip("노크 시 화면고정 위치 (화면고정 상호작용의 anchor 와 같은 방식). 정문 자식으로 배치")]
    [SerializeField] private Transform knockAnchor;
    [Tooltip("노크 시 지정 손님 스프라이트가 스폰될 위치 (문틈/문 앞 빈 오브젝트)")]
    [SerializeField] private Transform guestSpawnPoint;

    [Header("새벽 잠금 시 함께 비활성화할 가구")]
    [Tooltip("내부문 · 침대 · 기타 상호작용 가구. 원하는 만큼 추가")]
    [SerializeField] private Interactable[] sealedInteractables;

    public int RoomNumber => roomNumber;
    public Transform KnockAnchor => knockAnchor;
    public Transform GuestSpawnPoint => guestSpawnPoint;

    // 이 방에 배정된 이번 밤 손님. 없으면 null.
    public NpcData NightGuest =>
        GuestManager.Instance != null ? GuestManager.Instance.GuestInRoom(roomNumber) : null;

    private void Awake()
    {
        if (knockTarget != null) knockTarget.SetActive(false);
    }

    private void Start()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged += Apply;
        Apply(DayPhaseManager.Instance != null ? DayPhaseManager.Instance.Current : DayPhase.Morning);
    }

    private void OnDestroy()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged -= Apply;
    }

    private void Apply(DayPhase phase)
    {
        bool seal = phase == DayPhase.Dawn && NightGuest != null;

        if (frontDoor != null)
        {
            if (seal) frontDoor.SetState(false);   // 닫기 연출
            frontDoor.enabled = !seal;             // 여닫기 상호작용 차단 (CanInteract 가 enabled 확인)
        }

        if (sealedInteractables != null)
            foreach (var it in sealedInteractables)
                if (it != null) it.enabled = !seal;

        if (knockTarget != null) knockTarget.SetActive(seal);   // 노크 상호작용 노출
    }

    // KnockEffect 가 호출 — 정문을 지정 각도로 스윙 (Interactable.IsOn 안 건드림).
    public void PeekDoor(float angle, float time)
    {
        var hinge = frontDoor != null ? frontDoor.GetComponent<HingeEffect>() : null;
        if (hinge != null) hinge.SwingTo(angle, time);
    }
}
