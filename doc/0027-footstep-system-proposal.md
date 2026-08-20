# 0027 - 발소리(Footstep) 시스템 제안

## 날짜
2026-08-20

## 요청 내용 (원문)
> 발소리가 나도록 하고 싶은데 발소리 시스템으로 해서 사운드 매니저에다가 발소리도 추가해줘
> 발소리는 4가지 종류로 변하며 나무, 콘크리트, 철(메탈), 흙(grass) 로 나누어져서 4가지 소리가 바뀌면서 나고
> 밟고 있는 각 오브젝트의 layer를 감지하여 변경된 발소리가 출력되도록 해줘
> 현재 사용하고 있는 firstPersonController를 통해 캐릭터가 이동하는데
> 이를 사용해도 되고 아니면 부가적인 스크립트를 붙여서 만드는것도 좋은거 같아
> Layer가 필요하면 일단 4가지 layer만들고 내가 씬에선 직접 layer 설정 할게

## 조사 내용
- 실제 사용 중인 컨트롤러 확인: `Assets/AssetsFolder/StarterAssets/FirstPersonController/Prefabs/PlayerCapsule.prefab`가 StarterAssets `FirstPersonController.cs` (guid `55919ac3...`)를 참조하고 있음 → 이게 실제 플레이어. `ModularFirstPersonController`는 프로젝트에 있지만 프리팹에서 참조되지 않아 미사용으로 판단.
- StarterAssets `FirstPersonController.cs`는 벤더 패키지 코드라 직접 수정하지 않고, `CharacterController`를 요구하는 별도 스크립트(`FootstepSystem`)를 같은 플레이어 오브젝트에 부착하는 방식 채택 (요청에서도 "부가적인 스크립트" 옵션 언급).
- 기존 `Assets/My/Scripts/Audio/SoundManager.cs`: 낮/밤 앰비언트 루프 재생만 담당, 싱글톤 아님. 발소리 재생을 여기 추가하려면 다른 스크립트(FootstepSystem)가 참조할 수 있도록 `static Instance`가 필요.
- 발소리용 AudioSource는 앰비언트용(loop) AudioSource와 분리해야 함 → SoundManager에 `footstepSource` 필드 추가(별도 AudioSource, 인스펙터에서 수동 연결 또는 자동 추가).
- Layer: `ProjectSettings/TagManager.asset` 확인 결과 7~31번 슬롯이 비어있음. 7~10번에 `Wood`, `Concrete`, `Metal`, `Grass` 4개 레이어 이름을 추가할 예정 (씬 오브젝트에 레이어 지정은 사용자가 직접 진행).

## 계획

### 1. `ProjectSettings/TagManager.asset` - 레이어 4개 추가
```yaml
  layers:
  - Default
  - TransparentFX
  - Ignore Raycast
  - 
  - Water
  - UI
  - PostProcess
  - Wood       # 7번
  - Concrete   # 8번
  - Metal      # 9번
  - Grass      # 10번
  -            # 11번 이후 계속 비움
  ...
```

### 2. `Assets/My/Scripts/Audio/SoundManager.cs` - 싱글톤 + 발소리 재생 추가
기존 코드에 아래 내용 추가 (앰비언트 재생 로직은 그대로 유지):
```csharp
public static SoundManager Instance { get; private set; }

[Header("Footstep")]
[SerializeField] private AudioSource footstepSource;
[SerializeField] private AudioClip woodStepClip;
[SerializeField] private AudioClip concreteStepClip;
[SerializeField] private AudioClip metalStepClip;
[SerializeField] private AudioClip grassStepClip;

private int woodLayer, concreteLayer, metalLayer, grassLayer;

private void Awake()
{
    Instance = this;
    source = GetComponent<AudioSource>();
    source.loop = true;

    woodLayer = LayerMask.NameToLayer("Wood");
    concreteLayer = LayerMask.NameToLayer("Concrete");
    metalLayer = LayerMask.NameToLayer("Metal");
    grassLayer = LayerMask.NameToLayer("Grass");
}

public void PlayFootstep(int groundLayer)
{
    if (footstepSource == null) return;

    AudioClip clip =
        groundLayer == woodLayer ? woodStepClip :
        groundLayer == concreteLayer ? concreteStepClip :
        groundLayer == metalLayer ? metalStepClip :
        groundLayer == grassLayer ? grassStepClip : null;

    if (clip == null) return;
    footstepSource.PlayOneShot(clip);
}
```
- 인스펙터에서 `footstepSource`(별도 AudioSource, loop 꺼짐), 그리고 4개 AudioClip을 수동으로 연결해야 함.

### 3. `Assets/My/Scripts/Player/FootstepSystem.cs` (신규) - 플레이어에 부착
```csharp
using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FootstepSystem : MonoBehaviour
{
    [SerializeField] private float stepDistance = 2f;
    [SerializeField] private float rayDistance = 1.5f;
    [SerializeField] private LayerMask groundMask = ~0;

    private CharacterController controller;
    private float distanceAccumulator;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!controller.isGrounded)
        {
            distanceAccumulator = 0f;
            return;
        }

        Vector3 horizontalVelocity = new Vector3(controller.velocity.x, 0f, controller.velocity.z);
        float speed = horizontalVelocity.magnitude;
        if (speed < 0.1f)
        {
            distanceAccumulator = 0f;
            return;
        }

        distanceAccumulator += speed * Time.deltaTime;
        if (distanceAccumulator >= stepDistance)
        {
            distanceAccumulator = 0f;
            PlayFootstep();
        }
    }

    private void PlayFootstep()
    {
        if (!Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, rayDistance, groundMask, QueryTriggerInteraction.Ignore))
            return;

        SoundManager.Instance?.PlayFootstep(hit.collider.gameObject.layer);
    }
}
```
- 이동 거리를 누적해서 일정 거리(`stepDistance`, 기본 2m)마다 한 번씩 발밑으로 레이캐스트 → 맞은 오브젝트의 layer를 SoundManager에 전달.
- 걷기/달리기 속도에 비례해 자연스럽게 발소리 간격이 달라짐 (별도 sprint 분기 불필요).
- StarterAssets `FirstPersonController.cs`는 전혀 수정하지 않음 (벤더 코드 보존).

## 결과
승인 후 계획대로 적용 완료.

## 남은 작업 (씬 작업, 사용자 진행)
1. 플레이어(PlayerCapsule) 오브젝트에 `FootstepSystem` 컴포넌트 부착
2. SoundManager 오브젝트에 발소리 전용 AudioSource(footstepSource, loop 꺼짐)를 만들어 연결하고, 4개 발소리 AudioClip(woodStepClip/concreteStepClip/metalStepClip/grassStepClip) 연결
3. 씬의 바닥 오브젝트들에 Wood/Concrete/Metal/Grass 레이어 직접 지정 (Unity 에디터에서 레이어 4개는 이미 생성됨)

## 변경된 파일
- `ProjectSettings/TagManager.asset` — Wood/Concrete/Metal/Grass 레이어 4개 추가 (7~10번 슬롯)
- `Assets/My/Scripts/Audio/SoundManager.cs` — 싱글톤화, 발소리 전용 AudioSource/클립 필드, `PlayFootstep(int layer)` 추가
- `Assets/My/Scripts/Player/FootstepSystem.cs` (신규) — 이동 거리 누적 기반으로 발밑 레이캐스트, SoundManager에 layer 전달
