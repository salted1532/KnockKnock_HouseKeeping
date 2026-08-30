# 0116 - 손님 입·퇴장 구간별 바라보는 방향 명시 지정

날짜: 2026-08-30
관련: `doc/0114`(옆모습 스프라이트 auto), `doc/0110`(회전 고정), `Docs/ReceptionManager.md`

## 요청 (원문)

> 진입 할때 Entry path 0 -> 1까지가 옆으로 이동(왼쪽 바라보는거)
> 1 -> 2 일때 정면
> 나갈때도 1 -> 2 갈때 오른쪽 보기
> 룸으로 갈때 1 -> 2 갈때 왼쪽 보면서 가기

## 1. 현황 (씬 실측)

```
guestSpawn Guest_Spawn (-13.6, 12.5)
entryPath: [0]Guest_Pos1(-23,4)  [1]Guest_Pos2(-28,4)  [2]Guest_Pos3(-28,0)
exitPath : [0]Guest_Pos3(-28,0)  [1]Guest_Pos2(-28,4)  [2]Guest_Pos1(-23,4)
roomPath : [0]Guest_Pos3(-28,0)  [1]Guest_Pos2(-28,4)  [2]Guest_Pos4(-32,4)
```
`WalkThrough(path)` : 이터레이션 i = 현재위치→`path[i]` 로 걷기. 즉 "`i` 방향" = `path[i-1]→path[i]` 레그.

- `doc/0114` : `GuestView.SetSide(이동방향)` 가 **화면 기준 수평 성분 auto 판정** 으로 옆모습(`sidePortrait` 1장 + `flipX`)/정면 결정. `sidePortrait` = `접객_왼쪽_옆모습.png` (flipX=false → 왼쪽, true → 오른쪽).
- 요청은 이 auto 대신 **구간별 명시 지정**.

요청 매핑:
| 구간 (레그) | 배열 인덱스 | 방향 |
|---|---|---|
| entry Pos1→Pos2 (순수 -X) | `entryFacing[1]` | **왼쪽 옆모습** |
| entry Pos2→Pos3 (순수 -Z, 카운터 쪽) | `entryFacing[2]` | **정면** |
| exit Pos2→Pos1 (순수 +X) | `exitFacing[2]` | **오른쪽 옆모습** |
| room Pos2→Pos4 (순수 -X) | `roomFacing[2]` | **왼쪽 옆모습** |

## 2. 설계 (코드만, 3파일)

### `GuestView`
```csharp
public enum Facing { Auto, Front, Back, Left, Right }

public void SetWalkFacing(Facing f, Vector3 worldMoveDir)
{
    switch (f)
    {
        case Facing.Auto:  SetSide(worldMoveDir); break;                     // 기존 화면-수평 판정
        case Facing.Front: SwapWalk(npc.Portrait(Expression.Neutral), false); break;
        case Facing.Back:  SwapWalk(npc.backPortrait, false); break;
        case Facing.Left:  SwapWalk(npc.sidePortrait, false); break;
        case Facing.Right: SwapWalk(npc.sidePortrait, true);  break;
    }
}
// s 있을 때만 스왑 + flipX. 없으면 유지.
```
`WalkThrough` 끝의 `EndSide()` 는 그대로 → 도착하면 `restSprite`(entry=정면, exit/room=뒷모습, ShowBack 이 세팅) 로 복귀.

### `GuestMover`
`WalkThrough(waypoints, IReadOnlyList<GuestView.Facing> facings = null)` — 레그 i 에서 `view.SetWalkFacing(facings?[i] ?? Auto, dir)`. (기존 무인자 호출 호환)

### `ReceptionManager`
```csharp
[SerializeField] private GuestView.Facing[] entryFacing;   // 각 waypoint 로 걷는 동안의 방향
[SerializeField] private GuestView.Facing[] exitFacing;
[SerializeField] private GuestView.Facing[] roomFacing;
```
`WalkThrough(entryPath)` → `WalkThrough(entryPath, entryFacing)` (exit 3곳, room 1곳도).

기본값 세팅(내가 인스펙터에 채움):
- `entryFacing = [Auto, Left, Front]`
- `exitFacing  = [Auto, Auto, Right]`
- `roomFacing  = [Auto, Auto, Left]`

## 3. 구현 완료 (2026-08-30, 확인: 인덱스 0→1 레그 / 나머지 Auto / 기본값 채움)

| 파일 | 내용 |
|---|---|
| `Dialogue/GuestView.cs` | `enum Facing { Auto, Front, Back, Left, Right }` + `SetWalkFacing(Facing, worldMoveDir)` (Auto→`SetSide`, 나머지는 명시 스프라이트). `SwapWalk(s, flip)` 헬퍼 |
| `Dialogue/GuestMover.cs` | `WalkThrough(waypoints, IReadOnlyList<GuestView.Facing> facings = null)` — 레그 i 에서 `view.SetWalkFacing(facings?[i] ?? Auto, dir)`. 무인자 호출 호환 |
| `Game/ReceptionManager.cs` | `entryFacing`/`exitFacing`/`roomFacing` (`GuestView.Facing[]`) 필드 + 5개 `WalkThrough` 호출에 전달 |
| `InGame.unity` `ReceptionManager` | `entryFacing=[Auto,Left,Front]`, `exitFacing=[Auto,Auto,Right]`, `roomFacing=[Auto,Auto,Left]` |
| `Docs/DialogueSystem.md` | 갱신 |

### 검증
- `uloop compile` : Success, Error 0, Warning 0.
- Play 모드 `SetWalkFacing` per 값: `Front`→접객_0(flipFalse), `Back`→접객_뒷모습_0, `Left`→접객_왼쪽_옆모습_0(flipFalse), `Right`→접객_왼쪽_옆모습_0(flipTrue), `EndSide`→restSprite 복귀 ✓
- 인스펙터 배열 값 저장 확인 ✓
- 실제 세션 걷기 타이밍 전환은 인게임 확인 요망.

## 4. 스킵 (YAGNI)

- 좌·우 옆모습 별도 2장 (flipX 로 충분, `doc/0114` 결정 유지).
- 레그 중간 방향 보간.

## 5. 좌/우 반전 수정 (2026-08-30, 추가 요청)

> 현재 왼쪽 오른쪽이 서로 반대로 되어있어 변경해줘

`GuestView` 에서 `sidePortrait` 원본이 화면 **오른쪽**을 향하는 그림이었음 (파일명과 무관):
- `Facing.Left` : `flipX` false → **true**
- `Facing.Right` : `flipX` true → **false**
- `SetSide`(Auto) : `flipX = screenX > 0f` → `screenX < 0f`

검증(Play): `Facing.Left → flipX=True`, `Facing.Right → flipX=False` ✓

## 상태

2026-08-30 구현·검증 완료 (좌/우 반전 수정 포함). 인게임 걷기 확인 대기.
