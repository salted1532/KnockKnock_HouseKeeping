# 0163 – CRTMonitor 프리팹 ↔ 씬 UI 동기화

## 요청
`CRTMonitor.prefab` 을 열면 씬과 UI 화면이 다르게 나옴. 동기화.

## 원인 (프리팹 3겹 중첩)
| 레이어 | 버튼 배치 | 나가기 |
|---|---|---|
| `CRTMonitor.prefab` (베이스) | 옛날 (106~110 우→좌, 101 위) | `EnterUIModeEffect` |
| `Owner's_Motel_Room.prefab` (중첩, 버튼 위치 오버라이드) | 새 배치 | `EnterUIModeEffect` |
| 씬 `InGame` (Owner에서 상속, 컴포넌트 오버라이드) | 새 배치 | `MonitorViewEffect` (씬만) |

실제 차이는 **버튼 9개 `anchoredPosition`** + **나가기 이펙트** 뿐. 캔버스/Road/배경은 동일.

## 수정 — 베이스 하나로 수렴
1. **`CRTMonitor.prefab`**: 버튼 9개 위치를 씬 값으로 복사(스크립트로 씬→프리팹 읽어 씀), `EnterUIModeEffect` 제거 → `MonitorViewEffect`(anchor=`Anchor`) 추가
2. **씬 `InGame`**: CRTMonitor 의 added `MonitorViewEffect` / removed `EnterUIModeEffect` 오버라이드 revert (베이스가 이제 제공) → 씬 인스턴스 오버라이드 0
3. **`Owner's_Motel_Room.prefab`**: 중첩 CRTMonitor 버튼 rect 의 위치 오버라이드 revert (베이스와 동일) + 이펙트는 베이스에서 자동 상속

## 검증
```
[d] CRTMonitor.prefab vs 씬 인스턴스 (ScreenUI): (동일)
[c1] scene instance button-pos property mods: 0
[c1] scene CRTMonitor comps: ... MonitorViewEffect  (x1, EnterUIModeEffect x0)
[s2] Owner's prefab CRTMonitor comps: ... MonitorViewEffect  (버튼 오버라이드 10개 revert)
```
이제 `CRTMonitor.prefab` / `Owner's_Motel_Room.prefab` / 씬 어디를 열어도 동일 (새 배치 + 클릭/ESC 나가기). doc/0162 의 Road_Image·AspectRatioFitter 수정과 별개, 함께 유효.
