# 0179 – NPC 스프라이트가 맵 조명에 반응 (SpriteLightResponse)

## 요청
"npc 스프라이트가 좀 밝은 거 같은데 맵의 빛에 따라 어두워지고 밝아지도록 할 수 있나?"

## 배경
Guest.prefab 의 `Square`(SpriteRenderer)는 unlit 스프라이트 머티리얼이라 씬 라이트를 완전히
무시 → 어두운 복도에서도 대낮처럼 밝다. 이 프로젝트는 URP **3D**(2D 라이트 렌더러 아님)라
URP 2D lit 스프라이트 경로는 못 씀. 셰이더 교체(URP Lit)는 SpriteRenderer 텍스처 바인딩이
URP 에서 어긋날 위험 → 코드로 `SpriteRenderer.color` 를 조명 밝기에 맞춰 곱하는 방식 선택.

## 구현

### 신규 `Assets/My/Scripts/Environment/SpriteLightResponse.cs`
- `[RequireComponent(SpriteRenderer)]`. `Awake` 에 `sr.color` 를 `baseColor` 로 캡처.
- `LateUpdate`: `updateInterval`(0.15s)마다 주변 광량 샘플 → 목표색, 매 프레임 `lerpSpeed`(6)로 수렴.
- 샘플: `RenderSettings.ambientLight` + 씬의 각 `Light` 기여 합
  - Directional: `intensity`
  - Point/Spot: `intensity × (1 - d/range)²`, range 밖·스포트 원뿔 밖은 스킵
  - 휘도(Rec.601) × `sensitivity` → `Clamp(min 0.15, max 1)` → `baseColor × 밝기`
- 씬 Light 목록은 `static` 캐시, 0.5s 마다 `FindObjectsByType<Light>` 갱신(실시간 조명이라 점멸함).
- `OnDisable` 에서 `baseColor` 복구 — GuestView 표정/스프라이트 교체 로직과 색 충돌 없음
  (GuestView 는 `flipX`/`sprite` 만 건드리고 `color` 는 안 건드림. SpriteOutline 은 별도 자식 렌더러).
- `[ContextMenu("Self Check")]` — `Brightness()` 클램프/휘도/sensitivity 단조성 assert.

### `Assets/My/InGame/Prefabs/Guest.prefab`
- `Square` 에 `SpriteLightResponse` 부착 (기본값). `ForceUpdate` 재임포트로 확인:
  `SpriteLightResponse=True, missingScripts=0`.

## 인스펙터 노브 (Square > Sprite Light Response)
| 필드 | 기본 | 용도 |
|---|---|---|
| minBrightness | 0.15 | 가장 어두운 곳 잔여 밝기 (0 = 완전 검정) |
| maxBrightness | 1 | 밝기 상한 |
| sensitivity | 1 | 방이 전체적으로 너무 어두우면 ↑ |
| updateInterval | 0.15 | 재계산 간격(초) |
| lerpSpeed | 6 | 방 넘나들 때 팍 튐 방지 |

플레이하며 어두운 복도/밝은 방 대비 보고 `sensitivity`·`minBrightness` 로 튜닝.

## 검증
- `uloop compile` → Success, 0 error / 0 warning.
- 프리팹 재임포트 후 컴포넌트 부착 + missing script 0 확인.

## 변경 파일
- 신규 `Assets/My/Scripts/Environment/SpriteLightResponse.cs`
- `Assets/My/InGame/Prefabs/Guest.prefab`
