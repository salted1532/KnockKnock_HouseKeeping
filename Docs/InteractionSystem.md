# 상호작용 시스템 (개편판)

`Interactable` + `InteractionEffect` **컴포넌트 조합** 방식. 구 `InteractionType` enum + 거대 switch 방식은 폐기.
전체 설계·마이그레이션 이력은 [`doc/0078`](../doc/0078-interaction-system-redesign.md), 상세 레퍼런스는 [`doc/0079`](../doc/0079-interaction-effects-reference.md).

## 모델

```
GameObject (Interaction 레이어 11 + Collider + Outline[off])
├─ Interactable            ← 플레이어가 찾는 대상, 디스패처 (1개)
├─ InteractionEffect …     ← 실제 동작 (여러 개 스택)
└─ InteractionCondition …  ← 상호작용 가능 여부 게이트 (0~N, 선택)
```

- 플레이어 쪽 **Interactor**(`GazeInteractor` 화면중앙+E / `CursorInteractor` 마우스+클릭)가 대상을 찾아 `Interactable.Interact()` 호출.
- `Interactable`은 붙어 있는 모든 `InteractionEffect.Play(ctx)`를 컴포넌트 순서대로 실행 + `onInteracted` UnityEvent 발동.
- 효과는 `InteractionContext`(대상/주체/IsOn/히트지점)를 읽는다.

## 작업 흐름

큰 행동 카테고리를 `Interactable`에서 프롬프트로 지정 → 컴포넌트 우클릭 **"Prompt Type에 맞게 효과 재설정"** →
해당 카테고리 표준 스크립트 자동 추가/정리(+콜라이더·레이어·Outline·컴포넌트 순서) → 각 효과의 오브젝트/클립 필드 수동 연결 → 완성.

행동 카테고리에도 들어가기 힘든 특별한 작동만 새 `InteractionEffect` 서브클래스를 만들어 붙인다. 큰 틀은 건드리지 않는다.

## InteractionPrompt (행동 카테고리)

| 값 | 표시 문구 | 우클릭 재설정 시 붙는 효과 | isToggle |
|---|---|---|---|
| 상호작용 | "상호작용" | `SfxEffect` | — |
| 여닫기 | "열기"↔"닫기" (IsOn) | `HingeEffect` + `SfxEffect` | ✓ |
| 켜고끄기 | "켜기"↔"끄기" (IsOn) | `ChangeObjectEffect` + `SfxEffect` | ✓ |
| 줍기 | "줍기" | `PickupEffect` + `SfxEffect` + `ItemImpactSound` | — |
| 사용 | "사용" | `SpawnObjectEffect` + `SfxEffect` | — |
| 조사 | "조사" | `SfxEffect` | — |
| 정리하기 | "정리하기" | `ChangeObjectEffect` + `SfxEffect` | ✗ |
| 밀기 | "밀기" | `PushEffect` + `SfxEffect` + `ItemImpactSound` | ✗ |
| 접객 | "접객" | `EnterUIModeEffect` + `SfxEffect` + `PhaseCondition` | ✗ |
| 직접입력 | `customPrompt` | `SfxEffect` | — |

## "재설정" 우클릭 메뉴가 하는 일

1. promptType 에 맞는 효과 **추가** + 필요 없는 managed 효과 **제거** (Undo 가능, 콘솔 로그)
   - managed = `Sfx / ChangeObject / Hinge / Push / Pickup / SpawnObject / EnterUIMode` 만 자동 제거.
   - `ItemImpactSound` · `PhaseCondition` · 커스텀 효과 · `onInteracted` 는 추가만, 제거 안 함.
2. `SfxEffect` 는 항상 포함 → `[RequireComponent(AudioSource)]` 로 AudioSource 자동(3D/논플레이온어웨이크).
3. 콜라이더 없으면 `BoxCollider`(메시 bounds 크기) 추가 + `Interaction` 레이어. 자식에 있으면 경고.
4. `Outline` 없으면 추가 → `enabled=off`, 모드 `OutlineVisible`.
5. 컴포넌트 순서 정렬: `Transform → MeshFilter → Renderer → Collider → Rigidbody → Interactable → Condition → 일반 Effect → SfxEffect → Outline·기타 .cs → AudioSource → 그 외`.

## 스크립트별 문서

| 스크립트 | 역할 |
|---|---|
| [Interactable](Interactable.md) | 디스패처. promptType/isToggle/onInteracted + 효과 실행 + 우클릭 재설정 메뉴 |
| [InteractionEffect](InteractionEffect.md) | 효과 베이스 + `InteractionContext` |
| [InteractionCondition](InteractionCondition.md) | 게이트 베이스 |
| [SfxEffect](SfxEffect.md) | 효과음 (토글이면 on/off 2클립) |
| [ChangeObjectEffect](ChangeObjectEffect.md) | 오브젝트 켜기/끄기 스왑 (구 TidyBed/Curtain) |
| [HingeEffect](HingeEffect.md) | 경첩 회전 (구 Door), hinge·axis 지정 가능 |
| [PushEffect](PushEffect.md) | 물리 밀기 (구 Push) |
| [PickupEffect](PickupEffect.md) | 인벤토리 획득 (구 Pickup/Flashlight), itemId 연결 |
| [SpawnObjectEffect](SpawnObjectEffect.md) | 프리팹 생성 (구 ItemDispenser) |
| [EnterUIModeEffect](EnterUIModeEffect.md) | UI 모드 진입 (책상 접객) |
| [PhaseCondition](PhaseCondition.md) | 하루 단계 게이트 |
| [GazeInteractor](GazeInteractor.md) | 화면중앙 레이 + E (구 InteractionOutline) |
| [CursorInteractor](CursorInteractor.md) | 마우스 레이 + 클릭 (UI 모드용) |
| [UIInteractionMode](UIInteractionMode.md) | 접객 UI 모드 매니저 |
| [DayPhaseManager](DayPhaseManager.md) | 아침/점심/저녁/새벽 진행 |
| [HandItemRegistry](HandItemRegistry.md) | ItemId → 손 오브젝트 조회 (`ItemId`, `HandItem` 포함) |
