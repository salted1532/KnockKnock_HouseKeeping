# 0180 – 손님 스프라이트 숨쉬기 (멈춰 있을 때)

## 요청
"새벽 대화에서 npc가 숨쉬듯이 움직이도록 했으면 좋겠어"

## 접근
`GuestWalkBob` 이 이미 Guest.prefab `Square` 의 localPosition/Scale 을 소유(걷기 총총 바운스 +
스프라이트 교체 시 base 재캡처)하므로, 새 컴포넌트 대신 여기에 idle 숨쉬기를 얹음. 두 컴포넌트가
같은 transform 다투는 상황 회피.

## 구현 — `Assets/My/Scripts/Dialogue/GuestWalkBob.cs`
- 신규 필드: `breatheAmount`(0.012 = ±1.2%), `breathePeriod`(3.6s).
- `Awake` / 스프라이트 교체 프레임에 `baseLocalScale` 도 재캡처 (기존 `baseLocalPos` 옆에).
- `LateUpdate`: **걷지 않을 때만** `localScale.y = baseY × (1 + sin(t·2π/period)·amount)`.
  걷는 중엔 총총 스텝 우선, 스케일 안 건드림. 걷기 시작하면 `baseLocalScale` 로 복귀.
- `OnDisable` 에서 `baseLocalScale` 복구 (프리팹 손님마다 재활용).
- ponytail: 피벗 기준 스케일이라 발끝이 ~2cm 오르내림 — 눈에 안 띄어 위치 보정 생략.

새벽 노크 대화·저녁 접객 대화 모두 같은 프리팹이라 자동 적용. `SpriteLightResponse`(doc/0179)·
`SpriteOutline`(자식 렌더러) 과 충돌 없음.

## 프리팹
- 코드 기본값(0.012 / 3.6)이 자동 적용 — Guest.prefab 편집 불필요. 톤 조절은 인스펙터에서.

## 검증
- `uloop compile` → Success, 0 error / 0 warning.

## 변경 파일
- `Assets/My/Scripts/Dialogue/GuestWalkBob.cs`
