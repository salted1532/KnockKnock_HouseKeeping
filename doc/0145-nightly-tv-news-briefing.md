# 0145 - 일차 종료 TV 뉴스 브리핑 (설계안)

날짜: 2026-09-05 (구현·배선: 2026-09-06)
상태: **코드 + 씬 배선 완료, 런타임 검증 완료. 닫는 페이드 추가(2026-09-06 2차)**
관련: `doc/0118`(새벽 노크), `doc/0119`(CRT World Canvas + RT 파이프라인), `doc/0131`(새벽 대화), `Docs/DayPhaseManager.md`, `Docs/ScreenFader.md`, `Docs/UIInteractionMode.md`, `Docs/DialogueSystem.md`

## 요청 (원문 요약)

- 각 일차가 끝날 때(새벽 → 아침 사이) TV 뉴스 연출을 넣는다.
- UI만 띄우는 대신 **연출**을 위해: 플레이어를 특정 위치로 보내고(화면 고정) 상호작용을 막는다.
- 대화창은 뜨지만 **선택지는 없음** — 설명만 듣는다.
- 화면 구성: **왼쪽 중앙 = 대화창**, **오른쪽 = 인게임 TV**. 뉴스가 그날의 상황·구별법 등을 알려준다.
- 대화를 다 보면 아침으로 넘어간다.
- 플레이어를 옮기는 장면이 **보이면 안 됨** — 암전 중 즉시 **텔레포트**.

## 현재 상태

| 조각 | 지금 |
|---|---|
| `DayPhaseManager` | 아침→점심→저녁→새벽 순환. `TransitionTo(target)` 가 `ScreenFader.FadeThrough` 경유. 암전 시 `Current` 갱신·`DayCount++`(아침 진입)·`OnPhaseChanged`, 페이드 인 후 `OnPhaseChangeFinished`. 디버그 `N`/`Q` = `Advance()` |
| 새벽 → 아침 트리거 | **확인됨**: `Owner's_Motel_Room` 안 `bed_03_Interior` 인스턴스 = `Interactable`(promptType 15) + `PhaseCondition`(Dawn) + `PhaseSwitchEffect(from=3 Dawn, to=0 Morning)` + `SfxEffect`. 침대 상호작용 → `TransitionTo(Morning)` |
| `ScreenFader` | `FadeThrough(atBlack, done)` — 암전 → 콜백 → 유지 → 밝아짐 → 콜백. 없으면 콜백 즉시 실행 (null-safe) |
| `UIInteractionMode` | `FreezeForOverlay(true)` = **이동 없이** FPC 정지 + Gaze suspend + 커서 표시. `Active` 면 무시. `TransitionTo` 가 전환 중 이걸 켰다가 `Done()` 에서 끔 |
| `SpeechBubble` | `billboard` 끄면 스크린 스페이스 대화 패널로 동작. `Show(npc, lines)` = 줄별 타이핑 + 클릭/E/Space 로 넘김. 선택지·허브 없음 (그건 `DialogueRunner`/`QuestionPanel`) |
| `CampaignData.DayPlan` | `{ day, eveningGuestIds, string nightNews }` — **`nightNews` 는 "SYS-11 취침 뉴스" 용으로 필드만 있고 미사용** |
| 인게임 TV | **없음**. CRT 모니터 프리팹(World Space Canvas + `RenderTextureGraphicRaycaster`, doc/0119)만 존재 |
| 월드 렌더 | MainCamera → RenderTexture → RawImage. 월드에 놓은 오브젝트/월드 캔버스는 그대로 화면에 나옴 (별도 RT 불필요, [[ingame-rendertexture-pipeline]]) |

핵심: 대사 타이핑·암전·플레이어 정지·커서는 **이미 다 있다**. 새로 필요한 건 (1) 뉴스가 선형 나레이션이라 분기 대화 그래프를 안 태우는 얇은 재생 경로, (2) 새벽→아침 전환 앞에 브리핑 시퀀스를 끼우는 훅, (3) 씬의 TV 오브젝트.

## 설계

> ⚠️ 아래는 최초 제안. 실제 구현은 **"## 구현" / "### 2차 변경"** 참조 (`onComplete` 콜백 제거, 닫는 페이드 자체 처리 등 차이 있음).

