# 0118 - 모니터 방배정 UI + RoomController + 새벽 노크 상호작용 (제안)

날짜: 2026-08-31
관련: `doc/0100`(모니터/화면고정), `doc/0104`(접객 승인·거절 흐름, 모니터 방배정은 후속으로 미룸), `doc/0117`(손님 문 여닫기), `doc/0119`(CRT 모니터 World Canvas uGUI + RT 클릭 보정), `Docs/ReceptionManager.md`, `Docs/InteractionSystem.md`

## 요청 (원문 요약)

1. 접객 시 **모니터 UI** 로 각 접객 손님에게 방을 부여. **UI Controller** 를 만들어 각 방을 연결, **101~110 (10개)** 지정 가능.
2. 지정된 방은 **Room Controller** 가 문·상호작용 가구들을 **직접 조작** (내가 인스펙터로 직접 연결).
3. 새벽에 손님이 배정된 방문은 "여닫기" 대신 **"노크"**. 노크 → 3초 후 문이 살짝 열림(각도 지정 가능) + 플레이어 화면고정 → 문틈에 배정 손님 상반신 → 질문 대화 → 종료 시 문 닫힘 + 화면고정 해제. 방에 못 들어가게 문 제어.
4. 이걸로 새벽 시간대 게임 파트를 구성.

## 현재 상태

| 조각 | 지금 |
|---|---|
| `GuestManager.GuestState` | `{ npc, room, verdict, checkInDay }` — **방 번호를 이미 손님별로 저장**. `CheckIn(npc, room, day)` 가 세팅 |
| `ReceptionManager` 승인 | 손님 클릭(열쇠 소지) → `GuestManager.CheckIn(npc, nextRoom++, DayNow())`. `nextRoom` 은 `firstRoomNumber`(101) 부터 **자동 증가**. 플레이어 선택 없음 |
| 승인 손님 이동 | `GuestMover.WalkThrough(roomPath)` (공용 경로, `doc/0117`). 이번에 **변경 없음** |
| 모니터 | `CRTMonitor` 프리팹 = `Interactable`(화면고정) + `EnterUIModeEffect`(anchor) + `screenON/ScreenUI` **World Space Canvas** + `RenderTextureGraphicRaycaster`(`doc/0119`) → **uGUI 버튼 정상 동작** (화면고정/접객 모드에서만, 게이트 = `UIInteractionMode.Active`) |
| 문 | `Interactable`(isToggle, OpenClose) + `HingeEffect` + `SfxEffect`. `SetState(bool)` 로 연출 조작 (`doc/0117`) |
| `UIInteractionMode` | `Enter(anchor)` 앵커 스택 + 화면고정 + 커서. 접객 전용 아님. ESC 로 한 겹 |
| `DialogueRunner.Play(npc, bubble, Situation, onResult)` | `Situation.Dawn` **이미 enum 존재**, CSV 행만 있으면 재생 |
| 새벽(Dawn) | 시간대 전환만. **게임플레이 없음** |
| 방 오브젝트 | 씬에 `Motel_Rooms` 프리팹 인스턴스뿐. 방별 문/앵커 미구축 (사용자 작업) |

## 확정 (2026-08-31)

