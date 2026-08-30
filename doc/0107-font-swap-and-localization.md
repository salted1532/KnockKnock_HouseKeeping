# 0107 - 폰트 교체(Galmuri11) + 영어/한글 로컬라이제이션 (제안)

날짜: 2026-08-30
관련: `doc/0102`·`doc/0104`(대화 시스템), `doc/0106`(프롬프트 영어화)

## 요청 (원문)

> 1. 현재 존재하는 텍스트들의 폰트를 **Galmuri11 SDF** 로 바꿔줘.
> 2. 지금 출력되는 대사 텍스트나 그런 걸 전부 뽑아다가 **영어/한글 버전**을 만들어서 변환 가능하도록 해줘.
> 3. **로컬라이제이션 매니저**를 씬에 배치해서 영어/한글을 인스펙터 **콤보 박스**로 지정 → **게임 시작 시** 해당 언어로 출력.
> 4. 깨지는 문자가 있으면 **기존 폰트(LiberationSans SDF)를 Fallback** 으로 대처.

## 1. 조사

### 폰트 현황
| | 값 |
|---|---|
| 현재 전역 기본 폰트 (`TMP Settings.m_defaultFontAsset`) | `LiberationSans SDF` (guid `8f586378…`) — 한글 글리프 없음 |
| 씬 `InGame.unity` 안 `m_fontAsset` 명시 참조 | 17개, **전부** `LiberationSans SDF` |
| `Assets/My/**` 프리팹의 폰트 참조 | **0개** (프리팹 TMP 텍스트 없음). 런타임 생성 버튼은 씬 안 템플릿에서 복제 |
| 목표 폰트 | `Assets/My/font/Galmuri11 SDF.asset` (guid `a3e2ed54…`) |
| Galmuri11 커버리지 | ASCII 32~ + 한글 음절 전체(44032~55203), 12054 글리프. **정적 아틀라스**(런타임 글리프 생성 불가). `·`(U+00B7), em‑dash 등 일부 기호 없음 |

→ 폰트 교체는 **(a) `TMP Settings` 기본 폰트 + (b) 씬 17개 참조** 두 곳만 바꾸면 됨. `·` 등은 (4)의 폴백으로 해결.

### "출력되는 텍스트" 출처 전수 조사

| # | 출처 | 현재 | 처리 |
|---|---|---|---|
| 1 | 대사 줄 | `sample.csv` `text` 열 (영어만) → `DialogueLine.text` | CSV `_en`/`_ko` 열 + `DialogueLine.Text` |
| 2 | 질문/선택지 버튼 문구 | `sample.csv` `label` 열 → `DialogueEntry.label` / `Choice.label` | CSV `_en`/`_ko` 열 + `.Label` |
| 3 | 손님 이름 (`SpeechBubble.nameLabel`) | `NpcData.displayName` | `displayNameKo` + `DisplayName` |
| 4 | 상호작용 프롬프트 (`CursorInteractor.promptLabel`) | `Interactable.Prompt` — 영어 리터럴 | `LocalizationManager.T(en, ko)` 로 감쌈 |
| 5 | 시간대 라벨 (`PhaseLabel`) | `"Day 3 · Evening"` — enum 이름 영어 | 한글일 때 `"3일차 · 저녁"` |
| 6 | "대화 종료" 버튼 (`QuestionPanel.doneButton`) | 씬 정적 텍스트 | `Show()` 에서 `T("Done","대화 종료")` 로 설정 |
| 7 | Guest 프리팹 "Check in" 프롬프트 | `promptType=Custom`, `customPrompt="Check in"` | `InteractionPrompt.CheckIn` enum 값 추가 |
| 8 | `CampaignData.nightNews` | 데이터에만 존재, **표시 코드 없음** | 이번 범위 밖 (표시 UI 생기면 그때) |
| 9 | 씬 정적 라벨 `"sex"`, `"New Text"`, StarterAssets 안내문(`"Press E"`, `"Use flashlight…"` 등) | 프로토타입 잔여물 | 로컬라이즈 안 함 (정리 대상). 필요 시 `LocalizedLabel` |

## 2. 설계

### A. 폰트 교체 + 폴백 — 에디터 메뉴 1개

`Assets/My/Scripts/Localization/Editor/FontTool.cs` (신규):