새 컴포넌트 **1개** + 얇은 효과 1개 + `SpeechBubble`·`CampaignData`·`DayPhaseManager` 소폭 수정. 새 "모드" 없음.

### 왜 대화 시스템(CSV/`DialogueRunner`)을 안 쓰나

뉴스는 **분기 없음 / 선택지 없음 / NPC별 데이터 없음 / 판정 없음** 인 선형 나레이션이다. `DialogueDatabase`·`Situation` enum·가짜 뉴스 NPC `NpcData`·스크립트 수정마다 CSV 재임포트 — 전부 얻는 것 없이 비용만 붙는다. 뉴스 텍스트는 `CampaignData` 에서 일차별로 관리하고(디자이너가 이미 편집하는 에셋), 타이핑 연출만 `SpeechBubble` 에서 재사용한다.

### 1. `SpeechBubble` — NPC 없는 한 줄 재생 추가

```csharp
// 기존 Show(npc, lines) 는 이걸 반복하도록 리팩터 (선택). 타이핑·넘김 로직은 그대로.
public IEnumerator ShowLine(string text)
{
    if (root != null) root.SetActive(true);
    yield return TypeLine(text);
    yield return WaitForAdvance();
}
```

- `portrait`/`nameLabel` 은 건드리지 않음 → 뉴스 패널 프리팹에선 비워두거나 고정 "속보/BREAKING" 라벨.
- 뉴스 패널 = `SpeechBubble`(billboard off) 를 왼쪽 중앙에 배치한 **별도 스크린 스페이스 패널**. 문틈 대화용 `dawnPanel` 과 다른 오브젝트.

### 2. `CampaignData.DayPlan` — 뉴스 콘텐츠 필드

```csharp
[Serializable]
public class DayPlan
{
    public int day = 1;
    public List<int> eveningGuestIds = new();

    [Header("일차 종료 뉴스 (SYS-11)")]
    [TextArea(1, 3)] public List<string> newsLinesEn = new();   // 한 줄 = 대화창 한 줄
    [TextArea(1, 3)] public List<string> newsLinesKo = new();   // 비면 En 폴백
    public List<Sprite> newsSlides = new();                     // TV 슬라이드. 줄 i → slides[i] (모자라면 마지막 유지)

    // nightNews(string) 는 제거하거나 [TextArea] 디자이너 메모로 남김
}
```

- 언어 선택: `LocalizationManager.Korean` → `newsLinesKo`, 아니면 `newsLinesEn` (`newsLinesKo` 비면 En).
- **TV 슬라이드는 글자 없는 이미지** 권장 (사진·몽타주·도표). 문구는 왼쪽 나레이션이 담당 → 슬라이드 로컬라이즈 불필요. 슬라이드에 텍스트가 꼭 필요하면 `newsSlidesEn/Ko` 로 분리.
- 슬라이드 1장이면 리스트에 1개. 줄 수와 슬라이드 수가 달라도 됨(마지막 슬라이드 유지).
- 오늘 일차에 `newsLinesEn` 이 비어 있으면 → 브리핑 스킵, 기존대로 바로 아침 전환.

### 3. `NightNewsBriefing` (신규, 씬 오브젝트 1개)

```csharp
public class NightNewsBriefing : MonoBehaviour
{
    public static bool Playing { get; private set; }

    [SerializeField] private CampaignData campaign;
    [SerializeField] private Transform briefingAnchor;      // 플레이어 텔레포트 위치/정면 (카메라가 TV를 오른쪽에 담도록 배치)
    [SerializeField] private SpeechBubble newsPanel;        // 왼쪽 중앙 스크린 패널 (billboard off)
    [SerializeField] private GameObject tv;                 // 씬의 TV 오브젝트. 시작 비활성
    [SerializeField] private Image tvImage;                 // TV 화면 Image/RawImage — 슬라이드 표시
    [SerializeField] private Transform playerRoot;          // PlayerCapsule
    [SerializeField] private Transform cameraPitchPivot;    // PlayerCameraRoot
    [SerializeField] private MonoBehaviour characterController; // 스냅 이동 전 비활성화
    [SerializeField] private AudioSource sfx;               // 선택 — 브리핑 시작 시그니처음
    [SerializeField] private AudioClip startClip;           // 선택

    // 침대 상호작용에서 호출. 콘텐츠 없으면 false 반환 → 호출측이 바로 전환.
    public bool Play(Action onComplete);
}
```

