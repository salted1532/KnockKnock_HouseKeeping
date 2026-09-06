# 0147 – InGame 씬 환경을 아침(Morning) 세팅으로 변경

## 요청
현재 씬(`InGame.unity`)의 글로벌 볼륨/환경 세팅을 아침 환경으로 변경.

## 배경
씬이 Dawn(새벽/MidNight) 상태로 저장돼 있었음. `PhaseVisuals`(GameObject `DayNightSwitcher`)의 `looks[0] Morning` 값을 그대로 에디트 모드에 재현.

## 적용 (uloop execute-dynamic-code, `PhaseVisuals.Apply(Morning)` 재현)
| 항목 | 이전 | 이후 |
|---|---|---|
| `RenderSettings.skybox` | `Time/MidNight/MidNight.mat` | `Time/Morning/Morning.mat` |
| `RenderSettings.fog` | true | false |
| Global Volume `sharedProfile` | `Night-VolumeProfile.asset` | `Day-VolumeProfile.asset` |
| 디렉셔널 라이트 | 4개 전부 off | `Directional Light(Morning)` on, 나머지 3개 off |

`EditorSceneManager.SaveScene` 로 `Assets/Scenes/InGame.unity` 저장.

## 결과
- 스크린샷(scratchpad Scene_20260906_230723): 파란 주간 하늘 + 구름, 완전 주광, fog 없음 → 정상.
- 컴파일 에러/경고 없음.
- 공급 스크립트 중 `RenderSettings.GetLightmapSettings()`(non-public) 한 줄만 제거(no-op 가드였음).

## 주의
- `InGame.unity` diff 519+/36− 중 실제 환경 변경은 ~6줄. 나머지는 Unity가 저장 시마다 재직렬화하는 **ProBuilder 메쉬 churn**(`pb_Mesh-*`), 이번 변경과 무관. 커밋 시 분리 여부 판단 필요.
- 스크린샷에 보인 회색 체커보드 메쉬(머티리얼 미할당) + 파란 반투명 평면은 기존 씬 요소, 이번 작업과 무관.
