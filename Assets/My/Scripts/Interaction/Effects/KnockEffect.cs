using System.Collections;
using UnityEngine;

// 새벽 노크 상호작용. RoomController.knockTarget 에 부착 (Interactable promptType = Knock).
// 앵커·손님 스폰 위치는 부모 RoomController 에서 읽는다 (방 배선을 RoomController 한 곳에 모음).
// 노크 → 즉시 화면고정 → knockWait 대기 →
//   새벽이 아니거나(아침·점심 청소 시간) 거절 손님(NpcData.refusesDawnKnock): (dawnPanel + CSV "refuse" 노드 있으면 한 마디) → 화면고정 해제
//   새벽 + 수락: 정문 peekAngle 만큼 열림 + 문틈에 배정 손님 스프라이트 + 새벽 대화 → 종료 시 문 닫힘 + 화면고정 해제
// 시퀀스 동안 자기 Interactable 을 꺼서 재노크·노크음 반복 차단.
// ESC 로 화면고정을 빠져나가면(더 이상 최상위 앵커 아님) 시퀀스 취소: 대화 중단·손님 제거·문 닫기·재노크 허용. doc/0118·0131.
public class KnockEffect : InteractionEffect
{
    [Tooltip("접객과 같은 Guest 프리팹. 비우면 ReceptionManager 의 것을 사용")]
    [SerializeField] private GameObject guestPrefab;
    [Tooltip("비우면 스폰된 손님의 자식 SpeechBubble 사용. 거절 대사는 이게 있어야 표시됨")]
    [SerializeField] private SpeechBubble dawnPanel;
    [Tooltip("노크 후 응답까지 대기 시간")]
    [SerializeField] private float knockWait = 3f;
    [Tooltip("정문이 살짝 열리는 각도")]
    [SerializeField] private float peekAngle = 18f;
    [SerializeField] private float openTime = 0.5f;
    [Tooltip("노크 화면고정의 가장자리 둘러보기 배율 (0 = 완전 고정, 1 = 기본). 낮게 = 거의 안 움직임")]
    [SerializeField] private float lookScale = 0.25f;

    [System.Serializable]
    public struct Flavor { public string en; public string ko; }

    [Tooltip("노크가 거절됐을 때 화면 중앙에 랜덤으로 하나 뜨는 관찰 문구 (영/한). ScreenMessage 로 표시")]
    [SerializeField]
    private Flavor[] refuseMessages =
    {
        new() { en = "The knock is refused.",             ko = "노크가 거절됐다." },
        new() { en = "They won't open the door.",         ko = "문을 열어주지 않는다." },
        new() { en = "Only a voice answers, muffled.",    ko = "문 너머로 대답만 돌아온다." },
    };
    [Tooltip("거절 문구를 읽는 동안 화면고정을 유지할 시간(초)")]
    [SerializeField] private float refuseReadTime = 2.5f;

    private Interactable self;
    private bool busy;

    private void Awake() => self = GetComponent<Interactable>();

    // 아침 등으로 knockTarget 이 꺼질 때 — 시퀀스 중이었다면 안전 리셋.
    private void OnDisable()
    {
        StopAllCoroutines();
        busy = false;
        if (self != null) self.enabled = true;
    }

    public override void Play(in InteractionContext ctx)
    {
        if (busy) return;

        var rc = GetComponentInParent<RoomController>();
        var npc = rc != null ? rc.NightGuest : null;
        if (npc == null) { Debug.Log("[KnockEffect] 이 방에 배정된 손님 없음", this); return; }

        var prefab = guestPrefab != null ? guestPrefab
                   : ReceptionManager.Instance != null ? ReceptionManager.Instance.GuestPrefab : null;
        if (UIInteractionMode.Instance == null || DialogueRunner.Instance == null || prefab == null)
        {
            Debug.LogWarning("[KnockEffect] UIInteractionMode / DialogueRunner / guestPrefab 미할당", this);
            return;
        }

        StartCoroutine(Knock(rc, npc, prefab));
    }

