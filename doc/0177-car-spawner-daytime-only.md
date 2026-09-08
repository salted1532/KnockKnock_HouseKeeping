# 0177 – 자동차 스포너 저녁·새벽엔 정지

## 요청
밤(저녁·새벽 시간대)에는 CarSpawner 에서 자동차가 안 돌아다니도록.

## 배경
`CarSpawner` (doc/0148) 는 `Start` 에서 `autoStart` 로 스폰 루프를 켜고 하루 종일 돈다.
시간대 개념이 없음. `ActiveInPhases` (doc/0156) 패턴처럼 `DayPhaseManager.OnPhaseChanged`
를 구독해 낮(Morning·Noon)에만 돌리면 됨.

## 적용 수정 (`Assets/My/Scripts/Environment/CarSpawner.cs`)
- 신규 필드 `[SerializeField] DayPhase[] activePhases = { DayPhase.Morning, DayPhase.Noon };`
- `Start`: `autoStart` 여도 즉시 `StartSpawning` 하지 않고, `DayPhaseManager.Instance` 구독 후
  현재 페이즈로 `ApplyPhase` 호출. 매니저 없으면 기존대로 바로 시작(폴백).
- `ApplyPhase(DayPhase)`: 현재 페이즈가 `activePhases` 에 있으면 `StartSpawning`, 아니면 `StopSpawning`.
  - `StopSpawning` 은 루프만 멈춤. 이미 도로에 있는 차는 트윈 끝나면 스스로 소멸(자연스러움).
    필요하면 즉시 정리하는 옵션도 가능하지만 기본은 놔둠.
- `OnDestroy` 에서 구독 해제.
- `autoStart` 는 유지(매니저 없을 때 폴백 및 수동 제어 겸용).

기존 `StartSpawning` 의 "loop != null 이면 return" 가드가 있어 페이즈가 낮으로 여러 번 들어와도 안전.

### 구현 메모
- `Start`: 매니저 있으면 구독 + 현재 페이즈로 `ApplyPhase`, 없으면 `autoStart` 폴백.
- `ApplyPhase`: `activePhases` 포함 → `StartSpawning`(중복 가드 있음), 아니면 `StopSpawning`.
- `OnDestroy` 구독 해제.
- `using System;` 추가 시 `Random` 이 `UnityEngine.Random`/`System.Random` 로 모호해져 CS0104 →
  `System.Array.IndexOf` 로 풀 네임 사용.

## 검증
- `uloop compile` 클린 (기존 `AssetOrganizer.cs` CS0162 경고만, 무관).
- PlayMode 런타임 확인은 미실행.

## 씬 작업
- 코드에 기본값이 들어가므로 `InGame.unity` 의 CarSpawner 는 별도 배선 불필요(기본 Morning·Noon).
  다르게 하려면 인스펙터 **Active Phases**.
