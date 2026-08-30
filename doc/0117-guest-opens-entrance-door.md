# 0117 - 손님이 입·퇴장 시 출입문 여닫기 (제안)

날짜: 2026-08-30
관련: `doc/0116`(구간별 방향), `doc/0104~0105`(손님 큐), `Docs/InteractionSystem.md`(문 = Interactable + HingeEffect)

## 요청 (원문)

> 손님 이동에서 진입시 1 -> 0 이나, 나갈때 0 -> 1 일때 문이 닫혀있으면 문을 열고 나가도록.
> ReceptionManager 에다가 맵에 있는 문 오브젝트랑 연결해서 손님이 직접 문을 열고 닫고 하는 것처럼 보이도록.

## 해석

`entryPath`/`exitPath` 의 특정 레그(출입문이 놓인 구간)를 손님이 지날 때 문을 열고, 지나고 나면 닫는다.
`doc/0116` 레그 규칙: `WalkThrough(path)` 이터레이션 `i` = 현재위치→`path[i]`.

씬 실측(`doc/0116`):
```
guestSpawn(-13.6,12.5)
entryPath: [0]Pos1(-23,4)  [1]Pos2(-28,4)  [2]Pos3(-28,0=카운터)
exitPath : [0]Pos3          [1]Pos2         [2]Pos1
```
바깥 = 스폰/Pos1 쪽, 안 = 카운터. 출입문은 **스폰↔Pos1 사이** 로 추정 →
- 입장: 레그 **0** (스폰→Pos1) 이 문 통과 구간
- 퇴장: 레그 **2** (Pos2→Pos1) 이 문 통과 구간

## 설계 (코드 3파일 + 씬 배선)

### `Interactable` — 연출용 상태 설정
```csharp
// 코드/연출용: 토글 상태를 강제 설정하고 효과 재생 (CanInteract·isToggle 무시).
// NPC 가 문 여는 연출 등. 이미 그 상태면 아무것도 안 함.
public void SetState(bool on)
{
    if (IsOn == on) return;
    IsOn = on;
    var ctx = new InteractionContext(this, null, on, transform.position);
    foreach (var e in effects) if (e != null && e.enabled) e.Play(in ctx);
}
```
문 = `Interactable`(isToggle) + `HingeEffect`(`ctx.IsOn` 으로 여닫음) + `SfxEffect`(문소리 자동). `SetState` 하나로 애니메이션+소리까지.

### `GuestMover` — 레그 시작 콜백
```csharp
public IEnumerator WalkThrough(IReadOnlyList<Transform> waypoints,
                               IReadOnlyList<GuestView.Facing> facings = null,
                               Action<int> onLegStart = null)
```
레그 `i` 시작 직전 `onLegStart?.Invoke(i)`. (기존 2인자 호출 호환)

### `ReceptionManager`
```csharp
[Header("입·퇴장 문 (선택)")]
[SerializeField] private Interactable guestDoor;      // 맵의 출입문
[SerializeField] private int entryDoorLeg = 0;        // 이 레그 동안 열림. -1 = 안 씀
[SerializeField] private int exitDoorLeg  = 2;

private void DoorForLeg(int leg, int doorLeg)
{
    if (guestDoor != null && doorLeg >= 0) guestDoor.SetState(leg == doorLeg);
}
private void CloseDoor() { if (guestDoor != null) guestDoor.SetState(false); }
```
- 입장: `WalkThrough(entryPath, entryFacing, i => DoorForLeg(i, entryDoorLeg))` → `CloseDoor()`
- 퇴장(3곳): `WalkExit(mover)` 헬퍼로 묶음 — `WalkThrough(exitPath, exitFacing, i => DoorForLeg(i, exitDoorLeg))` → `CloseDoor()`
- 방으로(roomPath): 문 없음, 그대로

동작: 문-레그 시작 → 열림 / 다른 레그 시작 or 걷기 종료 → 닫힘. 손님이 문 앞에서 열고, 지나가면 닫음.

## 영향 파일