| # | 결정 |
|---|---|
| 범위 | **한 번에 전부** — 모니터 방배정 + `RoomController` + 새벽 노크 |
| 방배정 | **필수** — 모니터에서 방 안 고르면 손님 클릭해도 승인 안 됨. `nextRoom` 자동증가 제거 |
| 열쇠 대조 | **범위 밖** — `CheckInGuestEffect` 는 `IsKey` 아이템이면 승인. `RoomKey` 대조는 별도 doc |
| 새벽 대화 UI | 문 앞에 배정 손님 스프라이트(상반신) 스폰 + 대화 패널 |
| A. 화면고정 시점 | **노크 즉시** 화면고정 → 3초 대기(문 앞에서 기다림) → 수락이면 문 열림+대화 / 거절이면 화면고정 해제 |
| A. 노크 거절 | 손님별 `NpcData.refusesDawnKnock` 플래그. 거절 시: 3초 뒤 (대사 패널 있으면 "refuse" 노드 한 줄) → 문 안 열고 화면고정만 해제. 손님 스폰 안 함 |
| A. 노크 앵커 | 문 오브젝트 자식에 앵커 Transform 따로 배치 (화면고정 상호작용과 동일 방식). `KnockEffect.anchor` |
| A. RoomController | 문 + **이 방의 상호작용 가구들**을 인스펙터 리스트로 직접 연결 → 새벽 잠금 시 함께 비활성 |
| B. 모니터 조작 | **둘 다 가능** — 접객 자리 화면고정 중에도, 모니터 화면고정으로 크게 봐도 조작. `doc/0119` 게이트가 `UIInteractionMode.Active` 라 자동으로 둘 다 됨 (추가 작업 없음) |
| C. 손님 이동 | 승인 손님은 **현재 `roomPath` 나가기 경로 그대로**. 방별 경로 분기 안 함 |
| D. 모니터 버튼 | **uGUI Button ×10** (`ScreenUI` 아래, `doc/0119` 방식). 물리 Quad 아님 |
| E. 버튼 표시 | 방번호 라벨 + 상태(배정가능/선택됨/사용중) 색·`interactable` |
| F. 재노크 | 대화 종료 후 같은 손님 재노크 가능. 횟수 제한·행동력 없음 |
| G. `Knock` enum | `InteractionPrompt` 끝에 추가, 로컬라이즈 "노크"/"Knock" |

## 설계

3개 새 컴포넌트 + `Interactable`/`HingeEffect`/`ReceptionManager`/`CheckInGuestEffect` 소폭 수정. 새 "모드" 없음 — 기존 화면고정·대화·조건 조합 재사용.

### A. `RoomController` (신규, 방마다 1개)

방의 문·상호작용 가구를 소유하고 새벽에 잠근다. "Room Controller 가 직접 조작" 의 실체.

```csharp
public class RoomController : MonoBehaviour
{
    [SerializeField] private int roomNumber = 101;              // 101~110
    [SerializeField] private Interactable door;                 // 방문 (Interactable+HingeEffect+Sfx)
    [SerializeField] private GameObject knockTarget;            // 노크용 자식 (Interactable(Knock)+KnockEffect+Collider). 시작 비활성
    [SerializeField] private Interactable[] sealedInteractables; // 새벽 잠금 시 함께 비활성화할 이 방 가구들 (인스펙터로 직접 연결)

    public int RoomNumber => roomNumber;

    // 이 방에 배정된 이번 밤 손님. 없으면 null.
    public NpcData NightGuest
    {
        get
        {
            var gm = GuestManager.Instance;
            if (gm == null) return null;
            foreach (var g in gm.Active)
                if (g.verdict == Verdict.Approved && g.room == roomNumber) return g.npc;
            return null;
        }
    }

    private void Awake() { if (knockTarget != null) knockTarget.SetActive(false); }

    private void Start()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged += Apply;
        Apply(DayPhaseManager.Instance != null ? DayPhaseManager.Instance.Current : DayPhase.Morning);
    }
    private void OnDestroy()
    {
        if (DayPhaseManager.Instance != null) DayPhaseManager.Instance.OnPhaseChanged -= Apply;
    }

    private void Apply(DayPhase phase)
    {
        bool seal = phase == DayPhase.Dawn && NightGuest != null;

        if (door != null)
        {
            if (seal) door.SetState(false);        // 닫기 연출
            door.enabled = !seal;                   // 여닫기 상호작용 차단 (CanInteract 가 enabled 확인)
        }
        if (sealedInteractables != null)
            foreach (var it in sealedInteractables)
                if (it != null) it.enabled = !seal;

        if (knockTarget != null) knockTarget.SetActive(seal);   // 노크 상호작용 노출
    }

    // KnockEffect 가 호출 — 방문을 지정 각도로 (Interactable.IsOn 안 건드림)
    public void PeekDoor(float angle, float time)
    {
        var hinge = door != null ? door.GetComponent<HingeEffect>() : null;
        if (hinge != null) hinge.SwingTo(angle, time);
    }
}
```

