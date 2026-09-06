# 0154 – Briefing_TV 화면 (뉴스 브리핑 TV 슬라이드)

## 요청
`TVs/Briefing_TV` (프리팹, 사용자가 Canvas 추가해둠)에 CRTMonitor처럼 Image 넣고 스프라이트 연결해서 화면에 나오게. `NightNewsBriefing.tvImage` 배선. 캔버스 크기는 그대로.

## 참고: CRTMonitor 화면 구조
`CRTMonitor/screenON/ScreenUI` = WorldSpace Canvas(800×600, fit scale, CanvasScaler ConstantPixelSize), `RenderTextureGraphicRaycaster`(RT 파이프라인 클릭 보정), 자식 `Background` Image + 방버튼. 화면이 보이는 건 MainCamera→Posterize RT→`Canvas/RawImage` 전체 렌더 경로 덕분(모니터별 카메라 없음).

## 작업
1. **스프라이트** `Assets/My/InGame/UI/BriefingTV_TestPattern.png` 생성 — 800×600 SMPTE 컬러바(상단 70% 7색 + 하단 30% `#1a1a20`). Sprite(Single), Bilinear, no-mip, Clamp, sRGB.
2. **프리팹** `Assets/My/InGame/Prefabs/Briefing_TV.prefab` — `Canvas` 밑에 `Screen`(RectTransform+CanvasRenderer+Image) 추가. 앵커 0→1 풀스트레치, sprite=테스트패턴, preserveAspect, raycastTarget=false. Canvas의 기존 GraphicRaycaster는 안 건드림.
3. **배선** (씬) `NightNewsBriefing.tv` → `TVs/Briefing_TV`, `tvImage` → `Briefing_TV/Canvas/Screen`. 씬 저장.

`CampaignData.newsSlides`가 비어 있어 런타임에도 이 테스트패턴이 표시됨(슬라이드 작성 전까지).

## 결과
스크린샷: 레트로 TV 유리면에 컬러바 정상 표시. z-fighting/뒷면/edge-on 없음.

## 미해결 — 사용자 Canvas transform 유래 (미수정, 승인 대기)
사용자 Canvas: `localScale (0.005537, 0.004458, 0.010)` 비균일, `localEuler (270, 180, 0)`.
1. **좌우 반전** — Y 180°로 캔버스 뒷면 표시. 대칭 테스트패턴이라 안 보이지만 **실제 뉴스 슬라이드(텍스트/로고)는 거울상**. 수정: `localEuler (270, 0, 0)` (또는 안쪽 향하면 `(90,0,0)`).
2. **가로 ~24% 늘어남** — 비균일 스케일(X:Y=1.24:1). 수정: `localScale (0.004458, 0.004458, 0.004458)` 균일.
3. **레터박스 초록 띠** — preserveAspect + 캔버스 비율 불일치로 상하 여백에 TV emissive(Emissor) 노출. #2 수정 후 축소, 완전 제거하려면 preserveAspect=false 또는 sizeDelta 4:3.

권장 통합 수정: Canvas `localScale (0.004458)³` + `localEuler (270, 0, 0)`.
