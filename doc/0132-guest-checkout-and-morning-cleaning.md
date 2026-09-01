# 0132 - 손님 체크아웃 + 청소 요청 기반 아침 객실 개방 (제안)

날짜: 2026-09-01
관련: `doc/0118`(RoomController / 새벽 노크), `doc/0120`(숙박 일수 대사 플레이버), `doc/0122`(객실 분위기 — 미구현), `doc/0131`(새벽 대사 5명), `Docs/RoomController.md`, `Docs/DayPhaseManager.md`

## 요청 (원문)

> 위 내용을 참고해서 처음에 숙박 시작할때 청소 요청을 한 손님의 방만 아침에 청소가 가능하도록 하고
> 만약 요청을 안한 손님의 경우는 아침, 점심, 저녁에도 그대로 문이 안열리도록 해줘
> 아침, 점심 때는 노크해도 거절하는 비율이 높거나 없어지도록
> 일단 설계안 작성해줘

배경(대화): 장기 투숙객도 체크아웃 전까지 아예 청소를 안 하는 건 아니지만, 게임에서는
**기본 = 체크아웃 후 대청소 / 숙박 중 청소 = 손님이 요청했을 때만** 이 자연스럽고, 숙박 중 청소 방문이
공포 이벤트 장치로 좋다.

## 현재 상태

| 조각 | 지금 |
|---|---|
| `GuestState` | `{ npc, room, verdict, checkInDay }`. `GuestManager.active` 리스트에 누적. `CheckOut(npc)` = 리스트에서 제거 — **어디서도 호출 안 함** (체크아웃 개념 없음) |
| 숙박 일수 | 코드상 추적 안 함. 대사 플레이버만 (`doc/0120`) |
| `RoomController.Apply(phase)` | `seal = phase == Dawn && NightGuest != null`. **새벽에만** 정문 잠금 + `knockTarget` 활성. 그 외 단계는 방 정상 |
| 새벽 노크 | `KnockEffect` 완성. `NpcData.refusesDawnKnock` 로 거절 (`doc/0118`·`0131`) |
| 객실 mess/mood | `doc/0122` 제안만, **미구현**. 방에 `Bed`(CleanUp) 등 개별 상호작용은 이미 있음 |
| 하루 순환 | `Morning → Noon → Evening(접객) → Dawn`, `Morning` 진입 시 `DayCount++` |
| 모니터 방배정 | `GuestManager.RoomTaken(room)` = 그 방에 `Approved` 손님 있나. 체크아웃 안 하면 계속 `true` (재배정 차단) |

즉 지금은 **손님이 영원히 체크인 상태**로 남고, 새벽 외 시간대엔 방문이 그냥 열린다.

## 설계 목표

1. 손님마다 **아침 청소 허용 여부** 플래그.
2. 허용 손님: 체크아웃하는 아침에 방문이 열려 들어가 청소 가능 → 그 뒤 방 비워짐.
3. 미허용 손님: 새벽 이후 **아침·점심·저녁 내내 방문 잠김**. 하루 뒤 아침에 (뒤늦게) 체크아웃 → 그때 개방.
4. 새벽 외(아침·점심) 노크: **기본 = 노크 자체가 없음** (문만 잠긴 상태). 옵션으로 "항상 거절".
5. 최소 변경 — 새 매니저·새 SO 없이 `RoomController` + `GuestState` 로.

## 설계

### A. `NpcData` — 청소 허용 플래그 (정적)

```csharp
[Header("게임 로직 ...")]
public bool refusesDawnKnock;      // 기존
public bool allowsMorningCleaning; // 신규 — 체크아웃/청소를 위해 아침에 방을 열어줌. 기본 false = "방에 들어오지 마세요"
```

- `refusesDawnKnock` / `visitorOnly` 와 같은 자리, 같은 성격 (손님 정체성). 플레이어에게 안 보임.
- 손님은 접객 대사로 자기 성향을 밝힘 ("아침에 청소 좀 부탁합니다" / "묵는 동안 방엔 들어오지 마세요"). **대사는 플레이버, 코드 분기 아님.**
- 기본 `false` = 대부분의 손님이 방을 안 열어줌 (공포 톤 유지). 열어주는 손님이 예외.

