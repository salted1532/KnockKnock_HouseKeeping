# 0176 – 접객 모드 인벤토리: 슬롯 선택·휠은 허용, 던지기·사용만 차단

## 요청
접객 모드에서 던지기만 막고, 휠로 인벤토리 이동 + 1~5 슬롯 조작은 되도록.

## 현황
`InventorySystem.Update`: `UIInteractionMode.MovementLocked` (화면고정·오버레이) 이면 **아이템 조작 전면 차단** (1~5, 휠, F 던지기, 좌클릭 사용 모두).

## 수정 (`Assets/My/Scripts/Inventory/InventorySystem.cs`)
```
locked = UIInteractionMode.MovementLocked
receptionUI = locked && UIInteractionMode.Active
              && ReceptionManager.Instance != null && ReceptionManager.Instance.InSession

if (locked && !receptionUI) return;        // 접객 외 UI 모드(노크·모니터 단독·노트·페이드)는 전부 차단
  1~5 슬롯 선택
  휠 스크롤 슬롯 이동
if (locked) return;                        // 접객 UI 포함 — 여기서 던지기·사용 차단
  F 던지기
  좌클릭 사용
```
- 좌클릭 "사용" 도 접객 중엔 차단 유지 (좌클릭은 손님·모니터 클릭용이라 겸용 시 오작동).
- 접객 세션에서 ESC 로 데스크에서 물러난 상태(`Active=false`, `MovementLocked=false`)는 `locked=false` → 인벤토리 전 기능 정상.

## 검증 (플레이모드)
```
접객 UI (InSession=T, Active=T, MovementLocked=T): locked=T receptionUI=T → 1~5·휠 통과, F·클릭 차단
노크/비접객 (InSession=F, Active=T):              locked=T receptionUI=F → 전부 차단
SelectSlot 동작 확인
```
`uloop compile` 클린. (실키 입력은 이 환경 플레이모드 리페인트 문제로 미확인 — 분기 플래그·SelectSlot 로 확인)
