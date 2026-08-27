# ChangeObjectEffect

`Assets/My/Scripts/Interaction/Effects/ChangeObjectEffect.cs`

오브젝트 켜기/끄기 스왑. 구 `TidyBed`(정리하기, 비토글) + `Curtain`(켜고끄기, 토글) 을 하나로 대체.

## 필드

| 필드 | 설명 |
|---|---|
| `onObjects` (`GameObject[]`) | "on" 상태에서 활성화할 오브젝트들 |
| `offObjects` (`GameObject[]`) | "on" 상태에서 비활성화할 오브젝트들 (off 상태에선 반대) |

## 동작

- `on = ctx.Interactable.IsToggle ? ctx.IsOn : true`
- `onObjects` 전부 `SetActive(on)`, `offObjects` 전부 `SetActive(!on)`
- 토글 상호작용: `IsOn` 따라 왕복.
- 비토글 상호작용: 상호작용할 때마다 항상 on 상태로 (되돌리기 없음 — 침대 정리 등).

## 외곽선 유지

스왑되는 두 메쉬가 공통 부모 아래 있고 부모에 `Outline` 이 있으면, QuickOutline 로컬 패치([`doc/0076`](../doc/0076-swapped-mesh-outline-fix.md))로 스왑돼도 외곽선이 유지됨.

## 관련
[Interactable](Interactable.md) · [InteractionSystem](InteractionSystem.md) · [`doc/0075`](../doc/0075-curtain-toggle-interaction.md)
