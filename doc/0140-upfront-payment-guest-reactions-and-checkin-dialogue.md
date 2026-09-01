# 0140 - 선불 요구에 대한 손님별 반응 + 선불 시 체크인 대사

날짜: 2026-09-02
관련: `doc/0137`(결제 시스템), `doc/0138`(결정 토픽 소진), `Docs/Wallet.md`, `Docs/ReceptionManager.md`, `Docs/DialogueSystem.md`

## 요청 (원문)

> 선불 입금을 요구하는 선택지를 클릭시 대사를 선불을 거부하는 손님도 있고 선불로 드리겠다는 말을 하는
> 손님도 있도록 해주고 후불로 정할수도 있고 그런식으로 해주고 진짜로 방지정하고 열쇠를 줘서 승인해야지만
> 그떄 돈을 주는거로 해줘
> 선불 요구한 경우에는 승인시 대사가 좀 달라지도록 해줘 여기 선불금입니다. 뭐 조용히 지내겠습니다 등
> 원래 대사에서 선불금을 준 행동을 한거처럼 대사를 추가해줘

## 이미 만족하던 것

- **돈은 승인(방 배정 + 열쇠) 시에만 지급.** doc/0137 구현이 이미 그러함 — `HandleReceptionNode` 는
  `pendingPayUpfront` 만 기록하고, 실제 `Wallet.Add` 는 체크인 승인 `else` 블록(방 배정·`ConfirmCheckIn`
  이후)에서만. 대화 중 선불 선택 → 승인 전 손님이 나가면 입금 없음. **변경 없음, 재확인만.**

## 변경

### 1. `Dialogue/DialogueRunner.cs` — `SayNode` 에 폴백 nodeKey

```csharp
// 기존
public void SayNode(NpcData npc, SpeechBubble bubble, Situation situation, string nodeKey, Action onDone = null)
```
↓
```csharp
// fallbackNodeKey: nodeKey 노드가 없으면 대신 재생 (예: "checkin_paid" 없으면 "checkin").
public void SayNode(NpcData npc, SpeechBubble bubble, Situation situation, string nodeKey,
                    Action onDone = null, string fallbackNodeKey = null)
{
    ...
    var node = ... GetNode(npc.id, situation, nodeKey, day) ...;
    if (node == null && !string.IsNullOrEmpty(fallbackNodeKey) && ...)
        node = database.GetNode(npc.id, situation, fallbackNodeKey, day);
    ...
}
```

### 2. `Game/ReceptionManager.cs`

`HandleReceptionNode` switch 에 거부 케이스:
```csharp
case "stay_pay":          pendingPayUpfront = true;  break;   // 손님이 선불 수락 → 승인 시 입금
case "stay_trust":        pendingPayUpfront = false; break;   // 나갈 때 정산
case "stay_pay_refused":  pendingPayUpfront = false; break;   // 손님이 선불 요구 거부 → 후불
```

체크인 승인 대사 — 선불 손님이면 `checkin_paid`, 없으면 `checkin` 폴백:
```csharp
// 기존
DialogueRunner.Instance.SayNode(npc, bubble, Situation.Reception, "checkin", () => said = true);
```
↓
```csharp
bool paidUpfront = gs != null && gs.payUpfront;
DialogueRunner.Instance.SayNode(npc, bubble, Situation.Reception,
    paidUpfront ? "checkin_paid" : "checkin", () => said = true, "checkin");
```

### 3. `sample.csv` — nodeKey 규약 + 손님별 반응

| nodeKey | 의미 | 훅 |
|---|---|---|
| `stay_pay` | 손님이 선불 수락 (돈은 승인 시) | `pendingPayUpfront = true` |
| `stay_pay_refused` | 손님이 선불 요구 거부 → 후불 | `pendingPayUpfront = false` |
| `stay_trust` / `stay_later` / `stay_skip` 등 | 플레이어가 후불 선택 | 기본값(false) |
| `checkin_paid` | 선불 손님의 승인 대사 (선불금 건네는 행동 포함) | — |

