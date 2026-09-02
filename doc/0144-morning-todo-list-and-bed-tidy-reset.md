# 0144 - 아침 할일 리스트 UI + 침대 tidy/messy 리셋 (제안)

날짜: 2026-09-03
관련: `doc/0132`(체크아웃/아침 청소 개방·messyObjects/tidyObjects), `doc/0133`(아침 태스크·게시판 점심 전환), `Assets/My/Scripts/Interaction/RoomController.cs`, `Assets/My/Scripts/Game/DayPhaseManager.cs`

## 요청 (원문)

> 게임 시작시 RoomController로 모든 방들의 침대가 다 tidy 되어있도록하고
> 손님 받고 나서 손님 지정된 방들이 아침에 침대 어지럽혀있도록 해줘
> 그리고 아침의 오른쪽 위에 todo list가 생기도록 해줄래
> 토글 박스처럼 "□ 침대 개기 0/6" 뭐 이런식으로 나타나도록 해주고
> 할일 다 끝나면 게시판 상호작용하면 점심으로 가도록 내가 게시판 추가할거야
> 아침에 할일 리스트 오른쪽 위에 UI로 보이도록 추가해줘

## 현재 상태

| 조각 | 지금 |
|---|---|
| 침대 상태 | `RoomController.messyObjects`(Bed_02, 초기 활성) / `tidyObjects`(Bed_01, 초기 비활성). **게임 시작 = 흐트러짐** |
| messy 전환 | `Apply(phase)` 가 `roomOpenForCleaning`(체크아웃 아침 or 하우스키핑 아침)일 때만 `SetMessy()`. 그 외 단계엔 침대 안 건드림 → 한번 messy 면 계속 messy |
| 침대 정리 | 방 안 `Bed` 의 `Interactable(CleanUp)` + `ChangeObjectEffect`(onObjects=tidy, offObjects=messy). 1회 실행 = tidy |
| 아침→점심 전환 | `doc/0133` 권장안: 게시판 `Interactable(EndMorning)` = 자동으로 `PhaseSwitchEffect(Morning→Noon)` + `PhaseCondition(Morning)`. **할일 완료 게이트 미구현** (`TasksCompleteCondition` 예정) |
| 할일 UI | 없음 |

## 설계

새 스크립트 2개 + `RoomController` 소폭 수정. 새 매니저 SO 없음.

### A. `RoomController` — 시작 tidy + 청소 아침만 messy

`Apply(phase)` 끝의 침대 처리를 양방향으로:

```csharp
// 아침 청소 창이면 흐트러진 상태, 아니면(=평소·시작 포함) 정리된 상태.
if (roomOpenForCleaning) SetMessy(); else SetTidy();

openForCleaning = roomOpenForCleaning;   // 신규 필드 — 할일 카운터가 읽음
```

- `SetTidy()` = `SetMessy()` 반대 (tidyObjects on, messyObjects off). 5줄.
- `Apply` 는 `Start()` 에서 1회 자동 실행되므로 **게임 시작 = 모든 방 tidy** (Day1 아침엔 손님 0 → 전부 `else` 분기). 프리팹 초기 활성값은 손 안 대도 됨 (첫 프레임에 교정, 씬 로드 페이드에 가려짐).
- 손님 받은 방은 기존대로 체크아웃/하우스키핑 아침에 `roomOpenForCleaning == true` → messy.
- 신규 공개 프로퍼티: `public bool NeedsCleaning => openForCleaning;`, `public bool BedMade` (messyObjects 가 전부 비활성이면 true).

### B. `MorningTasks` (신규) — 카운터 + 오른쪽 위 UI + 완료 게이트

`Assets/My/Scripts/Game/MorningTasks.cs`. 씬 HUD Canvas 의 우상단 패널 오브젝트에 붙인다.

