# ReceptionManager

`Assets/My/Scripts/Game/ReceptionManager.cs`

저녁 "접객" 파트 관리자. 싱글턴. 손님 심사 로직(SYS-03~06)은 아직 없고, **세션 진입/종료 골격만** 있다.

## 필드

| 필드 | 설명 |
|---|---|
| `receptionAnchor` (`Transform`) | 접객 시 플레이어가 앉을 위치/정면 — 접객 테이블의 `Player_Anchor` |
| `debugEndKey` (bool, 기본 on) | 디버그: 세션 중 `K` 로 정상 종료(→새벽) |

## 프로퍼티 / 이벤트

| 이름 | 설명 |
|---|---|
| `Instance` (static) | 싱글턴 |
| `InSession` (bool) | 접객 세션 진행 중 |
| `OnSessionStarted` / `OnSessionEnded` (`event Action`) | 손님 큐/신분증 심사/정산을 이 위에 얹음 |

## 동작

- `DayPhaseManager.OnPhaseChanged` 구독 → `Evening` 되면 `BeginSession()`:
  `UIInteractionMode.Enter(receptionAnchor)` + `InSession=true` + `OnSessionStarted`.
  (암전 중 진입 → 앵커 이동이 페이드에 가려짐, 페이드 인 시 이미 착석)
- **`EndSession()`** (public) — 그날 접객 완료 시 호출할 정식 API. `UIInteractionMode.ExitAll()` + `OnSessionEnded` + `DayPhaseManager.Advance()`(→새벽 페이드).
- `UIInteractionMode.Exited` 구독 → ESC 로 접객 레벨까지 빠져나오면 `HandleUIExit`: 세션만 정리, **하루 전환은 안 함** (테스트용).

## 알려진 한계

- **정상 종료 트리거 = 디버그 `K`** (`ponytail:` 주석). 손님 시스템(SYS-03~06/09) 나오면 "전원 처리 완료" 가 `EndSession()` 을 호출하도록 교체.
- `receptionAnchor` 미할당 시 경고 로그 + 이동 없이 세션만 시작.

## 관련

[DayPhaseManager](DayPhaseManager.md) · [UIInteractionMode](UIInteractionMode.md) · [EnterUIModeEffect](EnterUIModeEffect.md) · [`doc/0097`](../doc/0097-reception-ui-mode-anchor.md) · [`doc/0100`](../doc/0100-note-and-monitor-interactions-design.md)