- **방 잠금** = `door.SetState(false)` + `door.enabled=false` + `sealedInteractables` 전부 비활성 + `knockTarget` 활성. 문이 유일 출입구면 못 들어감. 별도 콜라이더 벽 불필요.
- **문 ↔ 노크는 별개 오브젝트** — `Interactable` 은 한 오브젝트 1개 상호작용만 (`doc/0100` §7). `knockTarget` 은 문틀(회전 안 하는 부분)의 자식. 평소 비활성, 새벽·배정 시만 활성.
- 새벽 외 단계나 미배정 방이면 문·가구 정상, 노크 숨김.

### B. `KnockEffect` (신규, `InteractionEffect`)

`knockTarget` 에 부착. 노크 → (노크음, `SfxEffect` 자동) → **즉시 화면고정** → 3초 대기 → **수락**: 문 `peekAngle` 열림 + 문틈 손님 + 새벽 대화 → 종료 시 문 닫힘 + 화면고정 해제 / **거절**: (대사 한 줄) → 화면고정 해제, 문 안 열림.

```csharp
public class KnockEffect : InteractionEffect
{
    [SerializeField] private Transform anchor;          // 화면고정 포즈 (문 오브젝트 자식). 비우면 이 오브젝트
    [SerializeField] private Transform guestSpawnPoint; // 문틈에 손님이 설 위치 (내가 빈 오브젝트로 지정)
    [SerializeField] private GameObject guestPrefab;    // 접객과 같은 Guest 프리팹 (GuestView + 자식 SpeechBubble)
    [SerializeField] private SpeechBubble dawnPanel;    // 비우면 스폰된 손님의 자식 SpeechBubble 사용. 거절 대사엔 이게 있어야 표시됨
    [SerializeField] private float knockWait = 3f;      // 노크 후 응답까지
    [SerializeField] private float peekAngle = 18f;     // 살짝 열리는 각도 (여기서 지정)
    [SerializeField] private float openTime  = 0.5f;

    private bool busy;

    public override void Play(in InteractionContext ctx)
    {
        if (busy) return;
        var rc  = GetComponentInParent<RoomController>();
        var npc = rc != null ? rc.NightGuest : null;
        if (npc == null) { Debug.Log("[KnockEffect] 이 방에 배정된 손님 없음", this); return; }
        if (UIInteractionMode.Instance == null || DialogueRunner.Instance == null || guestPrefab == null) return;
        StartCoroutine(Knock(rc, npc));
    }

    private IEnumerator Knock(RoomController rc, NpcData npc)
    {
        busy = true;
        UIInteractionMode.Instance.Enter(anchor != null ? anchor : transform);   // 노크 즉시 화면고정 (이동도 여기서 멈춤)

        yield return new WaitForSeconds(knockWait);                               // 문 앞에서 기다림

        if (npc.refusesDawnKnock)                                                 // 거절
        {
            if (dawnPanel != null)   // "refuse" 노드 있으면 문 너머 한 마디
            {
                bool said = false;
                DialogueRunner.Instance.SayNode(npc, dawnPanel, Situation.Dawn, "refuse", () => said = true);
                yield return new WaitUntil(() => said);
            }
            UIInteractionMode.Instance.Exit();
            busy = false;
            yield break;
        }

        // 수락
        rc.PeekDoor(peekAngle, openTime);
        Transform stand = guestSpawnPoint != null ? guestSpawnPoint : transform;
        var guest  = Instantiate(guestPrefab, stand.position, stand.rotation);
        var view   = guest.GetComponentInChildren<GuestView>();
        var bubble = dawnPanel != null ? dawnPanel : guest.GetComponentInChildren<SpeechBubble>(true);
        view?.Apply(npc);

        DialogueRunner.Instance.Play(npc, bubble, Situation.Dawn, _ =>
        {
            view?.Clear();
            Destroy(guest);
            rc.PeekDoor(0f, openTime);            // 문 닫기
            UIInteractionMode.Instance.Exit();    // 화면고정 해제
            busy = false;
        });
    }
}
```