`Tools > Localization > Apply Galmuri Font`
1. `TMP Settings` : `m_defaultFontAsset` → Galmuri11 SDF, `m_fallbackFontAssets` 에 `LiberationSans SDF` 추가 (SerializedObject).
2. 열린 씬 + `Assets/My/**` 모든 프리팹의 `TMP_Text.font = Galmuri` (머티리얼도 자동 동기화) → `SetDirty` → 저장.

> 직접 YAML GUID 치환 대신 스크립트를 쓰는 이유: `.font` 세터가 `m_sharedMaterial` 을 새 폰트 머티리얼로 같이 갱신 → 분홍색 깨짐 방지. 재사용도 됨(씬/프리팹 추가 시 다시 실행).

폴백은 **전역**(`TMP Settings`)으로 건다 → Galmuri 에 없는 글리프(`·`, 악센트 문자 등)는 자동으로 LiberationSans 에서 렌더. `PhaseLabel` 의 `·` 는 그대로 둠(폴백이 처리).

### B. 로컬라이제이션 데이터

**CSV 스키마** (`_en`/`_ko` 열 쌍):
```
npcId,situation,day,role,nodeKey,label_en,label_ko,expression,text_en,text_ko,goto,outcome
```
- `text_ko` / `label_ko` 비면 `_en` 으로 폴백.
- 임포터 인덱스 갱신 (0→11), 최소 열 수 9 로.

**`DialogueLine`** (struct):
```csharp
public struct DialogueLine
{
    public Expression expression;
    [TextArea(1,4)] public string textEn;
    [TextArea(1,4)] public string textKo;
    public string Text => LocalizationManager.Korean && !string.IsNullOrEmpty(textKo) ? textKo : textEn;
}
```

**`Choice`** : `label` → `labelEn` + `labelKo` + `string Label => …`
**`DialogueEntry`** : `label` → `labelEn` + `labelKo` + `string Label => …`
**`NpcData`** : `displayNameKo` 추가 + `string DisplayName => …`

**소비처 수정** (텍스트만):
- `SpeechBubble.TypeLine(line.text)` → `line.Text`, `npc.displayName` → `npc.DisplayName`
- `DialogueRunner.QuestionHub` : `q.label` → `q.Label`, `PlayNode` : `c.label` → `c.Label`
- `DialogueImporter` : 새 열 파싱, Self-Check 문자열 `textEn/textKo` 로

### C. LocalizationManager (씬 배치)

`Assets/My/Scripts/Localization/LocalizationManager.cs` (신규):
```csharp
public enum Language { English, Korean }

[DefaultExecutionOrder(-500)]
public class LocalizationManager : MonoBehaviour
{
    [SerializeField] private Language language = Language.English;   // ← 인스펙터 콤보 박스

    public static Language Current { get; private set; } = Language.English;
    public static bool Korean => Current == Language.Korean;

    private void Awake() => Current = language;                       // 게임 시작 시 확정
    public static string T(string en, string ko) => Korean && !string.IsNullOrEmpty(ko) ? ko : en;

#if UNITY_EDITOR
    private void OnValidate() { if (!Application.isPlaying) Current = language; }
#endif
}
```
- `enum` 필드 → 인스펙터에서 드롭다운. 게임 중 전환 없음(요청: "게임 시작 시").
- 정적 문자열은 키 테이블 없이 `T("English","한글")` 인라인 페어로. (프롬프트 16개 + 시간대 4개뿐 — 테이블은 과설계)

**정적 UI 문자열 수정:**
- `Interactable.Prompt` : 각 반환값을 `LocalizationManager.T(...)` 로. `_ => ToString()` 폴백 → `Interact/Use/Push/Hang` 명시 case 로 (한글 필요).
- `InteractionPrompt` enum 끝에 `CheckIn` 추가 → `T("Check in","체크인")`. `Guest.prefab` `promptType 9→16`, `customPrompt` 비움.
- `PhaseLabel.Refresh` : 한글이면 `phase` → 아침/점심/저녁/새벽, 포맷 `"{day}일차 · {name}"`.
- `QuestionPanel.Show` : `doneButton` 자식 `TMP_Text` = `T("Done","대화 종료")`.

### D. LocalizedLabel (정적 라벨용, 선택)