**시퀀스**

1. `Play(onComplete)` — 오늘 `DayPlan` 조회. `newsLinesEn` 비면 `false` 반환.
2. `Playing = true`. `ScreenFader.FadeThrough(AtBlack, Done)`:
   - **AtBlack (암전 중 — 여기서 텔레포트, 안 보임):**
     - `UIInteractionMode.Instance.FreezeForOverlay(true)` — FPC 정지 + Gaze suspend + 커서 표시 (상호작용 원천 차단).
     - `characterController.enabled = false` → `playerRoot` 위치·yaw 를 `briefingAnchor` 로 **스냅** → `cameraPitchPivot` 로컬 회전 0 으로 스냅. (원래 위치·회전 저장)
     - `tv.SetActive(true)`, `tvImage.sprite = slides[0]`.
     - (선택) `sfx.PlayOneShot(startClip)`.
   - **Done (페이드 인 완료):** `StartCoroutine(NarrationRoutine())`.
3. `NarrationRoutine`:
   ```
   for (int i = 0; i < lines.Count; i++) {
       if (newsSlides.Count > 0) tvImage.sprite = newsSlides[Mathf.Min(i, newsSlides.Count - 1)];
       yield return newsPanel.ShowLine(lines[i]);   // 타이핑 + 클릭/E/Space 대기
   }
   newsPanel.Hide();
   yield return Finish();
   ```
4. `Finish`:
   - `tv.SetActive(false)`.
   - `playerRoot`·`cameraPitchPivot` 원래 위치·회전 복원 → `characterController.enabled = true`.
   - `Playing = false`.
   - `onComplete()` 호출 → `DayPhaseManager.TransitionTo(DayPhase.Morning)`.
   - `TransitionTo` 가 자체 페이드로 아침 비주얼 스왑 + `DayCount++` + `Done()` 에서 `FreezeForOverlay(false)` → 플레이어는 침대 자리에서 조작 복귀 ("잠에서 깬" 느낌).

> 이중 페이드(브리핑 아웃 → 아침 전환)는 의도적 — 뉴스와 아침 사이 검은 한 박자. 싫으면 4번에서 플레이어 복원을 생략하고 `briefingAnchor` 를 아침 시작 위치로 써도 됨(그럼 `morningSpawn` 불필요).

### 4. 트리거 — `NewsBriefingEffect` (신규, 얇음)

침대의 `PhaseSwitchEffect(Dawn→Morning)` 를 이걸로 교체.

```csharp
public class NewsBriefingEffect : InteractionEffect
{
    [SerializeField] private NightNewsBriefing briefing;

    public override void Play(in InteractionContext ctx)
    {
        var mgr = DayPhaseManager.Instance;
        if (mgr == null || mgr.Current != DayPhase.Dawn || NightNewsBriefing.Playing) return;

        if (briefing == null || !briefing.Play(() => mgr.TransitionTo(DayPhase.Morning)))
            mgr.TransitionTo(DayPhase.Morning);   // 브리핑 없음/콘텐츠 없음 → 기존 동작
    }
}
```

`PhaseSwitchEffect` 는 다른 시간대 전환(아침→점심 등)에 그대로 쓰인다. 새벽→아침만 이 효과가 대신한다.

### 5. `DayPhaseManager` — 브리핑 중 디버그 키 가드

```csharp
private void Update()
{
    if (!debugAdvanceKey || Keyboard.current == null || NightNewsBriefing.Playing) return;   // ← 가드 1줄
    ...
}
```

### 6. 씬의 TV 오브젝트

