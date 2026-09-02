using UnityEngine;

// 객실 1개의 관제. 각 방에 컴포넌트로 붙이고 방 번호(101~110)와 가구들을 인스펙터로 연결한다.
// 배정 손님이 있으면 체크인한 저녁을 빼고 체크아웃까지 정문을 잠그고(여닫기 차단) 노크 상호작용을 노출,
// 내부문·침대 등 sealedInteractables 도 함께 비활성. 새벽 노크 → KnockEffect 가 앵커/스폰포인트를 읽어
// 화면고정 + 손님 스프라이트 + 새벽 대화. 새벽이 아닌 노크는 항상 거절.
// 아침 청소 창(체크아웃 아침 · 청소 허용 손님의 숙박 중 아침)엔 정문을 열고 침대를 흐트러뜨린다.
// 체크아웃 아침이 지나면 GuestManager.CheckOut 으로 방을 비운다. doc/0118 · doc/0132.
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

    [Header("잠금 시 함께 비활성화할 가구")]
    [Tooltip("내부문 · 침대 · 기타 상호작용 가구. 원하는 만큼 추가")]
    [SerializeField] private Interactable[] sealedInteractables;

    [Header("잠금 시 방 안이 안 보이도록")]
    [Tooltip("잠금 시 커튼을 닫고(SetState true) 상호작용도 끈다")]
    [SerializeField] private Interactable[] curtains;
    [Tooltip("잠금 시 전등을 끄고(SetState false) 상호작용도 끈다")]
    [SerializeField] private Interactable[] lights;

    [Header("아침 청소 (청소 창이 열릴 때 흐트러진 상태로)")]
    [Tooltip("흐트러진 버전 (침대의 Bed_02 등, = Bed 의 ChangeObjectEffect offObjects). tidyObjects 와 같은 인덱스 = 같은 침대")]
    [SerializeField] private GameObject[] messyObjects;
    [Tooltip("정리된 버전 (침대의 Bed_01 등, = onObjects). messyObjects 와 같은 인덱스 = 같은 침대. 플레이어가 CleanUp 하면 켜짐")]
    [SerializeField] private GameObject[] tidyObjects;

    public int RoomNumber => roomNumber;
    public Transform KnockAnchor => knockAnchor;
    public Transform GuestSpawnPoint => guestSpawnPoint;

    // 이번 청소 아침에 흐트러뜨린 침대 수 (랜덤 1~2). 청소 창이 아니면 0.
    private int messyTargetCount;

    public int MessyTotal => messyTargetCount;                 // 개야 할 침대 수
    public int MessyRemaining                                  // 아직 안 갠 침대 수
    {
        get
        {
            int n = 0;
            if (messyObjects != null)
                foreach (var o in messyObjects) if (o != null && o.activeSelf) n++;
            return n;
        }
    }
    public int MessyDone => Mathf.Max(0, messyTargetCount - MessyRemaining);

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
        var gm = GuestManager.Instance;
        int day = DayPhaseManager.Instance != null ? DayPhaseManager.Instance.DayCount : 1;
        var g = gm != null ? gm.StateInRoom(roomNumber) : null;

        // 체크아웃 아침이 지났으면 정산 — 그 아침엔 방을 열어 청소하게 두고, 점심 이후 손님을 비운다.
        if (g != null && day >= g.CheckOutDay && !(day == g.CheckOutDay && phase == DayPhase.Morning))
        {
            gm.CheckOut(g.npc);
            g = null;
        }

        bool present = g != null;

        // 방문 잠금 판정:
        //  - 체크인한 그 저녁(접객 중, 손님이 걸어 들어오는 중)엔 안 잠금
        //  - 체크아웃 아침 = 개방 (손님 나감 → 대청소)
        //  - 청소 허용 손님은 숙박 중 매일 아침 개방 (하우스키핑)
        //  - 그 외 전부 잠금
        bool checkInEvening = present && phase == DayPhase.Evening && day == g.checkInDay;
        bool checkoutMorning = present && phase == DayPhase.Morning && day == g.CheckOutDay;
        bool cleaningMorning = present && phase == DayPhase.Morning && g.cleaningRequested
                               && day > g.checkInDay && day < g.CheckOutDay;
        bool roomOpenForCleaning = checkoutMorning || cleaningMorning;
        bool seal = present && !checkInEvening && !roomOpenForCleaning;

        // 후불 손님: 체크아웃 아침에 나가면서 숙박비 지불 (settled 로 1회만, 현금음 자동) — doc/0137
        if (checkoutMorning && !g.payUpfront && !g.settled && g.nightlyRate > 0)
        {
            Wallet.Instance?.Add(g.TotalCharge);
            g.settled = true;
        }

        // RoomController 의 상태 변경은 전부 소리 없이 (SetState silent) — doc/0141
        if (frontDoor != null)
        {
            if (seal) frontDoor.SetState(false, silent: true);   // 닫기 연출
            frontDoor.enabled = !seal;                            // 여닫기 상호작용 차단 (CanInteract 가 enabled 확인)
        }

        if (sealedInteractables != null)
            foreach (var it in sealedInteractables)
                if (it != null) it.enabled = !seal;

        // 잠금 시 커튼 OFF + 불 꺼서 창밖에서 방 안이 안 보이게 (doc/0136 · 0141)
        if (curtains != null)
            foreach (var it in curtains)
                if (it != null) { if (seal) it.SetState(false, silent: true); it.enabled = !seal; }
        if (lights != null)
            foreach (var it in lights)
                if (it != null) { if (seal) it.SetState(false, silent: true); it.enabled = !seal; }

        // 잠겨 있으면 노크 노출. 새벽이 아니면 KnockEffect 가 항상 거절 (doc/0132).
        if (knockTarget != null) knockTarget.SetActive(seal);

        // 아침 청소 창이면 침대를 흐트러진 상태로(랜덤 1~2개), 아니면(시작·평소) 정리된 상태로.
        if (roomOpenForCleaning) SetMessy(); else SetTidy();
    }

    // 청소 대상 침대 중 랜덤하게 1~2개를 흐트러뜨린다. Bed 의 CleanUp(ChangeObjectEffect) 이 개별로 되돌린다.
    // messyObjects[i] / tidyObjects[i] 는 같은 침대의 두 버전 (인덱스로 쌍).
    private void SetMessy()
    {
        int beds = Mathf.Min(messyObjects?.Length ?? 0, tidyObjects?.Length ?? 0);
        if (beds == 0) { messyTargetCount = 0; return; }

        // 인덱스 섞기 (Fisher-Yates) → 앞에서 dirty 개를 흐트러뜨림
        int[] order = new int[beds];
        for (int i = 0; i < beds; i++) order[i] = i;
        for (int i = beds - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        int dirty = Random.Range(1, beds + 1);   // 1..beds
        for (int k = 0; k < beds; k++)
        {
            int i = order[k];
            bool messy = k < dirty;
            if (messyObjects[i] != null) messyObjects[i].SetActive(messy);
            if (tidyObjects[i] != null) tidyObjects[i].SetActive(!messy);
        }
        messyTargetCount = dirty;
    }

    // 모든 침대를 정리된 상태로 (게임 시작·청소 창이 아닌 단계).
    private void SetTidy()
    {
        if (messyObjects != null)
            foreach (var o in messyObjects) if (o != null) o.SetActive(false);
        if (tidyObjects != null)
            foreach (var o in tidyObjects) if (o != null) o.SetActive(true);
        messyTargetCount = 0;
    }

    // KnockEffect 가 호출 — 정문을 지정 각도로 스윙 (Interactable.IsOn 안 건드림).
    public void PeekDoor(float angle, float time)
    {
        var hinge = frontDoor != null ? frontDoor.GetComponent<HingeEffect>() : null;
        if (hinge != null) hinge.SwingTo(angle, time);
    }
}
