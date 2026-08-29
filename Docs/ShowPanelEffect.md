# ShowPanelEffect

`Assets/My/Scripts/Interaction/Effects/ShowPanelEffect.cs`

"읽기" 상호작용: 노트/편지/사진 등. 상호작용 시 지정 오브젝트를 켜고 플레이어를 정지시킨다 (이동·시야 정지 + 커서 표시). ESC 또는 재상호작용으로 닫음.
`InteractionPrompt.읽기` 표준 효과 (managed — 우클릭 "재설정" 으로 자동 추가/제거).

## 필드

| 필드 | 설명 |
|---|---|
| `content` (`GameObject`) | 상호작용 시 켤 오브젝트. UI 이미지·패널이든 별도 Canvas 든 3D 오브젝트든 아무거나. `Awake` 에서 자동 비활성화. **필수** (구 필드명 `panel` — `[FormerlySerializedAs]` 로 기존 연결 유지) |

## 동작

- `Play()` → `content` 토글. 열 때 `UIInteractionMode.FreezeForOverlay(true)` (앵커 이동 없이 FPC 정지 + Gaze suspend + 커서 표시 + 크로스헤어 숨김). `UIInteractionMode` 없으면 커서만 처리.
- `Update` 에서 ESC → `Close()`. UI 닫기 버튼에서 `public Close()` 호출 가능 — **`content.SetActive(false)` 직접 호출 금지** (퍼즈가 안 풀림). `OnDisable` 도 안전 복구.
- `static ConsumesEsc` — 노트가 열려 있거나 이번 프레임 ESC 로 방금 닫혔으면 true.
  `UIInteractionMode`/`ReceptionManager` 의 ESC 분기가 이걸 체크 → **UI/접객 모드에서 노트 열고 ESC = 노트만 닫힘**, 다음 ESC 에 모드 탈출. Update 실행 순서 무관 (frame-stamp).
- `[RuntimeInitializeOnLoadMethod]` 로 `openCount` static 초기화 — Domain Reload 꺼도 ESC 영구 차단 안 됨.

## 관련

[Interactable](InteractionSystem.md) · [UIInteractionMode](UIInteractionMode.md) · [`doc/0100`](../doc/0100-note-and-monitor-interactions-design.md)
