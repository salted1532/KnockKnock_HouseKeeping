# 0137 - 1박 요금 + 접객 대화 결제 시스템 (선불/후불/2배) (제안)

날짜: 2026-09-02
관련: `doc/0132`(체크아웃·`GuestState.stayNights`), `doc/0135`(하우스키핑 선택지 `clean_yes/clean_no` 훅),
`기획/운영-업무-콘텐츠-및-새벽-룸서비스-설계안.md` §7(취침 정산), `기획/핵심컨셉-분석및제언.md` 12장(경제 가볍게),
`Docs/ReceptionManager.md`, `Docs/GuestManager.md`, `Docs/RoomController.md`, `Docs/DialogueRunner.md`

## 요청 (원문)

> 1박 비용을 정하고 현재 대사 및 선택지를 통해서 돈을 받는 시스템을 구축해줘
> canvas에 Money라는 텍스트를 추가했는데 현재 돈을 보여주는거야
> 그리고 선불을 받는다 이런 선택지가 있는데 후불로 하면 아침에 돈 들어오는식으로 하는거지
> 돈 들어올때 사운드도 연결할수 있도록 해주고
> 대화를 통해서 2배로 준다는것도 2배로 계산해서 들어가도록 해줘

## 확정 (2026-09-02, 확인 1~6 답변)

| # | 결정 |
|---|---|
| 1 요금 | `roomRate = 70` ($/박), `startingBalance = 100` |
| 2 기본값 | 선불/후불 **선택 안 하면 = 후불** (`pendingPayUpfront ?? false`) |
| 3 후불 정산 | 체크아웃 **아침**(방 열릴 때). 아침 입금에도 **현금 효과음** 발생 (`Wallet.OnChanged` → `MoneyHud`, 자동) |
| 4 2배 | 전체 숙박비 2배 = `rate × stayNights × 2` |
| 5 살해/거절 | 후불 손님이면 미지급 그대로 (Killed 손님 방 처리는 별도 미구현) |
| 6 2배 선택지 | npc 2(회사원)의 **돌려보내기 흐름**에 이미 "두 배 내겠다" 대사 있음. **두 배 제안 수락 선택지** 추가 → 수락 시 요금 2배(선불) + `돌려보낸다` 질문이 대화에서 사라짐 |

## 현재 상태

| 조각 | 지금 |
|---|---|
| 돈 시스템 | **없음.** `Money*.prefab`(3D 소품)만. 스크립트·재화 개념 0 |
| Canvas "Money" 텍스트 | 사용자가 추가. 아직 씬 저장 안 됨(`InGame.unity`에 없음). 붙은 컴포넌트 없음 |
| 접객 결제 선택지 | `sample.csv`: npc 1 `stay_pay`/`stay_trust`, npc 2 `stay_pay`/`stay_later`, npc 3 `stay_pay`/`stay_skip` 등 손님마다 **대사만** 있고 기계적 효과 0 |
| npc 2 돌려보내기 | `reject` 질문 → "두 배로 내겠습니다" → `reject_insist`(선택: 다시 거절 `reject_2` / 받아준다 →빈 goto). `reject_2` → `reject_final`(outcome=Rejected) |
| 대화 노드 → 로직 훅 | `DialogueRunner.OnNodeReached(npc, nodeKey)`. `ReceptionManager.HandleReceptionNode` 가 `clean_yes/clean_no` 잡아 `pendingCleaning` 저장 (doc/0135). 결제도 같은 패턴 |
| 질문 숨기기 | **없음.** `DialogueRunner.QuestionHub` 는 매 루프 `database.Query(...Question)` 전체를 다시 띄움 — 특정 질문을 대화 도중 제거하는 수단 없음 |
| `GuestState` | `{ npc, room, verdict, checkInDay, stayNights, cleaningRequested }`. `stayNights` 이미 있음. npc 2 = 2박 |
| 체크인 | `ReceptionManager.GuestQueue` → 승인 → `GuestManager.CheckIn` → `pendingCleaning` 반영 |
| 체크아웃 | `RoomController.Apply`: `day >= CheckOutDay` & 체크아웃 아침 아님 → `GuestManager.CheckOut`. 체크아웃 **아침**엔 방 개방(청소) |
| 사운드 | `SoundManager`(앰비언스), `SfxEffect`(상호작용음). HUD 원샷용 도구 없음 → 텍스트 오브젝트에 `AudioSource`+`PlayOneShot` 이 최단 |
| HUD 패턴 | `PhaseLabel` — `TMP_Text` + 매니저 이벤트 구독 + `Refresh` |

