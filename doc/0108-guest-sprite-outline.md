# 0108 - Guest 스프라이트 hover 아웃라인 (2차 SpriteRenderer 방식)

날짜: 2026-08-30
관련: `doc/0076`·`doc/0083`([[project_quickoutline-local-patch]]), `doc/0104`~`0105`(Guest), [[project_interaction-system-redesign]]

## 요청

> 현재 Guest 외곽선이 이상하게 나옴. 배경 투명 스프라이트라서인가? → 원인: QuickOutline(`Outline.cs`)은 3D 메쉬 실루엣용인데 Guest 는 2D `SpriteRenderer`. 노멀이 없어 실루엣을 못 만들고, fill 머티리얼이 스프라이트 쿼드를 흰 사각형으로 덧그림.
>
> **채택: 방법 2** — 같은 스프라이트를 조금 키우고 단색 틴트해서 뒤에 깔고 hover 시 켜기. (알파 모양 아님 = 중심 기준 균일 확대 → 실루엣이 살짝 두꺼워짐)

## 현재 구조 (Guest.prefab)

| 오브젝트 | 내용 |
|---|---|
| 루트 `Guest` (layer 11) | `MeshFilter`(빌트인 Quad) + `MeshRenderer`(**비활성**, 레거시) + `CapsuleCollider` + `GuestMover` + `Interactable`(promptType 16 CheckIn) + `CheckInGuestEffect` + `GuestView` + `AwaitingCheckInCondition` + **`Outline`**(비활성, Mode=OutlineVisible, width 6) |
| 자식 `Square` (layer 11) | `SpriteRenderer` (material `9dfc825a…`, sortingOrder 0) ← `GuestView.body` |

hover 시 `CursorInteractor`/`GazeInteractor` 가 `GetComponentInParent<Outline>()` → `.enabled = true`. `Outline.OnEnable` 이 자식 렌더러(SpriteRenderer 포함)에 fill/mask 머티리얼을 덧붙여 깨짐.

`Interactable.EnsureOutline()` 은 `OnValidate`/`Reset` 아님 — **컨텍스트 메뉴 "Prompt Type에 맞게 효과 재설정"** 에서만 호출. → 프리팹에서 `Outline` 제거해도 다시 안 생김.

## 설계

### A. `SpriteOutline` (신규 컴포넌트)

`Assets/My/Scripts/Interaction/SpriteOutline.cs`

```csharp
[DisallowMultipleComponent]
public class SpriteOutline : MonoBehaviour
{
    [SerializeField] private SpriteRenderer source;     // 비우면 자식에서 탐색 (= GuestView.body)
    [SerializeField] private Color color = new(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float scale = 1.06f;       // 원본 대비 확대 배율
    [SerializeField] private int sortingOffset = -1;    // 원본보다 뒤

    private SpriteRenderer outline;

    void Awake()
    {
        if (source == null) source = GetComponentInChildren<SpriteRenderer>();
        if (source == null) { enabled = false; return; }

        var go = new GameObject("SpriteOutline");
        go.layer = source.gameObject.layer;
        go.transform.SetParent(source.transform, false);   // 원본 스케일(초상화별 리스케일) 자동 상속
        go.transform.localScale = Vector3.one * scale;

        outline = go.AddComponent<SpriteRenderer>();
        outline.sharedMaterial = source.sharedMaterial;
        outline.color = color;
        outline.enabled = false;
    }

    void LateUpdate() { if (outline != null && outline.enabled) Mirror(); }

    void Mirror()
    {
        outline.sprite = source.sprite;
        outline.flipX = source.flipX; outline.flipY = source.flipY;
        outline.sortingLayerID = source.sortingLayerID;
        outline.sortingOrder = source.sortingOrder + sortingOffset;
    }

    public void SetHighlighted(bool on)
    {
        if (outline == null) return;
        if (on) Mirror();          // LateUpdate 이 이번 프레임 지났을 수 있어 즉시 1회
        outline.enabled = on;
    }
}
```

- 자식 GO 라 `GuestView.SetExpression` 이 `body.transform.localScale` 을 초상화마다 바꿔도 항상 `scale`× 크게 유지.
- `LateUpdate` 는 켜져 있을 때만 미러 (평소 비용 0).

### B. 드라이버 훅 (기존 `Outline` 처리와 평행하게, 추가만)

`CursorInteractor.cs` / `GazeInteractor.cs` 둘 다:
- 필드 `private SpriteOutline currentSprite;`
- 레이 히트 시 `Outline` 잡는 자리에서 같이: `var hitSprite = hit.collider.GetComponentInParent<SpriteOutline>();`
- 전환 블록 추가:
  ```csharp
  if (hitSprite != currentSprite)
  {
      if (currentSprite != null) currentSprite.SetHighlighted(false);
      if (hitSprite != null) hitSprite.SetHighlighted(true);
      currentSprite = hitSprite;
  }
  ```
