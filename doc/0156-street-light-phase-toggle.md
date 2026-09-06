# 0156 – 가로등 시간대별 점등 (저녁·새벽만)

## 요청
`street light` 프리팹의 `Open Cylinder`(램프 렌즈) + `Point Light` 가 저녁·새벽에만 켜지고 아침·점심엔 꺼지도록.

## 현황
"여러 시간대에 오브젝트 활성화" 컴포넌트 없음 (`PhaseVisuals`=시간대당 라이트 1개, `PhaseCondition`=상호작용 게이팅).

## 작업
**신규** `Assets/My/Scripts/Environment/ActiveInPhases.cs` (~40줄):
- `DayPhase[] activePhases` (기본 `[Evening, Dawn]`) + `GameObject[] targets`
- `DayPhaseManager.OnPhaseChanged`(암전 시점) 구독 → `targets` 를 해당 시간대에만 `SetActive`
- `targets` 비면 자식 전체 토글 (자신 끄면 이벤트 못 받으므로)
- 컴파일 클린 (에러 0)

**배선:** `street light` 프리팹 루트에 `ActiveInPhases` 부착, `activePhases=[Evening,Dawn]`, `targets=[Open Cylinder, Point Light]`. 폴 메시는 루트에 있어 항상 보임.

## 결과 (플레이모드 4단계 검증)
| 시간대 | Open Cylinder / Point Light |
|---|---|
| Morning | OFF (폴·랜턴 케이지만 보임) |
| Noon | OFF |
| Evening | ON (램프 발광 + 도로 광원뿔) |
| Dawn | ON |
| → Morning 순환 | OFF |

씬 인스턴스 6개 모두 상속·동시 토글 확인.

## 수정 파일
- `Assets/My/Scripts/Environment/ActiveInPhases.cs` (신규)
- `Assets/My/InGame/Prefabs/Item/street light.prefab` (ActiveInPhases 추가)
- `Assets/Scenes/InGame.unity` (저장)

## 참고
- 가로등 인스턴스가 현재 **6개** (`street light (2)(3)(5)(7)(8)(9)`). doc/0149 조명 베이크 땐 12개였음 — 그 사이 6개 삭제된 듯, 의도된 건지 확인 필요.