```
Interaction/Interactable.cs   SetState(bool) 추가
Dialogue/GuestMover.cs        WalkThrough 에 onLegStart 콜백
Game/ReceptionManager.cs      guestDoor / entryDoorLeg / exitDoorLeg + DoorForLeg/CloseDoor + WalkExit 헬퍼
InGame.unity ReceptionManager  guestDoor = 맵 출입문, leg 인덱스 (기본 0 / 2)
Docs/ReceptionManager.md · Interactable.md  갱신
```

## 확인 답변 (2026-08-30)

- 그대로 진행. 입장 레그 0 / 퇴장 레그 2.
- 문 1개 (입·퇴장 같은 문). `guestDoor` 필드 하나.
- **플레이어가 열어둔 문은 그대로 둔다** → `DoorForLeg` 가 `leg == doorLeg || restOpen`, 걷기 후 `SetState(restOpen)` 로 복원. 손님은 닫힌 문만 열고 닫음.

## 구현 완료 (2026-08-30)

| 파일 | 내용 |
|---|---|
| `Interaction/Interactable.cs` | `SetState(bool on)` — 토글/CanInteract 무시하고 `IsOn` 설정 + 효과 재생. 이미 그 상태면 no-op |
| `Dialogue/GuestMover.cs` | `WalkThrough(waypoints, facings, Action<int> onLegStart = null)` — 레그 i 시작 직전 `onLegStart(i)` |
| `Game/ReceptionManager.cs` | `guestDoor(Interactable)` / `entryDoorLeg`(0) / `exitDoorLeg`(2) 필드. `WalkExit(mover)` 헬퍼(퇴장 3곳 통합) + `DoorForLeg(leg, doorLeg, restOpen)`. 입장/퇴장 walk 에 `onLegStart` 콜백 + 걷기 후 문 원상복원. roomPath 는 문 없음 |
| `InGame.unity` ReceptionManager | `entryDoorLeg=0` / `exitDoorLeg=2` (기본값). `guestDoor` **미배선 — 사용자 작업** |
| `Docs/` | ReceptionManager.md · Interactable.md 갱신 |

### 검증
- `Interactable.SetState`: `IsOn` false→true→(no-op)→false, 예외 없음. `HingeEffect.Swing` 코루틴 시작 확인 (에디터 비포커스라 전체 스윙은 인게임 확인 요망 — `Play()` 경로와 동일 호출).
- `WalkThrough` 3인자 시그니처 컴파일 OK, 기존 2인자 호출 호환.
- `ReceptionManager` 새 필드 직렬화 + 기본값 확인.
- `uloop compile` Error 0.

### 사용자 작업
1. `ReceptionManager.guestDoor` 에 맵의 출입문(Interactable) 연결.
2. 인게임: 손님 입장 시 레그 0(스폰→Pos1)에서 문 열림 → Pos1 도착 시 닫힘 / 퇴장 시 레그 2(Pos2→Pos1)에서 열림 → 나가면 닫힘. 문 위치가 다른 레그면 `entryDoorLeg`/`exitDoorLeg` 조정.

## 스킵

- 손님 위치 기반 정밀 트리거(문에 콜라이더 트리거) — 레그 인덱스로 충분.
- 방문(roomPath 끝의 객실 문) — 별개.

## 7b. 트리거 타이밍 조정 (2026-08-30)

> 진입: waypoint 1 도착했을 때 열기 / 퇴장: waypoint 0(카운터) 떠날 때 열기 — 문이 Pos2↔Pos3(카운터) 구간에 있음.

`entryDoorLeg` `0 → 2` (Pos2 도착 후 카운터로 가는 레그 시작 시), `exitDoorLeg` `2 → 1` (카운터 떠나는 레그 시작 시). `onLegStart(i)` 는 레그 i 시작 = 이전 waypoint 도착 순간이라 타이밍 정확. 코드 기본값만 변경 (씬엔 아직 미직렬화).

## 7c. 승인 경로 누락 수정 + 트리거 정리 (2026-08-30)

> 아직 싱크 안 맞음: 진입은 waypoint 1 도착 시 / 나갈 때(**승인·거절 둘 다**) waypoint 0 출발 시.

