# 0029 - 발소리 클립 연결 및 기본 발소리(콘크리트) 처리

## 날짜
2026-08-20

## 요청 내용 (원문)
> Sound폴더 안에 FootStep폴더 안에 해당하는 사운드 클립들 넣어뒀어
> 콘크리트 경우 3개밖에 없으니깐 3개로 구성되도록 하고
> 만약 layer를 못찾아서 기본 발소리는 콘크리트 발소리야
> 해당하는 위치에 연결시켜줘

이미 승인된 발소리 시스템([[0027-footstep-system-proposal]], [[0028-footstep-clip-variation-proposal]])의 후속 연결 작업이라 별도 제안서 승인 없이 바로 진행.

## 조사 내용
- 클립 위치: `Assets/My/InGame/Sound/FootStep/` — wood 4개, concrete 3개(`footstep_concreate`, `2`, `3`), metal 4개, grass 4개.
- `SoundManager`가 실제로 배치된 곳: `Assets/Scenes/InGame.unity`의 `DayNightSwitcher`/`SoundManager`가 붙은 GameObject(fileID 1080674261). 씬에는 발소리 배열 필드가 예전 필드명(`woodStepClip` 등, 단일값)으로 남아있어 갱신 필요했음.
- 발소리 전용 AudioSource가 없어서 같은 GameObject 밑에 자식 오브젝트 `FootstepAudioSource`(Transform+AudioSource, PlayOnAwake 꺼짐)를 새로 만들어 `footstepSource`에 연결.
- 실제 플레이어는 `Assets/AssetsFolder/StarterAssets/FirstPersonController/Prefabs/PlayerCapsule.prefab` (여러 씬에서 공유) → 여기에 `FootstepSystem` 컴포넌트를 직접 추가해서 모든 씬에 자동 반영되게 함.

## 변경 내용

### `Assets/My/Scripts/Audio/SoundManager.cs`
레이어가 4종 중 어디에도 안 맞으면(찾지 못하면) concrete로 폴백:
```csharp
AudioClip[] clips =
    groundLayer == woodLayer ? woodStepClips :
    groundLayer == metalLayer ? metalStepClips :
    groundLayer == grassLayer ? grassStepClips : concreteStepClips;
```

### `Assets/Scenes/InGame.unity`
- SoundManager MonoBehaviour 필드를 새 배열 필드명으로 갱신하고 클립 연결
  - `woodStepClips`: wood1~4
  - `concreteStepClips`: concreate, concreate2, concreate3 (3개)
  - `metalStepClips`: metal1~4
  - `grassStepClips`: grass1~4
- `footstepSource`용 자식 GameObject `FootstepAudioSource`(AudioSource, Loop 꺼짐, PlayOnAwake 꺼짐) 신규 생성 후 연결

### `Assets/AssetsFolder/StarterAssets/FirstPersonController/Prefabs/PlayerCapsule.prefab`
- 루트 GameObject에 `FootstepSystem` 컴포넌트 추가 (stepDistance: 2, rayDistance: 1.5, groundMask: Everything)

## 남은 작업
- 사용자가 씬의 바닥 오브젝트들에 Wood/Concrete/Metal/Grass 레이어 직접 지정 (레이어 자체는 [[0027-footstep-system-proposal]]에서 이미 생성됨)
- Unity 에디터에서 실제 플레이해서 발소리 재생 확인 필요 (텍스트 기반 YAML 편집이라 에디터에서 한번 열어 정상 파싱되는지 확인 권장)

## 변경된 파일
- `Assets/My/Scripts/Audio/SoundManager.cs`
- `Assets/Scenes/InGame.unity`
- `Assets/AssetsFolder/StarterAssets/FirstPersonController/Prefabs/PlayerCapsule.prefab`
