# Docs/

스크립트별 레퍼런스 문서 모음. 세션 로그는 `doc/`(소문자, 별도 폴더)에 기록됨 — 혼동 금지.

> Windows는 폴더/파일명 대소문자를 구분하지 않으므로 `Doc/`(대문자)와 `doc/`(소문자)는 실제로 같은 폴더가 된다.
> 이를 피하기 위해 스크립트 레퍼런스 문서는 `Docs/`(복수형), 세션 로그는 `doc/`(단수, 소문자)로 이름을 분리한다.

## 상호작용 시스템

허브: **[InteractionSystem.md](InteractionSystem.md)** — 모델, 작업 흐름, 행동 카테고리, 우클릭 재설정 메뉴.

| 문서 | 스크립트 |
|---|---|
| [Interactable](Interactable.md) | `Interaction/Interactable.cs` |
| [InteractionEffect](InteractionEffect.md) | `Interaction/Core/InteractionEffect.cs` (+ `InteractionContext`) |
| [InteractionCondition](InteractionCondition.md) | `Interaction/Core/InteractionCondition.cs` |
| [SfxEffect](SfxEffect.md) | `Interaction/Effects/SfxEffect.cs` |
| [ChangeObjectEffect](ChangeObjectEffect.md) | `Interaction/Effects/ChangeObjectEffect.cs` |
| [HingeEffect](HingeEffect.md) | `Interaction/Effects/HingeEffect.cs` |
| [PushEffect](PushEffect.md) | `Interaction/Effects/PushEffect.cs` |
| [PickupEffect](PickupEffect.md) | `Interaction/Effects/PickupEffect.cs` |
| [SpawnObjectEffect](SpawnObjectEffect.md) | `Interaction/Effects/SpawnObjectEffect.cs` |
| [EnterUIModeEffect](EnterUIModeEffect.md) | `Interaction/Effects/EnterUIModeEffect.cs` (화면고정 — 모니터) |
| [ShowPanelEffect](ShowPanelEffect.md) | `Interaction/Effects/ShowPanelEffect.cs` (읽기 — 노트/편지) |
| [PhaseSwitchEffect](PhaseSwitchEffect.md) | `Interaction/Effects/PhaseSwitchEffect.cs` (하루종료 스위치 — 게시판/테이블/침대) |
| [KnockEffect](KnockEffect.md) | `Interaction/Effects/KnockEffect.cs` (새벽 노크 — 배정 방문) |
| [RoomController](RoomController.md) | `Interaction/RoomController.cs` (객실 관제 — 새벽 문 잠금/노크 전환) |
| [PhaseCondition](PhaseCondition.md) | `Interaction/Conditions/PhaseCondition.cs` |
| [GazeInteractor](GazeInteractor.md) | `Interaction/Drivers/GazeInteractor.cs` |
| [CursorInteractor](CursorInteractor.md) | `Interaction/Drivers/CursorInteractor.cs` |
| [RenderTextureGraphicRaycaster](RenderTextureGraphicRaycaster.md) | `Interaction/Drivers/RenderTextureGraphicRaycaster.cs` — 오브젝트 화면(CRT 모니터 등)에 얹은 World Space Canvas uGUI 클릭 보정 |
| [UIInteractionMode](UIInteractionMode.md) | `Interaction/Modes/UIInteractionMode.cs` |
| [ItemImpactSound](ItemImpactSound.md) | `Interaction/ItemImpactSound.cs` |
| [CartGroundAlign](CartGroundAlign.md) | `Interaction/CartGroundAlign.cs` |
| [OutlineWhenOff](OutlineWhenOff.md) | `Interaction/OutlineWhenOff.cs` — 꺼져 있을 때 외곽선 상시 표시 (조명 스위치) |
| [SpriteOutline](SpriteOutline.md) | `Interaction/SpriteOutline.cs` — 2D 스프라이트 손님 hover 하이라이트 (QuickOutline 대체) |

## 인벤토리 / 아이템

| 문서 | 스크립트 |
|---|---|
| [InventorySystem](InventorySystem.md) | `Inventory/InventorySystem.cs` |
| [HandItemRegistry](HandItemRegistry.md) | `Inventory/ItemId.cs`, `HandItem.cs`, `HandItemRegistry.cs` |

