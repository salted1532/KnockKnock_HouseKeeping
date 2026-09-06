# 0155 – 모든 TMP 텍스트에 검정 외곽선

## 요청
지금 모든 텍스트들에 검정색 외곽선 넣기.

## 현황
- 씬 TMP_Text 33개 + `Assets/My/**` 프리팹 21개 = **전부 `Assets/My/font/Galmuri11 SDF.asset` 단일 폰트** 사용 (TMP 프로젝트 기본값이기도)
- 공유 머티리얼 `Galmuri11 SDF Material`, 셰이더 `TextMeshPro/Mobile/Distance Field` (`_OutlineWidth`/`_OutlineColor` 지원)
- 예외 2개: `Canvas/DayEndTitle/Text`(이미 per-component outline 0.15), `Canvas/ScreenMessage/Text`(런타임 인스턴스 머티리얼 — 공유 머티리얼 변경 미상속)

## 작업 (uloop execute-dynamic-code)
1. 공유 머티리얼 `Galmuri11 SDF.asset` — `_OutlineColor` 검정(이미), `_OutlineWidth` 0 → **0.15**. → 씬 33 + 프리팹 21 전부 적용
2. `Canvas/ScreenMessage/Text` — 컴포넌트 `outlineWidth` 0 → 0.15, `outlineColor` 검정
3. `Canvas/DayEndTitle/Text` — 그대로 (이미 0.15)
4. 씬 저장

## 반복 조정 (0.15 → 0.3 → A안 → B안)
- **0.15**: 작은 HUD 라벨(밝은 하늘 위 `1일차·아침`, `$100`)에선 서브픽셀이라 회색 실루엣 수준 → 안 보임
- **0.3**: Galmuri11 픽셀 폰트 거의 최대치, 여전히 작은 라벨은 아쉬움
- **중간에 발견한 버그**: 진단 스크립트가 씬 TMP_Text 33개 전부를 frozen per-object 머티리얼 인스턴스에 고정시켜 공유 머티리얼 변경이 전달 안 됨 → 31/33을 공유 머티리얼로 되돌림(정리 효과, 씬 diff 큼)
- **A안 시도**: 셰이더 `Mobile/Distance Field` → 정식 `Distance Field` + 언더레이(드롭섀도). 언더레이가 HUD 스케일에선 서브픽셀이라 무의미 → **되돌림**
- **B안 채택** (사용자 결정): 외곽선 **0.25** + 밝은 하늘 위 상시 HUD 라벨에 반투명 검정 패널

## 최종 (B안)
- 공유 머티리얼: 셰이더 `Mobile/Distance Field`(원복), `_OutlineWidth 0.25`, 언더레이 off
- `ScreenMessage/Text`: `_OutlineWidth 0.25`, `DayEndTitle/Text`: 0.15 유지 (둘 다 인스턴스 머티리얼)
- **패널 2개 추가** (3개 예정이었으나 `Canvas/Watch`가 곧 일차/시간대 라벨 = PhaseLabel 컴포넌트, 별도 시계 없음):
  - `Canvas/Watch_BG`, `Canvas/Money_BG` — MorningTasks `TodoPanel` 스타일 복제: Image `RGBA(0,0,0,0.5)`, 스프라이트 없음, 라벨 뒤 sibling, `raycastTarget=false`, sizeDelta = 라벨 + 패딩(24×12) = 224×62 고정
  - `# ponytail`: 고정 크기 패널, 텍스트 폭 추적 안 함 (MorningTasks와 동일 방식). `$100`~`$99999` 는 200px 라벨 rect 안에 들어감

## 수정 파일
- `Assets/My/font/Galmuri11 SDF.asset` (`_OutlineWidth: 0 → 0.25`) — 셰이더는 HEAD와 동일(Mobile). ⚠️ 이 애셋 diff는 세션 시작부터 있던 아틀라스 4096² 베이크(~4700줄/32MB)가 포함됨, 이번 작업과 무관하나 SaveAssets로 확정됨
- `Assets/Scenes/InGame.unity` — TMP 33개 머티리얼 바인딩 정리 + Watch_BG/Money_BG 추가 + 인스턴스 머티리얼 2개