**원인**: 이전엔 거절/방문객(`WalkExit` = exitPath)만 문 처리. **승인 시 방으로 가는 `roomPath` 는 문 처리가 없었음** — roomPath 도 카운터→Pos2 로 같은 문을 지남.

| 변경 | |
|---|---|
| `WalkExit` → 범용 **`WalkWithDoor(mover, path, facing, doorLeg)`** | rest 상태 캡처 → 레그별 `SetState(i == doorLeg \|\| rest)` → 걷기 후 `SetState(rest)` |
| 적용 | 입장(`entryPath`, `entryDoorLeg`=2) / 퇴장 3곳(`exitPath`, `exitDoorLeg`=1) / **입실(`roomPath`, `roomDoorLeg`=1, 신규)** 전부 |
| 트리거 | `onLegStart(i)` = 레그 i 시작 = 직전 waypoint 도착 순간. 입장 2 = "waypoint 1 도착 즉시", 퇴장/입실 1 = "waypoint 0(카운터) 출발 즉시" (leg 0 은 degenerate) |

컴파일 Error 0. 코드 로직 검증(SetState·onLegStart 개별). 전체 타이밍은 `guestDoor` 배선 후 인게임 확인.

## 7d. 트리거를 "레그 시작" → "웨이포인트 도착" 으로 (2026-08-30)

> 증상: 다음 손님이 스폰하자마자 문이 열림. 나갈 때(승인·거절)는 waypoint 0 출발 즉시, 진입은 waypoint 1 도착 시 열려야 함.

**원인**: `onLegStart(i)` = 레그 i **시작**(직전 waypoint 도착 순간). 입장 첫 레그(스폰→Pos1) 시작 = 스폰 순간. `entryDoorLeg` 가 그 근처 값이면 스폰하자마자 열림.

| 변경 | |
|---|---|
| `GuestMover.WalkThrough` | `onLegStart(i)` → **`onArrive(i)`** — `waypoints[i]` 에 **도착한 직후** 호출 (while 루프 break 뒤) |
| `ReceptionManager` 필드 | `entryDoorLeg`/`exitDoorLeg`/`roomDoorLeg` → **`entryDoorElement`(1)** / **`exitDoorElement`(0)** / **`roomDoorElement`(0)** (도착할 웨이포인트 인덱스) |
| `WalkWithDoor` | `onArrive` 에서 `i == doorElement && !guestDoor.IsOn` → `SetState(true)`. 걷기 후 `SetState(doorWasOpen)` 로 복원 |

동작:
- 입장: `entryPath[1]`(Pos2) **도착** 시 문 체크·열기. 스폰(스폰→Pos1→Pos2 2레그 뒤)이라 **스폰 시엔 절대 안 열림**.
- 퇴장/입실: `exitPath[0]`/`roomPath[0]`(카운터=시작점) — `onArrive(0)` 이 degenerate 레그 0 직후(같은 프레임) 발동 → **카운터 출발 즉시** 열림.
- 문이 이미 열려 있으면 안 건드림. 걷기 끝나면 원래 상태로.

> ⚠️ 필드명이 바뀌었으니 인스펙터에서 구 `entryDoorLeg` 등에 넣었던 값은 사라지고 새 기본값(1/0/0) 적용됨. `guestDoor` 참조는 유지.

컴파일 Error 0. `Interactable.SetState`·`onArrive` 배치 코드 검증. 전체 타이밍은 인게임(`guestDoor` 배선 후) 확인.

## 7e. 자동 닫기 제거 (2026-08-30)

> 문 자동으로 닫히는 거 빼줘 — 열고 그냥 가도록.

`WalkWithDoor` 에서 `doorWasOpen` 캡처 + 걷기 후 `SetState(doorWasOpen)` 복원 제거. 이제 도착 시 닫혀 있으면 열기만 하고 끝. 닫는 건 플레이어 몫.

## 상태

2026-08-30 — 웨이포인트 도착 트리거(`*DoorElement`), 자동 닫기 없음(열고 감). 인게임 확인 대기.
