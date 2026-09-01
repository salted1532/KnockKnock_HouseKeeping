using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 저녁 "접객" 파트 관리자.
// 저녁으로 전환되면(암전 시점) 접객 자리로 플레이어를 옮기고 세션을 연다.
// 손님 큐: guestPrefab 을 세션 시작 시 1개 인스턴스화해 재활용한다.
//   일차 편성(CampaignData)의 eveningGuestIds 순서대로:
//   GuestView 로 겉모습 교체 → 스폰 지점으로 워프 → entryPath 로 걸어옴 → 대화 →
//   거절/방문객이면 exitPath / 아니면 플레이어가 손님 클릭 → roomPath → 다음.
//   큐 끝 → 인스턴스 파괴 + EndSession() → 새벽 페이드.
//   ponytail: (b) 승인 = "손님 클릭 = 다음 방번호 자동". 모니터 방배정·열쇠 대조는 후속 doc.
public class ReceptionManager : MonoBehaviour
{
    public static ReceptionManager Instance { get; private set; }

    [Header("접객 자리")]
    [Tooltip("접객 시 플레이어가 앉을 위치/정면 (접객 테이블의 Player_Anchor)")]
    [SerializeField] private Transform receptionAnchor;

    [Header("편성 / 손님")]
    [SerializeField] private CampaignData campaign;
    [SerializeField] private NpcCatalog catalog;
    [Tooltip("접객 중 NPC 로 쓸 Guest 프리팹 (GuestMover + GuestView, 선택적으로 자식 SpeechBubble). 세션당 1개 인스턴스 재활용. 새벽 노크(KnockEffect)도 이걸 폴백으로 씀")]
    [SerializeField] private GameObject guestPrefab;
    public GameObject GuestPrefab => guestPrefab;
    [Tooltip("대사 출력용 SpeechBubble. 비우면 손님 프리팹 자식에서 찾음. 스크린 대화 패널(Dialogue_Panel)이면 여기에 연결")]
    [SerializeField] private SpeechBubble speechBubble;

    [Header("손님 이동 경로 (씬 트랜스폼)")]
    [SerializeField] private Transform guestSpawn;      // 스폰/리셋 위치
    [SerializeField] private Transform[] entryPath;     // 스폰 → 카운터
    [SerializeField] private Transform[] exitPath;      // 카운터 → 밖 (거절/방문객)
    [SerializeField] private Transform[] roomPath;      // 카운터 → 배정된 방

    [Header("경로별 바라보는 방향 (waypoint[i] 로 걷는 동안. 짧으면 Auto)")]
    [SerializeField] private GuestView.Facing[] entryFacing;
    [SerializeField] private GuestView.Facing[] exitFacing;
    [SerializeField] private GuestView.Facing[] roomFacing;

    [Header("입·퇴장 문 (선택)")]
    [Tooltip("맵의 출입문 (Interactable + HingeEffect). 손님이 지정 웨이포인트에 도착하면 닫혀 있을 때 연다")]
    [SerializeField] private Interactable guestDoor;
    [Tooltip("이 웨이포인트에 도착 시 문 체크·열기. 입장 = entryPath[1] 도착 시")]
    [SerializeField] private int entryDoorElement = 1;
    [Tooltip("퇴장(거절/방문객) = exitPath[0](카운터, 시작점)")]
    [SerializeField] private int exitDoorElement = 0;
    [Tooltip("입실(승인) = roomPath[0](카운터, 시작점)")]
    [SerializeField] private int roomDoorElement = 0;

    [Header("기타")]
    [SerializeField] private float enterDelay = 0.6f;   // 착석/페이드 후 첫 손님까지
    [SerializeField] private bool debugEndKey = true;   // K 로 즉시 종료(큐 중단 → 새벽)

    public bool InSession { get; private set; }
    public bool Paused { get; private set; }            // ESC 로 UI 모드 이탈 = 일시정지 (세션 유지)
    public bool AwaitingCheckIn { get; private set; }   // 대화 끝, 손님 클릭 대기 중
    public NpcData CurrentGuest { get; private set; }   // 현재 큐 손님 (모니터 방배정 보드용)
    public int PendingRoom { get; private set; } = -1;  // 모니터에서 현재 손님에게 배정한 방 (-1 = 미배정)
    public event Action OnSessionStarted;
    public event Action OnSessionEnded;

    private Coroutine queue;
    private GameObject guestInstance;
    private GuestMover guestMover;
    private bool checkInConfirmed;
    private bool replayRequested;
    private bool dlgWasVisibleOnPause;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged += HandlePhase;
        else
            Debug.LogWarning("[ReceptionManager] DayPhaseManager 없음 — 접객 세션 시작 안 됨", this);

