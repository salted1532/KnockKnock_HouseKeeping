# 0154 – 뉴스 브리핑(TV) 중 침대 상호작용/외곽선/마커 정지

## 요청
새벽 TV 브리핑 중에 침대 UI 마커·외곽선이 계속 보임. 브리핑 작동 중엔 해당 UI/외곽선이 꺼지도록.

## 원인
- `OutlineWhileInteractable` / `ObjectiveMarker` 는 매 `LateUpdate` 에서 `Interactable.CanInteract` 만 보고 외곽선·HUD 마커를 다시 켬.
- 브리핑 중에도 침대의 `CanInteract == true` (플레이어가 TV에 고정돼 있어도 조건상 상호작용 가능 상태).
- doc/0153 의 `HideHud` 로 마커 오브젝트를 꺼도 `ObjectiveMarker.LateUpdate` 가 다음 프레임에 되살림.

## 수정 (코드 1줄) — `Interactable.cs`
`CanInteract` getter:
```csharp
if (!enabled || !gameObject.activeInHierarchy) return false;
if (NightNewsBriefing.Playing) return false;   // 뉴스 브리핑 연출 중엔 모든 상호작용/마커/외곽선 정지
```
- 브리핑 중엔 모든 Interactable 이 불활성 → 외곽선(`OutlineWhileInteractable`), HUD 마커(`ObjectiveMarker`), 상호작용 프롬프트, 재트리거가 전부 `CanInteract` 를 타므로 한 번에 꺼짐.
- 브리핑 종료(`Playing=false`) 시 자동 복구.
- 시간대(Dawn)와 분리하는 방식은 `DayPhaseManager`/`PhaseCondition`/씬 전반을 건드려야 해 과함 → 이 가드로 대체.

## 확인
- 컴파일 에러 0.
- Play + `NightNewsBriefing.Playing` 토글: 침대(`bed_03_Interior`) `CanInteract` True → **False(브리핑 중)** → True(종료 후). `OutlineWhileInteractable`·`ObjectiveMarker` 둘 다 `CanInteract` 만 참조하므로 연동 확정.

## 미적용 (옵션)
- `OutlineWhenOff.cs` (토글-off 외곽선, 예: 조명 스위치) 는 `IsOn` 만 봐서 이 가드 영향 없음. 새벽 주인방에 그런 오브젝트 있으면 별도 가드 필요. 침대엔 불필요.
