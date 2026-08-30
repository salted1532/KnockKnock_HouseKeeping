# 0110 - Guest 이동 중 회전 고정 (항상 플레이어 방향, y = -180)

날짜: 2026-08-30
관련: `doc/0104`~`0105`(Guest), `doc/0108`(Guest 스프라이트 아웃라인)

## 요청 (원문)

> Guest 오브젝트가 스폰해서 이동할때 대화종료후 나갈때도 회전값 y -180을 유지했으면 좋겠어
> 계속 플레이어 방향을 쳐다보도록

## 1. 현황

`GuestMover.WalkThrough()` 가 웨이포인트로 직선 이동하면서 **이동 방향으로 회전**(`Quaternion.LookRotation(to)` → `Slerp`).
`GuestView.body` 는 2D 스프라이트(자식 `Square`, 로컬 회전 0). 루트가 옆/뒤로 돌면 스프라이트가 옆모습·뒷모습으로 보임.
`WarpTo(guestSpawn)` 은 스폰 트랜스폼의 회전을 그대로 복사.

→ entryPath(들어옴) · exitPath/roomPath(나감) 전부 `WalkThrough` 통과하므로 한 곳만 고치면 됨.

## 2. 설계 — `GuestMover.cs` 만 수정

| 위치 | 변경 |
|---|---|
| 필드 | `[Header("바라보는 방향")] [SerializeField] float faceYaw = -180f;` 추가. `turnSpeed` 제거(미사용화) |
| `WarpTo(t)` | 위치는 `t` 에서, 회전은 항상 `Quaternion.Euler(0, faceYaw, 0)` |
| `WalkThrough` | 이동 방향 회전(`LookRotation`/`Slerp`) 삭제. 루프에서 `transform.rotation = Quaternion.Euler(0, faceYaw, 0)` 유지 (다른 데서 건드려도 원복) |

- 씬·프리팹 수정 없음. `guestSpawn` 회전값은 이제 무관.
- 스프라이트는 위아래 안 눕도록 X/Z 는 0 고정, y 만 `faceYaw`.
- 플레이어는 접객 중 `receptionAnchor` 에 고정 → 고정 yaw = "플레이어 쳐다봄". (플레이어가 움직이는 상황은 없음)

## 3. 확인

1. `faceYaw` 기본 -180, 인스펙터 노출(미세조정용). OK?

## 4. 스킵 (YAGNI)

- 실시간 `LookAt(player)` 빌보드 — 접객 중 플레이어 위치 고정이라 불필요.
- 걷는 방향 표시(발 애니메이션 방향 등).

## 5. 구현 완료 (2026-08-30, 확인: 이대로 진행)

`Assets/My/Scripts/Dialogue/GuestMover.cs`:
- `faceYaw = -180f` 필드 추가 (`[Header("바라보는 방향")]`), `turnSpeed` 필드 제거
- `WarpTo(t)` : `SetPositionAndRotation(t.position, Quaternion.Euler(0, faceYaw, 0))` — 스폰 트랜스폼 회전 무시
- `WalkThrough` : `LookRotation`/`Slerp` 삭제, 루프 매 프레임 `transform.rotation = Quaternion.Euler(0, faceYaw, 0)` 유지

`Guest.prefab` : `turnSpeed: 12` 줄 삭제, `faceYaw: -180` 추가 (YAML 직접 수정).

### 검증
- `uloop compile` : Success, Error 0, Warning 0.
- Play 모드 `WarpTo(spawn)` (스폰을 y=37 로 줘도) → 손님 `eulerAngles = (0, 180, 0)` ✓
- `Guest.prefab` GuestMover 직렬화 필드 확인: `faceYaw=-180`, `speed=1.3`, `arriveDistance=0.06`, `turnSpeed` 제거됨 ✓
- `WalkThrough` 코루틴은 에디터 비포커스 상태라 프레임이 안 돌아 실보행 테스트는 생략. 루프가 매 프레임 동일 `facing` 쿼터니언을 대입하므로 자명. 인게임 확인 요망.

## 상태

2026-08-30 구현 완료. 인게임(접객 세션 손님 입장/퇴장) 확인 대기.