## 설계

새 스크립트 2개(`Wallet`, `MoneyHud`) + 기존 4개 소폭 수정. 새 매니저 아키텍처 없음.

### A. `Game/Wallet.cs` (신규, 씬 싱글턴)

```csharp
using System;
using UnityEngine;

// 플레이어 소지금. 씬에 싱글턴 오브젝트로 배치 (GuestManager 옆).
// 접객 대화의 선불/후불/2배 선택 → ReceptionManager 가 Add 호출. HUD·사운드는 MoneyHud 가 OnChanged 구독.
public class Wallet : MonoBehaviour
{
    public static Wallet Instance { get; private set; }

    [SerializeField] private int startingBalance = 100;
    [Tooltip("기본 1박 요금 ($). 낡은 시골 독립 모텔")]
    [SerializeField] private int roomRate = 70;

    public int Balance { get; private set; }
    public int RoomRate => roomRate;

    // (delta, newBalance) — delta>0 = 입금(사운드 트리거). HUD 갱신용.
    public event Action<int, int> OnChanged;

    private void Awake() { Instance = this; Balance = startingBalance; }
    private void Start() => OnChanged?.Invoke(0, Balance);   // HUD 초기 표시(구독 순서 방어)

    public void Add(int amount)
    {
        if (amount == 0) return;
        Balance += amount;
        OnChanged?.Invoke(amount, Balance);
    }
}
```

### B. `UI/MoneyHud.cs` (신규)

```csharp
using TMPro;
using UnityEngine;

// 소지금 HUD. Wallet.OnChanged 구독 → 텍스트 갱신 + 입금(delta>0) 시 효과음.
public class MoneyHud : MonoBehaviour
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private AudioSource sfx;
    [Tooltip("돈이 들어올 때(delta>0) 재생하는 현금 효과음")]
    [SerializeField] private AudioClip cashClip;

    private void Reset() => label = GetComponent<TMP_Text>();

    private void OnEnable()
    {
        if (label == null) label = GetComponent<TMP_Text>();
        if (Wallet.Instance != null)
        {
            Wallet.Instance.OnChanged += Refresh;
            Show(Wallet.Instance.Balance);
        }
    }
    private void OnDisable()
    {
        if (Wallet.Instance != null) Wallet.Instance.OnChanged -= Refresh;
    }

    private void Refresh(int delta, int balance)
    {
        Show(balance);
        if (delta > 0 && sfx != null && cashClip != null) sfx.PlayOneShot(cashClip);
    }
    private void Show(int balance)
    {
        if (label != null) label.text = $"${balance:N0}";
    }
}
```

선불 입금·후불 아침 입금 모두 `Wallet.Add` → `OnChanged(delta>0)` → 같은 현금음. (확인 3)

### C. `Game/GuestManager.cs` — `GuestState` 에 결제 필드 3개

```csharp
// 기존
public int stayNights = 1;
public bool cleaningRequested;
```
↓
```csharp
public int stayNights = 1;
public bool cleaningRequested;
public int nightlyRate;         // 체크인 시 확정 ($/박, 대화로 2배 가능). 0 = 미설정
public bool payUpfront;         // true=선불(체크인 시 입금) / false=후불(체크아웃 아침 입금). 기본 false (확인 2)
public bool settled;            // 입금 완료 여부 (중복 입금 방지)

public int TotalCharge => (nightlyRate > 0 ? nightlyRate : 0) * (stayNights < 1 ? 1 : stayNights);
```

`CheckIn`/`CheckOut` 은 안 건드림.

### D. 질문 숨기기 — `DialogueRunner` 변경 불필요 (doc/0135 의 `consumedTopics` 재사용)

