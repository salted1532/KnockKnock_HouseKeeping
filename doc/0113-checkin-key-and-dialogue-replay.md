# 0113 - 접객 승인: 대화 종료 후 상호작용 / 빈손=재대화 / 열쇠=승인

날짜: 2026-08-30
관련: `doc/0104`(접객 흐름), `Docs/ReceptionManager.md`, `Docs/InventorySystem.md`, `Docs/HandItemRegistry.md`

## 요청 (원문)

> 접객 모드에서 처음에 손님은 상호작용 안되다가 대화 시작하고 내가 대화종료 버튼 눌러서
> 대화창이 꺼지면 그때부터 상호작용 가능하도록 해줘. 그냥 상호작용하면 다시 대화창이 켜지고,
> 만약 내 손에 열쇠가 있는 상태에서 상호작용 클릭 시 그때 열쇠가 인벤토리에서 없어지고
> 손님은 방으로 가는거야 (승인 부분).

## 1. 현황

`ReceptionManager.GuestQueue` per-손님:
1. 걸어옴 → `DialogueRunner.Play(Reception)` → `onResult`
2. `visitorOnly` → 퇴장 / `Rejected` → 퇴장
3. 그 외: `AwaitingCheckIn = true` → `WaitUntil(checkInConfirmed)` → **아무 클릭이나 = 승인** → `CheckIn` + "checkin" 대사 + `roomPath`

- 손님 상호작용 게이트: `AwaitingCheckInCondition.IsMet == rm.AwaitingCheckIn`. 대화 중엔 false → 상호작용/아웃라인 없음. **이미 원하는 대로.**
- `CheckInGuestEffect.Play` : `AwaitingCheckIn` 이면 `rm.ConfirmCheckIn()` 무조건.
- 열쇠 감지: `InventorySystem.Instance.ActiveHandItem?.IsKey` (HookEffect 가 이미 이 패턴 사용). 제거: `InventorySystem.Instance.RemoveActiveItem()` → 월드 오브젝트 반환.

## 2. 설계

### A. `CheckInGuestEffect` — 열쇠 유무로 분기

```csharp
public override void Play(in InteractionContext ctx)
{
    var rm = ReceptionManager.Instance;
    if (rm == null || !rm.AwaitingCheckIn) return;

    var inv = InventorySystem.Instance;
    var held = inv != null ? inv.ActiveHandItem : null;

    if (held != null && held.IsKey)
    {
        var world = inv.RemoveActiveItem();   // 인벤토리에서 제거
        if (world != null) Destroy(world);    // 손님이 가져감
        rm.ConfirmCheckIn();                  // 승인 → 방으로
    }
    else
    {
        rm.RequestDialogueReplay();           // 빈손 → 대화창 다시
    }
}
```

### B. `ReceptionManager` — 재대화 신호 + 루프

```csharp
private bool replayRequested;
public void RequestDialogueReplay() { if (AwaitingCheckIn) replayRequested = true; }
```

`GuestQueue` 의 `else`(대화 정상 종료) 블록을 루프로:

```csharp
else
{
    // 대화 종료 → 이때부터 손님 상호작용 가능(AwaitingCheckIn).
    //  빈손 클릭 → 대화 다시 / 열쇠 든 채 클릭 → 열쇠 소모 + 승인
    while (InSession && result != Verdict.Rejected && !checkInConfirmed)
    {
        checkInConfirmed = false;
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

    if (!InSession) break;

    if (result == Verdict.Rejected)          // 재대화에서 거절 선택
    {
        GuestManager.Instance?.SetVerdict(npc, Verdict.Rejected, DayNow());
        if (mover != null) yield return mover.WalkThrough(exitPath);
    }
    else                                     // checkInConfirmed
    {
        GuestManager.Instance?.CheckIn(npc, nextRoom++, DayNow());
        // "checkin" 대사 후 방으로  (기존과 동일)
        ...
        if (mover != null) yield return mover.WalkThrough(roomPath);
    }
}
```