```csharp
public class MorningTasks : MonoBehaviour
{
    public static MorningTasks Instance { get; private set; }

    [SerializeField] private GameObject panel;   // 우상단 할일 패널 루트 (아침에만 켜짐)
    [SerializeField] private TMP_Text line;      // "□ 침대 개기 3/6"

    private RoomController[] rooms;

    public bool AllDone =>
        DayPhaseManager.Instance != null
        && DayPhaseManager.Instance.Current == DayPhase.Morning
        && Made >= Total;

    private int Total { get { int n = 0; foreach (var r in Rooms()) if (r.NeedsCleaning) n++; return n; } }
    private int Made  { get { int n = 0; foreach (var r in Rooms()) if (r.NeedsCleaning && r.BedMade) n++; return n; } }

    private RoomController[] Rooms() =>
        rooms ??= FindObjectsByType<RoomController>(FindObjectsSortMode.None);

    private void Awake() => Instance = this;

    private void Update()
    {
        bool morning = DayPhaseManager.Instance != null
                       && DayPhaseManager.Instance.Current == DayPhase.Morning;
        if (panel != null) panel.SetActive(morning);
        if (!morning || line == null) return;

        int total = Total, made = Made;
        string box = made >= total ? "☑" : "☐";   // ☑ / □
        line.text = $"{box} {LocalizationManager.T("Make beds", "침대 개기")} {made}/{total}";
    }
}
```

- 폴링 방식 (매 프레임 10방 스캔). 이벤트 배선·`Bed` 쪽 수정 0. 방 개수 규모에서 무시할 비용.
  `ponytail: 매 프레임 FindObjectsByType 캐시 + 10방 순회, 방이 수백개 되면 이벤트로.`
- 할일 항목은 지금은 "침대 개기" 하나. 항목이 늘면 `line` 대신 항목별 TMP 행을 배열로.
  `ponytail: 태스크 1종, 다종 되면 행 배열로.`
- `Total == 0`(청소할 방 없음) → `Made(0) >= Total(0)` → `AllDone == true` → 게시판 바로 통과.

### C. `TasksCompleteCondition` (신규) — 게시판 게이트

`Assets/My/Scripts/Interaction/Conditions/TasksCompleteCondition.cs`. `AwaitingCheckInCondition` 과 동일 패턴 (9줄).

```csharp
public class TasksCompleteCondition : InteractionCondition
{
    public override bool IsMet =>
        MorningTasks.Instance != null && MorningTasks.Instance.AllDone;
}
```

- 게시판 `Interactable` 에 이 컴포넌트를 얹으면 할일 전 완료 전까진 상호작용/아웃라인/프롬프트가 안 뜬다.
- 미완료 시 "남은 할일 목록 표시"(ShowPanelEffect) 는 이번 범위 밖 — 우상단 UI 가 이미 `3/6` 를 상시 표시하므로 생략. 원하면 후속.

## 영향 파일

```
Interaction/RoomController.cs                수정  SetTidy() + else 분기 + NeedsCleaning/BedMade 프로퍼티 + openForCleaning 필드
Game/MorningTasks.cs                         신규  카운터 + 우상단 UI + AllDone
Interaction/Conditions/TasksCompleteCondition.cs  신규  게시판 게이트
Docs/RoomController.md                       갱신
```

새 매니저 SO 0개. `Bed` / `ChangeObjectEffect` / `GuestManager` / `DayPhaseManager` 수정 0.

## 사용자 작업 (씬/에셋)

1. **우상단 할일 UI**: InGame HUD Canvas 밑에 패널 오브젝트(우상단 앵커) + 그 안에 TMP_Text 1개.
   패널에 `MorningTasks` 붙이고 `panel`(자기 자신 또는 배경 이미지 루트), `line`(TMP_Text) 연결.
   → 원하면 uloop 로 내가 만들어 배선 가능.
2. **게시판** (사용자가 추가 예정): `Interactable` promptType = `EndMorning` → 우클릭 "효과 재설정"
   하면 `PhaseSwitchEffect(Morning→Noon)` + `PhaseCondition(Morning)` + `SfxEffect` 자동.
   여기에 `TasksCompleteCondition` 컴포넌트 하나 더 추가.
3. (선택) `Bed.prefab` 초기 활성값을 tidy 로 뒤집기 — A 로 첫 프레임 교정되므로 필수 아님.

## 검증 (플레이)

- 게임 시작(Day1 아침): 모든 방 침대 tidy. 우상단 `☑ 침대 개기 0/0` (청소할 방 없음). 게시판 바로 통과.
- Day1 저녁 손님 N명 배정 → Day2 아침: 그 방들 침대 messy, 우상단 `□ 침대 개기 0/N`.
- 침대 하나 정리할 때마다 카운트 증가, 다 하면 `☑ N/N`.
- 완료 전 게시판: 아웃라인/프롬프트 안 뜸. 완료 후: "근무 종료" 프롬프트 → 상호작용 → 점심 전환.
- `npx uloop-cli compile` Error 0.

