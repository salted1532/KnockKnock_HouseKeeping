# 0127 – 한글 줄바꿈 규칙: 어절 단위로 변경

## 요청
한글 번역이 단어 중간에서 줄바꿈됨 (`상\n호작용`, `마\n우스` 등). 자연스럽게 읽히도록 줄바꿈 처리. 영어도 같은 문제 있는지 확인.

## 원인
번역 문자열 자체엔 줄바꿈 문자 없음. TMP의 한글 줄바꿈 규칙이 "전통(traditional)" 이라
컨테이너 폭이 부족하면 음절 단위 아무 위치에서나 끊음.

`Assets/Scenes/InGame.unity` 의 `LocalizedLabel` HUD 4종이 대표 사례:
- Interaction_Text: `E 키로 상호작용`
- HowToUse_Flashlight: `손전등 사용: 마우스 휠 클릭`
- Tip_Text: `낮/밤 전환: Q 키`
- Throw_item: `F 키로 아이템 던지기`

CSV 대사(`Assets/My/Data/Dialogue/sample.csv`)도 좁은 대사창에서 동일 증상 가능.

**영어:** 해당 버그 없음. 라틴 문자는 공백에서만 줄바꿈 → 단어 중간 분리 불가.

## 변경
`Assets/TextMesh Pro/Resources/TMP Settings.asset`

```
m_UseModernHangulLineBreakingRules: 0  →  1
```

"현대 한글 줄바꿈" = 한글을 영어처럼 어절(공백) 단위로만 줄바꿈. 한국어 표준 조판 방식.
전역 설정이라 대사·상호작용 프롬프트·HUD 등 모든 TMP 텍스트에 적용.

## 검토 후 제외
- 문자열별 수동 `\n` / `<nobr>` 삽입 — 문자열 수 많고 컨테이너 폭 바뀌면 깨짐.
- HUD RectTransform 폭 확대 — 전역 설정 변경으로 불필요.
- 설정만으로 부족한 특정 라벨이 생기면 그때 개별 조정.

## 확인 필요 (사용자)
Play → 한글 모드로 접객/HUD 진입 → 대사·프롬프트·HUD 라벨이 어절 단위로 줄바꿈되는지 확인.
