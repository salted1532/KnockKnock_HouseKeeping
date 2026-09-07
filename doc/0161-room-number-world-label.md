# 0161 – 방 번호 월드 라벨 (Room_Number 큐브)

## 요청
`Motel_Room` 안 `Room_Number` 큐브의 Canvas에 방 번호를 방마다 연동해 표시. 텍스트 비율 유지, Canvas 크기 = 큐브 크기.

## 현황
- 방 10개 전부 `Assets/My/InGame/Prefabs/MotelRoom/Motel_Room.prefab` 인스턴스. 번호는 `RoomController.roomNumber`(101~110) 인스턴스 오버라이드.
- 프리팹에 `Room_Number/Canvas/Text (TMP)` 이미 존재하나 텍스트 "102" 하드코딩, 폰트크기 1·TopLeft·sizeDelta(1,1) 미설정.
- `Room_Number` 큐브 스케일 비균등 `(1, 0.36106, 0.13955)` → 자식 Canvas/텍스트가 세로로 찌그러짐.

## 적용

### 1. 새 컴포넌트 `Assets/My/Scripts/Interaction/RoomNumberLabel.cs`
`[ExecuteAlways]` + `[RequireComponent(typeof(TMP_Text))]`. `OnEnable`/`OnValidate`(delayCall) 에서 `GetComponentInParent<RoomController>().RoomNumber` 를 TMP 텍스트에 씀. 런타임·에디터 모두 자동 반영.

### 2. 프리팹 편집 (`Motel_Room.prefab`, 10 인스턴스 자동 반영)
- `Room_Number/Canvas`:
  - 부모 큐브 역스케일 `localScale = (k/1, k/0.36106, k/0.13955)`, `k=0.001` → `(0.001, 0.00277, 0.00717)`
  - `sizeDelta = (rnScale.x/k, rnScale.y/k) = (1000, 361)` → 월드 렉트 = 큐브 앞면 **정확히 1.000 × 0.361 m**
  - lossyScale 이 균등 `(0.001, 0.001)` 이 되어 텍스트 비율 정상
  - `GraphicRaycaster` 제거
- `Room_Number/Canvas/Text (TMP)`:
  - 앵커 stretch(0,0~1,1), offset 0, pivot 0.5, 가운데정렬, `NoWrap`
  - `Auto Size` on (min 1, max 500) → 숫자가 판에 맞게 자동 크기
  - `Raycast Target` off
  - **색 검정** (판 머티리얼이 흰색 URP/Lit → 흰 글자는 안 보임)
  - `RoomNumberLabel` 부착

## 검증 (EditMode, 10 인스턴스 전수)
```
#101 text="101" canvasWorld=1.000 x 0.361 m  lossyScale=(0.00100,0.00100)  hasLabel=True
... (102~110 동일 패턴, 각자 번호) ...
#110 text="110" canvasWorld=1.000 x 0.361 m  lossyScale=(0.00100,0.00100)  hasLabel=True
```
렌더 상태(#103): `text='103' renderedFont=309.5 autoSize=True color=black textBounds=(567,361) rect=1.000x0.361m`.
`uloop compile` 클린(기존 무관 경고 1건만).

## 남은 선택지 (사용자)
폰트 색/윤곽선/스타일은 검정 기본. 다르게 원하면 프리팹 `Text (TMP)` 수정.
