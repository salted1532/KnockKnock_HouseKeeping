# 0033 - Flashlight 스크립트 동작 확인 및 그림자 문제 조사

## 날짜
2026-08-20

## 요청 내용 (원문)
> flashlight 스크립트를 좀 확인해줄래 어떤식으로 작동하는 스크립트고
> 현재 flashlight 프리팹의spot light가 그냥 검은색으로만 보이거든 왜 그런지 좀

> 마우스 휠 클릭으로 켜지는것도 알았는데
> 그래도 뭔가 그림자 같이 어두운 부분이 같이 나타나는데 왜그럴까?

## 조사 내용 (순수 Q&A, 코드 변경 없음)

### 1. `Flashlight.cs` 동작 방식
`Assets/AssetsFolder/Flashlight/Flashlight/Scripts/Flashlight.cs` (임포트된 에셋 스크립트, `Game.PlayerHandItem` 네임스페이스):
- 실제 Spot `Light`(`flashlight` 필드)와 반투명 원뿔 메쉬 `volumetricLight`(빛줄기 비주얼, 커스텀 쉐이더의 `_Range`/`_Alpha`/`_BaseColor` 제어)를 같이 조작.
- **가운데 마우스 버튼(휠 클릭)**으로 `IsOpen` 토글. 켜져있을 때 마우스 휠로 스팟 각도 vs 사거리를 반비례로 조절(좁게 모으면 멀리, 넓게 퍼뜨리면 가깝게).
- 꺼짐 상태에서는 볼류메트릭 콘 알파를 0으로 만들고 오브젝트를 살짝 뒤로 당겨 "접힌" 느낌을 줌.

### 2. 처음에 스팟 라이트가 검은색으로 보이던 이유
`flashlight.prefab`의 `Flashlight` 컴포넌트에 `<IsOpen>k__BackingField: 0`(기본 꺼짐)으로 저장되어 있었음. `Start()`에서 `_lightIntensity`를 캐싱한 직후 `if (!IsOpen) flashlight.intensity = 0f;`로 밝기를 0으로 덮어써서, `Light` 자체는 `Color: 흰색, Intensity: 5`로 정상 세팅되어 있음에도 시작 시 꺼진 것처럼 보임. 버그가 아니라 "기본은 꺼진 손전등" 의도된 동작 — 가운데 마우스 버튼으로 켜짐 확인됨.

### 3. 빛과 함께 나타나던 그림자 같은 어두운 부분
- `volumetricLight`로 참조되는 "Open Cylinder" 메쉬(`Flashlight_Only.mat`, Fake Volumetric 쉐이더그래프, `_Alpha: 0.19`인 반투명 머티리얼)의 `MeshRenderer`에서 **Cast Shadows가 기본값(On)으로 켜져 있었음**.
- Unity는 알파 블렌딩 머티리얼이라도 그림자맵 계산 시 알파를 무시하고 메쉬 실루엣 그대로 불투명 그림자를 만들기 때문에, 거의 안 보여야 할 원뿔 메쉬가 빛줄기 모양 그대로 어두운 그림자를 드리우고 있었음.
- 사용자가 해당 오브젝트의 `Cast Shadows`를 `Off`로 변경 → 문제 해결 확인됨.

## 결과
두 질문 모두 코드 버그가 아니라 프리팹/컴포넌트 설정(인스펙터) 문제로 확인되어 사용자가 직접 씬에서 수정, 해결 완료.

## 변경된 파일
없음 (조사/설명 및 사용자의 인스펙터 설정 변경만 있었음)