- **화면고정 즉시** = `UIInteractionMode.Enter` 가 플레이어를 앵커로 이동·정지시킴 → 3초 대기 중 이동 문제 없음. 별도 "이동 잠금" 코드 불필요.
- **거절** (`NpcData.refusesDawnKnock`): 3초 뒤 문 안 열고 화면고정만 해제. `dawnPanel`(스크린 패널) 배선돼 있고 CSV 에 `situation=Dawn, nodeKey=refuse` 노드 있으면 문 너머 한 마디. 없으면 조용히 물러남.
- 문 열기 = `RoomController.PeekDoor` → `HingeEffect.SwingTo(angle, time)` (신규 public, 기존 `Swing` 코루틴 재사용, `Interactable.IsOn` 안 건드림).
- 손님은 `guestSpawnPoint`(문 안쪽)에 스폰 → 문짝 메시가 하반신 가려 상반신만. 프레이밍은 `anchor` 로 씬에서 조정.
- `busy` 가드 = 대화·대기 중 재노크·중복 스폰 차단. 종료 후 재노크 가능 (거절 손님도 다시 노크 가능 — 계속 거절).
- 화면고정·대화 스택·ESC·표정은 `UIInteractionMode`+`DialogueRunner`+`GuestView` 가 이미 처리.
- **거절 여부는 지금 정적 플래그.** 밤/스토리별로 바뀌어야 하면 나중에 CSV 조건·`GuestState` 로 (별도 doc).

### C. `Interactable` — `Knock` 프롬프트 추가

```csharp
public enum InteractionPrompt { ..., CheckIn, Knock }   // 끝에만 추가 (직렬화 값 = 순서)

// DefaultPrompt switch
InteractionPrompt.Knock => LocalizationManager.T("Knock", "노크"),
```

- `SyncEffectsToPrompt`(우클릭 재설정): `Knock` → `KnockEffect` + `SfxEffect`. `ManagedEffects` += `KnockEffect`.

### D. `HingeEffect` — 임의 각도 스윙

```csharp
// 기존 openAngle/openRot 는 그대로. 임의 각도 버전만 추가.
public void SwingTo(float angleDeg, float time)
{
    Vector3 a = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.up;
    Quaternion target = closedRot * Quaternion.AngleAxis(angleDeg, a);
    if (swing != null) StopCoroutine(swing);
    swing = StartCoroutine(SwingRoutine(target, time));   // 기존 Swing 을 time 파라미터화 or 별 코루틴
}
```

### E. `MonitorRoomBoard` (신규, "UI Controller") — `ScreenUI` 아래 패널

요청의 "UI Controller". 모니터 화면(uGUI)의 방 버튼 10개를 들고 상태를 갱신 + 클릭 라우팅. `doc/0119` 인프라 위에 얹음.

```csharp
public class MonitorRoomBoard : MonoBehaviour
{
    [Serializable] public struct RoomButton { public int roomNumber; public Button button; public Image tint; public TMP_Text label; }

    [SerializeField] private RoomButton[] rooms;      // 10개, 인스펙터 배선 (roomNumber 101~110)
    [SerializeField] private TMP_Text header;         // "손님: OOO — 방을 선택하세요" (선택)
    [SerializeField] private Color vacant, selected, occupied;

    private void Awake()
    {
        foreach (var r in rooms)
        {
            int n = r.roomNumber;
            r.button.onClick.AddListener(() => ReceptionManager.Instance?.AssignRoom(n));
        }
    }

    private void OnEnable() { Refresh(); }
    private void Update()   { Refresh(); }            // 10개 갱신 — 부담 없음

    private void Refresh()
    {
        var rm = ReceptionManager.Instance;
        if (header != null)
            header.text = rm != null && rm.CurrentGuest != null
                ? LocalizationManager.T($"Guest: {rm.CurrentGuest.DisplayName}", $"손님: {rm.CurrentGuest.DisplayName}")
                : LocalizationManager.T("No guest", "대기 중인 손님 없음");

        foreach (var r in rooms)
        {
            bool occ = GuestManager.Instance != null && Occupied(r.roomNumber);
            bool pick = rm != null && rm.PendingRoom == r.roomNumber;
            r.tint.color = occ ? occupied : pick ? selected : vacant;
            r.button.interactable = !occ && rm != null && rm.CurrentGuest != null;
            if (r.label != null) r.label.text = $"{r.roomNumber}";
        }
    }

    private static bool Occupied(int room) { /* GuestManager.Active 에 room 사용중인 Approved 있나 */ }
}
```