- `doc/0119` CRT 모니터 프리팹을 시작점으로 복제 → **`RenderTextureGraphicRaycaster` 제거**(클릭 없음), World Space Canvas 아래 `Image`(또는 `RawImage`) 1개 = `tvImage`.
- 또는 단순 쿼드 + emissive 머티리얼 + 자식 World Canvas. 새 URP 머티리얼이면 Smoothness 0 ([[material-smoothness-default-zero]]).
- 시작 비활성, `NightNewsBriefing.tv` 로 연결. `briefingAnchor` 는 이 TV가 화면 **오른쪽**에, 왼쪽엔 뉴스 패널 공간이 남도록 카메라 정면/거리 조정해 배치.
- 월드 오브젝트라 MainCamera 가 같이 그림 → RT 파이프라인에 자동 반영, 별도 RenderTexture 불필요.

## 변경 요약

| 파일 | 변경 | 규모 |
|---|---|---|
| `Dialogue/SpeechBubble.cs` | `ShowLine(text)` 추가 (기존 `Show` 가 재사용하도록 리팩터 선택) | ~6줄 |
| `Game/CampaignData.cs` | `DayPlan` 에 `newsLinesEn/Ko`, `newsSlides`. `nightNews` 제거/메모화 | ~4줄 |
| `Game/NightNewsBriefing.cs` | **신규** — 시퀀스·텔레포트·슬라이드 동기·복원 | ~90줄 |
| `Interaction/Effects/NewsBriefingEffect.cs` | **신규** — 침대 트리거 | ~15줄 |
| `Game/DayPhaseManager.cs` | `Update` 에 `NightNewsBriefing.Playing` 가드 | 1줄 |
| 데이터/씬 | TV 오브젝트, 뉴스 패널(`SpeechBubble`), `briefingAnchor`, `NightNewsBriefing` 배치·연결, 침대 효과 교체, `CampaignData` 에 일차별 뉴스 문구·슬라이드 입력 | 사용자 |

## 스킵 (YAGNI)

- **동영상 TV** (`VideoPlayer` + 클립) — 정지 슬라이드로 충분. 특정 일차에 모션 필요하면 그때 `tvImage` 를 `VideoPlayer` 타깃으로.
- **CSV / `DialogueDatabase` / `Situation.News` / 뉴스 NPC** — 선형 나레이션엔 분기 그래프가 필요 없음.
- **뉴스 앵커 얼굴 초상화** — 원하면 뉴스 패널 프리팹에 고정 스프라이트 1장. 코드 불필요.
- **ESC 로 스킵 / "이미 본 날" 스킵** — 요청은 "다 보게" 임. 재플레이가 번거로워지면 그때 `Playing` 중 ESC 훅 추가.
- **아침 전용 복귀 스폰(`morningSpawn`)** — 침대 자리 복원으로 충분. 그 자리가 부적절할 때만 추가.
- **행동력·판정 연동** — 뉴스는 정보 전달만. 밤 판정은 별도.

## 구현 (2026-09-06)

승인된 범위: 새벽 → 침대 상호작용 → 페이드 아웃 → 앵커로 순간이동(화면고정·상호작용 차단) → 대화창 나레이션 → 아침. TV 슬라이드도 코드에 포함(선택, 미배선 시 무시).

| 파일 | 변경 |
|---|---|
| `Dialogue/SpeechBubble.cs` | `IEnumerator ShowLine(string)` 추가 — NPC 없는 한 줄 타이핑 + 넘김 대기 (초상화 off) |
| `Game/CampaignData.cs` | `DayPlan` 에 `newsLinesEn` / `newsLinesKo` (List<string>) + `newsSlides` (List<Sprite>). `nightNews` 는 메모용으로 유지 |
| `Interaction/Modes/UIInteractionMode.cs` | `FreezeForOverlay(bool, Transform anchor)` 오버로드 — `anchor` 주면 CC 끄고 그 위치/정면으로 **즉시 순간이동**(전환 애니 없음), 해제 시 원위치 복원. 중복 `on` 호출 무시(`if (on && FrozenForOverlay) return;`). 기존 `FreezeForOverlay(bool)` 은 `(on, null)` 위임 |
| `Game/NightNewsBriefing.cs` | **신규**. `static bool Playing`. `Play(Action onComplete)` → 오늘 `DayPlan` 뉴스 없으면 `false`. 시퀀스: `ScreenFader.FadeThrough(atBlack: FreezeForOverlay(true, briefingAnchor)+TV on, done: 나레이션)` → 줄마다 `newsPanel.ShowLine` + TV 슬라이드 → 끝 → TV off + `Playing=false` + `onComplete()`. 언어: `LocalizationManager.Korean` && Ko 리스트 있으면 Ko, 아니면 En |
| `Interaction/Effects/NewsBriefingEffect.cs` | **신규**. `Current==Dawn` && `!Playing` 확인 → `briefing.Play(() => TransitionTo(Morning))`, 브리핑 없음/콘텐츠 없음이면 `TransitionTo(Morning)` 직행 |
| `Game/DayPhaseManager.cs` | `Update` 디버그 키 가드에 `|| NightNewsBriefing.Playing` |

