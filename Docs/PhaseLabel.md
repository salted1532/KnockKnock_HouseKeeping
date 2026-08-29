# PhaseLabel

`Assets/My/Scripts/Game/PhaseLabel.cs`

현재 시간대(선택적으로 일차)를 TMP 텍스트에 표시. `DayPhaseManager.OnPhaseChanged` 구독.
Canvas 의 "Watch"(손목시계) 텍스트 등에 붙인다.

## 필드

| 필드 | 설명 |
|---|---|
| `label` (`TMP_Text`) | 표시 대상. 비우면 같은 오브젝트의 `TMP_Text` (`Reset`/`Start` 에서 자동) |
| `showDayCount` (bool, 기본 on) | 켜면 `"Day 3 · Evening"`, 끄면 `"Evening"` |

## 동작

`Start` 에서 구독 + 즉시 1회 갱신. `DayPhaseManager` 없으면 `"-"` 표시.
문구는 enum 이름 그대로 (Morning / Noon / Evening / Dawn).

## 관련

[DayPhaseManager](DayPhaseManager.md)
