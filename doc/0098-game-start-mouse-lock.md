# 0093 - 게임 시작 시 마우스 락 + 커서 숨김

## 요청
> 게임 시작 시 마우스를 화면 가운데 고정(마우스 락). 나중에 UI 모드 들어갈 땐 락 풀고 커서 보이게.

## 조사
`Assets/AssetsFolder/StarterAssets/InputSystem/StarterAssetsInputs.cs` — 커서 상태 로직이 이미 여기 있음:
- `cursorLocked = true` 필드, `SetCursorState()`.
- 근데 `OnApplicationFocus` 때만 호출. 게임 시작(`Start`)에서 락 안 걸고, `Cursor.visible` 은 아예 안 건드림.
- UI 모드 쪽(`UIInteractionMode`)은 이미 진입 시 `lockState=None; visible=true`, 해제 시 저장값 복원 — 손댈 것 없음.

빠진 건 "시작 시 락" 하나뿐.

## 계획
`StarterAssetsInputs.cs` 2곳:
```csharp
private void Start()
{
    SetCursorState(cursorLocked);
}

private void SetCursorState(bool newState)
{
    Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    Cursor.visible = !newState;   // 락이면 커서 숨김
}
```

## 리스크
- 낮음. `Start` 훅 추가 + `Cursor.visible` 한 줄.
- 벤더 코드 — `Tools/Starter Assets/Reinstall Dependencies` 재실행 시 덮어써짐 (doc 0091, [[project_quickoutline-local-patch]] 동종 주의).

## 결과 (2026-08-28, 승인 후 적용)
계획대로 적용. `Start()` 신규 추가, `SetCursorState` 에 `Cursor.visible = !newState` 추가.
- 게임 시작 → 커서 중앙 고정 + 숨김.
- UI 모드 진입/해제는 `UIInteractionMode` 가 그대로 처리 (변경 없음).

## 검증
- 정적 확인만. Play 모드에서 시작 시 커서 안 보이고 화면 중앙 고정, UI 모드 진입 시 커서 나타나는지 확인 필요.

## 상태
2026-08-28 완료.
