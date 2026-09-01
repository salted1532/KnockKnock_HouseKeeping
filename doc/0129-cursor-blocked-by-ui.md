# 0129 - UI 창에 가려진 월드 오브젝트는 상호작용 차단

## 요청
대화 UI 창과 월드 오브젝트 상호작용이 겹치면 둘 다 작동함. UI 창에 가려진(뒤에 있는) 오브젝트는 상호작용 안 되게, UI 만 반응하도록.

## 원인
`CursorInteractor.Update()` 가 커서가 UI 위에 있는지 안 보고 무조건 뷰포트 보정 레이 → `Physics.Raycast` → 좌클릭 시 `Interact()`. 대화 패널 위를 클릭해도 패널 뒤 월드 오브젝트까지 같이 반응.

## 수정
`CursorInteractor.Update()` 맨 앞에 게이트:
```csharp
if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
{
    ClearHover();
    return;
}
```
- 커서가 raycastTarget UI(대화 패널·버튼·모니터 화면고정 캔버스 등) 위면 그 아래 월드는 무시.
- `Dialogue_Panel` 루트 Image 는 이미 `raycastTarget = true` → 패널 전체 영역이 막음 (버튼뿐 아니라 배경도).
- 풀스크린 게임뷰 RawImage 는 `raycastTarget = false`(doc/0119) 라 평소엔 게이트 안 걸림 — 실제 UI 위에서만.
- `RenderTextureGraphicRaycaster`(모니터) 도 EventSystem 에 등록돼 있어 모니터 버튼 호버 시에도 자동 차단 (원하는 동작 — 모니터 UI vs 뒤 오브젝트).

`GazeInteractor`(게임플레이 중앙 레이 + E)는 손 안 댐 — UI 패널 뜰 때는 항상 `Suspended`(ShowPanelEffect/UIInteractionMode) 라 애초에 안 돎.

## 상태
2026-08-31 완료. 컴파일 0에러.
