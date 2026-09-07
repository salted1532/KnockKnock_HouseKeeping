# 0166 – ActionPointsBar 에 "행동력" 라벨 추가

## 요청
`ActionPointsBar` 의 pip 4칸 **왼쪽에 "행동력" 텍스트**를 추가.

## 현황
- `HUD/ActionPointsBar` = `RectTransform` + `HorizontalLayoutGroup`(MiddleLeft, spacing 6) + `ActionPointsHud`
  - 자식: `Pip0`~`Pip3` (28×28 Image), 텍스트 없음
  - `anchoredPosition (120, 976.3)`, `sizeDelta (150, 32)`, pivot 0.5
- `ActionPointsHud.Update()` 가 `DayPhase.Dawn` 일 때만 각 pip 을 `SetActive(true)` → **새벽에만 바가 보임**
- HUD 텍스트 폰트: Galmuri11 SDF (`guid a3e2ed54c4c08d249bbe6e8d01aa6933`), Watch/Money 는 fontSize 36

## 작업

### 1. 씬: `Label` 자식 추가 (uloop dynamic code)
- `ActionPointsBar` 의 **첫 번째 자식**(siblingIndex 0)으로 `Label` GameObject 생성
- `TextMeshProUGUI`: text `"행동력"`, 폰트 Galmuri11 SDF, fontSize 24, color 흰색, 정렬 MidRight
- `RectTransform sizeDelta (64, 28)`
- HorizontalLayoutGroup 이 pip 왼쪽에 자동 배치 (spacing 6)

### 2. `ActionPointsHud.cs`: 라벨도 새벽에만
```
// 변경 전
[SerializeField] private Image[] pips = new Image[4];

// 변경 후
[SerializeField] private Image[] pips = new Image[4];
[SerializeField] private GameObject label;   // "행동력" — pip 과 함께 토글
```
```
// Update() 변경 전
foreach (var p in pips)
    if (p != null && p.gameObject.activeSelf != dawn) p.gameObject.SetActive(dawn);

// 변경 후
foreach (var p in pips)
    if (p != null && p.gameObject.activeSelf != dawn) p.gameObject.SetActive(dawn);
if (label != null && label.activeSelf != dawn) label.SetActive(dawn);
```
- `ActionPointsHud.label` 에 새 `Label` 오브젝트 연결

### 3. 위치 보정
- 라벨(64) + spacing(6) 만큼 pip 들이 오른쪽으로 밀림 → 바 `anchoredPosition.x` 를 약 -35 조정하거나 그대로 두고 스크린샷으로 눈대중 확정

## 안 하는 것 (필요하면 말해주세요)
- **로컬라이제이션**: 영어 대응 없이 한글 고정. `LocalizedLabel` 붙이려면 별도 요청.
- 배경(`_BG`) 이미지: Watch/Money 처럼 깔지 않고 텍스트만.

## 결과 (구현 완료)
- `ActionPointsBar` 첫 자식으로 `Label` 추가 (fileID 439935794)
  - TMP: text `행동력`, Galmuri11 SDF, fontSize 24, 흰색, 정렬 MidRight, raycastTarget off
  - RectTransform 64×28, `m_IsActive 0` (pip 과 동일하게 새벽에만 켜짐)
- `ActionPointsHud.cs`: `label` 필드 + `Update()` 에서 pip 과 함께 dawn 토글, `label` = 새 Label 연결
- 컴파일 클린 (에러/경고 0)
- 레이아웃 확인 (ForceRebuildLayout): `Label`(0~64) → 6px → `Pip0`(70~98) → … → `Pip3`
  - 라벨 폭+spacing 만큼(70px) pip 들이 우측 이동. 바는 새벽 전용 + 좌상단 여백이라 위치 보정 생략.
- 플레이모드 스크린샷 검증은 에디터 비포커스로 프레임 루프 정지 → 구조/레이아웃만 확인. 인게임 새벽에서 눈으로 최종 확인 필요.

## 수정 파일
- `Assets/Scenes/InGame.unity` (Label 오브젝트, ActionPointsBar 자식, ActionPointsHud.label 배선)
- `Assets/My/Scripts/UI/ActionPointsHud.cs` (+2줄)