`Assets/My/Scripts/Localization/LocalizedLabel.cs` (신규, ~12줄):
```csharp
[RequireComponent(typeof(TMP_Text))]
public class LocalizedLabel : MonoBehaviour
{
    [SerializeField, TextArea] private string english;
    [SerializeField, TextArea] private string korean;
    private void OnEnable() => GetComponent<TMP_Text>().text = LocalizationManager.T(english, korean);
}
```
지금 당장 붙일 정적 라벨은 없음(#9는 정리 대상). 메뉴/HUD 텍스트 생기면 쓰라고 넣어둘지 여부 → **확인 6**.

## 3. 한글 번역 (검토 요망)

### 정적 UI
| 영어 | 한글 |
|---|---|
| Interact | 상호작용 |
| Open / Close | 열기 / 닫기 |
| Turn on / Turn off | 켜기 / 끄기 |
| Pick up | 줍기 |
| Use | 사용 |
| Inspect | 살펴보기 |
| Clean up | 정리하기 |
| Push | 밀기 |
| View | 보기 |
| Read | 읽기 |
| Hang | 걸기 |
| End shift | 근무 종료 |
| Close up | 마감 |
| Sleep | 잠자기 |
| Check in | 체크인 |
| Done | 대화 종료 |
| Morning / Noon / Evening / Dawn | 아침 / 점심 / 저녁 / 새벽 |

### 대사 (`sample.csv`)

**NPC 1 — 지친 나그네**
| en | ko |
|---|---|
| Evening. Got a room for one night? | 안녕하세요. 하룻밤 묵을 방 있습니까? |
| Been walking since dawn. I'm done in. | 새벽부터 걸었어요. 완전히 지쳤습니다. |
| Ask their name | 이름을 묻는다 |
| Just call me a traveler. Names don't buy much out here. | 그냥 나그네라고 부르세요. 이런 데선 이름 따위 쓸모없으니까. |
| Ask where they're headed | 어디로 가는지 묻는다 |
| Somewhere the road stops. You know any place like that? | 길이 끝나는 곳 어딘가로요. 그런 데 아세요? |
| I wish I did | 나도 알았으면 좋겠네요 |
| Just take the room | 그냥 방을 쓰세요 |
| Yeah. Figured as much. | 그렇죠. 그럴 줄 알았어요. |
| Fair enough. Lead the way. | 알겠습니다. 안내해 주세요. |
| Turn them away | 돌려보낸다 |
| No room? After all that walking? | 방이 없다고요? 그렇게 걸어왔는데? |
| I won't be trouble. Gone by first light. | 폐 끼치지 않을게요. 동트면 떠납니다. |
| Hold firm | 단호하게 거절한다 |
| Reconsider | 다시 생각해 본다 |
| ...Fine. I've slept in worse than a ditch. | ...알겠습니다. 도랑보다 못한 데서도 자봤어요. |
| Thank you. You won't hear a sound from me. | 고맙습니다. 소리 하나 안 낼게요. |
| What do you want at this hour? | 이 시간에 무슨 일이십니까? |
| Ask about the noise last night | 간밤의 소음에 대해 묻는다 |
| Noise? I was asleep the whole time. | 소음이요? 저는 계속 자고 있었는데요. |

**NPC 2 — 불안한 외판원**
| en | ko |
|---|---|
| Ah - yes. Hello. A room. Please. Just the one. | 아 - 네. 안녕하세요. 방이요. 부탁드립니다. 하나면 됩니다. |
| Long day. Very long. You have no idea. | 긴 하루였어요. 아주 길었죠. 상상도 못 하실 겁니다. |
| Ask about his suitcase | 가방에 대해 묻는다 |
| Samples! Brushes mostly. Nothing you'd want. Nothing at all. | 견본입니다! 대부분 솔이에요. 탐나실 만한 건 없어요. 전혀요. |
| Ask why he's shaking | 왜 떨고 있는지 묻는다 |
| Cold out. That's all. Just the cold. | 밖이 추워서요. 그게 다입니다. 그냥 추위 때문이에요. |
| Let it go | 넘어간다 |
| Press him | 추궁한다 |
| I said it's the cold! Are we done here? | 추위 때문이라고 했잖아요! 이제 됐습니까? |
| Turn them away | 돌려보낸다 |
| No? Please. I can pay double. | 안 된다고요? 제발요. 두 배로 내겠습니다. |
| Where else is open? Nowhere. You know that. | 다른 데가 어디 열었겠어요? 없어요. 아시잖아요. |
| Refuse again | 다시 거절한다 |
| Give in | 받아준다 |
| This is a mistake. You'll see. | 이건 실수예요. 두고 보세요. |
| Fine. FINE. I'm going. | 알겠어요. 알겠다고요. 갑니다. |
| Oh thank you. You won't even know I'm here. | 아 감사합니다. 있는 줄도 모르실 거예요. |

**NPC 3 — 무례한 단골**
| en | ko |
|---|---|
| Same as always. Key. Now. | 늘 하던 대로. 열쇠. 당장. |
| You're slow tonight. Move it. | 오늘 밤은 굼뜨군. 빨리 해. |
| Ask him to wait a moment | 잠시 기다려 달라고 한다 |
| I don't wait. That's your job. | 난 안 기다려. 그게 네 일이잖아. |
| Ask for identification | 신분증을 요구한다 |
| You've seen my face a hundred times. | 내 얼굴 백 번은 봤잖아. |
| Insist on ID | 신분증을 계속 요구한다 |
| Wave him through | 그냥 들여보낸다 |
| Here. Happy? Don't lose it this time. | 여기. 됐어? 이번엔 잃어버리지 마. |
| Turn them away | 돌려보낸다 |
| Excuse me? | 뭐라고? |
| You do NOT turn me away. Not me. | 날 돌려보낸다고? 나를? |
| Stand your ground | 물러서지 않는다 |
| Back down | 물러선다 |
| You'll regret this. Count on it. | 후회하게 될 거다. 틀림없이. |
| Finally. | 드디어. |

**NPC 4 — 경계하는 여인**
| en | ko |
|---|---|
| A room. Away from the road if you have one. | 방 하나요. 가능하면 길에서 먼 쪽으로. |
| Ask why away from the road | 왜 길에서 먼 쪽인지 묻는다 |
| I sleep lighter than most. | 저는 잠귀가 밝은 편이에요. |
| Ask if she's travelling alone | 혼자 여행 중인지 묻는다 |
| Is that a requirement? | 그게 조건인가요? |
| Ask gently | 부드럽게 묻는다 |
| Ask straight | 단도직입적으로 묻는다 |
| ...Yes. Alone. | ...네. 혼자예요. |
| That's my business. Not yours. | 그건 제 일이에요. 당신 일이 아니라. |
| Turn them away | 돌려보낸다 |
| I see. | 그렇군요. |
| I'll find somewhere. I always do. | 어디든 찾겠죠. 늘 그랬으니까. |
| Good. Don't knock after dark. | 좋아요. 어두워지면 문 두드리지 마세요. |

**NPC 5 — 수다스러운 노인**
| en | ko |
|---|---|
| Evening evening! Cold one out there tonight isn't it? | 안녕하세요 안녕하세요! 오늘 밤 바깥 꽤 춥죠? |
| Room for an old man's bones? | 늙은이 뼈 하나 뉠 방 있겠소? |
| Ask about his journey | 여정에 대해 묻는다 |
| Oh it's a long story. Which part do you want? | 아, 긴 이야기라오. 어느 대목이 궁금하시오? |
| The beginning | 처음부터 |
| The short version | 짧게 |
| Skip it | 됐어요 |
| It started forty years back in a town that isn't on the maps anymore... | 사십 년 전, 이제는 지도에도 없는 마을에서 시작됐다오... |
| Visiting family. Or what's left of them. | 가족을 보러 가오. 남은 사람이나마. |
| Ask about the weather | 날씨에 대해 묻는다 |
| Storm's coming. Mark my words. Sky goes green first. | 폭풍이 오고 있소. 내 말 명심하시오. 하늘부터 초록빛으로 변한다오. |
| Ask if he needs anything | 필요한 게 있는지 묻는다 |
| Kind of you to ask. | 물어봐 주니 고맙구려. |
| Offer an extra blanket | 담요를 더 준다 |
| Offer a hot meal | 따뜻한 식사를 권한다 |
| Nothing for now | 지금은 됐어요 |
| Bless you. These old knees will thank you. | 고맙기도 하지. 이 늙은 무릎이 고마워할 거요. |
| I wouldn't say no to that. | 그거라면 마다하지 않겠소. |
| Turn them away | 돌려보낸다 |
| Oh. Oh dear. On a night like this? | 이런. 저런. 이런 밤에 말이오? |
| I don't move so fast anymore. Have a heart. | 이제 걸음도 느리다오. 인정을 베푸시게. |
| Refuse | 거절한다 |
| Let him stay | 묵게 한다 |
| ...Alright. Sorry to have troubled you. | ...알겠소. 귀찮게 해서 미안하오. |
| Bless you son. Sleep well now. | 복 받으시게. 편히 주무시구려. |

## 4. 영향 파일

```
신규
  Localization/LocalizationManager.cs      enum Language + static Current/Korean/T
  Localization/LocalizedLabel.cs           (선택) 정적 TMP 라벨
  Localization/Editor/FontTool.cs          Tools > Localization > Apply Galmuri Font

수정 (코드)
  Dialogue/DialogueLine.cs                 DialogueLine{textEn,textKo,Text}, Choice{labelEn,labelKo,Label}, DialogueEntry{labelEn,labelKo,Label}
  Dialogue/SpeechBubble.cs                 line.Text / npc.DisplayName
  Dialogue/DialogueRunner.cs               q.Label / c.Label
  Dialogue/QuestionPanel.cs                doneButton 텍스트 = T("Done","대화 종료")
  Dialogue/NpcData.cs                      displayNameKo + DisplayName
  Dialogue/Editor/DialogueImporter.cs      12열 스키마, Self-Check
  Game/PhaseLabel.cs                       한글 시간대 이름
  Interaction/Interactable.cs              Prompt → T(...), InteractionPrompt.CheckIn 추가

수정 (에셋)
  Assets/My/Data/Dialogue/sample.csv       12열 + 한글
  Assets/TextMesh Pro/Resources/TMP Settings.asset   기본 폰트 Galmuri + LiberationSans 폴백
  Assets/Scenes/InGame.unity               LocalizationManager GO 추가, TMP 17개 폰트, (FontTool 로)
  Assets/My/InGame/Prefabs/Guest.prefab    promptType 9→16, customPrompt 비움

문서
  Docs/DialogueSystem.md, Docs/Interactable.md, Docs/PhaseLabel.md 갱신 + LocalizationManager.md 신규
```

## 5. 확인 필요

1. **CSV 스키마**: 위 12열(`label_en,label_ko,…,text_en,text_ko`) OK? (`sample.csv` 재작성됨)
2. **언어 전환 시점**: 게임 시작 시 고정(런타임 토글 없음) — 요청대로. OK?
3. **폴백 위치**: 전역(`TMP Settings.m_fallbackFontAssets`) vs Galmuri 에셋 자체(`m_FallbackFontAssetTable`). 전역 추천.
4. **`Guest` "Check in"**: `InteractionPrompt.CheckIn` enum 값 추가 방식 OK? (프리팹 1곳 수정)
5. **한글 번역** (§3): 톤/표현 수정할 것 있는지.
6. **`LocalizedLabel`**: 지금 넣어둘지 vs 실제 메뉴/HUD 텍스트 생길 때.
7. **폰트 적용 실행**: Unity 켜져 있으면 `FontTool` 을 바로 돌릴지, 스크립트만 두고 사용자가 실행할지.

## 6. 스킵 (YAGNI)

- 키 기반 문자열 테이블 / `.po` / Unity Localization 패키지 — 문자열 수가 적음. `T(en,ko)` 인라인으로 충분.
- 런타임 언어 전환 + 이벤트 리프레시.
- `nightNews` — 표시 UI 없음.
- 3번째 언어.
- StarterAssets 안내문/프로토타입 라벨 로컬라이즈.

## 7b. 구현 완료 (2026-08-30, 확인 답변: 그대로 진행 / LocalizedLabel 지금 넣기 / 내가 실행)

| 파일 | 내용 |
|---|---|
| `Localization/LocalizationManager.cs` | 신규. `enum Language{English,Korean}` + `[SerializeField] language`(콤보 박스) → `Awake` 에서 정적 `Current`. `Korean`, `T(en,ko)`. `[DefaultExecutionOrder(-500)]` |
| `Localization/LocalizedLabel.cs` | 신규. `[RequireComponent(TMP_Text)]`, `english`/`korean` → `OnEnable` 적용 |
| `Localization/Editor/FontTool.cs` | 신규. `Tools > Localization > Apply Galmuri Font` — TMP Settings 기본=Galmuri11 SDF + 폴백에 LiberationSans SDF, 열린 씬/`Assets/My` 프리팹 `TMP_Text.font` 일괄 교체 |
| `Dialogue/DialogueLine.cs` | `DialogueLine{expression,textEn,textKo,Text}`, `Choice{labelEn,labelKo,goToNode,Label}`, `DialogueEntry` `label`→`labelEn`/`labelKo`+`Label` |
| `Dialogue/SpeechBubble.cs` | `line.text`→`line.Text`, `npc.displayName`→`npc.DisplayName` |
| `Dialogue/DialogueRunner.cs` | `q.label`/`c.label` → `q.Label`/`c.Label` |
| `Dialogue/QuestionPanel.cs` | `Show` 에서 doneButton 자식 `TMP_Text` = `T("Done","대화 종료")` |
| `Dialogue/NpcData.cs` | `displayNameKo` + `DisplayName` |
| `Dialogue/Editor/DialogueImporter.cs` | 12열 스키마(인덱스 5~11), 최소 9열, `DialogueLine{textEn,textKo}`·`Choice{labelEn,labelKo}`, Self-Check |
| `Game/PhaseLabel.cs` | 한글이면 아침/점심/저녁/새벽 + `"{day}일차 · {name}"` |
| `Interaction/Interactable.cs` | `Prompt` 전 항목 `T(en,ko)`, `InteractionPrompt.CheckIn`(idx 16) 추가 |
| `Assets/My/Data/Dialogue/sample.csv` | 12열 재작성 + 한글 74행 (§3) |
| `Assets/TextMesh Pro/Resources/TMP Settings.asset` | 기본 폰트 Galmuri, 폴백 LiberationSans SDF |
| `Assets/Scenes/InGame.unity` | `LocalizationManager` GameObject 추가, TMP_Text 16개 폰트 교체 |
| `Assets/My/InGame/Prefabs/Guest.prefab` | `promptType 9→16`, `customPrompt` 비움 |
| Docs | `LocalizationManager.md` 신규, `DialogueSystem.md`·`Interactable.md`·`PhaseLabel.md`·`Overview.md` 갱신 |

### 검증
- `uloop compile` : Success, Error 0, Warning 0.
- `FontTool.Apply()` : TMP_Text 16개 교체 + TMP Settings 설정. 씬에 non-Galmuri `m_fontAsset` 0개.
- `DialogueImporter.Import()` : CSV 74행 → 노드 49개, goto 검사 통과, 미등록 npcId 없음.
- `DialogueDatabase.asset` 에 `textEn`/`textKo`/`labelEn`/`labelKo` 정상 기록.

### 사용자 작업
1. `LocalizationManager` 오브젝트의 `Language` 를 인스펙터에서 확인/변경 (기본 English). 한글 확인하려면 `Korean` 으로.
2. Play → 접객 진입 → 대사·질문·프롬프트·시간대 라벨이 선택 언어로 나오는지 확인.
3. NPC 한글 이름이 필요하면 `Npc_*.asset` 의 `displayNameKo` 입력.
4. 새 씬/프리팹에 TMP 텍스트를 추가하면 `Tools > Localization > Apply Galmuri Font` 재실행.

## 7c. HUD 안내 텍스트 4종 로컬라이즈 (추가 요청, 2026-08-30)

> 요청: `Tip_Text`, `HowToUse_Flashlight`, `Throw_item`, `Interaction_Text` 도 번역 기능에 추가. 현재 영어인 걸 번역.

`inventory_activate` 캔버스 밑 정적 라벨 4개에 `LocalizedLabel` 부착 (`english` = 기존 문구):

| 오브젝트 | english | korean |
|---|---|---|
| `Interaction_Text` | Press E | E 키로 상호작용 |
| `HowToUse_Flashlight` | Use flashlight: Click mouse wheel | 손전등 사용: 마우스 휠 클릭 |
| `Tip_Text` | Change day/night: Q key | 낮/밤 전환: Q 키 |
| `Throw_item` | Press F to Throw Item | F 키로 아이템 던지기 |

`InGame.unity` 만 수정 (uloop dynamic code 로 컴포넌트 추가 + 필드 설정 + 저장). 컴파일 Error 0.

## 상태

2026-08-30 구현 완료 (+ HUD 안내 4종). Unity 컴파일·임포트·폰트 적용 확인. 사용자 인게임 검증 대기.