- `ClearHover()` / `Clear()` 에 `if (currentSprite != null) currentSprite.SetHighlighted(false); currentSprite = null;`

(리팩터링 없음 — `Outline` 코드는 그대로, `SpriteOutline` 처리만 나란히 추가)

### C. `Interactable.EnsureOutline()` 가드 (1줄)

```csharp
private void EnsureOutline()
{
    if (GetComponent<SpriteOutline>() != null) return;   // 스프라이트 손님은 QuickOutline 안 씀
    ...
}
```
→ Guest 에 "효과 재설정" 눌러도 `Outline` 안 붙음.

### D. Guest.prefab

1. `Outline` 컴포넌트 **제거**.
2. 루트에 `SpriteOutline` 추가, `source` = `Square` 의 SpriteRenderer.
3. (선택) 죽은 `MeshFilter` + `MeshRenderer`(비활성) + Quad 참조 제거 — 별개 청소, 이번에 같이 할지 확인.

## 영향 파일

```
신규  Interaction/SpriteOutline.cs
수정  Interaction/Drivers/CursorInteractor.cs   SpriteOutline 훅
수정  Interaction/Drivers/GazeInteractor.cs     SpriteOutline 훅
수정  Interaction/Interactable.cs               EnsureOutline 가드 1줄
수정  Assets/My/InGame/Prefabs/Guest.prefab     Outline 제거 + SpriteOutline 추가/배선
신규  Docs/SpriteOutline.md
수정  Docs/Interactable.md · InteractionSystem.md (아웃라인 = Outline | SpriteOutline)
```

## 확인 필요

1. **틴트 색**: 따뜻한 노랑 `(1, 0.85, 0.2)` (추천) / 흰색 / 검정 / 기타.
2. **확대 배율**: `1.06` (6%). 더 두껍게/얇게?
3. **죽은 MeshFilter/MeshRenderer/Quad**: 이번에 같이 제거? (제거해도 기능 영향 없음)

## 스킵 (YAGNI)

- 알파 외곽선 셰이더 (방법 1) — 나중에 진짜 픽셀 단위 외곽선 필요하면.
- 4/8방향 오프셋 균일 halo — 중심 확대로 충분하다고 판단, 불만족 시 교체.
- `IHoverOutline` 인터페이스로 드라이버 일반화 — 대상이 Guest 하나뿐, 과설계.

## 확인 답변 (2026-08-30)

1. 틴트 색 = **따뜻한 노랑 `(1, 0.85, 0.2)`**
2. 확대 배율 = **1.06**
3. 죽은 MeshFilter/MeshRenderer/Quad = **같이 제거**

## 구현 완료 (2026-08-30)

| 파일 | 내용 |
|---|---|
| `Interaction/SpriteOutline.cs` | 신규. `Awake` 에서 `source`(비면 자식 SpriteRenderer) 밑에 자식 `SpriteOutline` GO + SpriteRenderer 생성 (sharedMaterial 복사, color, localScale=1.06, 시작 off). `SetHighlighted(bool)` 로 토글, 켜질 때 `Mirror()`(sprite/flip/sortingOrder = 원본 -1). `LateUpdate` 는 켜져 있을 때만 미러 |
| `Interaction/Drivers/CursorInteractor.cs` | `currentSprite` 필드 + 레이 히트에서 `GetComponentInParent<SpriteOutline>()` + `Outline` 옆에 평행 토글 블록 + `ClearHover` |
| `Interaction/Drivers/GazeInteractor.cs` | 동일 |
| `Interaction/Interactable.cs` | `EnsureOutline()` 맨 앞에 `if (GetComponent<SpriteOutline>() != null) return;` — "효과 재설정" 눌러도 Guest 에 QuickOutline 안 붙음 |
| `Assets/My/InGame/Prefabs/Guest.prefab` | `Outline` + 비활성 `MeshRenderer` + `MeshFilter` 제거. `SpriteOutline` 추가, `source` = `Square` 의 SpriteRenderer |
| `Docs/SpriteOutline.md` | 신규 |
| `Docs/Interactable.md` · `InteractionSystem.md` | 아웃라인 = `Outline`(메쉬) \| `SpriteOutline`(스프라이트) 명시 |

### 검증
- `uloop compile` : Success, Error 0, Warning 0. 씬 missing script 0.
- 플레이 모드 스모크: Guest 인스턴스 → `GuestView.Apply(Npc_2)` → 자식 `SpriteOutline` 생성됨. `SetHighlighted(true/false)` 로 `enabled` 토글. color `(1,0.85,0.2)`, `lossyScale` 0.17 vs body 0.16 (≈6%), sprite/sortingOrder(-1) 미러 정상, material `Sprite-Unlit-Default`. 콘솔 에러 0.

### 남은 사용자 검증
접객 진입 → 손님에 마우스 호버 → 노란 실루엣 halo 가 뜨고 이탈 시 사라지는지. 색/두께는 `SpriteOutline` 인스펙터(`color`/`scale`)에서 조정.

