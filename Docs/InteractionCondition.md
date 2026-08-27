# InteractionCondition

`Assets/My/Scripts/Interaction/Core/InteractionCondition.cs`

`Interactable` 에 붙이면 `IsMet` 가 false 인 동안 상호작용이 막힌다. 없으면 항상 가능.
`[RequireComponent(typeof(Interactable))]`.

```csharp
public abstract class InteractionCondition : MonoBehaviour
{
    public abstract bool IsMet { get; }
}
```

`Interactable.CanInteract` 가 `GetComponents<InteractionCondition>()` 의 `enabled` 인 것들을 전부 확인 → 하나라도 `IsMet == false` 면 상호작용·아웃라인·프롬프트가 모두 안 뜬다.

## 구현체
- [PhaseCondition](PhaseCondition.md) — 지정한 하루 단계에서만 허용

## 관련
[Interactable](Interactable.md) · [InteractionSystem](InteractionSystem.md)