        if (UIInteractionMode.Instance != null)
        {
            UIInteractionMode.Instance.Exited += HandleUIExit;
            UIInteractionMode.Instance.Entered += HandleUIEnter;
        }
    }

    private void OnDestroy()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged -= HandlePhase;
        if (UIInteractionMode.Instance != null)
        {
            UIInteractionMode.Instance.Exited -= HandleUIExit;
            UIInteractionMode.Instance.Entered -= HandleUIEnter;
        }
    }

    private void Update()
    {
        if (debugEndKey && InSession && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            EndSession();
    }

    private void HandlePhase(DayPhase phase)
    {
        if (phase == DayPhase.Evening && !InSession) BeginSession();
    }

    private void BeginSession()
    {
        InSession = true;

        if (UIInteractionMode.Instance != null)
        {
            if (receptionAnchor == null)
                Debug.LogWarning("[ReceptionManager] receptionAnchor 미할당 — 접객 자리로 이동 못 함", this);
            UIInteractionMode.Instance.Enter(receptionAnchor);
        }

        OnSessionStarted?.Invoke();

        var today = campaign != null ? campaign.Day(DayNow()) : null;
        if (today != null && guestPrefab != null && catalog != null)
            queue = StartCoroutine(GuestQueue(today));
        else
            Debug.Log($"[ReceptionManager] Day {DayNow()} 편성/guestPrefab/catalog 없음 — 손님 큐 없이 디버그 K 로만 종료", this);
    }

    private IEnumerator GuestQueue(CampaignData.DayPlan today)
    {
        yield return new WaitForSeconds(enterDelay);

        Vector3 spawnPos = guestSpawn != null ? guestSpawn.position : transform.position;
        Quaternion spawnRot = guestSpawn != null ? guestSpawn.rotation : Quaternion.identity;
        guestInstance = Instantiate(guestPrefab, spawnPos, spawnRot);

        var mover = guestInstance.GetComponentInChildren<GuestMover>();
        var bubble = speechBubble != null ? speechBubble : guestInstance.GetComponentInChildren<SpeechBubble>(true);
        var view = guestInstance.GetComponentInChildren<GuestView>();
        guestMover = mover;
        if (guestMover != null) guestMover.Frozen = Paused;

        foreach (int id in today.eveningGuestIds)
        {
            var npc = catalog.Get(id);
            if (npc == null) continue;

            CurrentGuest = npc;
            PendingRoom = -1;

            view?.Apply(npc);
            mover?.WarpTo(guestSpawn);
            yield return WalkWithDoor(mover, entryPath, entryFacing, entryDoorElement);

            Verdict result = Verdict.None;
            if (DialogueRunner.Instance != null && bubble != null)
            {
                bool done = false;
                DialogueRunner.Instance.Play(npc, bubble, Situation.Reception, v => { result = v; done = true; });
                yield return new WaitUntil(() => done);
            }
            else
            {
                Debug.LogWarning($"[ReceptionManager] '{npc.name}' 대화 스킵 — DialogueRunner 또는 SpeechBubble 없음", this);
            }

            if (npc.visitorOnly)
            {
                view?.ShowBack();
                yield return WalkExit(mover);
            }
            else if (result == Verdict.Rejected)
            {
                GuestManager.Instance?.SetVerdict(npc, Verdict.Rejected, DayNow());
                view?.ShowBack();
                yield return WalkExit(mover);
            }
            else
            {
                // 대화 종료 → 이때부터 손님 상호작용 가능(AwaitingCheckIn).
                //   빈손 클릭 → 대화 다시 재생 / 열쇠 든 채 클릭 → 열쇠 소모 + 승인
                checkInConfirmed = false;
                while (InSession && result != Verdict.Rejected && !checkInConfirmed)
                {
                    replayRequested = false;
                    AwaitingCheckIn = true;
                    yield return new WaitUntil(() => checkInConfirmed || replayRequested || !InSession);
                    AwaitingCheckIn = false;

                    if (replayRequested && InSession && DialogueRunner.Instance != null && bubble != null)
                    {
                        bool redone = false;
                        DialogueRunner.Instance.Play(npc, bubble, Situation.Reception, v => { result = v; redone = true; });
                        yield return new WaitUntil(() => redone);
                    }
                }

                if (!InSession) break;   // K/ESC 로 세션 종료됨

                if (result == Verdict.Rejected)   // 재대화에서 거절 노드 선택
                {
                    GuestManager.Instance?.SetVerdict(npc, Verdict.Rejected, DayNow());
                    view?.ShowBack();
                    yield return WalkExit(mover);
                }
                else                             // 방 배정 + 열쇠로 승인됨
                {
                    Debug.Log($"[ReceptionManager] 체크인 승인 — {PendingRoom}호 ← '{npc.DisplayName}' (id {npc.id}, Day {DayNow()})", this);
                    GuestManager.Instance?.CheckIn(npc, PendingRoom, DayNow());
                    PendingRoom = -1;

                    // 승인 → 손님이 "고맙다" 한마디 (CSV 의 checkin 노드) 후 뒷모습으로 방으로
                    if (DialogueRunner.Instance != null && bubble != null)
                    {
                        bool said = false;
                        DialogueRunner.Instance.SayNode(npc, bubble, Situation.Reception, "checkin", () => said = true);
                        yield return new WaitUntil(() => said);
                    }

                    view?.ShowBack();
                    yield return WalkWithDoor(mover, roomPath, roomFacing, roomDoorElement);
                }
            }

            view?.Clear();
            CurrentGuest = null;
            PendingRoom = -1;
        }

        queue = null;
        EndSession();
    }

    private IEnumerator WalkExit(GuestMover mover) =>
        WalkWithDoor(mover, exitPath, exitFacing, exitDoorElement);

    // path 로 걷되, doorElement 웨이포인트에 도착하면 문이 닫혀 있을 때 연다.
    // 자동으로 닫지는 않는다 — 손님은 열고 그냥 간다.
    private IEnumerator WalkWithDoor(GuestMover mover, Transform[] path,
                                     GuestView.Facing[] facing, int doorElement)
    {
        if (mover == null) yield break;
        yield return mover.WalkThrough(path, facing, i =>
        {
            if (guestDoor != null && i == doorElement && !guestDoor.IsOn)
                guestDoor.SetState(true);
        });
    }

    // CheckInGuestEffect 가 손님 클릭 시 호출. 방 배정(PendingRoom>0)돼 있어야 승인.
    public void ConfirmCheckIn()
    {
        if (AwaitingCheckIn && PendingRoom > 0) checkInConfirmed = true;
    }

    // 모니터 방배정 보드가 호출 — 현재 손님에게 방 배정 (토글: 같은 방 재클릭 = 선택 해제).
    // PendingRoom 이 정수 1개라 라디오처럼 항상 하나만 선택된다. 대기 손님 없거나 이미 찬 방이면 무시.
    public void AssignRoom(int room)
    {
        if (CurrentGuest == null) return;
        if (PendingRoom == room) { PendingRoom = -1; return; }
        if (GuestManager.Instance != null && GuestManager.Instance.RoomTaken(room)) return;
        PendingRoom = room;
    }

    // 승인 대기 중 빈손으로 손님 클릭 → 대화 다시 재생.
    public void RequestDialogueReplay()
    {
        if (AwaitingCheckIn) replayRequested = true;
    }

    // ESC 로 UI 모드를 빠져나옴 = 세션 일시정지 (종료 아님).
    //  손님은 그 자리에 멈추고 대화 UI 는 숨긴다. 코루틴/손님 인스턴스는 유지.
    //  접객 테이블을 다시 상호작용(E)하면 HandleUIEnter 로 재개.
    private void HandleUIExit()
    {
        if (!InSession || Paused) return;
        Paused = true;
        if (guestMover != null) guestMover.Frozen = true;
        if (DialogueRunner.Instance != null) DialogueRunner.Instance.Paused = true;

        dlgWasVisibleOnPause = speechBubble != null && speechBubble.IsVisible;
        if (dlgWasVisibleOnPause) speechBubble.SetVisible(false);

        Debug.Log("[ReceptionManager] 접객 일시정지 (ESC) — 테이블 재상호작용으로 재개", this);
    }

    // 접객 테이블을 다시 상호작용해 UI 모드로 재진입 = 재개.
    private void HandleUIEnter()
    {
        if (!InSession || !Paused) return;
        Paused = false;
        if (guestMover != null) guestMover.Frozen = false;
        if (DialogueRunner.Instance != null) DialogueRunner.Instance.Paused = false;

        if (dlgWasVisibleOnPause && speechBubble != null) speechBubble.SetVisible(true);
        dlgWasVisibleOnPause = false;

        Debug.Log("[ReceptionManager] 접객 재개", this);
    }

    // 그날 접객 완료 → 세션 닫고 새벽으로.
    public void EndSession()
    {
        if (!InSession) return;
        InSession = false;
        Paused = false;
        AwaitingCheckIn = false;
        CurrentGuest = null;
        PendingRoom = -1;
        if (DialogueRunner.Instance != null) DialogueRunner.Instance.Paused = false;
        StopQueue();

        if (UIInteractionMode.Instance != null)
            UIInteractionMode.Instance.ExitAll();

        OnSessionEnded?.Invoke();

        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.Advance();   // 저녁 → 새벽 (페이드)
    }

    private void StopQueue()
    {
        if (queue != null) { StopCoroutine(queue); queue = null; }
        if (guestInstance != null) { Destroy(guestInstance); guestInstance = null; }
        guestMover = null;
    }

    private static int DayNow() =>
        DayPhaseManager.Instance != null ? DayPhaseManager.Instance.DayCount : 1;
}
