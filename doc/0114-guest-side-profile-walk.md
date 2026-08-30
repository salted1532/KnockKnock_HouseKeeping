# 0114 - 손님 입·퇴장 시 옆모습 스프라이트 (제안)

날짜: 2026-08-30
관련: `doc/0104`~`0105`(Guest), `doc/0110`(고정 정면), `doc/0108`(스프라이트 표현·크기)

## 요청

> 접객 손님이 진입할 때·빠져나갈 때 **옆으로 이동하면 옆모습 스프라이트**가 나오도록. 왼쪽/오른쪽 **2방향**.

## 현재

- `GuestMover.WalkThrough` 가 웨이포인트로 직선 이동. 회전은 항상 `faceYaw`(정면 고정).
- `GuestView` 스프라이트: `neutralPortrait`/`angryPortrait`(정면), `backPortrait`(퇴장 뒷모습).
- 입장/퇴장 내내 정면(또는 뒷모습)인 채로 옆으로 미끄러짐.

## 제안 (3파일, 코드만)

### `NpcData`
```csharp
[Tooltip("옆모습 (걸어서 입·퇴장 시). 화면 오른쪽을 향한 그림 기준 — 왼쪽 이동 시 자동 좌우반전. 비면 정면 유지")]
public Sprite sidePortrait;
```
→ 스프라이트 1장 + `SpriteRenderer.flipX` 로 좌/우 2방향. (별도 좌·우 2장 안 만듦)

### `GuestView`
- `restSprite` (걷기 전/후 기본 스프라이트) 를 `SetExpression`/`ShowBack` 이 기록.
- `SetSide(Vector3 worldMoveDir)`:
  - `sidePortrait` 없으면 아무것도 안 함(기존 유지).
  - `screenX = Vector3.Dot(worldMoveDir, Camera.main.right)` — 화면 기준 좌/우. `|screenX|` 가 작으면(깊이 방향 이동) 옆모습 안 씀.
  - `body.sprite != sidePortrait` 일 때만 `ApplySprite(sidePortrait)` (매 프레임 스케일 재계산 방지). `body.flipX = screenX < 0`.
- `EndSide()`: `ApplySprite(restSprite)` + `flipX=false`.

### `GuestMover`
- `[SerializeField] GuestView view;` (`Reset` 에서 `GetComponentInChildren`).
- `WalkThrough` 루프에서 매 프레임 `view?.SetSide(to)` (`to` = 목표 방향, y=0). 루프 끝나면 `view?.EndSide()`.
- `WarpTo` 후에도 옆모습이 남지 않게: 다음 `SetExpression`(=`view.Apply` 시 호출) 이 정면으로 되돌림. 이미 그렇게 됨.

## 흐름

| 구간 | 표시 |
|---|---|
| 입장 걷기 (수평) | **옆모습** (이동 방향으로 flip) |
| 입장 걷기 (깊이 방향) | 정면 유지 |
| 대화 | 정면 + 표정 (기존) |
| 퇴장/입실 걷기 (수평) | **옆모습** |
| 퇴장/입실 걷기 (깊이 방향) | 뒷모습(`ShowBack` 후) |

## 영향 파일

```
Dialogue/NpcData.cs      sidePortrait 필드
Dialogue/GuestView.cs    restSprite + SetSide/EndSide
Dialogue/GuestMover.cs   view 참조 + 루프에서 SetSide, 끝나면 EndSide
Docs/DialogueSystem.md   Guest 항목 한 줄
```
씬/프리팹: `GuestMover.view` 는 `Reset` 자동연결이라 Guest 프리팹에서 한 번 확인만. `sidePortrait` 는 각 `Npc_*.asset` 에 사용자가 그림 배정.

## 확인 답변 (2026-08-30)

1장 + flipX / 깊이 방향은 정면·뒷모습 유지.

## 구현 완료 (2026-08-30)

| 파일 | 내용 |
|---|---|
| `Dialogue/NpcData.cs` | `Sprite sidePortrait` 추가 (backPortrait 뒤). 화면 오른쪽 향한 그림 기준 |
| `Dialogue/GuestView.cs` | `restSprite` 필드(기본 스프라이트). `SetExpression`/`ShowBack` 이 기록 + `flipX=false`. `SetSide(Vector3 worldDir)` — `Vector3.Dot(dir, Camera.main.right)` 로 화면 수평 성분, `|screenX|<0.15` (깊이 방향)면 무시, 아니면 `sidePortrait` + `flipX = screenX<0`. `EndSide()` → restSprite 복귀 |
| `Dialogue/GuestMover.cs` | `[SerializeField] GuestView view` (+ `Reset`/`Awake` 자동연결). `WalkThrough` 루프에서 매 프레임 `view.SetSide(dir)`, 루프 끝 `view.EndSide()` |
| `Guest.prefab` | `GuestMover.view` → GuestView 배선 |
| `Docs/DialogueSystem.md` | Guest 항목 갱신 |

### 검증 (플레이 스모크)
- `sidePortrait` 미배정: `SetSide` no-op, 기존 정면/뒷모습 그대로 (안전 — 아트 없어도 배포 가능).
- 임시 스프라이트 배정: 화면 오른쪽 이동 → 옆모습 flipX=false / 왼쪽 → flipX=true / 깊이 방향 → 안 바뀜 / `EndSide` → 기본 스프라이트 복귀. 스프라이트 교체 중에도 발 지면 정렬 유지.
- 컴파일 Error 0.

## 7b. 아트 연결 (2026-08-30)

> 사용자가 `image/숙박객/접객_왼쪽_옆모습.png` (**화면 왼쪽** 바라봄) 추가.

- `NpcData.sidePortrait` 기준 방향 = **화면 왼쪽** 으로 확정. `GuestView.SetSide` flip 조건 `screenX < 0` → **`screenX > 0`** (오른쪽 이동 시 반전). 툴팁도 갱신.
- `접객_왼쪽_옆모습_0` (guid `a22611f0…`) 를 5개 NpcData(id 1~5) `sidePortrait` 에 전부 배정.
- 플레이 스모크: 왼쪽 이동 flipX=false / 오른쪽 이동 flipX=true / EndSide 복귀 정상.

## 상태

2026-08-30 구현 + 아트(`접객_왼쪽_옆모습`) 연결 완료. 인게임 최종 확인 대기.
