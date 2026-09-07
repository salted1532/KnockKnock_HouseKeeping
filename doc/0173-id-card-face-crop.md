# 0173 – 신분증 profile_Image 에 손님 얼굴 크롭

## 요청
신분증 UI(`Canvas/ID_image`)에 추가한 `profile_Image` 에, 각 숙박객의 초상화 스프라이트에서 **얼굴 부분만** 잘라서 넣기.

## 현황 (조사)
- `Canvas/ID_image` 자식: `Text (TMP)`(정보) / `BG`(200×200 프레임) / **`profile_Image`**(Image, 200×200, 현재 `회사원_화남_0` 하드코딩)
- `ReceptionIdCard`(Canvas 부착) 가 `id_show` 노드에서 `Populate(npc)` 로 텍스트 채움 — 여기에 사진 세팅 추가하면 됨
- 초상화: `NpcData.neutralPortrait` 9명 전부 있음. `idCard.photo` 는 전부 비어 있음
- 스프라이트 = 풀바디, `spriteMode=Multiple`, `isReadable=false`, `sprite.textureRect` = 실제 픽셀의 타이트 바운딩(예 회사원: 765×1024 중 x189~565, y15~982). 얼굴 = textureRect 상단 ~30%
- `Sprite.Create(texture, subRect, pivot)` 는 non-readable 텍스처도 **렌더는 됨**(GetPixels 만 불가) → 런타임 얼굴 크롭 가능

## 답: 가능. 방식 2가지

### A. 자동 크롭 (설정 0, 근사)
`sprite.textureRect` 상단의 정사각형(높이 = textureRect.height × `faceHeightFrac`, 기본 0.34, x 는 textureRect 중앙)을 잘라 `Sprite.Create`. NpcData 건드릴 것 없음. 아트 프레이밍이 제각각이면 이마가 잘리거나 어깨가 들어올 수 있음.

### B. 손님별 얼굴 사각형 (정확, 1회 세팅)
`NpcData` 에 `Rect faceRect01`(textureRect 기준 정규화 0~1) 추가 → 인스펙터에서 4값 조절. 비어 있으면(0) A 휴리스틱으로 폴백.

**권장: A + B 폴백 둘 다 넣기.** 기본은 자동, 이상한 손님만 `faceRect01` 로 보정.

## 작업 (A+B)

### 1. 신규 `Assets/My/Scripts/Interaction/PortraitFaceCrop.cs` (~45줄)
- `Show(Sprite portrait, Rect faceRect01)`:
  - `faceRect01` 이 유효하면 그걸로, 아니면 휴리스틱으로 소스 픽셀 사각형 계산 (`portrait.textureRect` 기준)
  - `Sprite.Create(portrait.texture, srcRect, (0.5,0.5), portrait.pixelsPerUnit)` → `image.sprite`, `image.preserveAspect = true`
  - 이전에 만든 스프라이트는 `Destroy` (누수 방지)
- 인스펙터: `image`, `faceHeightFrac`(0.34), `faceTopPad`(0.02)

### 2. `NpcData` 에 필드
```csharp
[Header("신분증 얼굴 크롭 (비우면 자동)")]
[Tooltip("초상화 textureRect 기준 정규화된 얼굴 영역. w 또는 h 가 0 이면 자동 크롭")]
public Rect faceRect01;
```

### 3. `ReceptionIdCard`
- `[SerializeField] private PortraitFaceCrop profilePhoto;`
- `Populate(npc)` 끝에 `if (profilePhoto != null) profilePhoto.Show(npc.neutralPortrait, npc.faceRect01);`

### 4. 씬 배선 (`InGame.unity`)
- `Canvas/ID_image/profile_Image` 에 `PortraitFaceCrop` 부착, `image` = 자기 Image, `preserveAspect` on
- `ReceptionIdCard.profilePhoto` = 그 컴포넌트
- `uloop compile` + 플레이모드에서 9명 얼굴 크롭 확인, 이상한 손님은 `faceRect01` 로 보정

## 안 하는 것
- `idCard.photo`(수동 사진 에셋) 사용 안 함 — 초상화에서 파생 (단일 소스)
- 얼굴 인식/자동 감지 — 휴리스틱 + 수동 보정으로 충분

## 결과 (구현·플레이모드 검증 완료)
- 신규 `PortraitFaceCrop.cs`, `NpcData.faceRect01`, `ReceptionIdCard.profilePhoto` + `Populate` 호출. `uloop compile` 에러 0
- `Canvas/ID_image/profile_Image` 에 `PortraitFaceCrop`(image=자기 Image, faceHeightFrac 0.34, faceTopPad 0.02), `preserveAspect` on. `ReceptionIdCard.profilePhoto` 배선
- 9명 초상화 = 전부 풀바디, 캐릭터가 상단 중앙에 세로로 프레이밍 → 자동 휴리스틱(textureRect 상단 34% 정사각형, x 중앙) 이 잘 맞음
- 플레이모드: id1(Drifter), id7(Father, 원래 아버지+아들 이미지라 걱정했으나 아버지가 더 커서 상단 중앙에 얼굴 위치 → OK) 스크린샷 확인 — 얼굴+어깨 여권사진처럼 나옴
- `faceRect01` 오버라이드는 아직 아무 손님도 안 씀 (휴리스틱으로 충분). 이상하게 나오는 손님 생기면 인스펙터에서 4값 조절

### 후속: 정수리 잘림
`faceTopPad`(위에서 아래로 내려 자름) → `headroom`(정수리 위 여백, 양수=여백 더, 음수=정수리부터 자름)으로 교체. 기본 0.04. 스크린샷 확인 — Drifter 머리 위 여백 생기고 정수리 안 잘림.

## 수정 파일
- `Assets/My/Scripts/Interaction/PortraitFaceCrop.cs` (신규)
- `Assets/My/Scripts/Dialogue/NpcData.cs` (`faceRect01` 필드)
- `Assets/My/Scripts/Interaction/ReceptionIdCard.cs` (`profilePhoto` + Populate 호출)
- `Assets/Scenes/InGame.unity` (profile_Image 에 PortraitFaceCrop, ReceptionIdCard 배선)
