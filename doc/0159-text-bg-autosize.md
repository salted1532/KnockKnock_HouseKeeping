# 0159 – 텍스트 길이에 맞춰 BG 자동 크기 (적용 완료)

## 요청
"텍스트들 BG들 이미지 텍스트 길이나 크기에 맞춰서 BG 크기도 변경되도록 해줘"
- 대상(사용자 선택): 화면 중앙 문구(ScreenMessage), 일차 종료/페이즈 타이틀(DayEndTitle), HUD 라벨(Money, Watch)
- 방식(사용자 선택): 가로·세로 둘 다

## 현재 상태 (InGame.unity)
| 오브젝트 | 구조 | BG |
|---|---|---|
| `Canvas/ScreenMessage` | 루트(CanvasGroup+스크립트, 900×120 고정) → `BG`(검정 0.5α, 860×84), `Text`(루트에 stretch) | BG·Text 형제, 둘 다 고정 |
| `Canvas/DayEndTitle` | 루트(풀스크린) → `Text`("N일차", 폰트110) | **BG 없음** |
| `Canvas/Money` | 루트 = 텍스트("$ : 100", 200×50 고정, 우측정렬) → `Money_BG`(검정 0.5α, 224×62 고정) | BG가 텍스트의 자식, 둘 다 고정 |
| `Canvas/Watch` | 루트 = 텍스트(PhaseLabel "N일차", 200×50 고정, 좌측정렬) → `Watch_BG`(검정 0.5α, 224×62 고정) | 동일 |

돈이 커지거나("$ 1,000,000") 문구가 길어지면 텍스트가 고정 BG 밖으로 삐져나옴.

## 제안 (코드 0줄, 씬 컴포넌트만 — Unity 기본 레이아웃)
공통 패턴: **텍스트에 `ContentSizeFitter`(Horizontal=Preferred, Vertical=Preferred) → 텍스트 rect가 글자에 딱 맞음. BG는 텍스트의 자식으로 stretch(앵커 0,0~1,1) + 음수 offset 여백 → BG가 텍스트를 자동 추종.**

| 대상 | 구체 변경 |
|---|---|
| **Money** | 루트에 `ContentSizeFitter`(둘 다 Preferred). pivot (0.5,0.5)→(1,1)로 바꾸고 anchoredPosition 보정(우상단 고정, 왼쪽으로 자람). TMP `Margin` 0으로. `Money_BG`: 앵커 stretch, offset L/B −16, R/T +16(≈현재 여백) |
| **Watch** | 동일. pivot (0.5,0.5)→(0,1)(좌상단 고정, 오른쪽으로 자람). `Watch_BG` 동일 |
| **ScreenMessage** | `Text`에 `ContentSizeFitter`(둘 다 Preferred), 앵커 stretch→center, pivot (0.5,0.5). `BG`를 `Text`의 자식으로 이동, 앵커 stretch + offset L/B −24/−18, R/T +24/+18(≈현재 900−860, 120−84 여백). 루트는 그대로(스크립트는 alpha만 건드림) |
| **DayEndTitle** | 사용자 선택으로 **스킵**(풀스크린 페이드 타이틀, BG 불필요) |

### 안 하는 것 / 한계
- SpeechBubble(말풍선)은 사용자가 대상에서 뺌 → 손대지 않음.
- ScreenMessage 문구가 아주 길면 한 줄로 쭉 늘어남(TMP+ContentSizeFitter는 줄바꿈 무시). 현재 문구는 다 짧음. 길어져서 화면 넘치면 그때 max-width 추가.
- QuestionPanel(대화 선택지 패널)은 이미 자체 폭 맞춤 로직 있음(doc/0135) → 제외.

## 적용 (완료)
`uloop-execute-dynamic-code` 로 InGame.unity 에 컴포넌트 추가/앵커·pivot·offset 세팅 후 씬 저장. 코드(.cs) 변경 없음.

- Money `Canvas/Money`: `ContentSizeFitter`(H/V Preferred), pivot (0.5,0.5)→(1,0.5) + anchoredPos 보정, TMP margin 0. `Money_BG` 앵커 0,0~1,1 / offset ±(16,10)
- Watch `Canvas/Watch`: 동일, pivot (0.5,0.5)→(0,0.5). `Watch_BG` offset ±(16,10)
- ScreenMessage `Canvas/ScreenMessage`: 루트에 `HorizontalLayoutGroup`(childControl W/H, forceExpand off, MiddleCenter, padding 20/20/18/18) + `ContentSizeFitter`(H/V Preferred). `BG` 는 `LayoutElement.ignoreLayout=true` + 앵커 0,0~1,1 / offset 0(패딩은 레이아웃그룹이 담당). `Text` 앵커·pivot (0,1) 로 정규화 (레이아웃그룹이 배치)
  - BG 가 Text 의 형제(뒤쪽)라 Text 자식으로 옮기면 렌더 순서상 텍스트를 덮음 → 리페어런트 대신 루트 레이아웃 방식 사용

## 검증 (EditMode, 짧은/긴 텍스트로 rect 측정)
```
Money      "$100"          text= 98x42  bg=130x62   (dW 32, dH 20)
Money      "$1,234,567,890" text=324x42 bg=356x62   (dW 32, dH 20)
Watch      "1일차"          text= 95x42  bg=127x62   (dW 32, dH 20)
Watch      "88일차 · 늦은 오후" text=349x42 bg=381x62 (dW 32, dH 20)
ScreenMsg  "응답이 없다"      text=228x49  bg=268x85   (dW 40, dH 36)
ScreenMsg  (27자 긴 문장)     text=910x49  bg=950x85   (dW 40, dH 36)
```
BG 가 가로·세로 모두 텍스트를 일정 여백으로 추종. 렌더 순서(부모/형제 관계) 미변경 → 텍스트 가시성 그대로.

## 한계
ScreenMessage 긴 문장은 줄바꿈 없이 한 줄로 늘어남(27자 → 910px, 화면 폭 근접). 실제 사용 문구는 다 짧음. 길어져 넘치면 `LayoutElement.preferredWidth` 상한 + `enableWordWrapping` 로 대응.
