# 0178 – CRTMonitor 씬 인스턴스 프리팹 동기화

## 요청
CRT 모니터만 프리팹이랑 동기화 (인스턴스 오버라이드 되돌리기, Transform 유지).

## 대상
`InGame.unity` 의 `/Owner's_Motel_Room/CRTMonitor` — `Owner's_Motel_Room` 인스턴스 안에
중첩된 `Assets/My/InGame/Prefabs/Item/CRTMonitor.prefab` 인스턴스. 딱 1개.

## 스캔 결과 (CRT 범위 실제 프로퍼티 오버라이드 26개)
| 대상 | 개수 | 처리 |
|---|---|---|
| `TextMeshProUGUI @ Text (TMP)` — `m_fontMaterial`/`m_sharedMaterial` 쌍 | 20 | **되돌림** (TMP 머티리얼 인스턴싱 찌꺼기) |
| `Point Light` 의 URP `UniversalAdditionalLightData` add/remove 재직렬화 | 1(+1) | **되돌림** (URP 라이트데이터 churn) |
| `Transform @ Point Light` `m_LocalPosition.x/y/z` | 3 | **유지** (Transform) |
| `Transform @ Anchor` `m_LocalPosition.y` | 1 | **유지** (Transform) |
| `m_StaticEditorFlags` @ `screenON`, `CRTMonitor` | 2 | **유지** (오클루전 static 세팅, doc/0152) |
| ProBuilder Mesh/Collider 직렬화 churn (GetObjectOverrides 상엔 150개로 뜸) | — | **안 건드림** (실제 diff 없음, ProBuilder 재생성 리스크) |

`GetAddedGameObjects`(Guest_Spawn/Pos1~4), `GetRemovedComponents`(PhaseSwitchEffect 등)는
CRT 가 아니라 바깥 `Owner's_Motel_Room` 소속 → 건드리지 않음.

## 방법
- `PrefabUtility.GetPropertyModifications(outerRoot)` 에서 대상이 CRTMonitor 서브트리이고
  프로퍼티가 `m_LocalPosition*` / `m_StaticEditorFlags` 가 **아닌** 항목만 골라
  `PrefabUtility.RevertPropertyOverride(mod, InteractionMode.AutomatedAction)`.
- `Point Light` 의 add된 `UniversalAdditionalLightData` 는 `RevertAddedComponent`,
  removed 는 `RevertRemovedComponent` (CRT 서브트리 한정).
- 씬 저장 (`EditorSceneManager.MarkSceneDirty` + `SaveScene`).

## 실행 결과 (2026-09-09)
`scratchpad/revert_crt.csx` → `uloop execute-dynamic-code` (자동 분류기가 막아서 사용자가
`! npx ... execute-dynamic-code` 로 직접 실행).

- `RevertPropertyOverride` 는 `SerializedProperty` 를 받음(`PropertyModification` 아님) → 인스턴스
  오브젝트별 `SerializedObject.FindProperty(path)` 후 revert 하도록 재작성.
- **되돌림 20개** — 전부 `TextMeshProUGUI @ Text (TMP)` 의 `m_fontMaterial`/`m_sharedMaterial`. missed 0.
- `UniversalAdditionalLightData` add-component revert 는 대상이 이미 destroy 되어 skip(무해, try/catch).
  removed 없음.
- 재스캔: CRT 범위 잔여 오버라이드 **6개** = Transform 4 (`m_LocalPosition` Point Light·Anchor) +
  `m_StaticEditorFlags` 2. 의도대로.
  - CONFIRM Point Light 위치 유지 = True, static 플래그 2개 유지, TMP 머티리얼 오버라이드 제거 = True.

### 주의 — SaveScene 부작용
`EditorSceneManager.SaveScene` 이 세션 동안 에디터에 쌓여 있던 **미저장 씬 변경 전체 + Unity 6
재직렬화**를 한꺼번에 디스크로 flush → `InGame.unity` diff ~17k 라인. CRT revert 자체는 그 일부.
(수정된 .prefab 파일들은 이 작업 이전 타임스탬프 = 사용자 본인 작업, 무관.)

## 롤백
`git checkout Assets/Scenes/InGame.unity` — 단, 위 부작용 때문에 세션 중 다른 씬 작업도 같이 날아감.