**전환 흐름**: 브리핑이 `FreezeForOverlay(true, anchor)` 로 순간이동·정지 → 나레이션 → `TransitionTo(Morning)` → 그 안의 `FreezeForOverlay(true)` 는 no-op(중복 가드) → 아침 페이드 → `Done()` 의 `FreezeForOverlay(false)` 가 CC 재활성 + **플레이어 원위치 복원**(침대 자리 = "잠에서 깬"). 이중 페이드(브리핑 아웃 → 아침 전환) 의도적.

**검증**: `uloop compile` Error 0 (기존 warning 1건 무관). 플레이 검증은 씬 배선 후.

### 씬 배선 (2026-09-06 완료, uloop)

| # | 한 것 |
|---|---|
| 1 | `Owner's_Motel_Room/bed_03_Interior` (씬 인스턴스): `PhaseSwitchEffect`(3→0) 제거 + `NewsBriefingEffect` 추가 (프리팹 오버라이드, `InGame.unity` 에 저장). `Interactable`·`PhaseCondition`(Dawn)·`SfxEffect` 유지 |
| 2 | 씬 루트에 `NightNewsBriefing` GameObject 생성 — `NightNewsBriefing` + `SpeechBubble`(billboard off) 컴포넌트 |
| 3 | `Canvas/News_Panel` 생성 — `Canvas/Dialogue_Panel` 복제 → `Button_Horizontal` 제거, `npc_name` 비활성, RT 좌측 중앙 앵커(anchorMin/Max=(0,0.5), pivot=(0,0.5), anchoredPos=(48,0)) |
| 4 | 연결: `SpeechBubble.root`→News_Panel, `.label`→Dialogue_Text, `.billboard`=false / `NightNewsBriefing.campaign`→Campaign.asset, `.newsPanel`→그 SpeechBubble / `NewsBriefingEffect.briefing`→NightNewsBriefing |
| 5 | **미연결(의도)**: `briefingAnchor`(사용자 배치), `tv`·`tvImage`(선택) |

### 2차 변경 — 닫는 페이드 (2026-09-06)

사용자 요청: "TV 화면 끝나고도 페이드 아웃 되고 화면 검게 되고 텔포". 기존엔 나레이션 끝 → `TransitionTo(Morning)` 의 자체 페이드였는데, 플레이어 원위치 복원이 페이드 **인 완료 후**(`Done()`)라 화면이 밝은 상태에서 순간이동이 보였음.

| 파일 | 변경 |
|---|---|
| `DayPhaseManager.cs` | `TransitionTo(DayPhase, bool fade)` 오버로드. `fade=false` 면 페이드·`FreezeForOverlay` 없이 `DayCount++`·`Current`·이벤트만. 기존 `TransitionTo(target)` = `(target, true)` 위임 (다른 호출부 영향 없음) |
| `NightNewsBriefing.cs` | `Play()` 가 콜백 안 받음 (스스로 아침 전환). `Run` 끝에 **닫는 `Fade()`** 추가 — 암전 중 `tv` 끄기 + `FreezeForOverlay(false)`(원위치 복원) + `TransitionTo(Morning, false)`. 아침 페이드 인은 이 `Fade` 가 담당 |
| `NightNewsBriefing.Fade()` | 소프트락 방지 강화: 앞선 페이드 최대 2초 대기 → `FadeThrough` → `done` 최대 3초 대기 → 안 오면 `atBlack()` 직접 (페이더 이상 시에도 상태는 넘어감) |
| `NewsBriefingEffect.cs` | `briefing.Play()` (콜백 인자 제거) |