- **npc 1** (나그네): `stay_pay` 수락 유지. `checkin_paid` 추가 — "여기 - 하룻밤치, 선불금입니다. 전액이요." + 기존 "고맙습니다. 소리 하나 안 내겠습니다."
- **npc 2** (회사원): `stay_pay` 선택지 → **`stay_pay_refused`** 로 변경 (기존 `stay_pay` 노드 삭제). "선불이요? 저 - 지금은 좀. 제발요. 나갈 때요. 전부 다 드릴게요." → 후불.
- **npc 3** (거만한 단골): `stay_pay` 수락 유지 (비꼬며 지불). `checkin_paid` 추가 — "돈 여기 있다. 숙박비 전액, 선불로. 꼭 유난을 떨어야 했으니까." + 기존 "드디어. 내일은 좀 덜 굼뜨든가."

`stay` 질문은 결정 토픽(doc/0138) — 어느 선택이든 한 번 고르면 `stay` 가 허브에서 사라져 번복 불가.

## 스킵

- **npc 4·5**: `stay` 협상이 "하루씩" / "일주일치" / "매일 아침 통보" 라 유동 숙박. 선불(= 전체 숙박비 선지급)
  개념이 안 맞아 이번엔 안 건드림. 필요하면 별도.
- 다박 손님 선불 = `rate × NpcData.stayNights` 전액 (실제 연장 숙박 미반영, doc/0132 스킵과 동일).

## 구현 완료 (2026-09-02)

`uloop compile` Error 0. CSV 재임포트 — 148행 → 90노드, goto 검사 통과.
검증: npc 1·3 `checkin_paid` 2줄 / `stay_pay` 존재, npc 2 `stay_pay_refused` 존재·`stay_pay` 없음.

| 파일 | 내용 |
|---|---|
| `Dialogue/DialogueRunner.cs` | `SayNode` 에 `fallbackNodeKey` 옵션 파라미터 |
| `Game/ReceptionManager.cs` | `HandleReceptionNode` 에 `stay_pay_refused` 케이스. 체크인 대사 `checkin_paid`/`checkin` 분기 (폴백 `checkin`) |
| `Assets/My/Data/Dialogue/sample.csv` | npc 1·3 `checkin_paid`, npc 2 `stay_pay`→`stay_pay_refused` |
| `Assets/My/Data/Dialogue/DialogueDatabase.asset` | 재임포트 |

### 검증 (플레이)

- npc 1: "선불로 받는다" → "…여기요. 하룻밤치 딱 맞게." → 방배정+열쇠 승인 → **이때 $70 입금**+현금음 → "여기 - 하룻밤치, 선불금입니다…"
- npc 2: "선불로 받겠다고 한다" → "선불이요? 저 - 지금은 좀…" → 승인 → 입금 없음 → 체크아웃 아침 $140 입금
- npc 3: "선불을 요구한다" → "선불. 나한테. 귀엽네. …여기." → 승인 → **$210 입금** → "돈 여기 있다. 숙박비 전액, 선불로…"
- 승인 전 손님이 거절/퇴장 → 입금 없음

## 버그 수정 (2026-09-02) — "선불금 받았는데 돈이 안 들어와"

플레이 확인 결과: `Wallet.Balance` 는 정상적으로 $100→$170 증가(입금은 됨). **`MoneyHud` 가 화면을
갱신 안 함** — `MoneyHud.OnEnable` 이 `Wallet.Awake`(Instance 세팅)보다 먼저 돌면 `Wallet.Instance == null`
이라 구독을 포기하고 재시도가 없었음. + 도메인 리로드 끔 상태에서 `Wallet.Instance` 가 파괴된 참조로 남는 케이스.

| 파일 | 수정 |
|---|---|
| `UI/MoneyHud.cs` | 구독을 `OnEnable` **+ `Start`** 양쪽에서 시도 (`subscribed` 가드). `Start` 는 모든 `Awake` 뒤라 `Wallet.Instance` 보장. `AudioSource` 캐시는 `Awake` 로 이동 |
| `Game/Wallet.cs` | `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 로 `Instance` static 초기화 (도메인 리로드 끔 대비). `OnDestroy` 에서 `Instance == this` 면 null 로 |
| `sample.csv` | npc 1·3 `stay_pay` 노드 대사를 **"지불" → "동의"** 로. npc 1 "선불로요. 알겠습니다. 준비해 두죠." / npc 3 "선불. 나한테. 귀엽군. ...좋아. 주지." — 실제 선불금 건네는 대사는 `checkin_paid`(승인 시) |

플레이 검증: `Add(70)` → HUD `$100` → `$170` 즉시 갱신 확인.

## 상태

코드+CSV 구현·컴파일·임포트·플레이 검증 완료. `MoneyHud`/`Wallet` 은 이미 씬에 배선돼 있음 (사용자가 추가).