    private IEnumerator Knock(RoomController rc, NpcData npc, GameObject prefab)
    {
        busy = true;
        if (self != null) self.enabled = false;   // 대화 종료까지 재노크·노크음 차단

        Transform anchor = rc.KnockAnchor != null ? rc.KnockAnchor : transform;
        UIInteractionMode.Instance.Enter(anchor, lookScale);   // 노크 즉시 화면고정 (거의 고정)

        GameObject guest = null;
        GuestView view = null;

        // ── knockWait 대기 (매 프레임 ESC 취소 감시) ──
        for (float t = 0f; t < knockWait && Locked(anchor); t += Time.deltaTime)
            yield return null;

        if (Locked(anchor))
        {
            // 새벽이 아니면(아침·점심·저녁 청소 시간) 손님이 문을 열어주지 않는다 — 항상 거절.
            bool isDawn = DayPhaseManager.Instance == null
                          || DayPhaseManager.Instance.Current == DayPhase.Dawn;

            if (!isDawn || npc.refusesDawnKnock)
            {
                if (refuseMessages != null && refuseMessages.Length > 0)
                {
                    var f = refuseMessages[Random.Range(0, refuseMessages.Length)];
                    ScreenMessage.Show(f.en, f.ko);
                }

                // 새벽 거절만 문 너머 대사 한 마디 (Dawn/refuse 노드). 아침·점심엔 응답 없이 물러남.
                if (isDawn && dawnPanel != null)
                {
                    bool said = false;
                    DialogueRunner.Instance.SayNode(npc, dawnPanel, Situation.Dawn, "refuse", () => said = true);
                    while (!said && Locked(anchor)) yield return null;
                }
                else
                {
                    for (float t = 0f; t < refuseReadTime && Locked(anchor); t += Time.deltaTime)
                        yield return null;
                }
            }
            else
            {
                // 수락 — 지정 방에서 지정 손님이 나옴
                rc.PeekDoor(peekAngle, openTime);

                Transform stand = rc.GuestSpawnPoint != null ? rc.GuestSpawnPoint : transform;
                guest = Instantiate(prefab, stand.position, stand.rotation);
                view = guest.GetComponentInChildren<GuestView>();
                var gi = guest.GetComponentInChildren<Interactable>();
                if (gi != null) gi.enabled = false;   // 새벽엔 손님 클릭 상호작용 없음
                var bubble = dawnPanel != null ? dawnPanel : guest.GetComponentInChildren<SpeechBubble>(true);
                if (view != null) view.Apply(npc);

                bool done = false;
                DialogueRunner.Instance.ResetConsumedTopics();   // 노크마다 탐문 결정 토픽 초기화
                ActionPoints.Instance?.Use(1);   // 새벽 손님과 대화 1회 = 행동력 1 소모
                DialogueRunner.Instance.Play(npc, bubble, Situation.Dawn, _ => done = true);
                while (!done && Locked(anchor)) yield return null;
            }
        }

        // ── 정리 (정상 종료 · ESC 취소 공통) ──
        bool cancelled = !Locked(anchor);
        if (cancelled && DialogueRunner.Instance != null) DialogueRunner.Instance.Cancel();
        if (view != null) view.Clear();
        if (guest != null) Destroy(guest);
        rc.PeekDoor(0f, openTime);   // 문 닫기 (안 열렸으면 닫힌 상태 유지)
        if (!cancelled && UIInteractionMode.Instance != null) UIInteractionMode.Instance.Exit();
        if (self != null) self.enabled = true;
        busy = false;
    }

    // 아직 이 노크의 화면고정이 최상위인가 (ESC 로 빠져나가면 false → 취소).
    private static bool Locked(Transform anchor) =>
        UIInteractionMode.Instance != null && UIInteractionMode.Instance.IsTopAnchor(anchor);
}