구현하며 확인: `doc/0135` 가 이미 **"결정 토픽"** 메커니즘을 넣어놨다. `EntryRole.Question` 노드가
`choices` 를 가지면 그중 하나를 고른 순간 그 질문이 허브에서 사라지고, 같은 손님 대화 내내(재대화 포함)
다시 안 뜬다 (`consumedTopics` HashSet, `ResetConsumedTopics()` 는 손님 교체 시에만).

따라서 `DialogueRunner` 는 안 건드린다. `sample.csv` 에서 npc 2 의 `reject` 를 **선택지 있는 결정 토픽**으로
바꾸기만 하면 (§G) 두 배 수락 → `돌려보낸다` 사라짐이 공짜로 나온다.

`ReceptionManager` 도 `hideQuestions` 전달 코드 불필요 — 결제 훅(§E)만 추가.

### E. `Game/ReceptionManager.cs` — 대화 선택 → 요금 확정 + 선불 입금

```csharp
// 기존 필드
private bool? pendingCleaning;
```
↓
```csharp
private bool? pendingCleaning;
private bool? pendingPayUpfront;   // stay_pay=true / stay_trust=false / null=후불 (확인 2)
private int pendingRateMult;       // 2배 노드(reject_double_accept) 도달 시 2
```

```csharp
// 기존
private void HandleReceptionNode(NpcData npc, string nodeKey)
{
    if (nodeKey == "clean_yes") pendingCleaning = true;
    else if (nodeKey == "clean_no") pendingCleaning = false;
}
```
↓
```csharp
private void HandleReceptionNode(NpcData npc, string nodeKey)
{
    switch (nodeKey)
    {
        case "clean_yes":  pendingCleaning = true;   break;
        case "clean_no":   pendingCleaning = false;  break;
        case "stay_pay":   pendingPayUpfront = true;  break;   // 선불로 받는다
        case "stay_trust": pendingPayUpfront = false; break;   // 나갈 때 정산
        case "reject_double_accept":                            // 두 배 제안 수락 (확인 6)
            pendingRateMult = 2;
            pendingPayUpfront = true;                           // 두 배 내면서 그 자리에서 지불
            break;
    }
}
```

`stay_later`/`stay_skip`/`stay_ok` 등 다른 후불성 노드는 기본값(후불)이라 별도 처리 불필요.
`돌려보낸다` 숨김은 §D — CSV 구조만으로 처리 (`pendingHideQuestions` 불필요).

```csharp
// 기존 — 손님별 리셋
CurrentGuest = npc;
PendingRoom = -1;
pendingCleaning = null;
```
↓
```csharp
CurrentGuest = npc;
PendingRoom = -1;
pendingCleaning = null;
pendingPayUpfront = null;
pendingRateMult = 0;
```

```csharp
// 기존 — 체크인 승인 직후
GuestManager.Instance?.CheckIn(npc, PendingRoom, DayNow());
if (pendingCleaning.HasValue)
{
    var gs = GuestManager.Instance?.Get(npc);
    if (gs != null) gs.cleaningRequested = pendingCleaning.Value;
}
PendingRoom = -1;
```
↓
```csharp
GuestManager.Instance?.CheckIn(npc, PendingRoom, DayNow());
var gs = GuestManager.Instance?.Get(npc);
if (gs != null)
{
    if (pendingCleaning.HasValue) gs.cleaningRequested = pendingCleaning.Value;

    int rate = Wallet.Instance != null ? Wallet.Instance.RoomRate : 0;
    if (pendingRateMult == 2) rate *= 2;
    gs.nightlyRate = rate;
    gs.payUpfront  = pendingPayUpfront ?? false;   // 선택 안 하면 후불 (확인 2)
    gs.settled     = false;
    if (gs.payUpfront) { Wallet.Instance?.Add(gs.TotalCharge); gs.settled = true; }
}
PendingRoom = -1;
```

### F. `Interaction/RoomController.cs` — 후불 정산 (체크아웃 아침)

