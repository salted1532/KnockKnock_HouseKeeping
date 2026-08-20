# 0036 - 손전등/안내 텍스트 시작 비활성화 + 참조를 코드 조회 방식으로 변경

## 날짜
2026-08-20

## 요청 내용 (원문)
> PlayerCapsule > PlayerCameraRoot > flashlight 라는 오브젝트가 게임 시작 처음엔 비활성화 되어있다가
> 손전등에다가 E키로 상호작용 했을때 맵에 있는 손전등은 비활성화 되고
> 플레이어의 flashlight는 활성화 되도록 해주면 돼
>
> (이어서) 그리고 HowToUse_Flashlight 라는 텍스트도 처음에 같이 꺼져있다가 상호작용 이후에 같이 활성화 되도록 해줘

[[0034-flashlight-pickup-interaction]], [[0035-flashlight-starts-inactive]]의 후속 — 별도 제안서 없이 바로 진행.

## 조사 내용 (중요 — 원인 분석)
- 씬을 다시 확인해보니 [[0035-flashlight-starts-inactive]]에서 추가했던 `flashlight`의 `m_IsActive: 0` 오버라이드와, [[0034-flashlight-pickup-interaction]]에서 연결했던 `Interactable.equipTarget` 참조(`flashlight` 오브젝트를 가리키는 값)가 씬 파일에서 사라져 있었음 (`equipTarget: {fileID: 0}`으로 비어있고, 내가 만들었던 로컬 참조용 stripped GameObject 블록 자체가 파일에서 통째로 없어짐).
- 원인으로 추정되는 것: `equipTarget`이 가리키던 대상은 손으로 새로 만든 "stripped GameObject" 로컬 참조였는데, 이건 해당 오브젝트가 속한 프리팹 인스턴스(`flashlight`, PrefabInstance 193342755) 자신의 override 목록(`m_Modifications`/`m_AddedComponents`) 그 어디에도 등록되지 않은 채로 다른 프리팹 인스턴스(`FlashLight_low-Poly`)의 컴포넌트에서만 참조됐음 → Unity 에디터가 씬을 열고 다시 저장하는 과정에서 이렇게 "다른 프리팹 인스턴스 경계를 넘어 참조되는, 정식으로 등록되지 않은" 로컬 참조를 깨진 참조로 판단해 정리(제거)한 것으로 보임. (반면 `FlashLight_low-Poly` 자신의 컴포넌트 추가는 `m_AddedComponents`에 정식으로 등록돼 있어서 문제없이 유지됨.)
- 즉 씬 파일을 텍스트로 직접 편집해서 "서로 다른 프리팹 인스턴스 사이의 오브젝트 참조"를 만드는 방식은 Unity 에디터가 씬을 다시 저장할 때 깨질 수 있는, 신뢰할 수 없는 방법이라는 걸 확인함.

## 해결 방향 변경
씬 파일에 손으로 크로스-인스턴스 참조를 심는 대신, 코드에서 런타임에 직접 찾아가는 방식으로 변경. 씬 저장/리로드에 영향받지 않음.

### `Assets/My/Scripts/Interaction/Interactable.cs`
- `InteractionType`에 `Flashlight` 케이스 추가
- `Flashlight` 케이스 처리: `GameObject.FindGameObjectWithTag("Player")`로 플레이어를 찾고 `transform.Find("PlayerCameraRoot/flashlight")`로 손전등 오브젝트를 찾아 활성화, `GameObject.Find("HowToUse_Flashlight")`로 안내 텍스트를 찾아 활성화, 자기 자신(맵의 `FlashLight_low-Poly`)은 비활성화
```csharp
private void ActivatePlayerFlashlight()
{
    GameObject player = GameObject.FindGameObjectWithTag("Player");
    Transform flashlight = player != null ? player.transform.Find("PlayerCameraRoot/flashlight") : null;
    if (flashlight != null)
        flashlight.gameObject.SetActive(true);

    GameObject hint = GameObject.Find("HowToUse_Flashlight");
    if (hint != null)
        hint.SetActive(true);

    gameObject.SetActive(false);
}
```
- 기존 `equipTarget` 필드/`Pickup()` 로직은 그대로 남겨둠(다른 줍기형 아이템에서 여전히 쓸 수 있음, 단 이번처럼 씬을 텍스트로 직접 편집해 연결하지 말고 에디터 인스펙터에서 드래그로 연결할 것 — 안 그러면 이번처럼 사라질 수 있음).

### `Assets/Scenes/InGame.unity`
- `FlashLight_low-Poly`의 `Interactable.type`을 `0`(Pickup) → `3`(Flashlight)으로 변경
- `flashlight`에 `m_IsActive: 0` 오버라이드 재추가 (시작 시 비활성화)
- `HowToUse_Flashlight`의 `m_IsActive`를 `1` → `0`으로 변경 (시작 시 비활성화). 이 오브젝트는 중첩 프리팹이 아닌 일반 오브젝트라 자기 자신의 필드를 직접 고치는 것뿐이라 안전함 — 이번 문제(크로스 인스턴스 참조)와 무관.

## 결과
계획대로 적용 완료.

## 남은 작업 / 확인 필요
- **중요**: Unity 에디터에 이 씬이 열려있는 상태였다면, 지금 바로 플레이하지 말고 씬을 다시 로드(창 닫았다 열기 또는 씬 애셋 우클릭 → Reimport)한 뒤 테스트해줘. 에디터가 내가 고친 파일을 못 읽은 상태에서 저장하면 이번 수정도 덮어써질 수 있음.
- `PlayerCameraRoot/flashlight`, `HowToUse_Flashlight` 이름/경로가 앞으로 바뀌면 `Interactable.cs`의 하드코딩된 경로도 같이 고쳐야 함 (이번 건 특정 오브젝트 전용 처리라 일반화하지 않음).

## 변경된 파일
- `Assets/My/Scripts/Interaction/Interactable.cs`
- `Assets/Scenes/InGame.unity`
