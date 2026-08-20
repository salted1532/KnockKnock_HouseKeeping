# 0030 - 점프 시 무한 상승 버그 원인 조사

## 날짜
2026-08-20

## 요청 내용 (원문)
> 현재 플레이어가 점프하면 그냥 계속 공중으로 올라가는데 이거 layermask랑 연관있는거 같은데 원인좀 찾아줘

순수 조사 요청(코드 변경 없음) — 확인 절차 대상 아님.

## 조사 내용
`Assets/Scenes/InGame.unity`의 PlayerCapsule 프리팹 인스턴스에 걸린 오버라이드 두 개가 원인:
- `m_Layer` → `0` (Default) — 프리팹 원본 기본값 `8`을 이 씬에서 `0`으로 덮어씀 (line 27087-27088)
- `GroundLayers.m_Bits` → `1921` — bit 0(Default) + bit 7(Wood) + bit 8(Concrete) + bit 9(Metal) + bit 10(Grass) (line 27079-27080)

플레이어 자신이 Default(레이어 0) 위에 있는데 `FirstPersonController.GroundedCheck()`가 쓰는 `GroundLayers`에도 Default가 포함되어 있어, `Physics.CheckSphere`가 발밑에서 자기 자신의 `CharacterController` 콜라이더를 "바닥"으로 감지함(self-detection). 그 결과 `Grounded`가 항상 `true`로 고정되고, `JumpTimeout`(0.1초)마다 점프 조건이 계속 참이 되어 위로 속도가 계속 더해짐 → 낙하 판정을 못 받고 끝없이 상승.

발소리 시스템(`SoundManager`/`FootstepSystem`) 코드와는 무관. Wood/Concrete/Metal/Grass 레이어를 `GroundLayers`에 포함시키면서 Default까지 같이 포함됐고, 플레이어 레이어가 이 씬에서 Default로 덮어써진 것이 겹쳐서 발생.

## 제안한 해결책
1. 플레이어를 `GroundLayers`에 없는 전용 레이어로 이동
2. 또는 `GroundLayers`에서 플레이어가 있는 레이어(Default)를 제외

## 결과
사용자가 직접 에디터에서 레이어 재지정하기로 함 — 코드/씬 변경 없이 조사만 완료.

## 변경된 파일
없음 (조사 전용)
