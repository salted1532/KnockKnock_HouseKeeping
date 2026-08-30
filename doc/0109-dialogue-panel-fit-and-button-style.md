# 0109 - Dialogue_Panel 버튼 맞춤 리사이즈 + 버튼 스타일

날짜: 2026-08-30
관련: `doc/0102`·`doc/0104`·`doc/0107` (대화 시스템 / 로컬라이제이션)

## 요청 (원문)

> Dialogue_Panel 의 Dialogue_Button 들에 맞게 패널 크기가 조절되도록 해주고
> 버튼도 패널처럼 회색 바탕 색깔에 흰색 글씨로 나오도록 해줘
> Exit 버튼은 Button_Horizontal 안에 넣어뒀는데 가장 오른쪽에 위치하도록 해줘

## 1. 현황 (에디터 씬 실측, 디스크 YAML 과 다름 — 저장 안 된 편집 있음)

```
Dialogue_Panel   RectTransform 900x300, anchor (0.5,0), pivot (0.5,0)
                 Image  color (0.05, 0.05, 0.06, 0.85)  ← 거의 검정
                 QuestionPanel  root=Button_Horizontal, buttonParent=Button_Horizontal,
                                buttonPrefab=Dialogue_Button, doneButton=Dialogue_Exit
├─ Dialogue          anchor (0,0.34)-(1,1) 스트레치, Image (0.06,0.06,0.07,0.92)
│   ├─ Dialogue_Text  TMP
│   └─ npc_name       TMP
└─ Button_Horizontal anchor (0,0)-(1,0.32) 스트레치
                     HorizontalLayoutGroup: spacing 0, padding 0, childAlignment UpperLeft,
                       ForceExpandWidth ON, ForceExpandHeight ON, ControlWidth/Height OFF
    └─ Dialogue_Exit  siblingIndex 0, 240x120, Image white(1,1,1,1)
        └─ Text (TMP) "Exit", fontColor (0.196 회색), 정렬 Left/Top
```

- `Dialogue_Button.prefab` : 240x60, Image white, Text(TMP) fontColor (0.196 회색), 정렬 Left/Middle
- 런타임: `QuestionPanel.Show()` 가 질문 수만큼 `Dialogue_Button` 을 `Button_Horizontal` 에 `Instantiate` → **Dialogue_Exit(index 0) 뒤에 붙음** → Exit 가 맨 왼쪽. (요청 3의 원인)
- HLG `ForceExpandWidth` 라 버튼은 240 유지하되 900 폭에 간격 벌어져 퍼짐. 패널은 900 고정 → 버튼 수와 무관하게 항상 같은 크기. (요청 1의 원인)

## 2. 설계

### A. Exit 를 맨 오른쪽으로 — `QuestionPanel.cs` 1줄

`Show()` 에서 doneButton 텍스트 세팅 직후:

```csharp
if (doneButton != null) doneButton.transform.SetAsLastSibling();
```

매 `Show()` 마다 생성 버튼들 뒤(맨 오른쪽)로 보냄. showDone=false 여도 무해.

### B. 패널이 버튼 폭에 맞게 리사이즈

**B-1. `Button_Horizontal` (씬) — 버튼이 실제 폭만큼만 차지하도록**

| 항목 | 현재 | 변경 |
|---|---|---|
| HLG ChildForceExpandWidth | ON | **OFF** |
| HLG ChildAlignment | UpperLeft | **MiddleCenter** |
| HLG Spacing | 0 | **12** |
| HLG Padding (L,R,T,B) | 0,0,0,0 | **16,16,8,8** |
| RectTransform anchor | (0,0)-(1,0.32) 스트레치 | **(0.5,0)-(0.5,0)**, pivot (0.5,0), anchoredPos (0,16) |
| ContentSizeFitter | 없음 | **추가** — Horizontal=PreferredSize, Vertical=Unconstrained |

→ `Button_Horizontal` 폭 = 버튼들 합계 + spacing + padding 으로 자동 축소/확장.

**B-2. `QuestionPanel.cs` — 패널 폭을 버튼 행에 맞춤**

```csharp
[SerializeField] private RectTransform panelRect;      // Dialogue_Panel
[SerializeField] private float sidePadding = 32f;
```

`Show()` 끝(Exit 정렬 후):

```csharp
if (panelRect != null)
{
    var row = (RectTransform)buttonParent;
    LayoutRebuilder.ForceRebuildLayoutImmediate(row);
    panelRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal,
        row.rect.width + sidePadding * 2f);
}
```

- 높이(300)는 **고정** — 위쪽 대사 영역(`Dialogue`)이 높이를 차지, 요청도 폭("버튼들에 맞게")만 언급.
- `Dialogue` 는 스트레치 앵커라 패널 폭이 바뀌면 같이 조정됨(별도 작업 없음).
- 인스펙터에서 `QuestionPanel.panelRect` 에 `Dialogue_Panel` 의 RectTransform 을 물려야 함(내가 설정).

### C. 버튼 회색 배경 + 흰 글씨

`Dialogue_Button.prefab` + `Dialogue_Exit`(씬) 동일 적용:

| 대상 | 현재 | 변경 |
|---|---|---|
| Image color | white (1,1,1,1) | 회색 **(0.20, 0.20, 0.23, 1)** |
| Text(TMP) fontColor | (0.196, 0.196, 0.196) | **흰색 (1,1,1,1)** |
| Text(TMP) 정렬 | Left/Middle(Exit 는 Left/Top) | **Center/Middle** |