## 7d. 후속 수정 (2026-08-30) — 손님 2배 + 외곽선 단색화

> 요청: 접객 스프라이트가 2배 정도 컸으면. 그리고 뒤 외곽선이 단색이어야 하는데 접객 이미지가 2개 겹친 것처럼 보임.

**원인**: `SpriteRenderer.color` 는 텍스처 RGB 를 **곱**할 뿐 대체하지 않음 → 노란 틴트된 초상화가 원본 뒤에 6% 크게 = 겹쳐 보임.

| 파일 | 내용 |
|---|---|
| `Assets/My/InGame/Shader/SpriteSilhouette.shader` (신규) | `Shader "My/SpriteSilhouette"` — 텍스처 알파만 사용, RGB 는 `SpriteRenderer.color`(정점 색)로 대체. `clip(a - _Cutoff)` + `SrcAlpha OneMinusSrcAlpha`. 단색 실루엣 |
| `Assets/My/InGame/Material/SpriteSilhouette.mat` (신규) | 위 셰이더 머티리얼 |
| `Interaction/SpriteOutline.cs` | `[SerializeField] Material material` 추가. 있으면 그것, 없으면 원본 머티리얼 복사(구동작). 외곽선 렌더러에 적용 |
| `Guest.prefab` | `SpriteOutline.material` = `SpriteSilhouette.mat`. `GuestView.worldHeight` `1.9 → 3.8` (2배) |

### 검증 (플레이 스모크)
- 외곽선 머티리얼 = `SpriteSilhouette` / shader `My/SpriteSilhouette`, color `(1,0.85,0.2)`.
- `body` 렌더러 월드 높이 ≈ 3.8 (lossyScale 0.16→0.32, 2배).
- 컴파일 Error 0, 콘솔 에러 0.
- (부수 확인) 오버레이 UI 캡처에서 Galmuri 한글 + `·` 폴백 정상 렌더 확인.

## 7e. 후속 — 크기 축소 + 콜라이더 동기화 + 지면 정렬

> 요청: 접객 크기 조금 줄이고, 그 크기에 맞춰 CapsuleCollider 도 맞추고, 스프라이트가 땅에 박히는 것 해결.

**원인**: `Square` 가 로컬 (0,2,0) 하드코딩 + `GuestView` 가 스케일만 건드리고 위치는 안 건드림 → 스케일된 스프라이트 바닥이 루트 발밑과 안 맞음. 콜라이더(center 2, height 2.4)도 스프라이트와 무관하게 고정.

| 파일 | 내용 |
|---|---|
| `Dialogue/GuestView.cs` | `worldHeight` 기본 `3.8 → 3.2`. 신규 필드 `feetLocalY`(스프라이트 바닥이 놓일 루트 로컬 Y, 기본 0), `bodyCollider`(CapsuleCollider), `colliderRadiusRatio`(0.16). `SetExpression` 이 스케일 후 `body.localPosition.y = feetLocalY - bounds.min.y*k` 로 바닥을 지면에 맞추고, `bodyCollider` 있으면 `height=worldHeight`, `radius=worldHeight*ratio`, `center.y=feetLocalY+worldHeight/2` 동기화 |
| `Guest.prefab` | `GuestView.worldHeight=3.2`, `bodyCollider`=캡슐 배선, `feetLocalY=0`. authored 프리뷰 값도 맞춤: `Square` scale 0.267 / localY 1.6, Capsule height 3.2 / radius 0.512 / center.y 1.6 |

경로 웨이포인트가 y≈0.009(지면)이고 `GuestMover.WarpTo` 가 루트를 거기로 → 발밑 정렬이면 스프라이트가 지면에 섬. 리셉션 바닥이 다르면 `feetLocalY` 로 조정.

### 검증 (플레이 스모크, 루트 y=0)
- `body.bounds`: min.y **0**, max.y 3.2 (바닥 = 지면, 높이 3.2) ✓
- 캡슐: height 3.2, radius 0.512, center.y 1.6 → bottom 0 / top 3.2 (스프라이트와 일치) ✓
- 표정 Angry 전환 후에도 바닥 0 / 높이 3.2 유지 ✓
- 컴파일 Error 0.

## 7f. 후속 — 외곽선 두께 축소

> 요청: 접객 뒤 외곽선 크기를 0.6 → 0.2 정도로.

`Guest.prefab` `SpriteOutline.scale` **1.06 → 1.02** (원본 대비 6% → 2% 확대 = halo 약 1/3). 코드 변경 없음.

## 상태

2026-08-30 구현 완료 (+ 크기 3.2 + 콜라이더 동기 + 지면 정렬 + 외곽선 1.02). 컴파일·플레이 스모크 확인. 인게임 최종 검증 대기.
크기 더 조정하려면 `Guest` 프리팹 `GuestView.worldHeight`(스프라이트·위치·콜라이더 일괄) / `SpriteOutline.scale`(외곽선 두께).
