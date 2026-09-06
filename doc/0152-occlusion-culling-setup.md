# 0152 – InGame 씬 오클루전 컬링 세팅 + 베이크

## 발단
"No Renderers that are marked static were found..." 경고 — **Occlusion Culling** 창의 Bake 를 눌러서 발생 (라이트맵 경고와 다른 시스템, doc/0149 와 별개). Static 플래그 렌더러가 0개라 구울 게 없었음.

## 분류 (렌더러 1157개, 시작 시 static 플래그 0개)
스크립트로 조상 컴포넌트/크기 기준 자동 분류:

| 분류 | 개수 | StaticEditorFlags | 대상 |
|---|---|---|---|
| **제외 (MOVING)** | 79 | 0 | 플레이어, 줍는 아이템(키/캔), 문(HingeEffect), Push/Pickup, 스킨드메쉬, 스폰 차량 |
| **OCCLUDEE만** | 628 | `OccludeeStatic` (16) | 가구·소품·키훅·쓰레기 + Interactable/ChangeObjectEffect 하위 + 크기<1.5m + **나무 전부** |
| **FULL STATIC** | 450 | `OccluderStatic\|OccludeeStatic` (18) | 건물/벽/지붕/바닥/가로등/주차차량/횡단보도/울타리/모텔방 외벽 |

수동 오버라이드: 나무(`Tree*` 경로) ~234개를 FULL→OCCLUDEE (알파 캐노피는 오클루더 비효율).

## 베이크
`StaticOcclusionCulling.Compute()` (이 Unity 버전에선 비동기 — `isRunning` 폴링 후 재저장).

## 결과 (디스크 검증)
- **`Assets/Scenes/InGame/OcclusionCullingData.asset`** 생성 (745,512 bytes PVS, guid `296d397d115dbf0458b47b658a8222a7`)
- `InGame.unity` — `OcclusionCullingSettings.m_OcclusionCullingData` 가 베이크 애셋 참조
- `m_StaticEditorFlags` 프리팹 인스턴스 오버라이드 1065개 (16×628, 18×437)
- 에러/경고 없음
- 기본 베이크 파라미터 (smallest occluder 등 미조정)

## 미처리
- 모텔방 가구 중 침대/옷장 등 큰 정적 오브젝트는 보수적으로 OCCLUDEE만 → 필요 시 수동으로 FULL 승격 가능
- 베이크 파라미터 튜닝 안 함 (기본값)
- 커밋 안 함 (병렬 세션 작업물과 섞여 있음)
