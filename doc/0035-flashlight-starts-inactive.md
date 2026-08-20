# 0035 - 플레이어 손전등 시작 시 비활성화

## 날짜
2026-08-20

## 요청 내용 (원문)
> E키 잘 작동하고 FlashLight_low-Poly도 비활성화 되는게 보이는데
> 게임 시작시 플레이어의 손전등이 비활성화 되고
> E키로 손전등을 획득 했을때 활성화 되도록 해줘

[[0034-flashlight-pickup-interaction]]의 후속 수정 — 별도 제안서 없이 바로 진행.

## 조사 내용
- `PlayerCapsule` 밑 `flashlight`(`Assets/My/InGame/Prefabs/FlashLight/flashlight.prefab` 인스턴스, PrefabInstance fileID 193342755)의 루트 GameObject(소스 fileID `6955010278166185207`)에는 `m_IsActive` 오버라이드가 없었음 → 여러 겹 중첩된 프리팹 체인의 기본값을 그대로 따라가고 있었고, 그 기본값이 활성화 상태였던 것으로 보임(그래서 게임 시작부터 손전등이 켜져 있었음).
- `Interactable.Pickup()`은 `equipTarget.SetActive(true)`만 호출하므로, 시작 시 꺼져있지 않으면 "주웠을 때 켜짐"이 의미가 없음 → 시작 상태를 명시적으로 꺼두는 게 필요.

## 변경 내용

### `Assets/Scenes/InGame.unity`
`flashlight` 인스턴스에 `m_IsActive: 0` 오버라이드 추가 (기존 `m_Name` 오버라이드 옆에):
```yaml
    - target: {fileID: 6955010278166185207, guid: c14333adec2890d47854f93f7d64703a, type: 3}
      propertyPath: m_IsActive
      value: 0
      objectReference: {fileID: 0}
```
- 중첩 프리팹 기본값이 무엇이든 상관없이 이 씬 인스턴스는 항상 비활성화 상태로 시작하도록 명시적으로 고정.

## 결과
계획대로 적용 완료. 게임 시작 시 `flashlight`가 꺼져있고, `FlashLight_low-Poly` 상호작용(E키) 시 `Interactable.Pickup()`이 켜줌([[0034-flashlight-pickup-interaction]]에서 이미 구현됨, 코드 변경 없음).

## 남은 작업
- Unity 에디터에서 씬 열어서 `flashlight`가 Hierarchy에서 회색(비활성화)으로 보이는지, 플레이 시작 시 꺼져있는지 확인

## 변경된 파일
- `Assets/Scenes/InGame.unity`
