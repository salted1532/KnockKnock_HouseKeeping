# InteractionEffect / InteractionContext

`Assets/My/Scripts/Interaction/Core/InteractionEffect.cs`

모든 효과의 추상 베이스. 한 GameObject에 여러 개 붙여 조합한다.
`[RequireComponent(typeof(Interactable))]` — 효과만 먼저 추가해도 `Interactable` 이 자동으로 딸려옴.

```csharp
public abstract class InteractionEffect : MonoBehaviour
{
    public abstract void Play(in InteractionContext ctx);
}
```

`Interactable.Interact()` 가 붙어 있는 모든 효과를 컴포넌트 순서대로 `Play` 호출 (`enabled` 인 것만).

## InteractionContext (readonly struct)

상호작용 1회의 정보. 효과들이 읽는다.

| 필드 | 설명 |
|---|---|
| `Interactable` | 이 상호작용의 `Interactable` (`IsToggle`/`IsOn` 조회용) |
| `Source` (`GameObject`) | 상호작용한 주체 (플레이어). Interactor 의 `Owner`. null 가능 |
| `IsOn` (bool) | 토글 상호작용이면 토글 후 상태, 아니면 항상 true |
| `Point` (`Vector3`) | 레이 히트 지점 (밀기 토크 계산 등) |

## 새 효과 만들기

```csharp
public class MyEffect : InteractionEffect
{
    public override void Play(in InteractionContext ctx) { /* ... */ }
}
```

`InteractionSystem`의 managed 목록·우클릭 재설정 매핑에는 안 들어가므로, 재설정 메뉴로 자동 제거되지 않는다(수동 관리).

## 관련
[Interactable](Interactable.md) · [InteractionSystem](InteractionSystem.md)
