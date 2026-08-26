# 0059 - 발소리 랜덤 재생 시 직전과 같은 클립 제외

## 요청 내용
> 발소리 나는거에서 랜덤으로 작동하는데 첫 발소리 이후 다음 발소리는 이전 발소리랑 같은거는 빼도록해줘

## 조사 내용
- `SoundManager.PlayFootstep(int groundLayer, float pitch)`: 바닥 레이어(wood/metal/grass/concrete)에 맞는 `AudioClip[]`을 고른 뒤 `Random.Range(0, clips.Length)`로 완전 무작위 선택 → 같은 클립이 연속으로 나올 수 있음
- 재생 클립을 저장해뒀다가, 다음 선택 시 그 클립과 같으면 배열 내 다음 인덱스로 넘겨서 항상 다른 클립이 나오도록 함 (배열 원소가 1개뿐이면 그대로 재생)
- 바닥 재질이 바뀌어도(예: wood → concrete) "직전 재생 클립"과 비교하므로 자연스럽게 동작 (다른 배열이라 클립 객체 자체가 다르면 그냥 새로 뽑은 게 유지됨)

## 계획된 변경

**`SoundManager.cs`**
```diff
     private int woodLayer, concreteLayer, metalLayer, grassLayer;
     private bool isNight = true;
+    private AudioClip lastFootstepClip;
```
```diff
         if (clips == null || clips.Length == 0) return;
+
+        AudioClip clip = clips[Random.Range(0, clips.Length)];
+        if (clips.Length > 1 && clip == lastFootstepClip)
+            clip = clips[(System.Array.IndexOf(clips, clip) + 1) % clips.Length];
+        lastFootstepClip = clip;
+
         footstepSource.pitch = pitch;
-        footstepSource.PlayOneShot(clips[Random.Range(0, clips.Length)]);
+        footstepSource.PlayOneShot(clip);
```

## 동작 요약
- 매 발소리마다 무작위로 하나 뽑되, 직전에 재생한 클립과 같으면 배열의 다음 클립으로 바꿔서 재생 (같은 클립 연속 재생 방지)

## 적용 결과
계획대로 적용함.