**업그레이드 씨앗:** 플레이어가 접객 중 "매일 하우스키핑 넣을까요?" 를 제안하고 손님이 수락/거절 →
결과를 `GuestState.cleaningRequested` 에 저장. 지금은 정적 플래그를 그대로 복사만 한다.

### B. `GuestState` / `GuestManager` — 체크아웃 일자

`GuestState` 에 필드 **1개** 추가. 체크아웃 일자는 계산.

```csharp
[Serializable]
public class GuestState
{
    public NpcData npc;
    public int room = -1;
    public Verdict verdict = Verdict.None;
    public int checkInDay;
    public bool cleaningRequested;          // 신규 — 체크인 시 npc.allowsMorningCleaning 복사

    // 청소 허용: 체크인 다음날 아침 체크아웃 / 미허용: 하루 더 잠그고 그 다음날 아침 체크아웃
    public int CheckOutDay => checkInDay + (cleaningRequested ? 1 : 2);
}
```

`CheckIn` 이 플래그를 채운다 (호출부 `ReceptionManager` 는 손 안 댐):

```csharp
public GuestState CheckIn(NpcData npc, int room, int day)
{
    var s = Get(npc) ?? AddNew(npc);
    s.room = room;
    s.verdict = Verdict.Approved;
    s.checkInDay = day;
    s.cleaningRequested = npc != null && npc.allowsMorningCleaning;   // 신규
    return s;
}
```

- `CheckOut(npc)` 는 이미 있음 (리스트에서 제거). 이제 `RoomController` 가 호출.
- `GuestInRoom` / `RoomTaken` 은 그대로 — 체크아웃 전까지 방이 "사용중" 으로 잡혀 재배정 차단됨. **변경 없음.**

### C. `RoomController.Apply(phase)` — 전 단계로 확장

핵심 변경. `seal` 을 새벽 전용에서 **체크인 다음부터 체크아웃까지** 로 넓히고, 체크아웃 아침만 개방.

```csharp
private void Apply(DayPhase phase)
{
    var gm  = GuestManager.Instance;
    var g   = gm != null ? gm.GetStateInRoom(roomNumber) : null;   // Approved + room 일치 (신규 헬퍼, GuestInRoom 의 GuestState 버전)
    int day = DayPhaseManager.Instance != null ? DayPhaseManager.Instance.DayCount : 1;

    // 체크아웃 아침이 지났으면 정산
    if (g != null && !(day == g.CheckOutDay && phase == DayPhase.Morning) && day >= g.CheckOutDay)
    {
        gm.CheckOut(g.npc);
        // ResetRoom();   // doc/0122(mess/mood) 들어오면 여기서 기본값 복구. 지금은 방에 mess 상태가 없어 불필요
        g = null;
    }

    bool present        = g != null;
    bool checkInEvening = present && phase == DayPhase.Evening && day == g.checkInDay;  // 접객 중 = 걸어들어오는 중
    bool cleanWindow    = present && phase == DayPhase.Morning && day == g.CheckOutDay; // 체크아웃 아침 = 청소 가능
    bool seal           = present && !checkInEvening && !cleanWindow;

    if (frontDoor != null)
    {
        if (seal) frontDoor.SetState(false);
        frontDoor.enabled = !seal;
    }
    if (sealedInteractables != null)
        foreach (var it in sealedInteractables)
            if (it != null) it.enabled = !seal;

    // 노크는 새벽에만 (요청 5: 아침·점심 노크는 "없어짐")
    if (knockTarget != null) knockTarget.SetActive(seal && phase == DayPhase.Dawn);
}
```

방별 시나리오 (체크인 = Day 1 저녁):

