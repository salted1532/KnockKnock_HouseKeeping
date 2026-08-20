# 0028 - 발소리 클립 변형(랜덤 재생) 제안

## 날짜
2026-08-20

## 요청 내용 (원문)
> 각 발자국 소리마다 한 4개정도씩 넣을수 있게 하고 랜덤하게 사운드가 재생되도록해줘

## 조사 내용
- 현재 `SoundManager.cs`는 재질별로 `AudioClip` 1개씩(wood/concrete/metal/grass)만 가지고 있음 ([[0027-footstep-system-proposal]] 참고).
- 재질별로 배열(`AudioClip[]`)로 바꾸고 재생 시 `Random.Range`로 하나 골라 재생하면 됨.

## 계획

### `Assets/My/Scripts/Audio/SoundManager.cs`
`AudioClip` 단일 필드 4개 → `AudioClip[]` 4개로 변경, 재생 시 배열에서 랜덤 선택:
```csharp
[Header("Footstep")]
[SerializeField] private AudioSource footstepSource;
[SerializeField] private AudioClip[] woodStepClips;
[SerializeField] private AudioClip[] concreteStepClips;
[SerializeField] private AudioClip[] metalStepClips;
[SerializeField] private AudioClip[] grassStepClips;
...
public void PlayFootstep(int groundLayer)
{
    if (footstepSource == null) return;

    AudioClip[] clips =
        groundLayer == woodLayer ? woodStepClips :
        groundLayer == concreteLayer ? concreteStepClips :
        groundLayer == metalLayer ? metalStepClips :
        groundLayer == grassLayer ? grassStepClips : null;

    if (clips == null || clips.Length == 0) return;
    footstepSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
}
```
- 인스펙터에서 각 배열에 클립을 4개씩(또는 원하는 개수만큼) 채우면 됨. 개수 제한은 두지 않음(가변 배열).

## 결과
요청 자체가 이미 구체적인 스펙을 포함하고 있어 계획대로 바로 적용함.

## 변경 파일
- `Assets/My/Scripts/Audio/SoundManager.cs` — 재질별 `AudioClip` → `AudioClip[]`로 변경, `Random.Range`로 랜덤 재생
