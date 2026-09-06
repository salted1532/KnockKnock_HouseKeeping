# CarSpawner

`Assets/My/Scripts/Environment/CarSpawner.cs`

도로에 자동차를 주기적으로 스폰 → 스폰 포인트 `forward` 방향으로 직선 주행 → 소멸. 주행 중 DOTween 셰이크로 엔진 덜덜거림. 설계·배선 이력 [`doc/0148`](../doc/0148-street-car-spawner.md). (DOTween: `Assets/Plugins/Demigiant/DOTween`)

## 필드

| 필드 | 기본 | 설명 |
|---|---|---|
| `carPrefabs` (`GameObject[]`) | — | 매 스폰마다 랜덤 선택 |
| `spawnPoints` (`Transform[]`) | — | 각 포인트의 **forward(파란 축)** 방향으로 주행. 여러 개 가능 |
| `travelDistance` (`float`) | 400 | 스폰 지점에서 전진할 거리 |
| `speed` (`float`) | 12 | units/sec. 이동 시간 = `travelDistance / speed` |
| `spawnInterval` (`Vector2`) | (4, 10) | 다음 스폰까지 랜덤 대기 (min, max) 초 |
| `maxAlive` (`int`) | 6 | 동시 존재 상한 |
| `autoStart` (`bool`) | true | `Start` 시 스폰 루프 시작 |
| `shakePositionStrength` / `shakeRotationStrength` | 0.02 / 0.4 | 엔진 진동 강도 |

## 동작

- 코루틴 루프: `spawnInterval` 랜덤 대기 → `alive` 정리 → `< maxAlive` 면 `Spawn()`.
- `Spawn()`: **홀더 GameObject**(이동 담당) + 자식으로 프리팹 인스턴스(진동 담당) — 트윈이 서로 안 덮어쓰게 분리.
  - 홀더 `DOMove(point + forward * travelDistance, dur).SetEase(Linear).OnComplete(Destroy)`.
  - 차체 `DOShakePosition` / `DOShakeRotation` `SetLoops(-1)`. `SetLink` 로 파괴 시 트윈 자동 정리.
- API: `StartSpawning()` / `StopSpawning()` / `SetSpawnInterval(min, max)`. `OnDisable` 에서 루프 중지.
- `carPrefabs` 또는 `spawnPoints` 비면 경고 후 스폰 안 함.

## 배선

빈 GameObject 에 부착 → 도로 양 끝에 스폰 포인트(빈 GameObject) 배치, 파란 축을 주행 방향으로 회전 → `spawnPoints` 연결. 물리·바퀴 회전·엔진 사운드·곡선 경로 없음 (직선 이동 + 시각 셰이크만).

## 관련

[`doc/0148`](../doc/0148-street-car-spawner.md) · [ActiveInPhases](ActiveInPhases.md)
