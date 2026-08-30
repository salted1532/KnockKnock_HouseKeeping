# 0115 - 접객 ESC = 종료 아닌 일시정지 / 재상호작용으로 재개

날짜: 2026-08-30
관련: `doc/0111`(테이블 Noon 전용 — 일부 되돌림), `doc/0113`(승인 흐름), `doc/0114`(뒷모습), `Docs/ReceptionManager.md`·`Docs/UIInteractionMode.md`

## 요청 (원문)

> 접객 모드에서 Esc 누르면 빠져나오는거, 빠져나오면 완전 접객모드 종료 되는 느낌인데
> 그냥 퍼즈 되는 느낌으로 하고, 다시 상호작용해서 접객모드로 들어가면 다시 진행되도록 해줘.
> 손님이 스폰된 상황에서 오고 있거나 나갈때 손님이 퍼즈 되도록 해줘(그자리에 멈춤).

## 1. 현황

- `UIInteractionMode` : ESC → `Exit()` → 스택 비면 `Teardown()` → `Exited` 이벤트 + 플레이어 원위치.
- `ReceptionManager.HandleUIExit` (`Exited` 구독) : **`StopQueue()`** (코루틴 중단 + `guestInstance` **Destroy**) + `InSession=false` + `OnSessionEnded`. → 세션이 사실상 끝나버림.
- `GuestMover.WalkThrough` : 웨이포인트로 매 프레임 이동. 멈추는 수단 없음.
- `doc/0111` 로 `Motel_Table` `allowedPhases = {Noon}` → 저녁엔 테이블 재클릭 불가 (당시 요구: ESC=종료라 재진입 없음). **이번 요구가 이걸 뒤집음.**
- `OnSessionStarted/Ended` 외부 구독자 **없음** (내부 전용).

## 2. 설계

### A. `GuestMover` — 얼림

```csharp
public bool Frozen { get; set; }   // true 면 이동 코루틴이 그 자리에 정지
```
`WalkThrough` 내부 루프 앞:
```csharp
if (Frozen) { SetWalking(false); while (Frozen) yield return null; SetWalking(true); }
```

### B. `ReceptionManager` — 일시정지/재개

```csharp
public bool Paused { get; private set; }
private GuestMover guestMover;   // GuestQueue 에서 세팅
```
- `Start()` : `UIInteractionMode.Instance.Entered += HandleUIEnter;` (+ `OnDestroy` 해제)
- `HandleUIExit()` (기존 → 교체) :
  ```csharp
  if (!InSession) return;
  Paused = true;
  if (guestMover != null) guestMover.Frozen = true;
  // StopQueue / OnSessionEnded 호출 안 함 — 코루틴·손님 그대로 유지
  ```
- `HandleUIEnter()` (신규) :
  ```csharp
  if (!InSession || !Paused) return;
  Paused = false;
  if (guestMover != null) guestMover.Frozen = false;
  ```
- `EndSession()` / `StopQueue()` : `Paused=false`, `guestMover.Frozen=false`, `guestMover=null` 정리.
- `debugEndKey`(K) 는 그대로 **완전 종료**(→ 새벽) 경로로 남김.

### C. `Motel_Table` `allowedPhases` : `{Noon}` → **`{Noon, Evening}`**

저녁에 테이블을 다시 클릭(E)해 UI 모드 재진입 → `Entered` → `HandleUIEnter` 재개.
- Noon 클릭 = 저녁 전환(기존). Evening 클릭 = (세션 중이면) 재개 / (`PhaseSwitchEffect` 는 `target==Current` 라 no-op).
- `doc/0111` 아웃라인-`CanInteract` 게이트 수정은 **유지**.

## 3. 파일

```
Assets/My/Scripts/Dialogue/GuestMover.cs        Frozen
Assets/My/Scripts/Game/ReceptionManager.cs      Paused + HandleUIEnter + HandleUIExit 교체
Assets/My/InGame/Prefabs/Item/Motel_Table.prefab + InGame.unity   allowedPhases {Noon,Evening}
Docs/ReceptionManager.md, Docs/PhaseCondition.md   갱신
```

## 4. 구현 완료 (2026-08-30, 확인: 테이블 재상호작용(E)으로 재개 / 일시정지 중 대화 UI 숨김 / K 유지)

| 파일 | 내용 |
|---|---|
| `Dialogue/GuestMover.cs` | `public bool Frozen { get; set; }`. `WalkThrough` 루프 앞: `if (Frozen) { SetWalking(false); while (Frozen) yield return null; SetWalking(true); }` |
| `Dialogue/DialogueRunner.cs` | `public bool Paused { get; set; }` |
| `Dialogue/SpeechBubble.cs` | `AdvancePressed()` → `Paused` 면 false (월드 클릭이 대사 넘김으로 안 샘). `TypeLine` 루프 `Paused` 면 프레임 넘김. `IsVisible` / `SetVisible(bool)` 추가 (`root`=Dialogue_Panel 토글) |
| `Game/ReceptionManager.cs` | `Paused` 프로퍼티 + `guestMover` 필드. `HandleUIExit` 를 **일시정지**로 교체(코루틴·손님 유지, `Frozen`/`DialogueRunner.Paused` on, 보이던 대화패널 숨김). `HandleUIEnter`(신규, `UIInteractionMode.Entered` 구독) → 재개. `EndSession`/`StopQueue` 정리. `GuestQueue` 스폰 직후 `guestMover.Frozen = Paused` |
| `Item/Motel_Table.prefab` + `InGame.unity` | `PhaseCondition.allowedPhases` `{Noon}` → **`{Noon, Evening}`** (`doc/0111` 부분 되돌림; 아웃라인 게이트 수정은 유지) |
| `Docs/ReceptionManager.md`, `Docs/PhaseCondition.md` | 갱신 |

### 동작
- 저녁 세션 중 ESC → UI 모드 이탈(플레이어 원위치) + **일시정지**: 손님 그 자리에 멈춤, 대화패널 숨김, 세션/코루틴/손님 인스턴스 유지. `OnSessionEnded` 안 쏨.
- 걸어서 접객 테이블로 가 **E** → UI 모드 재진입 → `Entered` → 재개: 손님 다시 이동, 대화패널 복원, 대화 입력 재개.
- **K** : 여전히 즉시 완전 종료(손님 파괴 → 새벽).

### 검증
- `uloop compile` : Success, Error 0, Warning 0.
- API 존재 확인: `GuestMover.Frozen`, `ReceptionManager.Paused`, `DialogueRunner.Paused`, `SpeechBubble.SetVisible` ✓. `Motel_Table.allowedPhases = [Noon Evening]` ✓
- 전체 세션 E2E(페이즈 전환 → 손님 → ESC → 재진입)는 에디터 플레이 프레임이 CLI 환경에서 안 돌아 인게임 확인 요망.

### 인게임 확인 절차
1. Noon 에 접객 테이블 E → 저녁 전환, 손님 입장 시작
2. 손님이 걸어오는 중 ESC → 손님 그 자리에 멈춤, 화면 대화패널 사라짐, 플레이어 원위치
3. 걸어서 테이블로 가 E → 손님 다시 걸어옴, 대화 진행 재개
4. 대화 중 ESC → 재개 시 대화패널·버튼 그대로 이어짐
5. 승인/거절로 손님 나갈 때 ESC → 손님 멈춤, 재진입 시 계속 퇴장

## 상태

2026-08-30 구현 완료. 인게임 확인 대기.