- Button 컴포넌트의 ColorTint(Normal=white, Highlighted 0.96, Pressed 0.78)는 그대로 → 회색 위에 눌림/호버 명암만 유지.
- 회색 값 (0.20,0.20,0.23) = 패널(0.05)보다 밝게 해 버튼이 배경과 구분되게. "패널과 완전 동일"을 원하면 (0.05,0.05,0.06) 으로. → **확인 1**

## 3. 영향 파일

```
수정 (코드)
  Assets/My/Scripts/Dialogue/QuestionPanel.cs   Exit SetAsLastSibling + panelRect/sidePadding 리사이즈

수정 (에셋)
  Assets/My/InGame/UI/Dialogue_Button.prefab     Image 회색, Text 흰색+Center
  Assets/Scenes/InGame.unity                     Button_Horizontal(HLG+RectTransform+ContentSizeFitter),
                                                 Dialogue_Exit(Image 회색, Text 흰색+Center),
                                                 QuestionPanel.panelRect 연결

문서
  Docs/DialogueSystem.md  (QuestionPanel 리사이즈/스타일 한 줄 추가)
```

## 4. 확인 필요

1. **버튼 회색 값**: (0.20,0.20,0.23) 추천 (패널보다 밝게, 버튼 구분). vs 패널과 동일 (0.05,0.05,0.06).
2. **패널 높이**: 300 고정 유지. (버튼 행만 폭 맞춤)
3. **적용 방식**: 승인되면 내가 uloop 로 prefab·씬·`.cs` 직접 수정 + 컴파일 검증.

## 5. 스킵 (YAGNI)

- 패널에 VerticalLayoutGroup + LayoutElement 재구성 — `Dialogue` 텍스트 블록과 충돌, 스크립트 6줄이 더 단순.
- 높이 동적 맞춤 / 애니메이션.
- `Close()` 시 폭 원복 — 다음 `Show()` 가 다시 세팅, 패널은 그동안 숨겨짐.

## 6. 구현 완료 (2026-08-30, 확인: 회색 (0.20,0.20,0.23) / 진행)

| 파일 | 내용 |
|---|---|
| `Dialogue/QuestionPanel.cs` | `panelRect`(Dialogue_Panel), `sidePadding=32` 필드 추가. `Show()` 에서 `doneButton.transform.SetAsLastSibling()` + `FitPanelToButtons()` (버튼 행 `ForceRebuildLayoutImmediate` 후 `panelRect` 폭 = 행폭 + sidePadding*2) |
| `Assets/My/InGame/UI/Dialogue_Button.prefab` | Image color → `(0.20,0.20,0.23,1)`, Text(TMP) color → 흰색, alignment → Center |
| `Assets/Scenes/InGame.unity` `Dialogue_Exit` | Image color → 회색, Text 흰색 + Center |
| `Assets/Scenes/InGame.unity` `Button_Horizontal` | HLG: ForceExpandWidth off, ControlWidth off, alignment MiddleCenter, spacing 12, padding (16,16,8,8). RectTransform: anchor (0.5,0), pivot (0.5,0), sizeDelta.y 76, anchoredPos (0,16). `ContentSizeFitter` 추가 (H=PreferredSize, V=Unconstrained) |
| `Assets/Scenes/InGame.unity` `QuestionPanel` | `panelRect` = Dialogue_Panel RectTransform 연결 |
| `Docs/DialogueSystem.md` | QuestionPanel 절에 리사이즈/Exit 정렬/버튼 스타일 반영 |

### 검증
- `uloop compile` : Success, Error 0, Warning 0.
- Play 모드에서 `QuestionPanel.Show(["Ask their name","Ask where they're headed","Just take the room"], _, showDone:true)`:
  - 버튼 행 자식 4개, 첫째 `Dialogue_Button(Clone)`, **마지막 `Dialogue_Exit`** (맨 오른쪽) ✓
  - 행 폭 1028 (4×240 + 3×12 + 32), 패널 폭 1092 (= 1028 + 64) ✓
  - 4개 전부 Image `RGBA(0.200,0.200,0.230,1)`, Text 흰색, 정렬 Center ✓

## 7. 추가 요청 (2026-08-30)

> 1. Dialogue_Panel 에 선택지 버튼이 없을 때 패널이 너무 작아짐 → 기본(최소) 크기를 버튼 3개 정도로.
> 2. Exit 버튼도 Dialogue_Button 처럼 크기 조절.

| 파일 | 내용 |
|---|---|
| `Dialogue/QuestionPanel.cs` | `minPanelWidth = 840f` 필드 추가. `FitPanelToButtons` 에서 `Mathf.Max(minPanelWidth, 행폭 + sidePadding*2)` → 버튼 적어도 840 밑으로 안 좁아짐. 840 = 버튼 3개(3×240 + 2×12 + 패딩 32 + sidePadding 64) 근사 |
| `InGame.unity` `Dialogue_Panel` | `RectTransform.sizeDelta.x` 900 → **840** (기본/휴지 상태 폭). `QuestionPanel.minPanelWidth` = 840 |
| `InGame.unity` `Dialogue_Exit` | `RectTransform.sizeDelta` `240×120` → **`240×60`** (Dialogue_Button 프리팹과 동일) |

### 검증 (Play 모드, 단일 Show)
- 질문 1개 + Exit → 패널 폭 840 (floor 유지) ✓
- 질문 3개 + Exit → 행 1028 / 패널 1092 (floor 초과분은 그대로 확장) ✓
- `Dialogue_Exit.sizeDelta` = (240, 60) = `Dialogue_Button` 프리팹과 일치 ✓
- `uloop compile` : Error 0

## 상태

2026-08-30 구현·검증 완료 (기본 크기 floor + Exit 크기 통일 포함). 사용자 인게임 확인 대기.
