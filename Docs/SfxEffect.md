# SfxEffect

`Assets/My/Scripts/Interaction/Effects/SfxEffect.cs`

상호작용 시 효과음. 모든 상호작용에 하나씩 붙이는 게 원칙 (없으면 `Interactable` 이 경고 로그).
`[RequireComponent(typeof(AudioSource))]` — 추가 시 AudioSource 자동 부착, `Reset()` 이 3D(`spatialBlend=1`)·`playOnAwake=off` 로 초기화.

## 필드

| 필드 | 설명 |
|---|---|
| `clip` | 비토글 상호작용용 단일 클립 |
| `onClip` / `offClip` | 토글 상호작용용. 켜질 때 / 꺼질 때 |
| `interrupt` (기본 on) | 재생 중 다시 상호작용하면 이전 소리를 끊고 새 소리로 교체 (문 스윙 도중 재토글 등). off면 `PlayOneShot` 으로 겹쳐 재생 |

## 동작

- `Play`: `ctx.Interactable.IsToggle` 이면 `ctx.IsOn ? onClip : offClip`, 아니면 `clip`.
- 클립이 null 이면 아무것도 안 함.
- `interrupt` 면 `Stop() → clip 교체 → Play()`, 아니면 `PlayOneShot(c)`.
- 볼륨/믹서 그룹은 이 AudioSource 컴포넌트에서 조정.

## 관련
[Interactable](Interactable.md) · [InteractionSystem](InteractionSystem.md)