## 대화 / 접객 손님

허브: **[DialogueSystem.md](DialogueSystem.md)** — CSV 스키마, 노드/선택지, NpcCatalog/CampaignData, 손님 큐, 거절 흐름.

| 문서 섹션 | 스크립트 |
|---|---|
| 데이터 | `Dialogue/NpcData.cs`, `NpcCatalog.cs`, `DialogueLine.cs`(DialogueEntry/Choice), `DialogueDatabase.cs`, `Situation.cs`, `Expression.cs`, `Editor/DialogueImporter.cs`, `Game/CampaignData.cs` |
| 런타임 | `Dialogue/SpeechBubble.cs`, `DialogueRunner.cs`, `QuestionPanel.cs`, `GuestMover.cs`, `GuestView.cs` |
| NPC 관리 | `Game/GuestManager.cs`, `ReceptionManager.cs`, `Interaction/Effects/CheckInGuestEffect.cs` |
| 방배정 / 새벽 노크 | [MonitorRoomBoard](MonitorRoomBoard.md), [RoomController](RoomController.md), [KnockEffect](KnockEffect.md) (`doc/0118`) |

## 게임 진행 / 환경 / 사운드

| 문서 | 스크립트 |
|---|---|
| [DayPhaseManager](DayPhaseManager.md) | `Game/DayPhaseManager.cs` (아침/점심/저녁/새벽 + 페이드 전환) |
| [ReceptionManager](ReceptionManager.md) | `Game/ReceptionManager.cs` (저녁 접객 손님 큐 + 모니터 방배정) — [DialogueSystem](DialogueSystem.md) 참조 |
| [MonitorRoomBoard](MonitorRoomBoard.md) | `Game/MonitorRoomBoard.cs` (CRT 모니터 방배정 uGUI 보드) |
| [PhaseLabel](PhaseLabel.md) | `Game/PhaseLabel.cs` (HUD 시간대 텍스트) |
| [ActivateOnAwake](ActivateOnAwake.md) | `Game/ActivateOnAwake.cs` (런타임에 UI 켜기 유틸) |
| ScreenMessage | `UI/ScreenMessage.cs` — 화면 중앙 임시 나레이션/관찰 문구 (노크 거절 "응답이 없다" 등). `ScreenMessage.Show(en, ko)` 싱글턴, 페이드 인/유지/아웃 |
| [ScreenFader](ScreenFader.md) | `Environment/ScreenFader.cs` (검정 페이드) |
| [PhaseVisuals](PhaseVisuals.md) | `Environment/PhaseVisuals.cs` (구 `DayNightSwitcher` 대체 — 4단계 조명/스카이박스) |
| [SoundManager](SoundManager.md) | `Audio/SoundManager.cs` |
| [FootstepSystem](FootstepSystem.md) | `Player/FootstepSystem.cs` |
| [LocalizationManager](LocalizationManager.md) | `Localization/LocalizationManager.cs` (영어/한글 — 씬 배치, 게임 시작 시 확정) + `LocalizedLabel.cs` + `Editor/FontTool.cs` |

## 서드파티 로컬 패치

`Assets/AssetsFolder/QuickOutline/Scripts/Outline.cs` 에 로컬 수정 있음 (`grep "LOCAL PATCH"`):
- **doc/0076** — `GetComponentsInChildren<...>(true)` (비활성 자식 포함, 스왑 메쉬 외곽선 유지)
- **doc/0083** — `excludeRoots` 필드 (특정 자식 트리 렌더러를 외곽선에서 제외, 조명 스위치 안 램프 등)

에셋 재임포트/업데이트 시 날아감 → 해당 doc 보고 재적용.

## 정리 완료 (doc/0081)

마이그레이션 완료 확인 후 삭제됨: `Interaction/Door.cs` → [HingeEffect](HingeEffect.md), `Interaction/ItemDispenser.cs` → [SpawnObjectEffect](SpawnObjectEffect.md), `Editor/InteractionMigrator.cs`(1회용), `Interactable.cs` 의 `LEGACY` 필드 블록.

남은 정리 검토 대상(Unity 템플릿 잔재 등)은 [README 스크립트 정리 분석](../README.md#스크립트-정리-분석) 참고.
