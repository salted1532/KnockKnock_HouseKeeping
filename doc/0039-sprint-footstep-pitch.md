# 0039 - 달리기 시 발소리 재생 속도 2배

## 날짜
2026-08-20

## 요청 내용 (원문)
> 현재 플레이어가 Shift를 누르면 달리기가 가능한데 그러면 재생속도도 2배로 빨라지도록 해줘

간단한 기능 추가라 별도 제안서 없이 바로 진행.

## 조사 내용
- Shift(스프린트) 입력 상태는 StarterAssets `StarterAssetsInputs.sprint`(public bool)에 이미 저장되고 있음 — `PlayerCapsule`에 이미 붙어있는 컴포넌트라 `FootstepSystem`에서 `GetComponent<StarterAssetsInputs>()`로 바로 가져다 쓸 수 있음.
- "재생 속도"는 `AudioSource.pitch`로 조절 — pitch를 2로 하면 클립이 2배 빠르게(+음높이도 같이 올라감) 재생됨. Unity에서 오디오 재생 속도를 바꾸는 표준적인 방법.

## 변경 내용

### `Assets/My/Scripts/Player/FootstepSystem.cs`
```diff
+using StarterAssets;
 using UnityEngine;

 [RequireComponent(typeof(CharacterController))]
 public class FootstepSystem : MonoBehaviour
 {
     [SerializeField] private float stepDistance = 2f;
     [SerializeField] private float rayDistance = 1.5f;
     [SerializeField] private LayerMask groundMask = ~0;
+    [SerializeField] private float sprintPitch = 2f;

     private CharacterController controller;
+    private StarterAssetsInputs input;
     private float distanceAccumulator;

     private void Awake()
     {
         controller = GetComponent<CharacterController>();
+        input = GetComponent<StarterAssetsInputs>();
     }
     ...
     private void PlayFootstep()
     {
         if (!Physics.Raycast(...)) return;
-        SoundManager.Instance?.PlayFootstep(hit.collider.gameObject.layer);
+        float pitch = (input != null && input.sprint) ? sprintPitch : 1f;
+        SoundManager.Instance?.PlayFootstep(hit.collider.gameObject.layer, pitch);
     }
```

### `Assets/My/Scripts/Audio/SoundManager.cs`
```diff
-    public void PlayFootstep(int groundLayer)
+    public void PlayFootstep(int groundLayer, float pitch = 1f)
     {
         if (footstepSource == null || footstepSource.isPlaying) return;
         ...
         if (clips == null || clips.Length == 0) return;
+        footstepSource.pitch = pitch;
         footstepSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
     }
```

## 결과
계획대로 적용 완료. 걷을 땐 기존대로, Shift로 달릴 땐 발소리가 2배 속도(피치)로 재생됨. `stepDistance` 기반 이동거리 누적 방식이라 달릴 때는 재생 "빈도"도 자연히 더 잦아짐 — 속도(피치)와 빈도가 같이 빨라져서 뛰는 느낌이 강조됨.

## 변경된 파일
- `Assets/My/Scripts/Player/FootstepSystem.cs`
- `Assets/My/Scripts/Audio/SoundManager.cs`
