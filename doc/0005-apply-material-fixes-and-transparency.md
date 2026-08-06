# 0005. Branch/Bush/Plants 투명화 + 머티리얼 깨짐 수정 적용

## 날짜
2026-08-06

## 요청 내용
> Branch, 01,02 Bush,01~07 plants,01~09 들 투명화 작업해주고 머티리얼 깨진부분도 고치는거 진행해줘

[[0003-assetsfolder-material-fix-proposal]], [[0004-bush-transparency-black-investigation]]에 대한 승인으로 판단하고 실제 적용함.

## 조사 내용 (추가 발견)
`All.fbx`를 (기존엔 `.dae`만 분석했던) 실제 `.fbx` 기준으로 다시 전수 비교하니, 0003에서 못 찾았던 참조가 하나 더 있었음: **`Curtains.jpg`** — 프로젝트 어디에도 존재하지 않는 파일(대체 후보도 없음). `All`에는 `Bush`/`Branch` 텍스처는 없고, `Plants.png` 1장을 `Plants`, `Plants_01`~`Plants_05` 총 6개 머티리얼 슬롯이 공유해서 쓰는 구조였음(알파 채널 있음, RGBA).

`models.fbx`(Models pack psx) 쪽은 기존 0003 조사(당시 `.dae` 기준)와 `.fbx` 기준 재확인 결과가 동일했음.

**정정 (사용자 확인 질문에 따른 재검증, 같은 날)**: 처음엔 `All.fbx`에 `Plants`, `Plants_01`~`Plants_05` 이렇게 별도 머티리얼 6개가 있는 줄 알고 6개를 다 만들었는데, FBX 내부 오브젝트 타입까지 확인해보니 실제 **머티리얼은 `Plants` 1개뿐**이었음. `Plants_01`~`Plants_05`는 머티리얼이 아니라 그 머티리얼을 같이 쓰는 별개의 메시/모델 인스턴스(화분 개체 5개) 이름이었음 — 착각해서 만든 `Plants_01.mat`~`Plants_05.mat`은 삭제함(실제 FBX에 그 이름의 머티리얼이 없어서 Unity가 매칭도 안 했을 파일들). `Models pack psx` 쪽 20개(Branch/Bush/Plants 계열)는 FBX 내부에 실제로 각각 별도 Material 오브젝트로 존재하는 걸 재확인함 — 그쪽은 정정 없음.

## 적용한 변경

### 1) 텍스처 파일명 불일치 수정 (머티리얼 깨짐, 파일 복사만 — 모델/머티리얼은 안 건드림)
Unity의 "이름이 일치하는 텍스처를 폴더에서 자동 검색" 기능을 이용하기 위해, 모델이 기대하는 파일명 그대로 기존 텍스처를 복사해서 추가함:

| 추가한 파일 | 원본 |
|---|---|
| `All/Textures/Marble.jpg` | `Marble_01.png`을 복사(가장 확신도 높은 기본값으로 선택. 다른 후보: `Marble_02`, `Marble_03`) |
| `All/Textures/Metal.jpg` | `Metal_01.jpg`를 복사(다른 후보: `Metal_02~06`, `Metal_08`) |
| `Models pack psx/Texture/Wood 6.jpg` | `Wood_06.jpg`를 복사 |
| `Models pack psx/Texture/Wood 7.jpg` | `Wood_07.jpg`를 복사 |

**여전히 못 고친 것(프로젝트 안에 대체할 원본 텍스처가 아예 없음, 원본 에셋 팩에서 다시 구해야 함):**
- `All`: `Curtains.jpg`
- `Models pack psx`: `Bur.png`, `Me.jpg`, `Me4.jpg`, `P10.jpg`, `P12.jpg`, `Te1.jpg`, `Wood (4).jpg`, `Wood 17.jpg`

### 2) Branch/Bush/Plants 투명화 (Alpha Clipping)
머티리얼이 모델에 내장돼 있어 직접 수정이 안 되므로, 모델 임포터가 이미 "이름이 일치하는 외부 머티리얼을 폴더에서 자동 검색"(`materialLocation: Use External Materials (Legacy)`)하도록 설정돼 있는 걸 이용해서, 모델과 같은 이름의 `.mat`을 새 `Materials` 폴더에 만들어 넣음(재임포트 시 Unity가 자동으로 연결):

- `Assets/AssetsFolder/Models pack psx/Materials/` 새로 생성 — `Branch`, `Branch_01`, `Branch_02`, `Bush`, `Bush_01`~`Bush_06`, `Plants`, `Plants_01`~`Plants_09` (총 20개, `Bush_07`은 알파 채널이 없는 `.jpg`라 제외)
- `Assets/AssetsFolder/All/Materials/` 새로 생성 — `Plants` (1개, 처음엔 `Plants_01`~`Plants_05`도 만들었으나 실제 머티리얼이 아님을 확인하고 삭제함. 아래 정정 참고)

각 머티리얼 설정(URP Lit 셰이더):
- Surface Type: Opaque 유지 + **Alpha Clipping 켬**(`_AlphaClip: 1`, `_Cutoff: 0.5`) — 나뭇잎류라 반투명 블렌딩보다 컷아웃이 정렬 문제 없이 안전함
- **Render Face: Both**(`_Cull: 0`) — 잎이 평면(카드/크로스) 형태로 만들어진 경우가 많아 뒷면도 보이게 기본값을 양면으로 설정함
- `_BaseMap`은 해당 텍스처를 그대로 연결, `_BaseColor`는 흰색(텍스처 원본 색 유지)

### 3) 텍스처 임포트 설정
위 20+6개 텍스처(`Bush_07.jpg` 제외) 전부 `Alpha Is Transparency: On`으로 변경 — 압축/밉맵 과정에서 완전 투명 픽셀의 RGB가 가장자리로 번져 생기는 검은 헤일로 방지.

## 확인 필요
직접 Unity 에디터를 실행해서 확인할 수 없어서 파일 레벨로만 작업함. 프로젝트를 열어서:
1. 재임포트가 자동으로 되는지 확인(안 되면 `All.fbx`, `models.fbx`를 각각 우클릭 → Reimport)
2. Branch/Bush/Plants가 투명하게 잘 나오는지 확인. 혹시 여전히 검게 나오면 새로 만든 해당 머티리얼을 Inspector에서 한 번 열고 Alpha Clipping 체크박스를 껐다 켜서 강제로 셰이더 키워드를 재적용해볼 것(에디터 밖에서 텍스트로 직접 만든 `.mat`이라 이 과정이 필요할 수 있음)
3. `Marble.jpg`/`Metal.jpg`로 채운 텍스처가 의도와 다르면(다른 후보가 맞다면) 말해주면 교체함

## 변경된 파일
- 추가: `Assets/AssetsFolder/All/Textures/Marble.jpg`, `Assets/AssetsFolder/All/Textures/Metal.jpg`
- 추가: `Assets/AssetsFolder/Models pack psx/Texture/Wood 6.jpg`, `Assets/AssetsFolder/Models pack psx/Texture/Wood 7.jpg`
- 추가: `Assets/AssetsFolder/All/Materials/Plants.mat` (1개. `Plants_01`~`Plants_05.mat`은 만들었다가 착오 확인 후 삭제)
- 추가: `Assets/AssetsFolder/Models pack psx/Materials/*.mat` (20개)
- 수정: `Assets/AssetsFolder/All/Textures/Plants.png.meta`, `Assets/AssetsFolder/Models pack psx/Texture/{Branch,Branch_01,Branch_02,Bush,Bush_01~06,Plants,Plants_01~09}.png.meta` (`alphaIsTransparency: 0` → `1`)