닫는 흐름: 마지막 줄 클릭 → **페이드 아웃 → 검은 화면** → (암전 중) TV 끄기 + 플레이어 원위치 순간이동 + 아침 단계 전환(DayCount++/조명 스왑) → **아침으로 페이드 인**. 순간이동이 화면에 안 보임.

### 4차 — 새벽 진입 문구 `PhaseMessage` (2026-09-06)

저녁(접객) 종료 → 새벽 전환 페이드 인 완료 직후 화면 중앙에 문구 1회.

| 파일 | 변경 |
|---|---|
| `Game/PhaseMessage.cs` | **신규** — `[phase, messageEn, messageKo]`. `DayPhaseManager.OnPhaseChangeFinished` 구독 → `phase` 일치 시 `ScreenMessage.Show`. `PhaseLabel` 과 동일 구독 패턴. 한 오브젝트에 여러 개 가능(시간대별) |
| `InGame.unity` | `GameManager` 에 `PhaseMessage` 추가 — `phase=Dawn`, KO `"손님은 다 온 것 같다. 이제 조용한 시간이다."` / EN `"That looks like the last of the guests. The quiet hours now."` |

검증: `OnPhaseChangeFinished(Dawn)` 직접 발동 → `ScreenMessage.label` 에 KO 문구 정상 세팅 확인 (`subs=1`). `uloop compile` Error 0.

새벽 문구 2개 순서: 진입 시 `PhaseMessage`("손님은 다 온 것 같다…") → 나중에 행동력 소진 시 침대 `ObjectiveMarker`("너무 늦었다…").

### 3차 — 새벽 종료 안내 문구 (2026-09-06)

침대(`bed_03_Interior`)의 `ObjectiveMarker` 는 행동력 소진(→ `ActionPointsDepletedCondition.IsMet`)으로 침대가 상호작용 가능해질 때 `ScreenMessage` 를 1회 띄운다. 컨셉(몽유병·새벽 이동 위험)에 맞게 문구 교체 — 씬 데이터만.

| | 기존 | 신규 |
|---|---|---|
| KO | 오늘 손님은 여기까지인 것 같다. 이제 자러 가자. | 너무 늦었다. 더 돌아다니는 건 위험하다 — 자러 가야 한다. |
| EN | That looks like all the guests for tonight. Time to sleep. | It's too late. Wandering any further is dangerous. Time to sleep. |

### 런타임 검증 (Play 모드, uloop)

| 케이스 | 결과 |
|---|---|
| 콘텐츠 없음 → `NightNewsBriefing.Play()` | `false` 반환, `NewsBriefingEffect` 가 `TransitionTo(Morning)` 직행 (fallback) ✓ |
| 아침에 침대 상호작용 | `Current!=Dawn` 가드로 무시 (전환 없음) ✓ |
| 새벽 + 콘텐츠 + 침대 상호작용 | `Playing=true` + `ScreenFader` 여는 페이드 ✓ |
| 여는 페이드 완료 후 | `FrozenForOverlay=true`(정지·상호작용 차단), `News_Panel` 활성, 첫 줄(한글) 타이핑, **플레이어가 `briefingAnchor` 로 순간이동**(`(-14.2,0.1,18.0)`→`(7.6,0,50.3)`) ✓ |
| `TransitionTo(Morning, false)` 직접 호출 | `Dawn→Morning`, `DayCount 1→2`, **`alpha 0→0`(페이드 없음)**, `Transitioning=false` ✓ |
| 나레이션 완료 → 아침 (초기 버전, 닫는 페이드 전) | `DayCount 1→2`, `Dawn→Morning`, `FrozenForOverlay` 해제, `Playing=false`, 플레이어 원위치 복원 ✓ |

닫는 `Fade()` 는 여는 `Fade()` 와 동일 헬퍼 + `TransitionTo(Morning,false)`(위에서 개별 검증) 조합이라, 각 조각은 검증됨. 동시 편집 세션이 Play 세션을 계속 도메인 리로드시켜 최종 end-to-end 캡처(닫는 페이드 중 alpha>0)는 못 남김 — 다음 안정 세션에서 실플레이 확인 권장.

