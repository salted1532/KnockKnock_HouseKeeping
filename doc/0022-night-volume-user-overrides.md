# 0022. Night Volume 오버라이드 (사용자 직접 적용)

## 날짜
2026-08-14

## 요청 내용
> Toon Mapping / Color Adjustments / Vignette / Shadows Midtiones Highlights / Bloom / File Grain / Chromatic Aberration / Split Toning
> 해당 내용으로 Night Volume에다가 적용시켰어

## 확인 내용
사용자가 에디터에서 직접 적용. 코드/에셋 변경을 Claude가 수행한 게 아니라 결과 확인만 함(변경 제안 절차 대상 아님).

`Assets/Scenes/InGame/Night-VolumeProfile.asset` 실제 내용 확인 — 8개 오버라이드 전부 존재:

| 오버라이드 | 주요 값 |
|---|---|
| Tonemapping | mode 2 (ACES) |
| ColorAdjustments | postExposure 1 / contrast 25 / saturation -40 |
| Vignette | intensity 0.4 |
| ShadowsMidtonesHighlights | shadows 파란빛 / midtones 따뜻한 톤 |
| Bloom | threshold 0.6 / intensity 0.1 / dirtTexture 지정 + dirtIntensity 6 |
| FilmGrain | intensity 0.5 |
| ChromaticAberration | intensity 0.2 |
| SplitToning | shadows 회청색 / highlights 핑크빛 / balance 10 |

## 참고 사항 (변경 아님, 확인용 메모)
- `SplitToning`과 `ShadowsMidtonesHighlights`를 동시에 켜두면 둘 다 밝기 구간별 색조 보정 도구라 효과가 중첩됨 — 의도한 결과면 무관하지만 예상과 다른 색감이 나오면 둘 중 하나만 켜보고 비교해볼 것
- 세션 중 `Packages/manifest.json`에 `com.unity.postprocessing: 3.5.4`가 새로 추가된 게 git diff에서 확인됨 (Unity가 기존 `Night-Post_ProcessProfile.asset` 참조 해석하며 자동 추가했을 가능성 높음) — 의도적 설치인지 사용자에게 확인 필요

## 변경된 파일
없음 (Claude가 수정한 파일 없음, 확인만 함)
