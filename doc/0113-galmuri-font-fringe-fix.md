# 0113 - 한글 폰트 글자 위 미세한 선(프린지) 수정 (제안)

날짜: 2026-08-30
관련: `doc/0107`(폰트 교체·로컬라이제이션), `Assets/My/font/Galmuri11 SDF.asset`

## 요청

> 한글 폰트에서 글자 위에 미세한 선 같은 게 생김. 패딩 5 넣어봤는데 안 됨. 픽셀 폰트 크리스프하게 나오도록 수정해줘.

## 근본 원인 (에셋 설정)

`Galmuri11 SDF.asset` 의 `m_CreationSettings` / 아틀라스:

| 항목 | 현재 값 | 문제 |
|---|---|---|
| `pointSize` (샘플링) | **16** | 원자 폰트를 16px 로 구워서 24~36px UI 로 확대 → 텍셀 2배↑ 확대 |
| `padding` / `m_AtlasPadding` | **0** | **SDF 인데 거리장 폭이 0** → 가장자리가 1텍셀 하드 전환 → bilinear 보간 시 회색 실선 = 그 "미세한 선" |
| `m_AtlasRenderMode` | `4165` (SDFAA) | 픽셀 폰트엔 부적합 (모서리 뭉갬) |
| 아틀라스 `m_FilterMode` | `1` (Bilinear) | 픽셀 폰트는 Point 가 크리스프 |
| `m_AtlasPopulationMode` | `0` (Static) | 12000+ 한글을 16px 로 미리 구움 |

→ 패딩 5 는 **에셋에 반영이 안 됐음** (파일엔 여전히 0). Font Asset Creator 에서 값만 바꾸고 "Update Atlas Texture" / "Generate" 를 안 눌렀거나 저장이 안 된 것.

또: 씬 UI 캔버스는 **Screen Space - Overlay + Pixel Perfect OFF**. RT/PxlCrush 경유 아님(그건 원인 아님). Pixel Perfect 를 켜면 텍스트가 정수 픽셀에 스냅돼 프린지가 더 줄어듦.

## 제안

### A. Galmuri11 SDF 재생성 (in-place, GUID 유지 → 참조 안 깨짐)

`TMP_FontAsset` API 로 기존 에셋을 그대로 두고 아틀라스만 재생성:

- **렌더 모드**: 픽셀 폰트라 SDF 대신 **비트맵**
  - `SMOOTH`(4113) — 안티에일리어싱 있는 래스터. 크리스프 + 홀수 크기에 관대 ← **추천**
  - `RASTER`(4217) — 1비트 하드 픽셀. 최대 크리스프지만 정수배 크기 아니면 계단
- **pointSize**: 88 (11×8) — 24~36px UI 로 축소해도 여유
- **padding**: 2 (비트맵은 거리장 불필요)
- **AtlasPopulationMode**: `Dynamic` — 필요한 글자만 .ttf 에서 즉석 생성 (`m_SourceFontFile` = Galmuri11.ttf 연결). 첫 등장 시 미세 히치(대사 게임엔 무의미), 아틀라스 크기 자동 증가
- **아틀라스 FilterMode**: `Point`
- 머티리얼: 비트맵 렌더 모드면 TMP 가 `TextMeshPro/Bitmap` 셰이더 자동 배정

구현: `EditorUtility.CopySerialized` 또는 `TMP_FontAsset` 재생성 API 로 기존 `Galmuri11 SDF.asset`(guid `a3e2ed54…`) 내용 교체. 이름은 `Galmuri11 SDF` 유지(참조·TMP Settings 기본/폴백 그대로).

### B. 캔버스 Pixel Perfect ON

`InGame.unity` 의 Overlay 캔버스 `m_PixelPerfect: 0 → 1`. (레이아웃엔 영향 없음, 텍스트/이미지가 정수 픽셀에 스냅)

### C. (해당 시) 폰트 사이즈 표준화 — 이번 범위 밖

비트맵은 구운 크기의 정수 분수에서 가장 예쁨. 지금 24·36 혼용. 크리스프가 최우선이면 후속에서 22/33/44 로 통일. 이번엔 손 안 댐.

