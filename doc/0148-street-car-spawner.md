# 0148 – 거리 자동차 스포너 (CarSpawner + DOTween 엔진 진동)

## 요청
- 맵을 돌아다니는 자동차 추가. 스폰 위치·방향으로 400 정도 이동 후 소멸.
- `CarSpawner` 스크립트 작성, 스폰 포인트를 인스펙터로 연결(여러 개 가능).
- DOTween 이용해 이동 + "자동차 엔진 덜덜거림"까지 표현.

## 사전 확인
- DOTween 존재: `Assets/Plugins/Demigiant/DOTween/DOTween.dll`, `Assets/Resources/DOTweenSettings.asset` → 셋업 완료.
- 프로젝트에 asmdef 없음 → `using DG.Tweening` 바로 사용 가능.
- 자동차 프리팹 존재: `Assets/My/Prefabs/Vehicles/Cars/Auto.prefab`, `Carro_su.prefab` 등.

## 제안 구현

### 파일 1개: `Assets/My/Scripts/Environment/CarSpawner.cs`

인스펙터 필드
| 필드 | 타입 | 기본 | 설명 |
|---|---|---|---|
| `carPrefabs` | `GameObject[]` | - | 스폰할 자동차 프리팹들 (랜덤 선택) |
| `spawnPoints` | `Transform[]` | - | 스폰 포인트 (여러 개, 인스펙터 연결). 각 포인트의 **forward 방향**으로 주행 |
| `travelDistance` | `float` | 400 | 스폰 지점에서 전진 거리 |
| `speed` | `float` | 12 | 주행 속도(units/sec). 이동시간 = distance/speed |
| `spawnInterval` | `Vector2` | (4, 10) | 다음 스폰까지 랜덤 대기(min,max) 초 |
| `maxAlive` | `int` | 6 | 동시 존재 자동차 상한 |
| `autoStart` | `bool` | true | Start 시 자동 스폰 루프 시작 |

동작
1. 코루틴 루프: `spawnInterval` 랜덤 대기 → 살아있는 차 < `maxAlive` 면 스폰.
2. 스폰: 랜덤 `spawnPoints[i]` + 랜덤 `carPrefabs[j]`.
   - 홀더 GameObject 생성 → `spawnPoint`의 위치/회전으로 셋. 그 자식으로 프리팹 Instantiate(localPos 0).
   - 홀더를 `DOMove(pos + forward*travelDistance, dist/speed).SetEase(Ease.Linear)` → `OnComplete`에서 `Destroy(홀더)`.
   - 홀더 이동 / 차체 진동을 분리해 서로 안 싸우게 함.
3. 엔진 덜덜거림(차 인스턴스에):
   - `car.transform.DOShakePosition(1f, strength:0.02, vibrato:28, randomness:90, fadeOut:false).SetLoops(-1).SetLink(car)`
   - `car.transform.DOShakeRotation(1f, strength:0.4, vibrato:20, fadeOut:false).SetLoops(-1).SetLink(car)`
   - `SetLink`로 오브젝트 파괴 시 트윈 자동 정리.
4. `OnDisable`에서 루프 중지. 스폰된 차의 트윈은 `SetLink`가 처리.

### 배선 (구현 후 사용자/에디터 작업)
- 빈 GameObject `CarSpawner` 씬에 추가 → 컴포넌트 부착.
- 도로 양 끝에 빈 GameObject 스폰 포인트 배치, **파란 축(Z/forward)** 을 주행 방향으로 향하게 회전. `spawnPoints`에 드래그.
- `carPrefabs`에 `Auto.prefab` 등 연결.

## 미포함 (필요시 추가)
- 충돌/물리·바퀴 회전·서스펜션: 안 넣음. "덜덜거림"은 시각 셰이크로만.
- 엔진 사운드: 요청 없음.
- 곡선 경로/웨이포인트: 직선 이동만.

## 승인 대기
위 설계로 `CarSpawner.cs` 작성 진행할지 확인 요청.
