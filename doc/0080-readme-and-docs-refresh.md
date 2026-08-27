# 0080 - README + Docs 갱신 (상호작용 시스템 반영)

## 요청
개편된 상호작용 시스템(doc/0078, 0079)을 반영해 README 갱신. 각 effect 스크립트별 작동 방식 문서를 `Docs/` 에 추가하고 README와 연결. 필요없거나 수정이 필요한 스크립트 분석.
(참고용으로 다른 프로젝트 NEOTECHWARZ2.0 의 README 포맷을 제시받음 — 스크립트 표 + Docs 링크 + 구현/로드맵 체크리스트 + 정리 분석 스타일.)

## 작업

### 신규 `Docs/` 문서 (스크립트별 레퍼런스)
- 허브: `Docs/InteractionSystem.md` (모델, 작업 흐름, 행동 카테고리 표, 우클릭 재설정 동작)
- 효과/코어: `InteractionEffect`, `InteractionCondition`, `SfxEffect`, `ChangeObjectEffect`, `HingeEffect`, `PushEffect`, `PickupEffect`, `SpawnObjectEffect`, `EnterUIModeEffect`
- 조건: `PhaseCondition`
- 드라이버/모드: `GazeInteractor`, `CursorInteractor`, `UIInteractionMode`
- 디스패처: `Interactable`
- 진행/환경/사운드: `DayPhaseManager`, `DayNightSwitcher`, `SoundManager`, `FootstepSystem`
- 인벤토리: `InventorySystem`, `HandItemRegistry`(ItemId/HandItem 포함)
- 기타: `ItemImpactSound`, `CartGroundAlign`
- `Docs/Overview.md` 전면 갱신 (구 "템플릿 2개뿐" → 전체 스크립트 표 + 정리 대상)

### README.md 전면 갱신
- 게임 소개/루프/전략/엔딩은 유지, "핵심 루프"에 4단계(DayPhaseManager) + 접객 UI 모드 흐름 추가
- 프로젝트 구조 트리 갱신 (Assets/My/Scripts 하위 폴더별)
- **상호작용 시스템** 섹션 신설: 컴포넌트 조합 모델, 작업 흐름, 행동 카테고리·효과 표, 핵심 스크립트 표(Docs 링크)
- **구현 완료 기능** 체크리스트 (플레이어/상호작용/인벤토리/하루진행/접객UI/아트)
- **로드맵** — SYS-01~12 기준 미구현 정리
- **스크립트 정리 분석** 신설:
  - 삭제 예정: `Door.cs`, `ItemDispenser.cs`, `InteractionMigrator.cs`, `Interactable` LEGACY 필드
  - 삭제 검토: `TutorialInfo/`, `TextMesh Pro/Examples`, `SampleScene`/`TestScene`, `StripTestRoomProBuilder`, `AssetOrganizer`
  - 수정 필요: SoundManager/DayNightSwitcher의 Q키 → DayPhaseManager 연결, SoundManager 확장, InventorySystem의 `GameObject.Find` 문자열 탐색·동일 ItemId 다중소지 충돌, CartGroundAlign의 `FromToRotation(forward, up)` 의심, Interactor null 가드
- 개발 프로세스 메모(doc/ vs Docs/, 제안서 우선)

## 상태
2026-08-27 완료. 코드 변경 없음(문서만). 정리 분석 항목의 실제 삭제/수정은 개별 승인 후.
