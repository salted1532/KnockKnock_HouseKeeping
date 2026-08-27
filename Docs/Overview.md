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
| [EnterUIModeEffect](EnterUIModeEffect.md) | `Interaction/Effects/EnterUIModeEffect.cs` |
| [PhaseCondition](PhaseCondition.md) | `Interaction/Conditions/PhaseCondition.cs` |
| [GazeInteractor](GazeInteractor.md) | `Interaction/Drivers/GazeInteractor.cs` |
| [CursorInteractor](CursorInteractor.md) | `Interaction/Drivers/CursorInteractor.cs` |
| [UIInteractionMode](UIInteractionMode.md) | `Interaction/Modes/UIInteractionMode.cs` |
| [ItemImpactSound](ItemImpactSound.md) | `Interaction/ItemImpactSound.cs` |
| [CartGroundAlign](CartGroundAlign.md) | `Interaction/CartGroundAlign.cs` |

## 인벤토리 / 아이템

| 문서 | 스크립트 |
|---|---|
| [InventorySystem](InventorySystem.md) | `Inventory/InventorySystem.cs` |
| [HandItemRegistry](HandItemRegistry.md) | `Inventory/ItemId.cs`, `HandItem.cs`, `HandItemRegistry.cs` |

## 게임 진행 / 환경 / 사운드

| 문서 | 스크립트 |
|---|---|
| [DayPhaseManager](DayPhaseManager.md) | `Game/DayPhaseManager.cs` |
| [DayNightSwitcher](DayNightSwitcher.md) | `Environment/DayNightSwitcher.cs` |
| [SoundManager](SoundManager.md) | `Audio/SoundManager.cs` |
| [FootstepSystem](FootstepSystem.md) | `Player/FootstepSystem.cs` |

## 정리 대상 (미문서화)

- `Interaction/Door.cs` — [HingeEffect](HingeEffect.md) 로 대체됨. 마이그레이션 검증 후 삭제 예정.
- `Interaction/ItemDispenser.cs` — [SpawnObjectEffect](SpawnObjectEffect.md) 로 대체. 자판기 재배선 후 삭제 예정.
- `Editor/InteractionMigrator.cs` — 1회용 마이그레이션 스크립트. 완료 후 삭제.
- `Interactable.cs` 의 `LEGACY` 필드 블록.

자세한 정리 계획은 [`doc/0078`](../doc/0078-interaction-system-redesign.md), [README 스크립트 정리 분석](../README.md#스크립트-정리-분석) 참고.
