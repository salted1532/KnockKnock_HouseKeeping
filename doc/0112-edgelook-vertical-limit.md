# 0112 - 화면고정 EdgeLook 상하 범위 축소 (제안)

날짜: 2026-08-30
관련: `Assets/My/Scripts/Interaction/Modes/UIInteractionMode.cs`, [[project_interaction-system-redesign]]

## 요청

> 화면고정에서 좌우는 멀리까지 둘러볼 수 있게 두고, **상하는 조금만** 추가로 보이게.
> 접객 대화 중 버튼 누르려고 커서를 내리면 **너무 아래를 쳐다봐서** 화면이 확 기울어짐 — 안 좋음.

## 현재 (`UIInteractionMode.EdgeLook`)

커서를 화면 가장자리로 가져가면 앵커 정면 기준 yaw/pitch 를 목표 각도로 Lerp:
- `yawRange = 40°` (좌우) — 유지 (요청: 좌우는 그대로)
- `pitchRange = 25°` (상하 **공통**) — 커서 상단 → 위 25°, 커서 하단 → 아래 25°
- `edgeDeadZone = 0.25` — 화면 중앙 25% 는 안 움직임

질문 패널 버튼은 화면 하단(스크린 오버레이). 클릭하러 커서를 내리면 `ny ≈ -0.8` → 아래로 최대 25° 틸트 → 지평선이 확 내려감.

## 제안 (파일 1개, `UIInteractionMode.cs`)

`pitchRange` (상하 공통) → **위/아래 분리**:

```csharp
[SerializeField] private float pitchUpRange = 12f;     // 커서 상단 → 위로 볼 수 있는 최대 각
[SerializeField] private float pitchDownRange = 4f;    // 커서 하단 → 아래 (버튼 영역이라 작게)
```

`EdgeLook()`:
```csharp
float targetYaw = EdgeFactor(nx) * yawRange;
float pr = ny >= 0f ? pitchUpRange : pitchDownRange;
float targetPitch = -EdgeFactor(ny) * pr;   // EdgeFactor 가 부호 유지 (상단 → 위, 하단 → 아래)
```

- `yawRange`(40)·`edgeDeadZone`·`lookLerp` 그대로.
- 구 `pitchRange` 필드 제거 → 씬(`InGame.unity`)의 `pitchRange: 25` 는 무시됨. 씬에 `pitchUpRange: 12` / `pitchDownRange: 4` 를 써준다.
- `Docs/`: `UIInteractionMode` 문서 없음 → `InteractionSystem.md` 의 EdgeLook 언급만 갱신(있으면).

## 확인 답변 (2026-08-30)

위 12° / 아래 4°. 좌우 40° 유지.

## 구현 완료 (2026-08-30)

| 파일 | 내용 |
|---|---|
| `Interaction/Modes/UIInteractionMode.cs` | `pitchRange` (상하 공통 25°) 제거 → `pitchUpRange`(12°) + `pitchDownRange`(4°). `EdgeLook` 에서 `ny >= 0 ? pitchUpRange : pitchDownRange` 선택. `yawRange`(40)·`edgeDeadZone`·`lookLerp` 그대로 |
| `Assets/Scenes/InGame.unity` | UIInteractionMode: `pitchRange: 25` → `pitchUpRange: 12` / `pitchDownRange: 4` |

## 스킵

- 상하 전용 데드존 분리 — range 축소로 충분.
- 버튼 hover 시 EdgeLook 잠금 — 오버엔지니어링. range 로 해결.

## 상태

2026-08-30 구현 완료. 컴파일 확인 진행 중 (Unity 빌드 대기). 인게임 검증 대기.
