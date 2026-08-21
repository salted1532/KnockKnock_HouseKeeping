# 0045 - Cube 테스트 오브젝트에 점프스케어 사운드 연결

## 요청 내용
> 테스트용으로 제네릭에서 상호작용시 점프스케어 사운드 클립이 작동하도록 하고 싶어
> 그냥 Cube야 이미 외곽선이랑 interactable은 있어

## 조사 내용
`Assets/Scenes/InGame.unity`에서 `Cube` GameObject 확인 (fileID `1263024214`):
- `Interactable` (fileID `1263024219`): `type: 2` = `Generic`, `onInteract.m_PersistentCalls.m_Calls: []` (비어있음)
- `Outline` (fileID `1263024220`): `m_Enabled: 0` (레이 안 닿으면 꺼져있는 정상 상태)
- `BoxCollider`, `MeshRenderer` 등 이미 구성됨

`Interactable.Interact()`는 `Generic` 타입일 때 `onInteract?.Invoke()`만 호출하므로, 코드 변경 없이 `onInteract`에 `AudioSource.Play()` 콜을 등록하면 E키 상호작용 시 사운드가 재생됨.

사운드 클립 파일은 아직 프로젝트에 없음 (사용자가 나중에 임포트 예정) → 이번 변경은 `AudioSource` 컴포넌트만 추가하고 `m_audioClip`은 비워둠. 클립 임포트 후 Inspector에서 Cube의 AudioSource → Audio Clip 슬롯에 드래그만 하면 됨.

## 계획된 변경
`Assets/Scenes/InGame.unity`, Cube GameObject(`1263024214`)에:

1. `m_Component` 리스트에 새 컴포넌트 추가
```yaml
  m_Component:
  - component: {fileID: 1263024218}
  - component: {fileID: 1263024217}
  - component: {fileID: 1263024216}
  - component: {fileID: 1263024215}
  - component: {fileID: 1263024220}
  - component: {fileID: 1263024219}
  - component: {fileID: 1263024221}
```

2. 새 `AudioSource` 컴포넌트 블록 추가 (fileID `1263024221`, Play On Awake 끔, 클립 없음 → 나중에 수동 할당)
```yaml
--- !u!82 &1263024221
AudioSource:
  m_ObjectHideFlags: 0
  m_CorrespondingSourceObject: {fileID: 0}
  m_PrefabInstance: {fileID: 0}
  m_PrefabAsset: {fileID: 0}
  m_GameObject: {fileID: 1263024214}
  m_Enabled: 1
  serializedVersion: 4
  OutputAudioMixerGroup: {fileID: 0}
  m_audioClip: {fileID: 0}
  m_PlayOnAwake: 0
  m_Volume: 1
  m_Pitch: 1
  Loop: 0
  Mute: 0
  Spatialize: 0
  SpatializePostEffects: 0
  Priority: 128
  DopplerLevel: 1
  MinDistance: 1
  MaxDistance: 500
  Pan2D: 0
  rolloffMode: 0
  BypassEffects: 0
  BypassListenerEffects: 0
  BypassReverbZones: 0
  rolloffCustomCurve: (기본 AnimationCurve, 기존 FootstepAudioSource와 동일 템플릿 사용)
  panLevelCustomCurve: (동일)
  spreadCustomCurve: (동일)
  reverbZoneMixCustomCurve: (동일)
```

3. `Interactable`(`1263024219`)의 `onInteract`에 위 AudioSource의 `Play()` 등록
```yaml
  onInteract:
    m_PersistentCalls:
      m_Calls:
      - m_Target: {fileID: 1263024221}
        m_TargetAssemblyTypeName: UnityEngine.AudioSource, UnityEngine.AudioModule
        m_MethodName: Play
        m_Mode: 1
        m_Arguments:
          m_ObjectArgument: {fileID: 0}
          m_ObjectArgumentAssemblyTypeName: UnityEngine.Object, UnityEngine
          m_IntArgument: 0
          m_FloatArgument: 0
          m_StringArgument: 
          m_BoolArgument: 0
        m_CallState: 2
```

## 적용 결과
승인 후 위 계획대로 그대로 적용함 (Play() 방식 선택, PlayOneShot 아님).

## 요약 / 남은 작업
- 코드(`Interactable.cs`) 변경 없음 — 기존 `Generic` + `UnityEvent` 구조 그대로 사용
- 점프스케어 클립 파일을 Assets에 임포트한 뒤 Cube의 AudioSource → Audio Clip에 직접 할당 필요 (이 작업은 자동화하지 않음)
- 변경 파일: `Assets/Scenes/InGame.unity`