- **버튼 자체가 배정 로직 없음** — `onClick` → `ReceptionManager.AssignRoom(n)` 만. 상태 표시는 `Refresh`.
- 접객 자리 화면고정에서도, 모니터 화면고정에서도 동일하게 동작 (`doc/0119` 게이트).
- `RoomController` 를 참조 안 함 — 배정 상태의 진실은 `GuestManager`.

### F. `ReceptionManager` 수정 (소폭)

```csharp
public int PendingRoom { get; private set; } = -1;
public NpcData CurrentGuest { get; private set; }        // 현재 승인 대기 손님 (보드 헤더/게이트용)

public void AssignRoom(int room)
{
    if (CurrentGuest == null) return;                     // 대기 손님 없으면 무시
    if (RoomOccupied(room)) return;                       // 이미 찬 방 무시
    PendingRoom = room;
}
```

- `GuestQueue` 루프: 승인 대기 진입 시 `CurrentGuest = npc; PendingRoom = -1;`. 손님 처리 끝/세션 끝 → `CurrentGuest = null`.
- **방배정 필수**: 승인 대기 루프에서 열쇠 들고 클릭해도 `PendingRoom <= 0` 이면 승인 안 됨.
  - `CheckInGuestEffect` 프롬프트: 열쇠 有 & `PendingRoom>0` → "체크인" / 열쇠 有 & 방 미배정 → "방 배정 필요" / 빈손 → "대화".
  - `ConfirmCheckIn()` 실행 조건에 `PendingRoom > 0` 추가.
- 체크인: `GuestManager.Instance?.CheckIn(npc, PendingRoom, DayNow()); PendingRoom = -1;` — 그 뒤 손님 이동은 **기존 `roomPath` 그대로**.
- `firstRoomNumber`/`nextRoom` 자동증가 **제거**.

## 조립 레시피

| 대상 | 구성 |
|---|---|
| **방 (×10)** | 루트에 `RoomController`(roomNumber 101~110). 배선: `door`(방문 `Interactable`+`HingeEffect`+`SfxEffect`), `knockTarget`, `sealedInteractables`(이 방 가구들). 자식: **KnockTarget**(문틀 자식, `Interactable` Knock + `KnockEffect` + BoxCollider(Interaction 레이어), **시작 비활성**) + **노크 앵커**(문 자식 Transform) + **손님 스폰 포인트**(빈 Transform, 문 안쪽) |
| **KnockEffect 배선** | `anchor`(노크 앵커), `guestSpawnPoint`(손님 스폰 포인트), `guestPrefab`(접객 Guest 프리팹), `dawnPanel`(거절 대사 표시용, 선택), `knockWait`/`peekAngle`/`openTime` |
| **모니터 방 버튼 (×10)** | `CRTMonitor/screenON/ScreenUI` 아래 uGUI `Button` ×10 (`doc/0119`). `MonitorRoomBoard`(같은 Canvas 밑) `rooms[]` 에 각 버튼+roomNumber 배선 |
| **새벽 대화 패널** | `KnockEffect.dawnPanel` 비우면 스폰 손님의 자식 `SpeechBubble`. 접객 `Dialogue_Panel` 재사용도 가능 |
| **침대** | 기존 `하루종료` (새벽→아침). 아침 되면 `RoomController.Apply` 가 문·가구 원복 |
| **CampaignData** | `eveningGuestIds` 손님 수 ≤ 10 |

## 데이터 작업

- `Assets/My/Data/Dialogue/*.csv` 에 `situation=Dawn` 행 (손님별 새벽 대사). 임포터 그대로. 최소: 수락 손님 1명분 + 거절 손님용 `nodeKey=refuse` 한 줄.
- 새벽 대사 없는 수락 손님 → `DialogueRunner` 경고 + 스킵 → 화면고정만 됐다 풀림.
- `refusesDawnKnock` 손님 에셋에 플래그 체크.

## 영향 파일