## 스킵 (YAGNI)

- 미완료 시 게시판이 "남은 할일" 패널 표시 — 우상단 UI 가 상시 표시라 불필요.
- 태스크 다종화(열쇠 회수·장부 대조, `doc/0133` 1+2+3) — 지금은 침대 개기 1종. 행 배열로 확장.
- 방별 청소 품질 채점 / 안 치우고 넘어갈 때 페널티.
- 태스크 상태 저장/로드.
- 침대 외 청소 대상(쓰레기 줍기) 카운트 — `BedMade` 대신 방별 "clean" 플래그로 일반화할 때 함께.

## 추가 요청 (2026-09-03) — 승인 + 방당 침대 2개, 랜덤 1~2개

> 게시판 추가했고 각 방마다 침대가 2개인데 2개중 랜덤하게 1~2개 침대 갤수 있도록해줘. 구현해줘

- 게시판 = `Owner's_Motel_Room/White_Board` (Interactable EndMorning + PhaseSwitch Morning→Noon + PhaseCondition + SfxEffect, 사용자가 배치 완료).
- `SetMessy()` 를 "전부 흐트러뜨림" → "랜덤 1~2개" 로. `messyObjects[i]`/`tidyObjects[i]` 를 같은 침대의 쌍으로 취급 (인덱스 정렬 전제).
- 할일 카운터는 방 단위가 아니라 **침대 단위** 합산: `☐ 침대 개기 (갠 침대)/(흐트러진 침대)`.

## 구현 (2026-09-03) — `uloop compile` Error 0

| 파일 | 변경 |
|---|---|
| `Interaction/RoomController.cs` | `messyTargetCount` 필드 + `MessyTotal`/`MessyRemaining`/`MessyDone` 프로퍼티. `SetMessy()` = Fisher-Yates 로 침대 인덱스 섞어 `Random.Range(1, beds+1)` 개만 흐트러뜨림, 나머지는 tidy. `SetTidy()` 신규 (전부 tidy, count=0). `Apply` 끝: `if (roomOpenForCleaning) SetMessy(); else SetTidy();` — 시작·평소 단계에 자동 tidy |
| `Game/MorningTasks.cs` | 신규. 우상단 HUD, 매 프레임 `FindObjectsByType<RoomController>` 캐시 후 `MessyTotal`/`MessyDone` 합산. `☐/☑ 침대 개기 made/total`. 아침에만 `panel` 토글 (컴포넌트는 항상 켜진 오브젝트에). `AllDone` = 아침 && made>=total |
| `Interaction/Conditions/TasksCompleteCondition.cs` | 신규. `IsMet => MorningTasks.Instance.AllDone` (9줄) |
| `Scenes/InGame.unity` | `White_Board` + `TasksCompleteCondition`. `Canvas/MorningTasks/TodoPanel/Line` UI 생성·배선 (uloop) |

### 침대 쌍 전제

`RoomController.messyObjects` / `tidyObjects` 는 **같은 인덱스 = 같은 침대**. `Motel_Room.prefab`
현재 배선(messy=[Bed/Bed_02, Bed(1)/Bed_02], tidy=[Bed/Bed_01, Bed(1)/Bed_01])이 이미 정렬돼 있음.
침대를 늘리면 두 배열에 같은 순서로 추가.

## 검증 (플레이) — 대기

- 게임 시작: 전 방 tidy, 우상단 `☑ 침대 개기 0/0`, 게시판 바로 통과.
- 손님 N명 → 다음 아침: 그 방들에서 랜덤 1~2개 침대만 messy. 우상단 총합 `☐ 침대 개기 0/M` (M = Σ 랜덤).
- 침대 하나 정리 → 카운트+1. 전부 정리 → `☑ M/M` → 게시판 "근무 종료" 프롬프트 → 점심.

## 추가 요청 (2026-09-03) — 완료 시 안내 + 게시판 외곽선 유도