- 재대화 중 `AwaitingCheckIn = false` → 손님 상호작용/아웃라인 다시 잠김 (요청대로).
- 재대화에서 거절 노드 선택 시 그 손님은 퇴장 처리.

## 3. 영향 파일

```
Assets/My/Scripts/Interaction/Effects/CheckInGuestEffect.cs   열쇠 분기
Assets/My/Scripts/Game/ReceptionManager.cs                    replayRequested + 승인 루프
Docs/ReceptionManager.md                                      흐름 갱신
```

씬/프리팹 무변경 (Guest 프리팹 구성 그대로).

## 4. 확인 필요

1. **빈손 클릭 = 재대화**: `DialogueRunner.Play` 전체 재생(인사 → 질문 허브)로 OK? (질문 허브만 다시 여는 방식도 가능)
2. **열쇠**: 아무 `HandItem.IsKey` 나 승인 성립(방번호 대조 없음, doc/0104 (b) 범위). 제거된 키는 **파괴**(손님이 가져감). OK?
3. **프롬프트 문구**: 지금은 항상 "체크인"(CheckIn). 빈손일 땐 "대화"로 바꿀지? (정적이라 동적 전환 코드 필요 — 선택, 기본은 "체크인" 유지)

## 5. 스킵 (YAGNI)

- 방번호별 열쇠 대조 / 모니터 방배정 (후속 doc).
- 재대화 횟수 제한.
- 열쇠 잘못 주면 되돌리기.

## 6. 구현 완료 (2026-08-30, 확인: 전체 대화 재생 / 프롬프트 빈손="대화"·열쇠="체크인" / 진행)

| 파일 | 내용 |
|---|---|
| `Interaction/Interactable.cs` | `interface IPromptOverride { string PromptOverride { get; } }` 추가. `Awake` 에서 `GetComponent<IPromptOverride>()` 캐시. `Prompt => promptOverride?.PromptOverride ?? DefaultPrompt` (기존 switch 는 `DefaultPrompt` 로 rename) |
| `Interaction/Effects/CheckInGuestEffect.cs` | `Interactable.IPromptOverride` 구현. `AwaitingCheckIn` 아니면 `null`(기본), 열쇠 있으면 `T("Check in","체크인")`, 없으면 `T("Talk","대화")`. `Play`: 열쇠(`HandItem.IsKey`) 있으면 `RemoveActiveItem()` + `Destroy` + `ConfirmCheckIn()`, 없으면 `RequestDialogueReplay()` |
| `Game/ReceptionManager.cs` | `replayRequested` + `RequestDialogueReplay()`. `GuestQueue` 승인 대기: `checkInConfirmed=false` 초기화 후 `while (InSession && result != Rejected && !checkInConfirmed)` — 매 회 `AwaitingCheckIn=true` → 대기 → `replayRequested` 면 `DialogueRunner.Play` 재실행. 루프 후 `Rejected` → 퇴장 / 아니면 승인(방으로) |
| `Docs/ReceptionManager.md`, `Docs/Interactable.md` | 갱신 |

### 검증
- `uloop compile` : Success, Error 0, Warning 0.
- Play 모드: Guest 프리팹 인스턴스화 시 `Interactable.Prompt` = "체크인"(override 가 null 반환 → 기본값), `IPromptOverride` 정상 연결 ✓
- 승인 루프 재검토: `checkInConfirmed` 를 루프 진입 전에 리셋 안 하면 **직전 손님의 true 가 남아 다음 손님이 클릭 없이 즉시 승인되는 버그** → 루프 앞에서 리셋하도록 수정함.
- 전체 접객 세션(대화 입력 필요) E2E 는 인게임 확인 요망.

## 상태

2026-08-30 구현 완료. 인게임 확인 대기 (대화 종료 → 손님 외곽선/상호작용 활성 → 빈손 클릭=재대화 / 열쇠 클릭=열쇠 소모+입실).
