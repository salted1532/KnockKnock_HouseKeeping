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
    [Tooltip("접객 중 NPC 로 쓸 Guest 프리팹 (GuestMover + GuestView, 선택적으로 자식 SpeechBubble). 세션당 1개 인스턴스 재활용")]
    [SerializeField] private GameObject guestPrefab;
    [Tooltip("대사 출력용 SpeechBubble. 비우면 손님 프리팹 자식에서 찾음. 스크린 대화 패널(Dialogue_Panel)이면 여기에 연결")]
    [SerializeField] private SpeechBubble speechBubble;

    [Header("손님 이동 경로 (씬 트랜스폼)")]
    [SerializeField] private Transform guestSpawn;      // 스폰/리셋 위치
    [SerializeField] private Transform[] entryPath;     // 스폰 → 카운터
    [SerializeField] private Transform[] exitPath;      // 카운터 → 밖 (거절/방문객)
    [SerializeField] private Transform[] roomPath;      // 카운터 → 배정된 방

    [Header("기타")]
    [SerializeField] private int firstRoomNumber = 101;
    [SerializeField] private float enterDelay = 0.6f;   // 착석/페이드 후 첫 손님까지
    [SerializeField] private bool debugEndKey = true;   // K 로 즉시 종료(큐 중단 → 새벽)

    public bool InSession { get; private set; }
    public bool AwaitingCheckIn { get; private set; }   // 대화 끝, 손님 클릭 대기 중
    public event Action OnSessionStarted;
    public event Action OnSessionEnded;

    private Coroutine queue;
    private GameObject guestInstance;
    private int nextRoom;
    private bool checkInConfirmed;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged += HandlePhase;
        else
            Debug.LogWarning("[ReceptionManager] DayPhaseManager 없음 — 접객 세션 시작 안 됨", this);

        if (UIInteractionMode.Instance != null)
            UIInteractionMode.Instance.Exited += HandleUIExit;
    }

    private void OnDestroy()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged -= HandlePhase;
        if (UIInteractionMode.Instance != null)
            UIInteractionMode.Instance.Exited -= HandleUIExit;
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
        nextRoom = firstRoomNumber;

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

        foreach (int id in today.eveningGuestIds)
        {
            var npc = catalog.Get(id);
            if (npc == null) continue;

            view?.Apply(npc);
            mover?.WarpTo(guestSpawn);
            if (mover != null) yield return mover.WalkThrough(entryPath);

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
                if (mover != null) yield return mover.WalkThrough(exitPath);
            }
            else if (result == Verdict.Rejected)
            {
                GuestManager.Instance?.SetVerdict(npc, Verdict.Rejected, DayNow());
                if (mover != null) yield return mover.WalkThrough(exitPath);
            }
            else
            {
                // 대화가 그냥 끝남 → 플레이어가 손님을 클릭해 체크인
                checkInConfirmed = false;
                AwaitingCheckIn = true;
                yield return new WaitUntil(() => checkInConfirmed || !InSession);
                AwaitingCheckIn = false;

                if (!InSession) break;   // K/ESC 로 세션 종료됨

                GuestManager.Instance?.CheckIn(npc, nextRoom++, DayNow());

                // 승인 → 손님이 "고맙다" 한마디 (CSV 의 checkin 노드) 후 방으로
                if (DialogueRunner.Instance != null && bubble != null)
                {
                    bool said = false;
                    DialogueRunner.Instance.SayNode(npc, bubble, Situation.Reception, "checkin", () => said = true);
                    yield return new WaitUntil(() => said);
                }

                if (mover != null) yield return mover.WalkThrough(roomPath);
            }

            view?.Clear();
        }

        queue = null;
        EndSession();
    }

    // CheckInGuestEffect 가 손님 클릭 시 호출.
    public void ConfirmCheckIn()
    {
        if (AwaitingCheckIn) checkInConfirmed = true;
    }

    // UI 모드가 완전히 닫혔을 때 (ESC 로 접객 레벨까지 빠져나온 경우 등). 하루 전환은 하지 않음.
    private void HandleUIExit()
    {
        if (!InSession) return;
        StopQueue();
        InSession = false;
        AwaitingCheckIn = false;
        OnSessionEnded?.Invoke();
        Debug.Log("[ReceptionManager] 접객 모드 탈출 — 세션 정리 (하루 전환 없음)", this);
    }

    // 그날 접객 완료 → 세션 닫고 새벽으로.
    public void EndSession()
    {
        if (!InSession) return;
        InSession = false;
        AwaitingCheckIn = false;
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
    }

    private static int DayNow() =>
        DayPhaseManager.Instance != null ? DayPhaseManager.Instance.DayCount : 1;
}