| 단계 | 청소 허용 손님 (CheckOutDay 2) | 미허용 손님 (CheckOutDay 3) |
|---|---|---|
| Day1 저녁 (체크인) | 개방 (걸어들어옴) | 개방 |
| Day1 새벽 | 잠김 + 노크(탐문) | 잠김 + 노크(탐문, `refusesDawnKnock` 적용) |
| **Day2 아침** | **개방 — 손님 없음, 방 청소** | 잠김, 노크 없음 |
| Day2 점심 | 정산 → 빈방 | 잠김, 노크 없음 |
| Day2 저녁 | 빈방 (재배정 가능) | **잠김** (요청 2·3 충족) |
| Day2 새벽 | — | 잠김 + 노크(탐문 2일차) |
| **Day3 아침** | — | **개방 — 뒤늦은 대청소** |
| Day3 점심 | — | 정산 → 빈방 |

- **청소 = 별도 미니게임 없음.** 방 안의 기존 `CleanUp` 상호작용(침대 정리 등)을 그대로 씀. `doc/0122` mess/mood 가 들어오면 그게 청소 대상이 되고, `ResetRoom()` 훅에 "안 치우고 체크아웃 시 페널티" 를 나중에 얹는다.
- **체크아웃 = 방문 개방 + `GuestManager.CheckOut`.** 손님 스프라이트는 새벽 `KnockEffect` 가 대화 끝에 이미 파괴 — 아침엔 방에 아무도 없다 (설정상 나갔음).
- `GetStateInRoom(int)` = `GuestInRoom` 의 `GuestState` 반환 버전. `GuestInRoom` 을 이걸로 감싸도 됨 (2줄).

### D. 노크 (요청 5)

`C` 의 `knockTarget.SetActive(seal && phase == DayPhase.Dawn)` 한 줄로 끝 — **아침·점심·저녁엔 노크 상호작용이 아예 안 뜬다.** 문만 잠겨 있음. 사용자가 제시한 "없어지도록".

**옵션 (원하면):** 아침·점심에도 노크는 보이되 항상 거절 — 손님이 능동적으로 문을 안 열어준다는 느낌. `KnockEffect` 에 `if (phase != Dawn) → refuseMessages + (CSV refuse 노드) → Exit()` 분기 (~5줄) + CSV `situation=Dawn nodeKey=refuse` 재사용. 이게 낫다 싶으면 그렇게.

### E. 대사 (코드 아님)

- 접객 CSV: 손님이 청소 성향을 한 줄 언급 (플레이버). 예:
  - 허용: "아침에 방 정리 좀 부탁드립니다." / "청소는 편하실 때 들어오세요."
  - 미허용: "묵는 동안엔 방에 안 들어오셨으면 해요." / "청소 필요 없습니다. 그냥 두세요."
- `Situation.Checkout` 은 **안 만든다** — 아침엔 손님이 이미 없어 대면 대화가 없음. 프런트 체크아웃 대화를 원하면 별도 doc.

### F. 샘플 5명 값 (제안)

| id | 컨셉 | allowsMorningCleaning |
|---|---|---|
| 1 떠돌이 | 하룻밤, 털털 | **true** |
| 2 외판원 (불안) | 짐 못 건드리게 함 | false |
| 3 거만한 단골 | "내 방 건드리지 마" | false |
| 4 경계심 여성 | 이미 `refusesDawnKnock` | false |
| 5 노인 | 장기 투숙, 살가움 | **true** |

## 영향 파일

```
Dialogue/NpcData.cs            수정  + bool allowsMorningCleaning
Game/GuestManager.cs           수정  GuestState + cleaningRequested / CheckOutDay,
                                     CheckIn 이 플래그 세팅, + GetStateInRoom(int)
Interaction/RoomController.cs   수정  Apply(phase) 재작성 (전 단계 seal + 체크아웃 정산 + 노크 새벽 한정)
Npc_1~5.asset                  에셋  allowsMorningCleaning (id 1, 5 = true)
Data/Dialogue/sample.csv       데이터 접객 대사에 청소 성향 한 줄씩 (플레이버)
Docs/RoomController.md         갱신
```

새 스크립트 0개. 새 씬 배선 0개 (기존 `RoomController` 필드 그대로).

## 스킵 (YAGNI)

