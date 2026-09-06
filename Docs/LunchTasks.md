# LunchTasks

`Assets/My/Scripts/Game/LunchTasks.cs` · `Assets/My/Scripts/Interaction/LunchTaskTarget.cs` · `Assets/My/Scripts/Interaction/Conditions/LunchTasksCompleteCondition.cs`

점심 일과(우상단 HUD) + 접객 테이블 저녁 전환 게이트. [`MorningTasks`](MorningTasks.md) 와 동일 패턴. 테스트용 일과(울타리 수리·불법주차 신고 등) — 일차별 변형·점프스케어는 아직 (`doc/0134`).

## LunchTaskTarget

점심 일과 오브젝트 1개에 붙인다 (`[RequireComponent(Interactable)]`, `InteractionCondition` 상속).

- `IsMet => !done` — 상호작용 1회 성공(`Interactable.Interacted`) 시 `done = true` → 이후 `CanInteract == false` 로 상호작용/프롬프트/아웃라인이 시간대와 무관하게 자동으로 막힌다.
- `IsDone` — `LunchTasks` 가 카운트에 씀.
- 완료 연출은 선택: 같은 오브젝트에 [`ChangeObjectEffect`](ChangeObjectEffect.md) 를 붙이면 `onObjects`/`offObjects` 스왑(울타리 고장→고침), 안 붙이면 오브젝트 변화 없음(차량 신고 — **삭제/비활성화 금지**).
- `ResetForNewDay()` — 새 날 아침에 `LunchTasks` 가 호출. `done = false` + `ChangeObjectEffect.ResetToOff()`. 지금은 같은 오브젝트를 매일 재활용.

## LunchTasks (싱글턴)

HUD Canvas 우상단 패널에 붙인다.

| 필드 | 설명 |
|---|---|
| `panel` (`GameObject`) | 점심에만 켜지는 패널 루트 (비면 `line` 만 토글) |
| `line` (`TMP_Text`) | `☐ 점심 일과 처리 done/total` |

- `Total` = 씬의 `LunchTaskTarget` 수, `Done` = `IsDone` 인 것. `AllDone` = `Current == Noon` && `Done >= Total`.
- `OnPhaseChanged(Morning)` 구독 → 모든 타깃 `ResetForNewDay()`.
- 폴링 방식 (`FindObjectsByType` 1회 캐시).

## LunchTasksCompleteCondition

`InteractionCondition`. `IsMet => LunchTasks.Instance.AllDone`. 접객 테이블(`Motel_Table`) `Interactable` 에 얹어 점심 일과 완료 전까진 저녁 전환 상호작용을 막는다.

## 관련

[MorningTasks](MorningTasks.md) · [Interactable](InteractionSystem.md) · [ChangeObjectEffect](ChangeObjectEffect.md) · [PhaseSwitchEffect](PhaseSwitchEffect.md) · [ObjectiveMarker](ObjectiveMarker.md)