> 침대 개기 다 하고 나면 "게시판에서 할일을 확인하세요" 문구가 중앙에 출력되고(노트 거절 문구 재활용)
> 게시판의 외곽선이 활성화되도록(방 스위치처럼). 외곽선이 벽 너머에서도 보이도록 할 수 있나?

### 구현 (2026-09-03) — `uloop compile` Error 0

| 파일 | 변경 |
|---|---|
| `Game/MorningTasks.cs` | 아침 단계 진입 감지(`lastPhase`) + `sawIncomplete`/`announced` 플래그. 이번 아침에 미완료를 본 적 있고(청소할 게 있었고) 방금 전부 완료 → `ScreenMessage.Show("Check the notice board for your tasks", "게시판에서 할일을 확인하세요")` 1회. 청소 0개인 아침엔 안 뜸 |
| `Interaction/OutlineWhileInteractable.cs` | 신규. `Interactable.CanInteract` 인 동안 `Outline.enabled = true` (LateUpdate 에서 Interactor 의 호버 해제 뒤 다시 켬). `OutlineWhenOff` 의 조건 버전 |
| `Scenes/InGame.unity` | `White_Board` + `OutlineWhileInteractable`. `White_Board` 의 `Outline.outlineMode` → `OutlineAll`(0) = 벽 너머로도 보임(ZTest Always). `Outline.enabled` 초기 false |

- 게시판 `CanInteract` = `PhaseCondition(Morning)` + `TasksCompleteCondition(AllDone)` → 아침에 할일 다 끝난 순간부터 외곽선 상시 ON, 상호작용해서 점심 넘어가면 (PhaseCondition 불만족) OFF.
- QuickOutline 머티리얼은 Outline 마다 인스턴스화 → `OutlineAll` 은 게시판에만 적용, 다른 아웃라인 영향 없음.

## 추가 요청 (2026-09-03) — 모든 페이즈 전환 오브젝트에 노란 벽너머 비콘

> 각 페이즈 넘어가는 effect 스크립트가 있는 오브젝트는 외곽선 벽너머로 + 노란색.
> 아침→침대 다 개면 게시판 / 점심→(할일 미구현)→접객 테이블 / 저녁→접객 끝나면 자동 새벽 / 새벽→행동력 소진→주인방 침대.

### 현재 `PhaseSwitchEffect` 오브젝트 (씬)

| 오브젝트 | 전환 | 게이트(CanInteract) |
|---|---|---|
| `White_Board` | Morning→Noon | `PhaseCondition(Morning)` + `TasksCompleteCondition` ✅ |
| `Motel_Table` | Noon→Evening | `PhaseCondition` 만. **점심 태스크 게이트 미구현** — 지금은 점심 내내 비콘 ON. 태스크 시스템 생기면 Condition 추가 |
| `bed_03_Interior` | Dawn→Morning | `PhaseCondition` 만. **행동력 소진 게이트 미구현** — 지금은 새벽 내내 비콘 ON. 행동력 시스템 생기면 `ActionPointsCondition` 추가 |

- Evening→Dawn 은 상호작용 없음 — `ReceptionManager` 가 접객 종료 시 `DayPhaseManager.Advance()` (이미 구현, `ReceptionManager.cs:400`).

### 구현 (2026-09-03) — uloop 씬 배선

3개 `PhaseSwitchEffect` 오브젝트 각각:
- `Outline.outlineMode` → `OutlineAll` (벽 너머로 보임)
- `Outline.outlineColor` → `Color.yellow` `(1, 0.921, 0.016)`
- `Outline.enabled` = false (초기)
- `OutlineWhileInteractable` 추가 → `CanInteract` 인 동안 외곽선 ON

**새 페이즈 전환 오브젝트 추가 시**: 위 4가지를 똑같이 해줘야 함 (자동 아님). `ponytail:` — 오브젝트 3개뿐이라 공통 컴포넌트/에디터 훅 대신 1회 배선.

## 외곽선이 흰색으로만 나오는 버그 (2026-09-03)

> 화면에서 외곽선 흰색 말고 다른 색이 표현이 안됨. 두께·색 바꿔도 흰색.

### 진단

