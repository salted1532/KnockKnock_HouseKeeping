# 0125 - 모니터 방배정 보드: 배정된 방 번호에 취소선

## 요청
방을 손님에게 지정하면 버튼 비활성화는 잘 됨. 추가로 "101" 처럼 방 번호에 선이 그어지도록.

## 변경
`Game/MonitorRoomBoard.cs` `Refresh()` — 매 프레임 라벨 텍스트를 방 상태에 맞게 갱신:
```csharp
if (r.label != null)
    r.label.text = occ ? $"<s>{r.roomNumber}</s>" : r.roomNumber.ToString();
```
- `occ` (= `GuestManager.RoomTaken`) 이면 TMP `<s>` 취소선, 아니면 그냥 번호.
- 버튼 라벨 TMP 는 전부 `richText = true` 확인됨.
- `Awake` 의 초기 `label.text = n.ToString()` 는 그대로 (첫 프레임 값).

## 상태
2026-08-31 완료. 컴파일 0에러.