## 영향 파일

```
Assets/My/font/Galmuri11 SDF.asset  아틀라스·설정 재생성 (GUID·이름 유지)
Assets/My/font/Galmuri11 SDF Atlas   (sub-asset) 교체
Assets/Scenes/InGame.unity           Overlay 캔버스 Pixel Perfect ON
Docs/LocalizationManager.md          폰트 항목 한 줄 갱신
```
코드(.cs) 변경 없음.

## 확인 답변 (2026-08-30)

SMOOTH 비트맵 / Dynamic / Pixel Perfect ON.

## 구현 (2026-08-30) — 실제로는 SDFAA 로 진행 (사유 하단)

### ⚠️ SMOOTH/RASTER 비트맵 + Dynamic 은 TMP 에서 불가능
- Dynamic(런타임 글리프 생성)은 **SDF 계열 렌더 모드만** 지원. SMOOTH/RASTER 로 만들면 `TryAddCharacters` 가 항상 false → 글자 0개 → 텍스트 안 보임 (실측 확인).
- SMOOTH 비트맵을 원하면 **Static** 로 전체 문자셋(≈13000)을 미리 구워야 함 → pointSize 낮추거나 멀티아틀라스 필요, 폰트 사이즈도 정수배로 통일해야 예쁨. 별도 작업.

### 실제 적용: SDFAA + Dynamic (프린지 원인 = padding 0 이 진짜 문제)
| 항목 | 값 |
|---|---|
| 렌더 모드 | **SDFAA** (`m_AtlasRenderMode` 4165) |
| pointSize | 90 (구 16) |
| padding | **9** (구 0 ← 프린지 주범) |
| population | Dynamic (`m_SourceFontFile` = Galmuri11.ttf) |
| 아틀라스 | 4096², Alpha8, **Bilinear**(SDF 는 Point 쓰면 안 됨) |
| 셰이더 | `TextMeshPro/Mobile/Distance Field` |
| 프리워밍 | ASCII + `sample.csv` 한글 (charTable 356) |

`Galmuri11 SDF.asset` **GUID 유지**(`a3e2ed54…`) — `EditorUtility.CopySerialized` in-place 교체. 서브에셋(Atlas/Material) 재생성.

| 파일 | 내용 |
|---|---|
| `Assets/My/font/Galmuri11 SDF.asset` (+ Atlas/Material 서브에셋) | SDFAA/Dynamic/pad9/pt90 로 재생성 |
| `Assets/My/Scripts/Localization/Editor/FontTool.cs` | `Retarget()` — 폰트 GUID 같아도 머티리얼(서브에셋 fileID 변경) 항상 재지정. (구 `if (t.font == galmuri) continue` 는 머티리얼 재동기화를 건너뛰어 폰트 재생성 후 텍스트가 안 보이던 원인) |
| `Assets/Scenes/InGame.unity` | Overlay 캔버스 Pixel Perfect ON (1개) + 15 TMP_Text 머티리얼 재지정 |
| Guest.prefab 등 | 프리팹 1개 TMP_Text 재지정 |

### 검증
- 플레이 모드 스크린샷: 한글/영문 HUD·인벤토리 숫자 정상 렌더, `·` 폴백 정상, 프린지 선 안 보임.
- 컴파일 Error 0.

### 남은 것
- **SDF = 부드러운 안티에일리어싱** (하드 픽셀 아님). 여전히 "픽셀 뭉개짐"이 거슬리면 → Static SMOOTH 비트맵 + 문자셋 축소(KS X 1001 2350자) + 폰트 사이즈 22/33/44 통일 (별도 doc).
- `m_CreationSettings` 는 인스펙터 표시용으로만 갱신됨(pointSize90/pad9). 실제 런타임 필드가 정답.

## 상태

2026-08-30 구현 완료. SDFAA/Dynamic/padding9 로 프린지 해결 + 텍스트 정상. 하드 픽셀 크리스프가 필요하면 Static 비트맵 별도 진행.
