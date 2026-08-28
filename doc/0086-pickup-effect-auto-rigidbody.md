# 0086 - 줍기(Pickup) 프롬프트 재설정 시 Rigidbody 자동 추가

## 요청
"줍기에 경우 rigidbody 추가하도록 해줘"

## 조사
`Assets/My/Scripts/Interaction/Interactable.cs`의 우클릭 메뉴 `SyncEffectsToPrompt()` (`Prompt Type에 맞게 효과 재설정`)가
promptType 별로 필요한 컴포넌트를 자동으로 붙여준다 (`EnsureColliderAndLayer` → Collider, `EnsureOutline` → Outline, `SfxEffect`의 `[RequireComponent(AudioSource)]` → AudioSource).

`ReorderComponents`의 `Rank()`에 이미 `Rigidbody`(rank 4, Collider 다음/Interactable 이전) 자리가 정의돼 있지만, 실제로 **추가하는 로직은 없음** — 지금은 누가 수동으로 붙여야 함.
`줍기` 케이스([[project_interaction-system-redesign]] 기준: `PickupEffect` + `SfxEffect` + `ItemImpactSound`)는 바닥에 놓인 아이템이라 물리적으로 놓여야 자연스러운데 Rigidbody가 없으면 항상 정적 콜라이더로만 존재.

## 계획
`Interactable.cs`에 `EnsureRigidbody()`를 추가하고, `SyncEffectsToPrompt()`에서 `promptType == InteractionPrompt.줍기`일 때만 호출 (다른 프롬프트는 기존과 동일하게 안 건드림).

```csharp
// 기존 EnsureOutline() 호출부 근처
EnsureOutline();
if (promptType == InteractionPrompt.줍기)
    EnsureRigidbody();
ReorderComponents();
```

```csharp
// 줍기 아이템은 바닥에 물리적으로 놓이므로 Rigidbody 필요. 이미 있으면 손대지 않음.
private void EnsureRigidbody()
{
    if (GetComponent<Rigidbody>() != null) return;
    UnityEditor.Undo.AddComponent<Rigidbody>(gameObject);
    Debug.Log($"[Interactable] '{name}' Rigidbody 추가 (줍기)", this);
}
```

- 이미 Rigidbody 있으면 건드리지 않음(설정값 보존).
- 기본 Rigidbody 설정(Unity 기본값: `useGravity=true`, `isKinematic=false`, mass=1)을 그대로 사용 — 필요하면 인스펙터에서 개별 조정.
- `ManagedEffects`/제거 로직과는 무관 (Rigidbody는 제거 대상 아님, 다른 프롬프트로 바꿔도 유지).
- `Docs/InteractionSystem.md`의 "재설정 우클릭 메뉴가 하는 일" 목록에 항목 추가.

## 리스크
- 낮음. 새 컴포넌트 추가만 하고 기존 동작 변경 없음. 이미 Rigidbody 있는 프리팹은 영향 없음.
- 재설정을 실행해야 적용됨 — 기존에 이미 만들어진 줍기 아이템 프리팹들은 이 변경만으론 소급 적용 안 됨(직접 재설정 눌러야 함).

## 결과 (2026-08-28, 승인 후 적용)
계획대로 `Interactable.cs`에 `EnsureRigidbody()` 추가, `줍기` 프롬프트일 때만 `EnsureOutline()` 다음·`ReorderComponents()` 전에 호출. `Docs/InteractionSystem.md`의 "재설정 우클릭 메뉴가 하는 일" 목록에 5번 항목 추가.

## 상태
2026-08-28 완료. 기존 줍기 아이템 프리팹들은 우클릭 재설정을 다시 눌러야 소급 적용됨.