```csharp
// 기존
bool roomOpenForCleaning = checkoutMorning || cleaningMorning;
```
↓ (아래 4줄 추가)
```csharp
bool roomOpenForCleaning = checkoutMorning || cleaningMorning;

// 후불 손님: 체크아웃 아침에 나가면서 숙박비 지불 (settled 로 1회만, 현금음 자동)
if (checkoutMorning && !g.payUpfront && !g.settled && g.nightlyRate > 0)
{
    Wallet.Instance?.Add(g.TotalCharge);
    g.settled = true;
}
```

`Apply` 는 `OnPhaseChanged` 마다 + `Start` 1회. `settled` 가드로 중복 입금 없음.

### G. CSV — npc 2 돌려보내기 → 두 배 수락 (확인 6)

`reject` 를 **선택지 있는 결정 토픽 Question** 으로 재구성 (§D). 어느 선택지든 고르면 `consumedTopics`
가 `reject` 를 허브에서 제거 → `돌려보낸다` 사라짐.

```
2,Reception,0,Question,reject,Turn them away,돌려보낸다,Neutral,"No? Please. I can pay double. Double, for one room.","안 된다고요? 제발요. 두 배로 내겠습니다. 방 하나에 두 배로요.",,
2,Reception,0,Question,reject,,,Neutral,"Where else is open? Nowhere. You know that.","다른 데가 어디 열었겠어요? 없어요. 아시잖아요.",,
2,Reception,0,Question,reject,Hold firm,그래도 거절한다,,,,reject_final,
2,Reception,0,Question,reject,Take the double,두 배를 받고 들인다,,,,reject_double_accept,
2,Reception,0,Question,reject,Let him stay,그냥 받아준다,,,,,
2,Reception,0,Node,reject_double_accept,,,Neutral,"Double. Yes. Thank you - thank you. Here - take it.","두 배요. 네. 감사합니다 - 감사합니다. 여기 - 받으세요.",,
2,Reception,0,Node,reject_final,,,Angry,"This is a mistake. You'll see.","이건 실수예요. 두고 보세요.",,
2,Reception,0,Node,reject_final,,,Neutral,"Fine. FINE. I'm going.","알았어요. 알았다고요. 갑니다.",,Rejected
```

- `reject` Question = 대사 2줄 + 선택지 3개: 그래도 거절(`reject_final`, Rejected) / 두 배 받고 들인다(`reject_double_accept`) / 그냥 받아준다(빈 goto = 정상 요금·후불).
- `reject_double_accept` 도달 → `HandleReceptionNode` 가 요금 2배 + 선불. 허브 복귀 시 `reject` 는 이미 소진 → `돌려보낸다` 없음.
- npc 2 = 2박 → 두 배 수락 = `70 × 2 × 2 = $280` 즉시 입금.
- `Tools > Dialogue > Import CSV` 재실행 필요.

## 영향 파일

```
Game/Wallet.cs                 신규
UI/MoneyHud.cs                  신규
Game/GuestManager.cs            수정  GuestState + nightlyRate/payUpfront/settled + TotalCharge
Game/ReceptionManager.cs       수정  pending 2필드, HandleReceptionNode switch, 리셋, 체크인 입금
Interaction/RoomController.cs   수정  후불 정산 5줄
Assets/My/Data/Dialogue/sample.csv  npc 2 reject 흐름을 결정 토픽으로 교체 (+reject_double_accept)
Docs/  Wallet.md·MoneyHud.md 신규 · ReceptionManager.md·GuestManager.md·RoomController.md 갱신
```

DialogueRunner 는 변경 없음 (doc/0135 `consumedTopics` 재사용).

## 사용자 씬 작업 (구현 후)

1. `uloop compile` 확인.
2. 씬에 **`Wallet`** 오브젝트: 빈 GameObject + `Wallet` (`GuestManager` 옆). `startingBalance 100`/`roomRate 70` 확인.
3. Canvas **"Money" 텍스트**: `MoneyHud` 추가 (`label` 자동). 같은 오브젝트에 `AudioSource`(2D, playOnAwake 끔) → `MoneyHud.sfx` 연결. `cashClip` = 현금 효과음.
4. `Tools > Dialogue > Import CSV`.
5. 검증:
   - `stay_pay` 선택 후 체크인 → 즉시 `$100 → $170` + 현금음.
   - 선택 안 함 / `stay_trust` → 체크인 시 잔액 그대로 → 체크아웃 아침 `+$70` + 현금음.
   - npc 2 `돌려보낸다` → "두 배로 내겠습니다" → **두 배를 받고 들인다** → 체크인 → `+$280` + 현금음. 재대화해도 `돌려보낸다` 없음.

