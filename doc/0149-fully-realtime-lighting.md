# 0149 – InGame 씬 전면 실시간 조명 전환

## 발단
"No Renderers that are marked static were found in the scene..." 경고. 사용자가 Lighting 창에서 **Generate Lighting** 을 직접 눌러 발생 (Auto Generate 는 이미 꺼져 있었음 = `giWorkflowMode.OnDemand`).

## 원인
씬이 베이크/혼합 조명 세팅인데 Static 렌더러가 0개:
- `Map/street light*` 포인트 라이트 12개 = **Baked**
- 시간대 디렉셔널 4개 (Morning/Afternoon/Night/MidNight) = **Mixed** (shadows=Soft)
- `Baked Global Illumination` = ON
- 나머지 ~35개 라이트 = Realtime

`PhaseVisuals` 가 런타임에 디렉셔널을 교체하므로 Mixed 베이크는 한 시간대 기준으로만 유효 → 데이/나이트 게임에 베이크 셋업 부적합.

## 결정 (사용자)
전부 실시간으로 전환.

## 작업 (uloop execute-dynamic-code)
1. `lightmapBakeType == Baked || Mixed` 인 라이트 16개 → `Realtime` (그림자/강도/색/범위/활성 상태 미변경)
2. `Lightmapping.lightingSettings.bakedGI = false` (realtimeGI 는 원래 false 유지)
3. 씬 저장

실시간 그림자 캐스터: 디렉셔널 4개(shadows=Soft, 동시 1개만 활성)뿐. 포인트 라이트는 전부 shadows=None → 포인트 그림자 비용 없음.

## 수정 파일
- `Assets/Scenes/InGame.unity` — 16개 `m_Lightmapping` 프리팹 인스턴스 오버라이드 추가
- `Assets/My/InGame/Prefabs/Time/MidNight/Cool Cloudy Night 3.lighting` — `m_EnableBakedLightmaps: 1 → 0` (씬이 참조하는 LightingSettings 애셋)

## 미처리
- 디렉셔널/가로등 라이트는 프리팹 인스턴스라 변경이 씬 오버라이드로 들어감. 프리팹 원본까지 고치려면 별도 작업. 현재 InGame 이 유일 게임씬이므로 오버라이드로 충분.
- 이제 Generate Lighting 눌러도 경고 안 뜸.