```
Interaction/RoomController.cs               신규
Interaction/Effects/KnockEffect.cs          신규
Game/MonitorRoomBoard.cs                    신규
Interaction/Interactable.cs                 수정  enum +Knock, DefaultPrompt, SyncEffectsToPrompt, ManagedEffects
Interaction/Effects/HingeEffect.cs          수정  public SwingTo(angle, time)
Interaction/Effects/CheckInGuestEffect.cs   수정  PendingRoom>0 게이트 + 프롬프트
Game/ReceptionManager.cs                    수정  PendingRoom/CurrentGuest/AssignRoom, 방배정 필수, nextRoom 제거
Dialogue/NpcData.cs                         수정  bool refusesDawnKnock (게임 로직 플래그)
Docs/  RoomController.md·KnockEffect.md·MonitorRoomBoard.md 신규 · ReceptionManager.md·Interactable.md·HingeEffect.md·Overview.md 갱신
Data/Dialogue/*.csv                         situation=Dawn 샘플 (+ 거절 손님용 nodeKey=refuse)
InGame.unity / CRTMonitor.prefab           씬·프리팹 작업 (방 10개, 모니터 버튼 10개, 보드)
```

## 스킵 (YAGNI)

- 객실 청소·상태 순환(SYS-01), `RoomData` SO — `GuestState.room` 로 충분.
- 새벽 행동력 / 노크 횟수 제한.
- "배정된 방 열쇠"(`RoomKey`) 대조 — 별도 doc.
- 밤 판정(몽유병자 처리) — 데이터(`isSleepwalker` vs `verdict`)만.
- 방 소진("빈 방 없음") 처리 — 손님 수 ≤ 10 가정.
- 방별 손님 이동 경로 — 공용 `roomPath`.

## 구현 완료 (코드, 2026-08-31)

`uloop compile` Error 0. 에디터 배선/검증 대기.

| 파일 | 내용 |
|---|---|
| `Interaction/RoomController.cs` | 신규. 각 방에 컴포넌트. `roomNumber`(101~110). **정문**: `frontDoor`(Interactable) + `knockTarget`(노크 자식, 시작 비활성) + `knockAnchor`(화면고정 위치) + `guestSpawnPoint`(손님 스폰). **가구**: `sealedInteractables[]`(내부문·침대 등 원하는 만큼). `OnPhaseChanged` 구독 → `Dawn` + `NightGuest != null` 이면 `frontDoor.SetState(false)`+`enabled=false`, `sealedInteractables` 전부 `enabled=false`, `knockTarget` 활성. 아침에 원복. `PeekDoor(angle,time)` → `HingeEffect.SwingTo`. `NightGuest` = `GuestManager.GuestInRoom(roomNumber)` |
| `Interaction/Effects/KnockEffect.cs` | 신규. `knockTarget` 에 부착. 앵커/스폰포인트는 부모 `RoomController` 에서 읽음. 노크 → `Enter(knockAnchor)` 즉시 화면고정 → `knockWait`(3s) → `npc.refusesDawnKnock` 면 (`dawnPanel`+CSV `refuse` 노드 있으면 한 마디) 후 `Exit()` / 아니면 `PeekDoor(peekAngle)` + `guestSpawnPoint` 에 손님 스폰(`GuestView.Apply`, 손님 Interactable 끔) + `DialogueRunner.Play(Dawn)` → 종료 콜백에서 손님 파괴 + `PeekDoor(0)` + `Exit()`. `busy` 가드. `guestPrefab` 비우면 `ReceptionManager.GuestPrefab` 폴백 |
| `Game/MonitorRoomBoard.cs` | 신규. `ScreenUI`(doc/0119) 아래. `RoomButton{roomNumber, button, tint, label}[]`. `Awake` 에서 `button.onClick → ReceptionManager.AssignRoom(n)`. `Update`→`Refresh`: 헤더에 `CurrentGuest` 이름, 각 버튼 색(빈방/선택됨/사용중) + `interactable`(대기 손님 有 & 미사용) |
| `Interaction/Interactable.cs` | `InteractionPrompt` 끝에 `Knock`. `DefaultPrompt` "Knock"/"노크". `SyncEffectsToPrompt` `Knock`→`KnockEffect`. `ManagedEffects` += `KnockEffect` |
| `Interaction/Effects/HingeEffect.cs` | `public SwingTo(float angleDeg, float time)` — 닫힘 기준 임의 각도 스윙, `IsOn` 안 건드림. 기존 `Swing` 을 `(target, dur)` 로 파라미터화해 공용 |
| `Interaction/Effects/CheckInGuestEffect.cs` | 열쇠 든 채 클릭 시 `rm.PendingRoom <= 0` 이면 **열쇠 소모 없이** 리턴 + 로그. 프롬프트: 열쇠+방배정="체크인" / 열쇠+미배정="방 배정 필요" / 빈손="대화" |
| `Game/ReceptionManager.cs` | `firstRoomNumber`/`nextRoom` 제거. `public NpcData CurrentGuest` / `public int PendingRoom(-1)` / `public void AssignRoom(int)` (대기 손님 有 & 미사용 방일 때 세팅). `public GameObject GuestPrefab` getter. 큐: 손님 시작 시 `CurrentGuest=npc; PendingRoom=-1`, 승인 시 `CheckIn(npc, PendingRoom)` 후 `-1`, 손님/세션 종료 시 클리어. `ConfirmCheckIn` 에 `PendingRoom > 0` 조건 추가 |
| `Game/GuestManager.cs` | `NpcData GuestInRoom(int room)` (Approved + room 일치) + `bool RoomTaken(int)` |
| `Dialogue/NpcData.cs` | `public bool refusesDawnKnock` (게임 로직 섹션) |
| `Docs/` | `RoomController.md`·`KnockEffect.md`·`MonitorRoomBoard.md` 신규, `ReceptionManager.md`·`Interactable.md`·`HingeEffect.md` 갱신 |