- 씬/머티리얼 색은 정상 (`Outline.outlineColor` = 노랑, `outlineFillMaterial` 필드 인스턴스도 노랑).
- 플레이 중 **렌더러에 실제 붙은** Fill 머티리얼에 직접 `SetColor` → 색 반영됨(빨강→빨강). 셰이더·SRP Batcher·포스트프로세스·컬러스페이스 전부 무관.
- **원인**: `Outline.cs` `OnEnable` 의 `renderer.materials = materials.ToArray()` 세터가 머티리얼을
  **복제**함. 그런데 `UpdateMaterialProperties()` 는 복제본이 아니라 `outlineFillMaterial`/`outlineMaskMaterial`
  **필드 인스턴스**에만 `SetColor`/`SetFloat`. → 렌더되는 복제본은 `OutlineFill.mat` 애셋 기본값
  (`_OutlineColor (1,1,1,1)` 흰색, width 2)에 고정. width·ZTest 는 기본값이 우연히 맞아 안 보였을 뿐.
- 추가: `OnEnable` 이 `needsUpdate=true` 를 안 세팅 → 재활성화 때 새 복제본에 프로퍼티 재적용 안 됨.

### 수정 — `Outline.cs` LOCAL PATCH (doc/0144), [[project_quickoutline-local-patch]]

- `liveMaskMaterials`/`liveFillMaterials` 리스트 필드 추가.
- `OnEnable`: `renderer.materials` 세팅 후 `renderer.sharedMaterials`(복제 안 하는 게터)에서 outline 셰이더
  머티리얼을 골라 리스트에 캐시. 말미에 `needsUpdate = true`.
- `OnDisable`: 참조(`Remove(field)`)가 아니라 셰이더로 `RemoveAll` (복제본이라 참조 안 맞음). 리스트 clear.
- `UpdateMaterialProperties`: switch 를 `ApplyProperties(mask, fill)` 헬퍼로 추출 → 필드 인스턴스 + live 복제본
  전부에 적용. 모드별 ZTest/width 매핑은 원본과 동일.
- `OutlineFill.shader` 의 `COLOR→TEXCOORD0` 시도는 오진 — 원복함.

### 검증 (플레이, uloop RT 픽셀 diff)

- 수정 전: 외곽선 픽셀 619개 전부 `RGBA(0.62,0.62,0.62)` 중립(흰색).
- 수정 후: 621개 전부 `RGBA(0.625, 0.567, 0.0)` — R≈G, **B=0** = 노란색. whitish 0개.
- `liveFillMaterials[0]` ≡ 렌더러의 실제 fill 인스턴스 (`ReferenceEquals`), `_ZTest=8`(Always, 벽 너머) 정상.
- 색은 "enable 다음 프레임"에 적용 (스톡 QuickOutline 과 동일 타이밍).

## 외곽선이 RawImage(PxlCrush) 통과 시 흰색 (2026-09-03)

> Scene 뷰에선 노랑인데 RawImage(게임 화면)로 넘어가면 흰색.

- 월드: `MainCamera → Posterize RT(1280x720, m_SRGB:0) → Canvas/RawImage(머티리얼 = PxlCrush 셰이더그래프)`.
  HUD 캔버스는 ScreenSpaceOverlay — RawImage 만 크러시되고 나머지 UI(MorningTasks·돈)는 멀쩡.
- **근본 원인 = PxlCrush 의 팔레트 스냅.** 스윕(uloop, 팔레트 6종 × 양자화 128/256 × 디더 0):
  - `_Quantization_Level`, `_Dither_Opacity` → 이 문제에 영향 0 (Posterize·Divide·Colorspace 아님).
  - 팔레트에 채도 높은 노랑(1,0.92,0.016) 근처 색이 없으면 최근접 = 흰색으로 스냅.
  - 팩 팔레트 중 노랑을 살리는 건 "Palette 3" 뿐 — 대신 씬 전체가 세피아로 웜시프트 (부작용).
- **추가 발견**: `Pxl Crush Material.mat` 의 `_Palette` guid `7bc00b71…` 가 프로젝트에 **없음(missing 참조)**.
  → 셰이더가 기본 텍스처로 샘플 → 밝은 색이 흰색으로. (이전 조사 에이전트가 머티리얼을 Palette4/128/5/0.3
  로 바꿔놓고 미복구 — 디스크 커밋 상태는 q64/s2/d0.25.)

### 결정 (2026-09-03): 방향 A — 3D 외곽선 비콘 폐기, HUD UI 마커로

