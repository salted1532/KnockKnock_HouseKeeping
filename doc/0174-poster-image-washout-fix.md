# 0174 – Poster 이미지 하얗게 보이는 문제

## 요청
`Poster` 프리팹(Canvas + 이미지 출력 하나만) 의 이미지가 하얗게/뿌옇게 보임. 고쳐줘.

## 원인
`Assets/My/InGame/Prefabs/Item/Poster.prefab` 루트에 **회색 Lit Plane 메쉬**가 있었음:
- `MeshFilter`(Plane) + `MeshRenderer`(머티리얼 `Lit`, URP/Lit, `_BaseColor` = 회색 0.5) + `MeshCollider`
- 이 판이 자식 `Canvas`(WorldSpace) 의 `Image` 와 거의 같은 평면에 겹쳐 있어서, 불투명 회색 Lit 면이 이미지를 덮음 → "이미지가 하얀색/뿌옇게" 보임.
- Canvas/Image 자체는 정상 (`UI/Default`, color 흰색, sprite 있음).

## 수정
`Poster.prefab` 루트에서 **`MeshRenderer` / `MeshFilter` / `MeshCollider` 제거**. "이미지 출력 하나만" 이 목적이라 판 메쉬는 불필요.
- 추가 정리: `Image.raycastTarget = false`, Canvas 의 `GraphicRaycaster` 제거 (볼거리일 뿐 클릭 대상 아님), `Image.color` 흰색 확정.
- 결과: 루트 = `Transform` 만. Canvas 의 Image 가 sprite 를 그대로 출력 (뿌옇지 않음).

## 검증
프리팹 스테이지 정면 뷰 스크린샷 — 이미지가 선명하게, 원래 색으로 표시됨 (하얗게 안 보임). 저장된 `Poster.prefab` 루트 컴포넌트 = `[Transform]` 만.

## 수정 2 — 어두운 곳에서도 밝게 나옴 (조명 미반영)
Canvas/Image 는 `UI/Default` = **무조명**. 어두운 방에서도 포스터만 풀밝기 → 라이트박스처럼 보임. URP 엔 조명 받는 기본 UI 셰이더가 없어 Canvas 구조로는 해결 불가.

**Canvas → Quad 메쉬로 교체:**
- `Poster.prefab` 루트: Canvas 자식·UI 컴포넌트 전부 제거 → `MeshFilter`(Quad) + `MeshRenderer`.
- 신규 `Assets/My/InGame/Prefabs/Item/Poster_Mat.mat` (URP/Lit, `_BaseMap` = 포스터 텍스처, Smoothness 0, Metallic 0). 그림자 던지기 Off(평면), 받기 On.
- 루트 rot (0,-90,0) scale (1,1,1) → 1×1 m, +X 향함.
- 결과 루트 = `[Transform, MeshFilter, MeshRenderer]`. **이제 씬 조명을 받아 어두운 방에선 어두워짐.**

## 참고
- `Poster_Mat.mat` 의 `Base Map` 이 현재 `BriefingTV_TestPattern` (프리팹에 있던 스프라이트). 실제 포스터 이미지로 바꾸려면 이 머티리얼의 Base Map 만 교체.
- 포스터는 로컬 +X 를 향함. 벽에 배치 시 인스턴스 회전으로 맞춤.
