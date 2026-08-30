# Interactable

`Assets/My/Scripts/Interaction/Interactable.cs`

플레이어의 레이/커서가 찾는 대상. 얇은 디스패처 — 실제 동작은 붙어 있는 `InteractionEffect` 들이 담당.
한 GameObject에 `Interactable` 1개 + Effect 여러 개 + Condition 0~N개를 조합한다.

## 필드

| 필드 | 설명 |
|---|---|
| `promptType` (`InteractionPrompt`) | 행동 카테고리 드롭다운. 표시 문구만 결정, 동작은 안 함 |
| `customPrompt` (string) | `promptType = 직접입력` 일 때만 쓰는 문구 |
| `isToggle` (bool) | on/off 왕복 상호작용인가. 켜면 상호작용마다 `IsOn` 이 뒤집힘 |
| `startOn` (bool) | 시작 상태 |
| `onInteracted` (`UnityEvent`) | 자유 연출 훅. 구 `Generic` 대체. (`onInteract` 에서 `[FormerlySerializedAs]` 로 이관) |

## 런타임 프로퍼티

| 이름 | 설명 |
|---|---|
| `IsOn` | 현재 토글 상태 (비토글이면 항상 `startOn`) |
| `IsToggle` | `isToggle` 값 |
| `Prompt` (string) | `promptOverride?.PromptOverride ?? DefaultPrompt`. `DefaultPrompt`: `OpenClose`/`Toggle` 는 `IsOn` 으로 동적, `Custom`→`customPrompt`, 그 외는 [LocalizationManager](LocalizationManager.md)`.T(en, ko)`. `CheckIn` = "Check in"/"체크인" |
| `CanInteract` (bool) | `enabled` && 활성 상태 && 모든 `Condition.IsMet`. false면 상호작용·아웃라인·프롬프트 전부 무시 |

### `IPromptOverride` (인터페이스)

같은 GameObject 의 컴포넌트가 구현하면 `Prompt` 를 상황에 따라 바꾼다. `PromptOverride` 가 `null` 이면 `DefaultPrompt` 사용. `Awake` 에서 `GetComponent<IPromptOverride>()` 로 1개만 캐시.
예: `CheckInGuestEffect` — 승인 대기 중 손에 열쇠 있으면 "체크인", 없으면 "대화".

## 메소드

- `Interact(Interactor source, Vector3 point)` — Interactor 가 호출.
  1. `CanInteract` 아니면 중단
  2. `isToggle` 면 `IsOn = !IsOn`
  3. `InteractionContext` 생성 (`IsOn` = 토글이면 새 상태, 비토글이면 항상 true)
  4. `GetComponents<InteractionEffect>()`(Awake 캐시)를 순서대로 `Play(ctx)` (`enabled` 인 것만)
  5. `onInteracted.Invoke()`
- `ForceState(bool on)` — 코드에서 IsOn 강제 설정 (연출/저장 로드용). 효과 재생 안 함.
- `SetState(bool on)` — IsOn 설정 **+ 효과 재생** (`CanInteract`·`isToggle` 무시). NPC 가 문 여는 연출 등. 이미 그 상태면 no-op. `ReceptionManager` 가 손님 입·퇴장 시 출입문에 호출 (`doc/0117`).
- `Awake()` — 효과·조건 캐시. 효과도 `onInteracted` 도 없으면 경고 로그.

## 에디터 전용 (`#if UNITY_EDITOR`)

- **우클릭 메뉴 "Prompt Type에 맞게 효과 재설정"** (`SyncEffectsToPrompt`) — [InteractionSystem.md](InteractionSystem.md#재설정-우클릭-메뉴가-하는-일) 참고. 효과 추가/제거 + 콜라이더·레이어 + Outline + 컴포넌트 순서 정렬.
- `LEGACY` 필드 블록 (`type`, `messyVisual`, `door` 등) — 마이그레이션(`Editor/InteractionMigrator.cs`)이 읽고 나면 정리 예정. 직접 쓰지 말 것.

호버 하이라이트는 **`Outline`**(QuickOutline, 3D 메쉬) 또는 **[`SpriteOutline`](SpriteOutline.md)**(2D 스프라이트 손님) 중 하나. `EnsureOutline` 은 `SpriteOutline` 이 있으면 `Outline` 을 안 붙인다. `CursorInteractor`/`GazeInteractor` 가 둘 다 토글.

## 관련
[InteractionEffect](InteractionEffect.md) · [GazeInteractor](GazeInteractor.md) · [InteractionSystem](InteractionSystem.md) · [`doc/0078`](../doc/0078-interaction-system-redesign.md)
