# 0172 – 손님 걷기 총총총 흔들림 (GuestWalkBob)

## 요청
손님이 이동할 때 "총총총" 걷는 것처럼, 발걸음에 맞춰 스프라이트가 위아래로 통통 튀고 좌우로 흔들거리게. DOTween 사용.

## 현황 (조사)
- 손님 = `Guest.prefab`: 루트 `Guest`(`GuestMover`+`GuestView`+`Interactable`…) → 자식 `Square`(SpriteRenderer = `GuestView.body`)
- `GuestMover.WalkThrough` 가 루트를 웨이포인트로 직선 이동, 매 프레임 루트 `rotation = faceYaw`(플레이어 쪽 고정). 스텝 애니메이션 없음(정지 스프라이트만 스왑)
- `GuestView.ApplySprite` 가 **스프라이트 교체 시** `Square.localScale`(균일 k) + `Square.localPosition.y`(발 정렬) 를 덮어씀 → 걷는 레그 중엔 안 불림, 레그 전환 때만
- DOTween 프로젝트에 있음(`CarSpawner`). 관용구 = `.SetLink(gameObject)` 로 파괴 시 자동 kill
- 손님 발소리 오디오는 없음 → "발걸음에 맞춰" = bob 리듬 자체가 스텝 박자

## 작업

### 1. `GuestMover` — 걷는 중인지 노출
```csharp
public bool Walking { get; private set; }
// SetWalking(bool v) 안에서 Walking = v;
```

### 2. 신규 `Assets/My/Scripts/Dialogue/GuestWalkBob.cs` (~70줄)
`Square`(body) 에 부착. `LateUpdate`:
- `mover.Walking` 시작 → DOTween 2개:
  - **hop**: `bobY` 0↔`hopHeight`, `stepInterval`(기본 0.2s), Yoyo 무한, OutQuad — 통통통 상하
  - **sway**: `bobRot` `-swayAngle`↔`+swayAngle`, `swayInterval`(기본 0.4s = 2스텝), Yoyo 무한, InOutSine — 좌우 기울기
- 매 프레임 `Square.localPosition = base + (0, bobY, 0)`, `localRotation = Euler(0,0,bobRot)`
- `base` 는 `body.sprite` 가 바뀐 프레임에 재캡처(그때 GuestView 가 방금 쓴 값 = bob 안 섞임) → GuestView 와 안 싸움
- 걷기 종료 → DOTween 으로 0 으로 0.15s 복귀. `OnDisable` → 하드 리셋(0). 모든 트윈 `.SetLink(gameObject)`
- 인스펙터: `hopHeight`(0.12), `stepInterval`(0.2), `swayAngle`(5°), `swayInterval`(0.4)

### 3. 프리팹 배선
- `Guest.prefab` → `Square` 에 `GuestWalkBob` 추가 (`mover`/`body` 는 Awake 자동, 명시 할당도)
- `uloop compile` + 플레이모드 확인

## 안 하는 것
- 스쿼시&스트레치(착지 눌림) — `GuestView` 가 scale 을 덮어써서 충돌, rotation+position 만
- NavMesh/실제 보행 애니메이션 — 스프라이트 연출이라 불필요

## 수정 파일
- `Assets/My/Scripts/Dialogue/GuestMover.cs` (+2줄, `Walking` 프로퍼티)
- `Assets/My/Scripts/Dialogue/GuestWalkBob.cs` (신규)
- `Assets/My/InGame/Prefabs/Guest.prefab` (Square 에 컴포넌트)
