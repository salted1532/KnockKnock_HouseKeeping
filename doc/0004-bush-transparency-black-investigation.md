# 0004. Bush 텍스처 투명 배경이 검은색으로 보이는 문제 조사

## 날짜
2026-08-06

## 요청 내용
> 현재 bush 관련한 이미지들이 배경이 투명으로 된 png파일들인데 이게 머티리얼에선 투명부분이 검은색으로 표현되는데 이걸 어떻게 해결할수 있을까?

## 조사 내용

`Assets/AssetsFolder/Models pack psx/Texture/Bush.png`, `Bush_01~06.png`는 PNG IHDR 컬러타입 6(RGBA, 알파 채널 있음)으로 실제로 투명 배경을 가지고 있음을 확인함(`Bush_07`은 `.jpg`라 알파 채널 자체가 없음 — 별개 텍스처로 보임).

**원인**: `models.fbx`의 머티리얼(`Bush`, `Bush_01`~`Bush_06` 등, 텍스처와 동일한 이름)이 아직 별도 `.mat` 에셋으로 추출되지 않고 모델에 내장된 상태([[0003-assetsfolder-material-fix-proposal]] 조사 시 확인)이며, Unity가 FBX 임포트 시 자동 생성하는 이 내장 머티리얼은 기본적으로 **Surface Type = Opaque**로 만들어진다. Opaque 셰이더는 텍스처의 알파 채널을 아예 무시하고 RGB만 그리는데, 투명 PNG의 완전 투명 픽셀은 보통 RGB가 검은색(0,0,0)으로 채워져 있어서 "투명해야 할 부분이 검은 사각형"으로 보이는 것.

이건 [[0003-assetsfolder-material-fix-proposal]]에서 다룬 "텍스처 파일명이 안 맞아서 아예 안 붙는" 문제와는 다른 원인이다(Bush는 텍스처 연결 자체는 정상, 알파를 해석하는 방식이 문제).

## 해결 방법

나뭇잎/덤불류는 가장자리가 부드럽게 반투명한 게 아니라 "있다/없다"로 딱 잘리는 형태라 **Alpha Clipping(컷아웃)**이 정석이다(알파 블렌딩(Transparent)으로 하면 겹칠 때 정렬 깨짐 등 부작용이 더 큼).

Unity 에디터에서 머티리얼 단위로:
1. `models.fbx` 선택 → Inspector의 **Materials** 탭 → **Extract Materials**로 `Bush` 관련 머티리얼들을 실제 `.mat` 파일로 뽑아낸다(지금은 모델 안에 내장돼 있어서 직접 수정 불가).
2. 뽑아낸 `Bush`, `Bush_01`~`Bush_06` 머티리얼 각각: Surface Type은 **Opaque 유지** + **Alpha Clipping** 체크 (Threshold 0.5 근처), 잎이 앞뒤로 다 보이게 하려면 **Render Face: Both**로.
3. (선택, 품질 개선) 각 `Bush*.png` 임포트 설정에서 **Alpha Is Transparency** 체크 후 Apply — 압축/밉맵 과정에서 완전 투명 픽셀의 RGB가 가장자리로 번져 나뭇잎 테두리에 검은 헤일로가 생기는 걸 방지함(현재 `alphaIsTransparency: 0`으로 꺼져 있는 상태를 확인함).

## 사용자 확인 필요 사항
- 위 작업(머티리얼 추출 + Alpha Clipping 설정 + 텍스처 Alpha Is Transparency 켜기)을 실제로 적용할지, 아니면 사용자가 직접 에디터에서 진행할지
- 적용한다면 `Bush` 계열뿐 아니라 [[0003-assetsfolder-material-fix-proposal]]에서 다룬 나머지(텍스처 미스매치) 수정도 같이 진행할지, 별도로 처리할지

## 변경된 파일
없음(조사만 진행, 코드/에셋 변경 없음)
