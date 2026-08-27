# 0081 - 구 상호작용 스크립트 삭제

## 요청
README "삭제 예정" 항목 삭제: `Door.cs`, `ItemDispenser.cs`, `InteractionMigrator.cs`, `Interactable` LEGACY 필드.

## 마이그레이션 완료 확인
- `Motel_Room.prefab` — Interactable 컴포넌트 0개 (개별 프리팹으로 분리 이관됨).
- `Bed`/`curtain`/`Door`/`Cash_register`/`shopping cart` 등 Item 프리팹에 `HingeEffect`/`ChangeObjectEffect`/`PickupEffect`/`SfxEffect` 참조 존재.
- `Can_Coke`/`FlashLight_low-Poly` — Interactable `type: 99`(Migrated) + 효과 컴포넌트.
- `InGame.unity` — 구 Interactable 인스턴스 0개.
- `Door.cs` guid(`85727d9...`), `ItemDispenser.cs` guid(`6ab6df0e...`) — 어떤 프리팹/씬에서도 참조 없음 (자판기 ItemDispenser 컴포넌트도 이미 제거됨).

→ 삭제해도 미싱 스크립트 발생 안 함.

## 변경
- 삭제(`git rm`): `Assets/My/Scripts/Interaction/Door.cs`(+meta), `Assets/My/Scripts/Interaction/ItemDispenser.cs`(+meta), `Assets/Editor/InteractionMigrator.cs`(+meta)
- `Interactable.cs`: 하단 `#if UNITY_EDITOR ... #endif` 뒤의 `LEGACY` 블록 전체 제거 —
  `enum LegacyType`, `type`/`itemName`/`itemIcon`/`equipTarget`/`useClip`/`consumeOnUse`/`messyVisual`/`tidyVisual`/`pushForce`/`rotationForce`/`door`/`curtainOpen`/`curtainClosed`/`curtainOpenClip`/`curtainCloseClip` 필드, `SetEquipTarget()`.
  `SyncEffectsToPrompt` 등 에디터 우클릭 메뉴 로직은 유지. `using UnityEngine.Serialization`(`[FormerlySerializedAs("onInteract")]`)도 유지.

## 검증
- `grep` — 코드에 `LegacyType`/`SetEquipTarget`/`InteractionMigrator`/`<Door>` 참조 없음 (주석 1건 제외).
- `Interactable.cs` 중괄호 균형 36/36.
- 기존 프리팹에 남은 `type: 99` 등 직렬화 잔여 프로퍼티는 Unity가 재직렬화 시 자동 폐기 (무해).

## 상태
2026-08-27 완료. `Docs/Overview.md`, `README.md` 반영.
