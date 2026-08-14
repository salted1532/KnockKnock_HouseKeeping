# 0021. Day/Night Volume Profile 생성

## 날짜
2026-08-14

## 요청 내용
> Day, Night 2개의 볼륨 프로필을 Settings에다가 만들어주고 Night에다가 내가 요청한 효과들을 오버라이드해줘

요청한 효과 목록(이전 대화):
Auto Exposure, Bloom, Color Grading, Screen Space Reflections, Vignette, Depth of Field, Lens Distortions, Motion Blur

## 조사 내용
- 프로젝트 URP 버전: `17.4.0`
- 기존 `Assets/Settings/*.asset` (`DefaultVolumeProfile`, `SampleSceneProfile`)에서 URP `VolumeProfile` 포맷과 오버라이드 스크립트 guid를 확인함
- `Library/PackageCache/com.unity.render-pipelines.universal@.../Runtime/Overrides/` 폴더를 직접 뒤져서 이 URP 버전에 실제로 존재하는 Volume 오버라이드 컴포넌트를 확인함

**결과: 요청한 8개 효과 중 2개는 이 URP 버전의 Volume 오버라이드로 존재하지 않음**

| 요청 효과 | URP Volume 오버라이드 존재 여부 |
|---|---|
| Bloom | ✅ `Bloom` |
| Color Grading | ✅ `ColorAdjustments` (Post Exposure/Contrast/Color Filter/Hue Shift/Saturation) |
| Vignette | ✅ `Vignette` |
| Depth of Field | ✅ `DepthOfField` |
| Lens Distortions | ✅ `LensDistortion` |
| Motion Blur | ✅ `MotionBlur` |
| Auto Exposure | ❌ 없음 — 이 URP 버전엔 `Exposure` Volume 오버라이드 자체가 없음(코드 전체 검색 결과 `Exposure.cs` 미존재) |
| Screen Space Reflections | ❌ 없음 — URP 패키지 어디에도 SSR 관련 클래스 없음(반사 프로브 관련 코드만 있음). Renderer Feature로도 미제공 |

→ 이 2개는 이번 Volume Profile 생성 범위에서 제외하고, 나머지 6개만 Night 프로필에 오버라이드로 추가하는 걸 제안합니다.

## 사용자 확인 결과
- "Auto Exposure/SSR을 PostProcess(레거시)랑 같이 쓸 수 없나?" → 안 됨 (아래 회신 참고)
- "일단 볼륨 프로필만 만들고 효과는 넣지 말아줘" → 오버라이드 없이 빈 프로필만 생성하기로 확정

## 적용한 변경
### 1) `Assets/Settings/Day-VolumeProfile.asset` (신규)
빈 `VolumeProfile` (`components: []`).

### 2) `Assets/Settings/Night-VolumeProfile.asset` (신규)
빈 `VolumeProfile` (`components: []`).

오버라이드는 추가하지 않음(사용자 요청). Bloom/Color Grading(ColorAdjustments)/Vignette/DepthOfField/LensDistortion/MotionBlur 6개는 이 URP 버전(17.4.0)에서 Volume 오버라이드로 존재 확인됨 — 필요할 때 에디터에서 Add Override로 직접 추가.

## 보류 사항: Auto Exposure / Screen Space Reflections
레거시 `com.unity.postprocessing`(PPv2)와 URP Volume을 동시에 못 쓰는 이유: PPv2의 `PostProcessLayer`는 활성 렌더 파이프라인이 SRP(URP/HDRP)인지 감지하면 자체적으로 렌더링을 비활성화합니다. 2개 효과만 골라서 PPv2로 돌리는 것도 불가능 — 레이어 자체가 URP 위에서 안 돌아감(SAO가 안 됐던 것과 같은 근본 원인).

대안(미구현, 논의 필요):
- **Auto Exposure**: 이 URP 버전엔 자동노출(Exposure Volume 오버라이드) 자체가 없음. `ColorAdjustments.postExposure`로 수동 노출값은 조절 가능하지만 "자동" 적응은 아님. URP 패키지를 더 최신 버전으로 업데이트하면 생길 수도 있는지는 미확인.
- **Screen Space Reflections**: 이 URP 버전 패키지 전체에 SSR 관련 코드 없음(Renderer Feature도 없음). Reflection Probe로 대체하거나, 커스텀 Renderer Feature를 새로 작성해야 함(별도 작업).

## 변경된 파일
- `Assets/Settings/Day-VolumeProfile.asset` (+ `.meta`) 신규
- `Assets/Settings/Night-VolumeProfile.asset` (+ `.meta`) 신규
