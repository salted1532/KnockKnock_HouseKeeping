# 0111 - 아웃라인이 InteractionCondition 을 존중하도록 (접객 테이블 등)

날짜: 2026-08-30
관련: `Docs/InteractionCondition.md`·`Docs/PhaseCondition.md`, [[project_interaction-system-redesign]]

## 요청 (원문)

> 접객 테이블에 경우 다른 시간대에선 상호작용 안되고 외곽선 생성도 안되도록하자

## 1. 조사

`Motel_Table` (프리팹 `Assets/My/InGame/Prefabs/Item/Motel_Table.prefab`) 구성:
`Interactable` + **`PhaseCondition allowedPhases = {Noon, Evening}`** + `EnterUIModeEffect`(Player_Anchor) + `PhaseSwitchEffect`(Noon→Evening) + `SfxEffect` + `Outline`.

- Noon: 클릭 = 저녁으로 전환(+UI 모드). Evening: 접객 데스크.
- **Morning / Dawn**: `PhaseCondition.IsMet == false` → `Interactable.CanInteract == false` → 클릭은 이미 막힘.

### 버그

`Docs/InteractionCondition.md` 는 "`IsMet == false` 면 상호작용·**아웃라인**·프롬프트가 모두 안 뜬다" 고 하지만, 실제로는:

`CursorInteractor.Update()` / `GazeInteractor.Update()` 에서
```csharp
hitOutline = hit.collider.GetComponentInParent<Outline>();   // ← CanInteract 확인 안 함
hitSprite  = hit.collider.GetComponentInParent<SpriteOutline>();
var candidate = hit.collider.GetComponentInParent<Interactable>();
if (candidate != null && candidate.CanInteract) hovered = candidate;   // 프롬프트/클릭만 게이트
```
→ 아웃라인은 `CanInteract` 무관하게 호버만 하면 켜짐. **Morning/Dawn 에도 접객 테이블에 외곽선이 뜸.**

## 2. 설계 — 인터랙터 2개만 수정 (씬·프리팹 무변경)

`CursorInteractor` · `GazeInteractor` 동일 패치: 아웃라인/스프라이트 하이라이트도 `CanInteract` 통과한 대상에서만 집는다.

```csharp
point = hit.point;
var candidate = hit.collider.GetComponentInParent<Interactable>();
if (candidate != null && candidate.CanInteract)
{
    hovered = candidate;                                 // (GazeInteractor 는 hitInteractable)
    hitOutline = candidate.GetComponent<Outline>();
    hitSprite  = candidate.GetComponent<SpriteOutline>();
}
```

- `Outline`/`SpriteOutline` 은 `Interactable.EnsureOutline()` 규약상 항상 `Interactable` 과 같은 GameObject → `candidate.GetComponent<>()` 로 충분.
- `Interactable` 없는 순수 장식 Outline 은 원래도 상호작용 대상이 아니었고, 이제 호버 하이라이트도 안 됨 (의도된 정리).

효과: 접객 테이블뿐 아니라 **모든** `InteractionCondition` 이 아웃라인까지 일관되게 막음 (문서대로).

## 3. 확인 답변 (2026-08-30)

> Noon 만 허용하고, 접객 모드에서(저녁) Esc 로 빠져나가면(테스트용) 다시 상호작용 불가하도록.

→ `Motel_Table` `PhaseCondition.allowedPhases` : `{Noon, Evening}` → **`{Noon}`**.
- Noon 클릭 = UI 모드 진입 + `PhaseSwitchEffect` Noon→Evening. 저녁 세션은 `ReceptionManager` 가 `OnPhaseChanged(Evening)` 로 자동 시작(테이블 클릭 불필요).
- Evening 중 Esc → `UIInteractionMode` 이탈 → `HandleUIExit` 세션 정리. 페이즈는 Evening → 테이블 `allowedPhases={Noon}` 이라 재클릭 불가. (요청대로)

`OutlineWhenOff`(토글 off 시 아웃라인 상시 on)는 이번 범위 밖 — 접객 테이블은 토글 아님.

## 4. 스킵 (YAGNI)

- 씬/프리팹 수정 — `PhaseCondition` 은 이미 올바르게 설정됨.
- 아웃라인 컴포넌트를 조건부로 비활성화하는 별도 스크립트 — 인터랙터 게이트로 충분.

## 5. 구현 완료 (2026-08-30)

| 파일 | 내용 |
|---|---|
| `Interaction/Drivers/CursorInteractor.cs` | 호버 레이 히트에서 `hitOutline`/`hitSprite` 획득을 `candidate != null && candidate.CanInteract` 블록 안으로 이동 |
| `Interaction/Drivers/GazeInteractor.cs` | 동일 |
| `Assets/My/InGame/Prefabs/Item/Motel_Table.prefab` | `PhaseCondition.allowedPhases` `{Noon,Evening}` → `{Noon}` |
| `Assets/Scenes/InGame.unity` | 씬 인스턴스 `Motel_Table` 도 `{Noon}` (RecordPrefabInstancePropertyModifications) |
| `Docs/InteractionCondition.md` 등 | 아웃라인 게이트 실제 동작 반영 |

`Owner's_Motel_Room.prefab` 은 `Motel_Table` 을 중첩 프리팹으로만 참조(오버라이드 없음) → `Item/Motel_Table.prefab` 수정이 그대로 전파.

### 검증
- `uloop compile` : Success, Error 0, Warning 0.
- `Motel_Table.allowedPhases` per 단계: `Morning:blocked  Noon:OK  Evening:blocked  Dawn:blocked` ✓
- 코드 변경은 히트 처리 한 블록 이동 — 로직 자명. 인게임(Morning/Dawn 에 테이블 호버 시 외곽선 안 뜨는지) 확인 요망.

## 상태

2026-08-30 구현 완료. 인게임 확인 대기.