- 플레이어가 청소 여부를 고르는 접객 선택지 — 정적 `NpcData` 플래그로 시작. 씨앗 = `GuestState.cleaningRequested`.
- 청소 품질 채점 / 안 치우고 체크아웃 시 페널티 — `ResetRoom()` 훅 위치만 잡아둠.
- 전용 청소 미니게임 — 방 안 기존 `CleanUp` 상호작용 재사용.
- `Situation.Checkout` 대화 / 프런트 체크아웃 절차.
- 2박 초과 숙박, 대사(`doc/0120`)에서 숙박 일수 읽어오기.
- `RoomData` SO / `HousekeepingManager` — `RoomController` + `GuestState` 로 충분.
- 새벽 외 노크 확률 굴림 — 노크는 그냥 안 뜸 (옵션 D 로 업그레이드).
- 살해(`Verdict.Killed`) 손님 방 처리 — `GuestInRoom` 이 `Approved` 만 보므로 방은 열림. 별도.

## 확인 답변 (2026-09-02)

1. **미허용 손님 체크아웃**: 숙박 일수를 손님마다 정확히 정함. 1박이면 다음날 아침에 나가고, **나간 방(체크아웃 아침)은 문 열고 들어가 청소** — 청소 허용/미허용 무관.
2. **새벽 외 노크**: **보이되 항상 거절**. (`KnockEffect` 가 `Current != Dawn` 이면 거절 분기)
3. **샘플 5명 청소 허용**: id **1·2·5** = true / 3·4 = false.
4. **숙박 일수**: `NpcData.stayNights` 로 손님마다 지정. **청소 허용 + stayNights ≥ 2** 면 숙박 중 **매일 아침** 해당 방 청소 가능(하우스키핑). 체크아웃은 `checkInDay + stayNights` 아침.

추가 요청:
- 모니터 방배정 UI 정보 계속 갱신 → `MonitorRoomBoard` 가 이미 `Update()` 마다 `RoomTaken` 재조회. 체크아웃 시 `GuestManager.CheckOut` 으로 `active` 에서 빠지면 자동으로 "빈방/재배정 가능" 표시. **코드 변경 없음.**
- 테스트: 매 저녁 접객마다 카탈로그의 5명이 랜덤 순서로 오도록 (`ReceptionManager.testShuffleAllGuests`, 기본 on). 이미 투숙 중인 손님은 제외.
- 아침 청소 창이 열릴 때 `RoomController` 가 침대를 흐트러진(정리 안 된) 상태로 전환 → 플레이어가 방 안 `Bed` 의 CleanUp 으로 정리.

## 구현 (2026-09-02) — `npx uloop-cli compile` Error 0

| 파일 | 변경 |
|---|---|
| `Dialogue/NpcData.cs` | `+ int stayNights = 1` (`[Min(1)]`), `+ bool allowsMorningCleaning` |
| `Game/GuestManager.cs` | `GuestState + int stayNights + bool cleaningRequested`, `CheckOutDay => checkInDay + stayNights`. `CheckIn` 이 npc 에서 두 값 복사. `+ GuestState StateInRoom(int)`, `GuestInRoom` 을 그 위에 재구현 |
| `Interaction/RoomController.cs` | `Apply(phase)` 재작성 — 전 단계 `seal` + 체크아웃 정산(`CheckOut` 호출) + 노크는 `seal` 이면 항상 노출. `+ GameObject[] messyObjects / tidyObjects` + `SetMessy()`, 아침 청소 창(체크아웃 아침 or 청소허용 손님 숙박중 아침) 진입 시 호출 |
| `Interaction/Effects/KnockEffect.cs` | 대기 후 분기 조건 `npc.refusesDawnKnock` → `!isDawn || npc.refusesDawnKnock` (새벽 아니면 무조건 거절) |
| `Game/ReceptionManager.cs` | `+ bool testShuffleAllGuests(기본 true)`. `BuildGuestIds()` — 테스트면 `catalog.npcs`(투숙중 제외) Fisher-Yates 셔플, 아니면 캠페인. `GuestQueue(DayPlan)` → `GuestQueue(List<int>)` |
| `NPC_Data/Npc_*.asset` | id1: 1박·청소O / id2: 2박·청소O / id3: 3박·청소X / id4: 2박·청소X / id5: 3박·청소O |