### 사용자 작업 (씬/에셋)

1. **방 ×10**: 각 방 루트에 `RoomController` → `roomNumber` 101~110. `frontDoor`(방문), `knockAnchor`(정문 자식 Transform, 화면고정 포즈), `guestSpawnPoint`(문 앞 빈 오브젝트), `sealedInteractables`(내부문·침대·기타). `knockTarget` = 정문 자식 오브젝트: `Interactable`(promptType=Knock, 우클릭 재설정 → `KnockEffect`+`SfxEffect`) + BoxCollider(Interaction 레이어), **비활성 시작**(RoomController Awake 가 꺼줌).
2. **KnockEffect**: `guestPrefab` 비워두면 `ReceptionManager` 것 사용. `dawnPanel` = 접객 `Dialogue_Panel`(거절 대사 표시하려면 필수). `knockWait`/`peekAngle`/`openTime` 취향.
3. **모니터**: `CRTMonitor/screenON/ScreenUI`(doc/0119) 아래 방 버튼 10개 + `MonitorRoomBoard`. `rooms[]` 에 각 버튼·roomNumber·tint(Image)·label(TMP) 배선. `RawImage.raycastTarget=false` 확인.
4. **CSV**: `situation=Dawn` 행 (수락 손님 대사) + 거절 손님용 `nodeKey=refuse` 한 줄. `Tools > Dialogue > Import CSV`.
5. **NpcData**: 거절 손님 에셋 `refusesDawnKnock` 체크.
6. **CampaignData**: 하루 손님 ≤ 10.

### 검증
- 저녁: 손님 대화 후 모니터 버튼 클릭 → 방 선택 색 → 열쇠 들고 손님 클릭 → 체크인(방 미선택이면 "방 배정 필요", 열쇠 안 닳음).
- 새벽: 배정된 방 정문 프롬프트가 "노크" → 노크 → 즉시 화면고정 → 3초 → 거절 손님이면 (한 마디) 화면 풀림 / 수락이면 문 `peekAngle` 열림 + 손님 스폰 + 질문 대화 → 종료 시 문 닫힘 + 화면 풀림.
- 미배정 방 정문은 평소처럼 여닫기. 아침 되면 전부 원복.

## 에디터 배선 + 플레이 검증 (2026-08-31)

씬/프리팹 확인 결과 RoomController 는 붙어 있었으나 **작동 불가 상태**였음:
- 10개 방 전부 `roomNumber = 101` (프리팹 기본값, 인스턴스 오버라이드 안 됨)
- `knockTarget` 이 `front_Door` 와 같은 오브젝트 (별도 노크 오브젝트·`KnockEffect` 컴포넌트 없음)
- `sealedInteractables` 에 정문 포함

