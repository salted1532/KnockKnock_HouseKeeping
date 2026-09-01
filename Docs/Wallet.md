# Wallet

`Assets/My/Scripts/Game/Wallet.cs` · HUD: `Assets/My/Scripts/UI/MoneyHud.cs`

플레이어 소지금. 씬에 싱글턴 오브젝트로 배치(`GuestManager` 옆). 접객 대화의 선불/후불/2배 선택을
[`ReceptionManager`](ReceptionManager.md) 가 읽어 `Add` 를 호출한다. `doc/0137`.

## Wallet 필드 / API

| 이름 | 설명 |
|---|---|
| `startingBalance` (int, 기본 100) | 시작 소지금 ($) |
| `roomRate` (int, 기본 70) | 기본 1박 요금 ($). 낡은 시골 독립 모텔 기준 |
| `Instance` (static) | 싱글턴 |
| `Balance` (int) | 현재 소지금 |
| `RoomRate` (int) | `roomRate` getter |
| `Add(int amount)` | 잔액 변경 + `OnChanged(amount, Balance)`. **음수 = 지출** (지출도 이 API). `amount == 0` 이면 무시 |
| `OnChanged` (`event Action<int, int>`) | `(delta, newBalance)`. `delta > 0` = 입금(HUD 가 현금음). `Start` 에서 `(0, Balance)` 1회 |

## MoneyHud

Canvas 의 "Money" 텍스트에 붙인다. `Wallet.OnChanged` 구독 → `$1,234` 표시, `delta > 0` 이면 효과음.

| 필드 | 설명 |
|---|---|
| `label` (`TMP_Text`) | 표시 텍스트. `Reset`/`Awake` 에서 같은 오브젝트에서 자동 획득 |
| `cashClip` (`AudioClip`) | 입금 시 `PlayOneShot`. **인스펙터에서 직접 연결** |

`[RequireComponent(typeof(AudioSource))]` — AudioSource 는 자동 생성·획득, `Reset` 이 2D·playOnAwake 끔으로 초기화. 붙이면 `cashClip` 만 넣으면 됨.

구독은 `OnEnable` **+ `Start`** 양쪽에서 시도한다 (`OnEnable` 이 `Wallet.Awake` 보다 먼저 돌 수 있으므로). `Wallet` 은 `[RuntimeInitializeOnLoadMethod]` 로 `Instance` 를 재생 시작마다 초기화(도메인 리로드 끔 대비). doc/0140.

## 결제 흐름 (doc/0137)

`GuestState` 필드(입금 상태): `nightlyRate`(체크인 시 확정, 2배면 이미 2배) · `payUpfront`(기본 false=후불) ·
`settled`(중복 입금 방지) · `TotalCharge`(= `nightlyRate × stayNights`).

돈은 **승인(방 배정 + 열쇠)** 시에만 지급된다. 대화 선택은 의도만 기록(`ReceptionManager.pendingPayUpfront`).

| nodeKey | 결과 | 지급 시점 |
|---|---|---|
| `stay_pay` | 손님이 선불 수락 | 체크인 승인 시 `Wallet.Add(TotalCharge)` + `settled=true` |
| `stay_pay_refused` | 손님이 선불 요구 거부 → 후불 | 체크아웃 아침 ([`RoomController`](RoomController.md)) |
| `stay_trust`/`stay_later`/미선택 | 플레이어가 후불 선택 | 체크아웃 아침 |
| `reject_double_accept` | 두 배 제안 수락 (npc 2 돌려보내기) | `nightlyRate ×= 2`, 선불 처리. `돌려보낸다` 는 `consumedTopics`(doc/0138)로 사라짐 |

- **선불 손님 승인 대사**: `checkin_paid` 노드("여기 선불금입니다…") 재생, 없으면 `checkin` 폴백 (`SayNode` 의 `fallbackNodeKey`). doc/0140.
- 살해/거절 손님의 후불은 미지급 (별도).

## 관련
[ReceptionManager](ReceptionManager.md) · [RoomController](RoomController.md) · [DialogueSystem](DialogueSystem.md) · [`doc/0137`](../doc/0137-room-rate-and-payment-system.md)
