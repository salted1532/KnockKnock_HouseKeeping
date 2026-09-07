# 0167 – 접객 모드에서 클릭 외 플레이어 행동 차단

## 요청
접객 모드(화면고정)에서 **던지기(F)** 를 막고, 나아가 **CursorInteractor 클릭 상호작용을 제외한 모든 플레이어 행동**을 제한.

## 현황 (조사)
접객 모드 = `UIInteractionMode.Active`. 진입 시 이미 잠기는 것:
| 시스템 | 상태 |
|---|---|
| `FirstPersonController` (이동·시야·점프·달리기) | `enabled = false` |
| `CharacterController` | `enabled = false` |
| `GazeInteractor` (E 상호작용) | `Suspended = true` → Update 초입 return |
| `FootstepSystem` (발소리) | `UIInteractionMode.MovementLocked` 로 게이트 |
| `CursorInteractor` (커서 클릭) | `enabled = true` ← **허용돼야 하는 "클릭"** |
| `RenderTextureGraphicRaycaster` (화면 버튼) | `Active` 일 때만 통과 |

**게이트 안 된 유일한 플레이어 입력** = `InventorySystem.Update()`:
- `F` → `ThrowActiveItem()` (던지기)
- 좌클릭 → `UseActiveItem()` (손전등 등 사용 — 커서 클릭과 동시 발화하는 버그도 겸함)
- `1`~`5`, 스크롤 → 슬롯 전환

Flashlight/HandItem/HandItemRegistry 는 자체 입력 없음 (InventorySystem 경유).

## 작업
`Assets/My/Scripts/Inventory/InventorySystem.cs` `Update()` 초입에 가드 1줄 — `FootstepSystem` 과 동일 패턴:

```csharp
// 변경 전
private void Update()
{
    if (Keyboard.current == null)
        return;

// 변경 후
private void Update()
{
    if (Keyboard.current == null)
        return;

    // 화면고정(접객·노크·모니터)·오버레이(노트·페이드) 중엔 아이템 조작 전면 차단.
    // 이때 허용되는 상호작용은 CursorInteractor 의 커서 클릭뿐.
    if (UIInteractionMode.Instance != null && UIInteractionMode.Instance.MovementLocked)
        return;
```

- `MovementLocked` = `Active || FrozenForOverlay` → 접객·노크·모니터·노트읽기·시간대전환 모두 커버
- 던지기/사용/슬롯전환 전부 차단, 커서 클릭(CursorInteractor)·화면버튼은 그대로 동작
- 컴파일 검증 (`uloop compile`)

## 안 하는 것
- 새 컴포넌트/추상화 없음 — 기존 `MovementLocked` 재사용, 1줄.

## 결과
- `InventorySystem.cs` `Update()` 가드 추가 (제안대로), `uloop compile` 에러/경고 0.

## 수정 파일
- `Assets/My/Scripts/Inventory/InventorySystem.cs` (+5줄)
