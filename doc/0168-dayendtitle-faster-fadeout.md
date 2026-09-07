# 0168 – "N일차" 타이틀 사라지는 속도 빠르게

## 요청
각 일차 표시 텍스트(`DayEndTitle`, doc/0150)가 사라지는 속도를 좀 더 빠르게.

## 현황
`DayEndTitle` (씬 오브젝트, 직렬화 값 = 코드 기본값):
- `fadeIn 0.5` → `hold 1.8` → `fadeOut 0.9` (초)
- "사라지는 속도" = `fadeOut`

## 작업 (씬 값만, 코드 변경 없음)
사용자 요청: 둘 다 살짝씩. `Assets/Scenes/InGame.unity` `DayEndTitle`:
- `hold: 1.8` → **`1.3`**
- `fadeOut: 0.9` → **`0.5`**
- `fadeIn 0.5` 유지

에디터 SerializedObject + SaveScene 로 적용 (디스크/에디터 동기화).

## 수정 파일
- `Assets/Scenes/InGame.unity`
