# 0092 - 줍기 소리 안 남 (SfxEffect가 PickupEffect보다 늦게 실행됨)

## 요청
> 줍기 상호작용에서 주울때 나는 소리가 안나 왜그럴까

## 조사
`Key1.prefab`의 컴포넌트 순서: `... Interactable → PickupEffect → SfxEffect → ItemImpactSound → Outline → AudioSource`.

`Interactable.Awake()`가 `effects = GetComponents<InteractionEffect>()`로 캐시하는데, 이건 **컴포넌트가 등록된 순서 그대로** 반환됨. `Interact()`는 이 배열을 순서대로 `Play()` 호출:

1. `PickupEffect.Play()` 실행 → `InventorySystem.AddItem(...)` 성공 시 `gameObject.SetActive(false)` (주운 오브젝트를 월드에서 감춤).
2. 이어서 `SfxEffect.Play()` 실행 → `src.Play()` 호출하지만, **오브젝트가 이미 비활성화된 상태**라 `AudioSource`가 소리를 재생하지 못함(콘솔 에러도 없이 조용히 무시됨).

즉 "줍기"는 관례상 `PickupEffect`(일반 Effect, rank 7)가 `SfxEffect`(rank 8)보다 먼저 오도록 정렬되는데, 하필 `PickupEffect`가 유일하게 **자기 자신을 비활성화하는** 효과라서 이 순서 조합에서만 문제가 생김.

**주의**: `Key2.prefab`은 중첩 프리팹(변형) 구조라 `m_Component` 순서가 이 파일에 직접 없음(`GameObject ... stripped`) — 프리팹 파일을 직접 고쳐서 순서를 맞추는 건 이런 변형 구조에서 위험/불확실함. `Can_Coke`, `FlashLight_low-Poly`도 프리팹마다 순서를 일일이 맞추는 건 앞으로 만들 모든 줍기 아이템에 계속 반복될 부담.

## 계획
프리팹 컴포넌트 순서를 건드리는 대신, **코드에서 실행 순서를 강제**한다 — `Interactable.Awake()`가 캐시할 때 `SfxEffect`를 항상 먼저 오도록 정렬. 인스펙터상 컴포넌트 나열 순서와 무관하게 항상 올바르게 동작하고, 기존 프리팹들 손 안 대도 됨.

```csharp
using System.Linq;
...
private void Awake()
{
    // SfxEffect 는 항상 먼저 실행 — 뒤따르는 효과(PickupEffect 등)가 오브젝트를 비활성화해도
    // 소리는 이미 재생을 시작한 뒤라 안 끊김. 인스펙터 컴포넌트 나열 순서와 무관하게 항상 이 순서로 실행.
    effects = GetComponents<InteractionEffect>()
        .OrderBy(e => e is SfxEffect ? 0 : 1)
        .ToArray();
    conditions = GetComponents<InteractionCondition>();
    IsOn = startOn;
    ...
}
```
(`OrderBy`는 안정 정렬이라 같은 그룹 내 상대 순서는 유지됨.)

`Docs/InteractionSystem.md`에 "실제 실행 순서는 SfxEffect가 항상 최우선 — 인스펙터 표시 순서(컴포넌트 정렬)와는 별개" 라고 한 줄 추가.

## 리스크
- 낮음. 정렬 기준이 "SfxEffect 먼저"뿐이라 다른 효과들 간 상대 순서(예: 여러 개의 커스텀 효과)는 그대로 유지됨.
- 프리팹 파일은 전혀 안 건드림 — 기존 4개(Key1/Key2/Can_Coke/FlashLight) 전부 자동으로 고쳐짐, 앞으로 만들 줍기 아이템도 자동 적용.

## 결과 (2026-08-28, 승인 후 적용)
`Interactable.cs`: `using System.Linq;` 추가, `Awake()`에서 `effects`를 `GetComponents<InteractionEffect>().OrderBy(e => e is SfxEffect ? 0 : 1).ToArray()`로 정렬. 프리팹 파일은 손 안 댐. `Docs/InteractionSystem.md`에 실행 순서 설명 한 줄 추가.

## 검증
- 정적 확인만 완료. Unity Play 모드에서 Key1/Key2/Can_Coke/FlashLight 줍기 시 소리 나는지 확인 필요.

## 상태
2026-08-28 완료.
