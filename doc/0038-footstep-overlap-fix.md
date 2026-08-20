# 0038 - 발소리 겹쳐 재생되는 문제 수정

## 날짜
2026-08-20

## 요청 내용 (원문)
> 발소리 사운드가 이전 사운드가 끝나기도 전에 재생되는거 같아
> 그래서 발소리가 너무 빠르게 다다다다 들리는데 이것좀 고쳐줘

간단한 버그 수정이라 별도 제안서 없이 바로 진행.

## 조사 내용
- `SoundManager.PlayFootstep()`이 `footstepSource.PlayOneShot(...)`을 매번 호출하는데, `PlayOneShot`은 이전 재생을 끊지 않고 새 소리를 겹쳐서 재생함.
- `FootstepSystem`은 이동 거리 누적 방식(`stepDistance` 기본 2m)으로 재생 주기를 정하는데, 씬의 `PlayerCapsule` 이동 속도가 `MoveSpeed: 12`, `SprintSpeed: 18`로 꽤 빠르게 세팅되어 있어 걷기만 해도 약 0.17초, 달리면 약 0.11초마다 재생 트리거가 발생함. 발소리 클립 길이가 이보다 길면 이전 소리가 끝나기 전에 다음 소리가 겹쳐 재생되어 "다다다다"처럼 들림.

## 변경 내용

### `Assets/My/Scripts/Audio/SoundManager.cs`
```diff
     public void PlayFootstep(int groundLayer)
     {
-        if (footstepSource == null) return;
+        if (footstepSource == null || footstepSource.isPlaying) return;
```
- 이전 발소리가 아직 재생 중이면 새 발소리를 재생하지 않도록 해서 겹침을 원천 차단. 이동 속도나 `stepDistance` 값과 무관하게 항상 한 번에 하나씩만 재생됨.

## 결과
계획대로 적용 완료.

## 변경된 파일
- `Assets/My/Scripts/Audio/SoundManager.cs`
