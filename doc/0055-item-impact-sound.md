# 0055 - 아이템이 바닥에 떨어질 때 소리 재생

## 요청
> 아이템이 바닥에 떨어졌을때 소리가 났으면 좋겠는데 어떤식으로 작동시키는게 좋을까
> (제안한 방향에 대해) 진행시켜줘

## 조사
- `Assets/My/Scripts/Audio/SoundManager.cs`의 `PlayFootstep()` 패턴 참고: 클립 배열 + `Random.Range`로 랜덤 재생, 짧은 `isPlaying` 체크로 남발 방지.
- 아이템은 `Interactable`이 붙은 원본 pickup 오브젝트(`InventorySystem.cs`의 `pickupSources`)가 던져지거나 필드에 그냥 놓여있을 수 있음 → `InventorySystem`에서 중앙 처리하면 "그냥 놓여있다 떨어지는" 케이스는 못 잡음. 아이템 프리팹 자체에 컴포넌트를 붙이는 방식으로 결정 (사용자 승인).

## 적용한 변경
새 파일 `Assets/My/Scripts/Interaction/ItemImpactSound.cs` 추가:
- `OnCollisionEnter`에서 `collision.relativeVelocity.magnitude`가 `minImpactVelocity`(기본 1.5) 이상일 때만 재생
- `cooldown`(기본 0.2초) 동안 재충돌 무시 → 바닥에 통통 튈 때 소리 난사 방지
- `impactClips` 배열에서 랜덤 선택, `AudioSource.PlayClipAtPoint`로 위치 기반 원샷 재생 (전용 AudioSource 불필요)

## 남은 작업 (에디터에서 수동으로 해야 함)
- 아이템 프리팹들(예: 소다 캔 등 `Interactable` Pickup 타입 오브젝트)에 `ItemImpactSound` 컴포넌트 추가
- `impactClips`에 낙하/충돌 사운드 클립 할당
- 필요하면 재질별로 다른 클립을 쓰고 싶을 때는 `SoundManager`의 레이어 분기(Wood/Concrete/Metal/Grass)처럼 `collision.collider.gameObject.layer` 기준 분기 추가 가능 (지금은 재질 구분 없이 클립 배열 하나만 사용하는 단순 버전)

## 후속 수정 - 랜덤 클립 연속 반복 방지
> 조금에 디테일을 주면 처음에 랜덤하게 발생한 소리는 다음 랜덤때는 바로 다시나오지 안도록해줄래

`lastClipIndex` 필드를 추가해 직전에 재생한 클립 인덱스를 기억하고, 클립이 2개 이상이면 같은 인덱스가 다시 뽑히지 않을 때까지 재추첨.

## 변경된 파일
- `Assets/My/Scripts/Interaction/ItemImpactSound.cs` (신규 + 후속 수정)