`uloop compile` Error 0. `briefingAnchor`·`tv` 는 다른 세션이 배치 완료(현재 씬에 `GameObject (1)`·`TV_01` 연결됨).

> ⚠️ 배선 중 다른 세션이 동시에 프로젝트 편집 중이었음(커밋 `f3d34cfe`, ActionPoints 스크립트). uloop 의 `AssetDatabase.SaveAssets()` 가 그 세션의 미저장 프리팹 변경(`Owner's_Motel_Room.prefab` 의 `ActionPointsDepletedCondition`, `Can_Coke.prefab`)도 디스크에 함께 flush 함 — 작업 손실은 없으나 커밋 분리 시 주의.

### 남은 작업 (사용자)

1. ~~`briefingAnchor` 배치~~ — 다른 세션이 `GameObject (1)` 로 연결함. 카메라 프레이밍(왼쪽 패널 / 오른쪽 TV) 확인·조정.
2. `CampaignData` (`Assets/My/Scripts/Dialogue/Campaign.asset`) 각 `DayPlan` 에 `newsLinesEn`/`newsLinesKo` 입력. 현재 Day 1 만 존재 — Day 2+ 는 `DayPlan` 자체가 없음(추가 필요).
3. ~~TV 오브젝트~~ — 다른 세션이 `TV_01` 로 연결함. `newsSlides` 이미지만 입력하면 됨.
4. `News_Panel` 위치·크기 취향대로 조정.
5. 안정된 Play 세션에서 실제 새벽→침대→브리핑→아침 1회 플레이 확인 (닫는 페이드 시각 확인).

### (원 설계) 씬 배선 참고

1. `Owner's_Motel_Room` / `bed_03_Interior` 인스턴스: `PhaseSwitchEffect`(from 3→to 0) 제거 → `NewsBriefingEffect` 추가. `Interactable`(promptType 15) · `PhaseCondition`(Dawn) · `SfxEffect` 유지.
2. 씬에 빈 GameObject `NightNewsBriefing` → 컴포넌트 붙이고 연결: `campaign`(CampaignData 에셋), `briefingAnchor`(새 Transform — 카메라가 왼쪽=대화창/오른쪽=TV 되게 배치), `newsPanel`(왼쪽 중앙 스크린 `SpeechBubble`, billboard off, `root`/`label` 연결), TV 쓸 거면 `tv`(시작 비활성)·`tvImage`.
3. `NewsBriefingEffect.briefing` ← 그 GameObject.
4. `CampaignData` 각 `DayPlan` 에 `newsLinesEn`/`newsLinesKo` 문구, (선택) `newsSlides` 이미지 입력. 비어 있으면 그 날은 브리핑 스킵 → 기존처럼 바로 아침.
5. `UIInteractionMode` 의 `playerRoot`·`cameraPitchPivot`·`characterController` 가 채워져 있어야 순간이동 동작 (접객용으로 이미 연결돼 있음).

## 확인 필요 (승인 전 → 일부 미해결, 씬 작업 중 판단)

1. ~~새벽→아침 트리거 위치~~ — 확인됨 (`Owner's_Motel_Room` / `bed_03_Interior`). 이 인스턴스의 `PhaseSwitchEffect` 를 `NewsBriefingEffect` 로 교체(`PhaseCondition` Dawn 은 유지).
2. TV = **월드 오브젝트**(권장, 연출 일관) vs 오른쪽 반쪽 스크린 UI 패널(CRT 베젤 스프라이트). 후자면 TV 오브젝트/`briefingAnchor` 카메라 조정 불필요, `NightNewsBriefing` 이 패널만 토글.
3. 이중 페이드 허용 vs 브리핑 끝에서 바로 아침(플레이어 복원 생략, `briefingAnchor` = 아침 시작 위치).
4. `nightNews`(string) 기존 필드 — 제거해도 되는지 (아무 데서도 안 읽음).
5. TV 슬라이드에 텍스트를 넣을 것인지 (넣으면 `newsSlidesEn/Ko` 분리).