## 미해결 / 후속

- 취침 정산 화면(수입−지출), 주간 고지서 — `기획/운영-업무-…` §7. 별도 doc.
- Killed 손님 방 처리 / 후불 미수금 표시.
- 숙박세(tax).
- 재고·수리 지출로 `Wallet.Add(음수)` — 지출 API 는 `Add` 가 이미 음수 허용(현금음은 delta>0 만).

## 구현 완료 (코드, 2026-09-02)

`uloop compile` Error 0 (기존 `AssetOrganizer.cs` 경고 1개만). CSV 재임포트 완료 — 144행 → 88노드,
goto 검사 통과. `reject` 질문 = choices 3 / lines 2 확인, `reject_double_accept` 노드 생성 확인.

| 파일 | 내용 |
|---|---|
| `Game/Wallet.cs` | 신규. 씬 싱글턴. `startingBalance 100`, `roomRate 70`. `Balance`, `RoomRate`, `Add(int)`(음수=지출 허용), `OnChanged(delta, balance)`. `Start` 에서 초기 `OnChanged(0, Balance)` |
| `UI/MoneyHud.cs` | 신규. `Wallet.OnChanged` 구독 → `$1,234` 표시. `delta>0` 이면 `PlayOneShot(cashClip)`. `[RequireComponent(AudioSource)]` — `label`·AudioSource 자동 획득(`Reset`/`Awake`), `cashClip` 만 수동 |
| `Game/GuestManager.cs` | `GuestState` + `nightlyRate`/`payUpfront`(기본 false)/`settled` + `TotalCharge`(= rate × stayNights) |
| `Game/ReceptionManager.cs` | 필드 `pendingPayUpfront`/`pendingRateMult`. `HandleReceptionNode` switch 로 확장 (`stay_pay`/`stay_trust`/`reject_double_accept`). 손님별 리셋에 2줄. 체크인 승인 시 `gs.nightlyRate`/`payUpfront` 세팅 + 선불이면 `Wallet.Add(TotalCharge)` + `settled=true` |
| `Interaction/RoomController.cs` | `Apply` 에 후불 정산 — `checkoutMorning && !payUpfront && !settled && nightlyRate>0` → `Wallet.Add(TotalCharge)` + `settled=true` |
| `Assets/My/Data/Dialogue/sample.csv` | npc 2 `reject` → 결정 토픽 Question(2줄+선택지 3). `reject_double_accept` 노드 추가. `reject_insist`/`reject_2` 제거 |
| `Assets/My/Data/Dialogue/DialogueDatabase.asset` | CSV 재임포트로 재생성 |

### 사용자 씬 작업 (남음)

1. 씬에 **`Wallet`** 오브젝트 (빈 GameObject + `Wallet`, `GuestManager` 옆). `startingBalance 100`/`roomRate 70` 확인.
2. Canvas **"Money" 텍스트**에 `MoneyHud` 추가 — `label` 과 AudioSource 는 자동. `cashClip` 에 현금 효과음만 연결.
3. 플레이 검증: `stay_pay` → 체크인 시 `$100→$170`+음 / `stay_trust`·미선택 → 체크아웃 아침 `+$70`+음 / npc 2 두 배 수락 → 체크인 시 `+$280`+음, 재대화해도 `돌려보낸다` 없음.

## 후속

- `doc/0140` — 선불 요구에 대한 손님별 반응(수락/거부) + 선불 시 체크인 대사(`checkin_paid`). 돈은 승인 시에만 지급(이 문서 구현이 이미 그러함).

## 상태

2026-09-02 코드 구현 + 컴파일 + CSV 임포트 완료 (+ doc/0140 후속). 씬 배선(`Wallet` 오브젝트 + `MoneyHud`) + 플레이 검증만 남음.