이유:
- PxlCrush 팔레트가 애초에 미할당(가져온 셰이더, `_Palette` = missing). 팔레트를 물리면 세계 색감이 바뀜.
- 외곽선 여러 개를 각기 다른 색으로 살리려면 팔레트에 색을 하나씩 넣어야 함 — 확장성 나쁨, 월드 색 오염 위험.
- 유도 힌트는 원래 HUD 레이어 소관 → 크러시 밖에서 그리는 게 맞음.

**구현:**
| 파일 | 변경 |
|---|---|
| `UI/ObjectiveMarker.cs` | 신규. `Interactable.CanInteract` 인 동안 HUD(ScreenSpaceOverlay Canvas)에 색 다이아몬드 마커 표시. `Camera.main.WorldToViewportPoint` → 캔버스 좌표. 화면 밖이면 가장자리 클램프 + 꼭짓점이 방향 가리킴. 색은 오브젝트별 인스펙터. |
| `Interaction/OutlineWhileInteractable.cs` | **삭제** (비콘 전용이었음). git 에 남음. |
| `Scenes/InGame.unity` | White_Board/Motel_Table/bed_03_Interior: `OutlineWhileInteractable` 제거, `Outline` 원복(OutlineVisible·흰색·enabled off), `ObjectiveMarker` 추가 (색: 게시판 노랑 / 테이블 시안 / 침대 마젠타). `Outline.outlineColor` OutlineAll 도 원복. |

- `Outline.cs` 색 버그 패치(LOCAL PATCH 3)는 **유지**.
- PxlCrush `_Palette` missing 은 별개의 기존 이슈로 남김 (게임 룩 판단 필요 → 사용자 몫).

### 추가 (2026-09-03): 마커 + 벽너머 흰 외곽선 병행

> UI 마커는 나침반 역할로 좋음. 여기다 외곽선 벽너머도 추가. 색은 흰색이면 됨.

- `OutlineWhileInteractable.cs` **재생성** (흰색이면 PxlCrush 크러시 무관 — 흰→흰). 3개 오브젝트에 다시 부착.
- `Outline`: `outlineMode` = OutlineAll (벽 너머), `outlineColor` = 흰색, `outlineWidth` = 5, `enabled` = false.
- HUD `ObjectiveMarker`(색 다이아몬드, 나침반) + 벽너머 흰 외곽선 **둘 다** = 방향 + 대상 식별.

### 추가 (2026-09-03): 시간대별 "○○으로 가자" 안내

- `ObjectiveMarker` 에 `messageEn`/`messageKo` 필드. `CanInteract` false→true 엣지에서 `ScreenMessage.Show` 1회.
- `MorningTasks` 의 기존 "게시판에서 할일을 확인하세요" 로직(sawIncomplete/announced)은 **제거** — 이제 게시판 `ObjectiveMarker` 가 담당 (일관성).
- 문구(앞문장 + "○○으로 가자", 인스펙터 수정 가능):
  - 게시판: "할 일을 다 했다. 게시판으로 가자."
  - 접객테이블: "곧 손님이 올 시간이다. 접객 자리로 가자."
  - 주인방침대: "오늘 손님은 여기까지인 것 같다. 이제 자러 가자."
- 점심/새벽은 아직 태스크 게이트가 없어 해당 페이즈 진입 시 바로 뜸 (게이트 생기면 "다 하면" 시점으로 자동 이동).

## 추가 (2026-09-03): 화면고정 나가기 = ESC 금지, Backspace 길게

> 새벽 대화·접객 모드에서 ESC 로 못 나가게, 특수 키로만.

- `UIInteractionMode.cs`: `Update` 의 `escapeKey.wasPressedThisFrame → Exit()` 를 제거.
  대신 `[SerializeField] Key exitKey = Key.Backspace` 를 `exitHoldTime`(0.5s) 동안 눌러야 `Exit()`.
  `exitHeld` 누적, 안 누르면 0, `Exit()`/`Teardown` 에서도 리셋 (홀드 소비 → 연속으로 여러 겹 안 벗겨짐).
  `ExitProgress` 0~1 프로퍼티 (나중에 채움 게이지용).
- 접객·새벽 대화 둘 다 `UIInteractionMode.Exit()` 를 통해 나가므로 이 한 곳만 바꾸면 됨
  (`KnockEffect` 는 `IsTopAnchor` 폴링으로 취소 감지 — 변경 불필요).
