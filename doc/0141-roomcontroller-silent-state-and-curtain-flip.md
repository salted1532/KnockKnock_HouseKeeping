# 0141 - RoomController 봉인: 커튼 OFF + 상태 변경 무음

날짜: 2026-09-02
관련: `doc/0118`·`doc/0132`·`doc/0136`(RoomController), `Docs/RoomController.md`, `Docs/Interactable.md`

## 요청 (원문)

> 손님이 지정된 방이 잠길때 방의 커튼이 OFF 되도록 해줘 지금은 ON으로 변하는거 같아(반대로 해주면 될듯)
> 그리고 RoomController가 변경하는건 사운드 발생 안해도 돼

## 현재 상태

`RoomController.Apply` 는 봉인(`seal`) 시:
- `frontDoor.SetState(false)` — 문 닫기
- `curtains → SetState(true)` — 커튼 "닫기" 의도였으나 사용자 셋업에선 ON = 커튼 열림
- `lights → SetState(false)` — 불 끄기

`Interactable.SetState(bool)` 은 붙은 효과를 **전부** 재생 → `SfxEffect` 도 울림. 봉인은 새벽 자동
연출이라 소리가 안 나야 함.

## 변경

### `Interaction/Interactable.cs` — `SetState` 에 `silent` 옵션

```csharp
// 기존
public void SetState(bool on)
{
    if (IsOn == on) return;
    IsOn = on;
    var ctx = new InteractionContext(this, null, on, transform.position);
    if (effects != null)
        foreach (var e in effects)
            if (e != null && e.enabled) e.Play(in ctx);
}
```
↓
```csharp
// silent: SfxEffect 는 건너뛴다 (RoomController 의 새벽 봉인 등, 소리 없이 상태만).
public void SetState(bool on, bool silent = false)
{
    if (IsOn == on) return;
    IsOn = on;
    var ctx = new InteractionContext(this, null, on, transform.position);
    if (effects != null)
        foreach (var e in effects)
        {
            if (e == null || !e.enabled) continue;
            if (silent && e is SfxEffect) continue;
            e.Play(in ctx);
        }
}
```

기존 호출부(`ReceptionManager` 의 `guestDoor.SetState(true)` — 손님이 정문 여는 소리는 유지) 는 1인자
그대로라 영향 없음.

### `Interaction/RoomController.cs` — 커튼 뒤집기 + 무음

```csharp
// 기존
if (seal) frontDoor.SetState(false);
...
if (it != null) { if (seal) it.SetState(true);  it.enabled = !seal; }   // curtains
if (it != null) { if (seal) it.SetState(false); it.enabled = !seal; }   // lights
```
↓
```csharp
if (seal) frontDoor.SetState(false, silent: true);
...
if (it != null) { if (seal) it.SetState(false, silent: true); it.enabled = !seal; }   // curtains — OFF
if (it != null) { if (seal) it.SetState(false, silent: true); it.enabled = !seal; }   // lights
```

- 커튼: `SetState(true)` → **`SetState(false)`** (봉인 시 OFF).
- 문·커튼·조명 3개 전부 `silent: true`.
- 봉인 해제(청소 창) 시 커튼/조명 복원은 안 함 — 기존과 동일(사용자가 토글). 요청 밖.

## 영향 파일

```
Interaction/Interactable.cs     수정  SetState(bool, bool silent = false)
Interaction/RoomController.cs   수정  커튼 SetState(false), 3개 SetState 무음
Docs/  RoomController.md · Interactable.md 갱신
```

## 구현 완료 (2026-09-02)

`uloop compile` Error 0 / Warning 0.

## 상태

코드 구현 + 컴파일 완료. 플레이 검증(새벽 봉인 시 커튼 OFF + 무음)만 남음.
