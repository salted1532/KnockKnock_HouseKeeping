# 0141 - 모니터 화면 UI 호버 시에도 모니터 메쉬 외곽선

날짜: 2026-09-02
관련: `doc/0129`(커서가 UI 위면 월드 무시), `doc/0130`(화면 배경 클릭=모니터 상호작용), `doc/0119`(CRT 모니터 World Canvas), `Assets/My/Scripts/Interaction/Drivers/CursorInteractor.cs`

## 요청 (원문)

> 모니터 UI 창을 마우스 호버하면 모니터 모델에 외곽선이 안생기네 UI도 호버시 모니터 매쉬에 외곽선 생기도록해줘

## 원인

`CursorInteractor.Update` 는 커서가 uGUI 위(`EventSystem.IsPointerOverGameObject()`)면
`ClearHover()` 후 즉시 리턴 (`doc/0129`) — 월드 레이도 안 쏘고 외곽선도 끈다.
그래서 모니터 **프레임** 을 호버하면 (월드 레이 → 콜라이더) 외곽선이 뜨는데,
모니터 **화면(방배정 버튼/배경)** 을 호버하면 UI 라서 아무 외곽선도 안 뜬다.

## 수정 — `CursorInteractor`

커서가 UI 위일 때 그냥 나가지 않고, 그 UI 가 얹힌 `Interactable`(모니터 등)의 외곽선을
**월드 호버와 똑같은 diff 코드로** 처리한다. 클릭·프롬프트는 안 건드림 (UI 요소가 자기 클릭 처리).

```csharp
bool overUI = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
if (overUI)
    ResolveUIOutline(out hitOutline, out hitSprite);   // hovered 는 null 로 둠 → 프롬프트/월드클릭 안 함
else if (TryCursorRay(...) && Physics.Raycast(...))
    { ... 기존 월드 경로 ... }

// 이후 hitOutline/hitSprite/hovered diff 블록은 공통 (기존 코드 그대로)

// ── 신규 ──
private void ResolveUIOutline(out Outline ol, out SpriteOutline so)
{
    // Mouse.current.position 로 EventSystem.RaycastAll
    // 히트 중 GetComponentInParent<Interactable>() 가 CanInteract 인 첫 항목의
    //   GetComponentInParent<Outline>() / SpriteOutline 반환
}
```

- 외곽선을 켜고 끄는 건 기존 `if (hitOutline != currentOutline)` 블록 하나가 담당 →
  UI 밖으로 나가면 `hitOutline=null` 이 되어 자동으로 꺼짐. **소유자 1개(CursorInteractor)** 유지.
- 프레임 → 화면 이동: 외곽선 유지, 프롬프트만 사라짐. 화면 → 프레임: 외곽선 유지 + 프롬프트 복귀.
- `RaycastAll` 은 `RenderTextureGraphicRaycaster` 를 거치므로 RT 파이프라인 좌표 보정도 그대로 먹음
  (게이트 `UIInteractionMode.Active` — 화면고정/접객 모드에서만).

`GazeInteractor` 는 안 건드림 — 커서 없음, 모니터 UI 는 UI 모드에서만 상호작용, 게임플레이 중엔 월드 레이로 프레임 히트.

## 검증 (플레이)

- `EventSystem.RaycastAll` @ 모니터 버튼 스크린 좌표 → `Text (TMP)` / `105_Button` / `Background` 히트 (전부 `RenderTextureGraphicRaycaster`).
- 해석 체인: 첫 히트 `GetComponentInParent<Interactable>()` → **`CRTMonitor`** (CanInteract) → `GetComponentInParent<Outline>()` → **`CRTMonitor` Outline**. ✅
- UI 모드에서 커서가 모니터 화면 위일 때 `CRTMonitor.Outline.enabled == True` 확인 (수정 전엔 `ClearHover` 로 강제 False).
- `uloop compile` Error 0.
- (에디터 비포커스로 외곽선 렌더 스크린샷은 못 잡음 — 상태값·레이캐스트 체인까지 검증)

## 영향 파일

```
Interaction/Drivers/CursorInteractor.cs   수정  overUI 분기 + ResolveUIOutline()
Docs/CursorInteractor.md                  갱신
```

## 상태

2026-09-02 코드 + 컴파일 + 레이캐스트 체인/상태 검증 완료. 인게임 외곽선 시각 확인은 사용자.