### `RoomController.Apply` 최종 로직

```
g = GuestManager.StateInRoom(roomNumber)                  // Approved + 방 일치
if g != null && day >= g.CheckOutDay && !(day==CheckOutDay && Morning):
    GuestManager.CheckOut(g.npc); g = null                // 체크아웃 아침 지남 → 정산

present         = g != null
checkInEvening  = present && Evening && day == checkInDay          // 접객 중 = 개방
checkoutMorning = present && Morning && day == CheckOutDay         // 대청소 = 개방
cleaningMorning = present && Morning && cleaningRequested
                  && checkInDay < day < CheckOutDay               // 숙박 중 하우스키핑 = 개방
seal = present && !checkInEvening && !(checkoutMorning || cleaningMorning)

frontDoor.enabled = !seal ; if seal: frontDoor.SetState(false)
sealedInteractables[*].enabled = !seal
knockTarget.SetActive(seal)                               // 잠겼으면 노크 노출, 새벽 아니면 항상 거절
if (checkoutMorning || cleaningMorning): SetMessy()       // 침대 등 흐트러뜨림
```

### 방별 진행 (체크인 = Day1 저녁, stayNights=N)

| 단계 | N=1 청소O/X | N=2 청소O | N=2 청소X | N=3 청소O |
|---|---|---|---|---|
| Day1 저녁 | 개방(입실) | 개방 | 개방 | 개방 |
| Day1 새벽 | 잠김+노크(탐문) | 잠김+노크 | 잠김+노크 | 잠김+노크 |
| Day2 아침 | **개방·대청소·정산** | **개방·하우스키핑** | 잠김·노크거절 | **개방·하우스키핑** |
| Day2 점심~새벽 | 빈방 | 잠김(재실) | 잠김 | 잠김 |
| Day3 아침 | — | **개방·대청소·정산** | **개방·대청소·정산** | **개방·하우스키핑** |
| Day3 점심~ | — | 빈방 | 빈방 | 잠김 |
| Day4 아침 | — | — | — | **개방·대청소·정산** |

## 사용자 작업 (씬/에셋)

1. ~~각 방 `RoomController` messyObjects/tidyObjects 배선~~ **완료** — `Motel_Room.prefab` 의
   `RoomController` 에 `messyObjects = [Bed/Bed_02.001, Bed (1)/Bed_02.001]` (흐트러짐, 초기 활성),
   `tidyObjects = [.../Bed_01.001 ×2]` (정리됨, 초기 비활성) 배선 (10개 인스턴스 상속). uloop 로 저장.
2. **`ReceptionManager.testShuffleAllGuests`**: 테스트 끝나면 끄고 `CampaignData` 편성 사용.
3. **`NpcCatalog`**: 5개 `NpcData` 다 등록돼 있는지 (우클릭 "프로젝트의 NpcData 전부 수집").
4. **접객 CSV**(선택): 손님이 청소 성향 한 줄 언급 (플레이버, 코드 무관).
5. **플레이 검증**: N/Q 로 단계 넘기며 위 "방별 진행" 표 확인 (특히 청소 아침 개방·침대 흐트러짐, 새벽 아닌 노크 거절, 체크아웃 후 모니터 재배정).

## 스킵 (YAGNI) — 유지

- 플레이어 선택지로 청소 여부 결정 (정적 `NpcData` 플래그. 씨앗 = `GuestState.cleaningRequested`)
- 청소 품질 채점 / 안 치우고 체크아웃 페널티
- 전용 청소 미니게임 (방 안 기존 `CleanUp` 재사용)
- `Situation.Checkout` 대화 / 프런트 체크아웃 절차
- `RoomData` SO / `HousekeepingManager`
- 방 소진("빈 방 없음") 처리 — 테스트 셔플로 5명 동시 투숙 시 방 부족 가능, 그때 손님은 체크인 못 함(거절/보류)
- 살해(`Verdict.Killed`) 손님 방 처리

## 상태

2026-09-02 코드 구현 + 컴파일 검증 완료. 씬 배선(messyObjects/tidyObjects) + 플레이 검증 대기.
