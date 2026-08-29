# 0101 - README / Docs 갱신 (접객·노트·모니터·하루 흐름 반영)

## 요청

현재 진행 사항(상호작용 시스템 개편, 접객 UI 모드, 하루 4단계 시스템, 노트/모니터)을 확인하고:
1. `README.md` 갱신.
2. 새로 추가된 스크립트 → `Docs/` 문서 생성 + 연결.
3. 수정된 기존 스크립트 → 작동 원리 문서 갱신.
4. 로드맵 갱신.

## 확인한 현재 코드 상태

README/Docs 마지막 갱신(`cf1835ee`, `e0b486de`) 이후 커밋 `ae0be04a`(doc/0100 구현)에서 들어온 변경:

| 스크립트 | 상태 |
|---|---|
| `Environment/PhaseVisuals.cs` | 신규 — 4단계 조명/스카이박스/볼륨/fog, `OnPhaseChanged` 구독 |
| `Environment/ScreenFader.cs` | 신규 — 풀스크린 검정 `FadeThrough(atBlack, done)` |
| `Environment/DayNightSwitcher.cs` | **삭제** |
| `Game/ReceptionManager.cs` | 재작성 — `Evening` 자동 진입, `EndSession()`(디버그 K), `Exited` 구독 |
| `Game/PhaseLabel.cs` | 신규 — HUD 시간대 텍스트 |
| `Game/ActivateOnAwake.cs` | 신규 — 런타임에 UI 켜기 |
| `Game/DayPhaseManager.cs` | `TransitionTo` 페이드 경유, `OnPhaseChangeFinished`, `Transitioning`, 디버그 N+Q |
| `Interaction/Effects/PhaseSwitchEffect.cs` | 신규 — `from→to` 단계 전환 |
| `Interaction/Effects/ShowPanelEffect.cs` | 신규 — `읽기`, 오브젝트 토글 + 플레이어 정지, `ConsumesEsc` |
| `Interaction/Modes/UIInteractionMode.cs` | 앵커 스택, `playerRoot`/`cameraPitchPivot`, `edgeLook`, `FreezeForOverlay`, `Depth`, `Entered/Exited`, `crosshair` |
| `Interaction/Drivers/CursorInteractor.cs` | RenderTexture 커서 좌표 보정, 가림 체크 추가 |
| `Interaction/Interactable.cs` | enum `접객`→`화면고정`, +`읽기`/`아침·점심·저녁·하루종료`; switch 케이스; `ManagedEffects` += ShowPanel, PhaseSwitch; `PhaseCondition.allowedPhases` 자동설정 |
| `Audio/SoundManager.cs` | Q 토글 삭제 → `OnPhaseChanged` 구독 (저녁·새벽=밤) |

`doc/0079` (효과 레퍼런스) 는 이미 `ae0be04a` 에서 갱신됨. `Docs/`(스크립트 문서)와 `README.md` 만 스테일이었음.

## 변경 내역

### 신규 `Docs/` 문서 (7)
- `Docs/PhaseVisuals.md`, `Docs/ScreenFader.md`, `Docs/ReceptionManager.md`,
  `Docs/PhaseSwitchEffect.md`, `Docs/ShowPanelEffect.md`, `Docs/PhaseLabel.md`, `Docs/ActivateOnAwake.md`

### 삭제
- `Docs/DayNightSwitcher.md` (스크립트 삭제됨)

### 갱신한 `Docs/` 문서
- `Docs/DayPhaseManager.md` — 페이드 전환, `TransitionTo`, `OnPhaseChangeFinished`, `Transitioning`, N+Q, 소비자 표
- `Docs/UIInteractionMode.md` — 앵커 스택, 새 필드(`playerRoot`/`cameraPitchPivot`/`edgeLook`/`crosshair`), `FreezeForOverlay`, `Depth`, ESC 계층
- `Docs/SoundManager.md` — Q 삭제, `OnPhaseChanged` 구독
- `Docs/CursorInteractor.md` — RenderTexture 커서 레이 보정, 가림 체크, 새 필드
- `Docs/EnterUIModeEffect.md` — `접객`→`화면고정` rename, `PhaseCondition` 자동추가 없음
- `Docs/Overview.md` — 신규 문서 7개 추가, `DayNightSwitcher` 제거
- `Docs/InteractionSystem.md` — 프롬프트 표(화면고정/읽기/걸기/종료4종), managed 목록, 스크립트 문서 목록

### 갱신한 `README.md`
- 핵심 루프: 4단계 트리거 오브젝트(게시판/테이블/침대), 페이드, 아침 청소·새벽 총기 등 게임 흐름 상세
- 프로젝트 구조 트리: Game/Environment 폴더 내용
- 상호작용 카테고리 표: `걸기`/`화면고정`/`읽기`/종료4종 추가
- 핵심 스크립트 표: `PhaseVisuals`/`ScreenFader`/`ReceptionManager`/`PhaseLabel`/`ActivateOnAwake`/`HookEffect`/`ShowPanelEffect`/`PhaseSwitchEffect` 추가, `DayNightSwitcher` 제거
- 구현 완료 기능: "하루 진행 / 환경" + "UI 모드 / 접객" 섹션 재작성
- 로드맵: "DayPhaseManager ↔ DayNightSwitcher/SoundManager 연결" 완료 처리(제거), 접객/일과/새벽 항목 현행화
- 스크립트 정리 분석: SoundManager/DayNightSwitcher Q 행 → 해결, `ReceptionManager`/`edgeLook` 개선 여지 추가, 삭제 완료에 `DayNightSwitcher` 추가

## 코드 변경 없음
문서만 수정. 스크립트/에셋/씬 무변경.

## 상태
2026-08-29 완료.
