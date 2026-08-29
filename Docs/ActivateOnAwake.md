# ActivateOnAwake

`Assets/My/Scripts/Game/ActivateOnAwake.cs`

게임 시작 시(`Awake`) 지정한 오브젝트들을 `SetActive(true)`.
에디터에선 꺼둬서 씬 뷰를 안 가리고, 런타임에만 켜고 싶은 UI(예: [`ScreenFader`](ScreenFader.md) 의 검정 오버레이)에 쓴다.

## 필드

| 필드 | 설명 |
|---|---|
| `targets` (`GameObject[]`) | 시작 시 켤 오브젝트들 |

**항상 활성인 오브젝트(부모 Canvas 등)에 붙일 것** — 자기가 꺼져 있으면 `Awake` 가 안 돈다.

## 관련

[ScreenFader](ScreenFader.md)
