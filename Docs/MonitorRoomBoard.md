# MonitorRoomBoard

`Assets/My/Scripts/Game/MonitorRoomBoard.cs`

CRT 모니터 화면(uGUI, [`doc/0119`](../doc/0119-crt-monitor-world-canvas-ui.md))의 **방배정 보드** = 요청의 "UI Controller".
접객 손님에게 방(101~110)을 배정한다. `doc/0118`.

## 필드

| 필드 | 설명 |
|---|---|
| `rooms` (`RoomButton[]`) | 방 버튼 10개. 각 항목 `{ roomNumber, button(Button), tint(Image), label(TMP_Text) }`. `tint` 비우면 `button.image` |
| `header` (`TMP_Text`) | 현재 배정 대상 손님 이름 표시 (선택) |
| `vacantColor` / `selectedColor` / `occupiedColor` | 버튼 상태 색 |

## 동작

- `Awake`: 각 `button.onClick` → `ReceptionManager.Instance.AssignRoom(roomNumber)`. `label` 에 방번호.
- `Update` → `Refresh`:
  - `header` = `ReceptionManager.CurrentGuest` 이름 (없으면 "대기 중인 손님 없음")
  - 각 버튼:
    - `tint.color` = 사용중(`GuestManager.RoomTaken`) → `occupiedColor` / 선택됨(`PendingRoom == n`) → `selectedColor` / 그 외 `vacantColor`
    - `button.interactable` = 대기 손님 있고 && 그 방이 비어 있을 때만

## 배치

`CRTMonitor/screenON/ScreenUI`(World Space Canvas + `RenderTextureGraphicRaycaster`, `doc/0119`) 아래에 방 버튼 10개와 이 컴포넌트. 클릭은 화면고정/접객 모드에서만 먹힘 (`doc/0119` 게이트 = `UIInteractionMode.Active`) — 접객 자리에서도, 모니터 화면고정에서도 조작 가능.

풀스크린 `RawImage.raycastTarget = false` 필요 (`doc/0119`).

## 관련
[ReceptionManager](ReceptionManager.md) · [`doc/0118`](../doc/0118-monitor-room-assignment-and-dawn-knock.md) · [`doc/0119`](../doc/0119-crt-monitor-world-canvas-ui.md)
