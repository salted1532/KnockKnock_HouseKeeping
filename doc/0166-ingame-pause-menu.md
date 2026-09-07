# 0166 – InGame Esc 옵션창 (메인화면으로 나가기)

## 요청
- InGame 에서 Esc → 옵션창. 버튼은 "메인화면으로 나가기" 하나만 → `MainScene` 로드.
- 접객 모드 중 Esc → 옵션창 뜸.
- 노트 / 모니터 / 새벽 노크 대화 중 Esc → 그 뷰만 빠져나가고 옵션창은 안 뜸.

## Esc 판정 규칙 (`PauseMenu.Update`)
```
Esc 눌림:
  옵션창 열려 있으면        → 닫기(재개)
  ShowPanelEffect.ConsumesEsc → 무시 (노트가 처리)
  UIInteractionMode.Active && UIInteractionMode.TopEscExits → 무시 (모니터·새벽 노크 = escExits:true)
  그 외 (자유 이동 / 접객)   → 옵션창 열기
```
접객은 `escExits:false` 라 `TopEscExits` 가 false → 옵션창 열림. (접객 Backspace-홀드 = 데스크에서 물러나기, 별개 유지 — doc/0115)

## 변경
| 파일 | 내용 |
|---|---|
| **신규** `Assets/My/Scripts/Game/PauseMenu.cs` | Esc 판정 + 열기/닫기. 열 때 `Time.timeScale=0` + `UIInteractionMode.FreezeForOverlay(true)`(커서·FPS·시선 정지, 접객 중이면 no-op) + `DialogueRunner.Paused=true`. 닫으면 되돌림. `ExitToMainMenu()` = `timeScale=1` + `SceneManager.LoadScene("MainScene")` |
| `Interaction/Modes/UIInteractionMode.cs` | `TopEscExits` `private` → `public` (PauseMenu 가 읽음) |
| `Interaction/Effects/KnockEffect.cs` | 새벽 노크 `Enter(anchor, lookScale)` → `Enter(anchor, lookScale, escExits: true)` — 이제 Esc 한 번에 취소(전엔 Backspace-홀드만) |
| `InGame.unity` | 신규 `PauseCanvas`(Overlay, sortingOrder 100) → `Panel`(검정 0.8α, 풀스크린, raycast blocking) → `ExitButton`("메인화면으로 나가기", Galmuri11, AutoSize). `PauseMenu` 컴포넌트 배선(panel ref + 버튼 onClick→ExitToMainMenu). Panel 시작 비활성 |
| Build Settings | `MainScene`(0), `InGame`(1) — doc/0165 에서 이미 등록 |

## 검증 (플레이모드, InGame)
- 자유 이동 실제 Esc 키 → `panel.active=True, timeScale=0`. 다시 Esc → `False, 1`.
- `SetOpen(true)`: 커서 표시 O, `DialogueRunner.Paused=True`. `SetOpen(false)`: 원복.
- `EscConsumedElsewhere()` 자유 이동에서 `False` (옵션창 열림).
- "메인화면으로 나가기" 버튼 Invoke → `activeScene=MainScene`, `timeScale=1`(다음 씬 안 얼음).
- `uloop compile` 클린.
- 모니터/노트/새벽 억제는 코드 판정으로 확인 (`ConsumesEsc` / `TopEscExits`).

## 알려진 사항
- 일부 HUD(인벤토리 등 별도 Canvas)가 옵션창 위로 살짝 비침. 기능엔 영향 없음. 완전히 가리려면 해당 Canvas sortingOrder 조정 or 옵션창 열릴 때 HUD SetActive(false).