- `ShowPanelEffect`(노트 패널)의 ESC 닫기는 그대로 — 별개.
- `exitHint` UI: `Canvas/ExitHint` 신규 (상단중앙 `(0,-36)`, 시작 비활성, `UIInteractionMode` 가 켬).
  Label("Backspace 길게 눌러 나가기" + LocalizedLabel) + BarBG/BarFill(Filled Image).
  `UI/ExitHintGauge.cs` — `Image.fillAmount = UIInteractionMode.ExitProgress` (홀드 중 채워짐).

### 앵커별 나가기 방식 — `escExits` (2026-09-03)

> 모니터 화면고정은 노트처럼 ESC 로. 접객·노크, 앞으로 추가될 연출은 ESC 불가.

- `UIInteractionMode` 에 `anchorEscExits` 스택 + `Enter(anchor, lookScale, escExits)` 3-인자 오버로드.
  **기본 `escExits = false`** = exitKey(Backspace) 홀드로만 나감 → 접객(`ReceptionManager`)·노크(`KnockEffect`)·
  앞으로의 연출은 `Enter` 만 부르면 자동으로 ESC 불가.
- **효과 스크립트 분리 (2026-09-03 후속)**:
  - `EnterUIModeEffect` = 연출용 화면고정 전용. `toggle`·`escExits` 필드 제거 → `Play` 는 `Enter(a, false)` 만.
    클릭으로 못 나옴(홀드만). `Motel_Table`(접객 자리) 이 이걸 씀 — 재클릭해도 접객 안 빠짐.
  - `MonitorViewEffect` (신규) = 모니터처럼 가볍게 보는 뷰. 클릭 진입 / 재클릭·ESC 로 해제(`escExits:true`).
    `CRTMonitor` 의 `EnterUIModeEffect` → `MonitorViewEffect` 로 교체 (anchor="Anchor").
  - `Interactable.SyncEffectsToPrompt` 의 `ViewScreen→EnterUIModeEffect` 는 그대로(테이블용). 모니터는 수동 배선 — "효과 재설정" 돌리지 말 것.
- `Update`: 최상위 앵커가 `escExits` 면 ESC 한 번에 pop(노트 `ConsumesEsc` 우선), 아니면 Backspace 홀드.
  `escExits` 뷰에선 ExitHint(Backspace 안내) 를 숨김.
- 모니터가 접객 위에 쌓인 상태 → ESC 는 모니터만 pop, 접객은 홀드로만.

### 검증 (2026-09-03, uloop 플레이모드 직접)

| 뷰 | ESC | Backspace 0.9s | 재클릭 |
|---|---|---|---|
| 모니터 (`MonitorViewEffect`, escExits) | 나감 ✅ | (나감) | 나감 ✅ |
| 접객 테이블 (`EnterUIModeEffect`) | 안 나감 ✅ | 나감 ✅ | **안 나감 ✅** |
| 모니터↑접객 스택 | ESC/재클릭 → 모니터만 pop, 접객 유지 ✅ | | |

앵커 스택(`anchors`/`anchorLookScales`/`anchorEscExits`) push/pop/clear 동기화 테스트 통과.

### 후속 (2026-09-03)

- **접객 테이블 마커/외곽선 = 점심만**: `Motel_Table.PhaseCondition.allowedPhases` `[Noon, Evening]` → `[Noon]`.
  저녁(접객 모드)엔 `CanInteract=false` → `ObjectiveMarker`·`OutlineWhileInteractable` 자동으로 꺼짐 (둘 다 CanInteract 추종). 점심엔 그대로 표시. 검증: Evening `CanInteract=False` / Noon `True`.
- **ExitHint 문구에 "(테스트용)"**: `Canvas/ExitHint/Label` LocalizedLabel → ko "Backspace 길게 눌러 나가기 (테스트용)", en "Hold Backspace to exit (debug)".

## 상태

2026-09-03: MorningTasks HUD + 침대 랜덤 messy + 게시판 TasksCompleteCondition + Outline.cs 색 패치 완료.
비콘 = HUD ObjectiveMarker 로 전환 (배선+플레이 검증 진행). 점심/새벽 태스크 게이트·다종화, PxlCrush 팔레트는 후속.