### 수정 (uloop execute-dynamic-code)

| 대상 | 변경 |
|---|---|
| `Motel_Room.prefab` | `front_Door` 자식에 **`Knock_Target`** 생성 — 빈 GO + `BoxCollider`(1.35×3.4×0.6, Interaction 레이어) + `Interactable`(promptType=Knock) + `KnockEffect`, **비활성 시작**. `RoomController.knockTarget` → `Knock_Target`. `sealedInteractables` 에서 `front_Door` 제거 (내부문 `Door` 는 유지) |
| `InGame.unity` 10개 인스턴스 | `roomNumber` = 이름의 번호(101~110). `knockTarget` = 각자 `Knock_Target`. `KnockEffect.dawnPanel` = `GameManager` 의 `SpeechBubble` (접객과 공용). `sealedInteractables` 에서 정문 제거 |

### 플레이 검증 (손님 id 1 → 103호 CheckIn → Dawn)

- ✅ Dawn 진입 시 **103호만** 봉인: `front_Door.enabled=false`, `Knock_Target` 활성, 프롬프트 "노크", `CanInteract=true`. 나머지 9개 방 그대로
- ✅ 노크 → 즉시 화면고정 (`UIInteractionMode` Depth 1)
- ✅ 3초 후 → `front_Door` Y축 18°(`peekAngle`) 열림 + 손님 스프라이트 스폰(`Guest_Spawn_Pos`, 초상화 `접객_0`) + `Situation.Dawn` 대화 재생 ("이 시간에 무슨 일이십니까?" + 질문 선택지)
- ✅ 대화 종료 → 손님 파괴 + 문 닫힘 + 화면고정 해제, `busy` 리셋 (재노크 가능)
- 예외 로그 없음. `uloop compile` Error 0

### 남은 튜닝 (사용자)

- **Dawn 대사**: 현재 CSV 에 id 1 만 있음. id 2~5 는 노크 시 화면고정 3초 후 대사 없이 풀림 → `situation=Dawn` 행 추가 필요
- **프레이밍**: `Knock_Anchor` 위치/피치 + `Guest_Spawn_Pos` 를 방마다 조정해 문틈에 손님 상반신이 잡히게 (새벽 조명이 매우 어두워 자동 확인 불가)
- `HingeEffect.hinge` 가 null 이라 `front_Door` 전체가 회전(자식 `Knock_Anchor`/`Knock_Target` 포함) — 화면고정이 문 열리기 전이라 무해. 문짝만 돌리려면 `hinge` 를 `Hinge` 자식으로
- `refusesDawnKnock` 손님 아직 없음 → 거절 경로는 코드만 있고 미검증

## 추가 수정 (2026-08-31, 노크 잠금 + 둘러보기 제한)

| 파일 | 변경 |
|---|---|
| `Interaction/Modes/UIInteractionMode.cs` | `Enter(Transform, float lookScale)` 오버로드 추가 (기존 `Enter(Transform)` = lookScale 1). 앵커별 `lookScale` 를 `anchorLookScales` 스택에 push/pop. `EdgeLook()` 이 `yawRange`/`pitchRange` 에 `CurLookScale` 곱함 (0=완전고정, 1=기본) |
| `Interaction/Effects/KnockEffect.cs` | ① `[SerializeField] float lookScale = 0.25f` → `Enter(anchor, lookScale)` 로 노크 화면고정은 거의 안 움직임. ② 시퀀스 시작 시 `self.enabled = false` (자기 Interactable), 종료(`EndSequence`)에 `= true` → **대화 종료까지 재노크·노크음 반복 차단**. `OnDisable` 안전 리셋 |

검증: 1차 노크 후 `Interactable.enabled=false`, `CanInteract=false`, 재노크 3회 무동작. `anchorLookScales=[0.25]`. `uloop compile` Error 0.

## 상태

2026-08-31 코드 + 프리팹/씬 배선 + 플레이 검증 완료. Dawn 대사 데이터 + 방별 프레이밍 튜닝만 남음.
