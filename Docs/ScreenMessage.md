# ScreenMessage

`Assets/My/Scripts/UI/ScreenMessage.cs`

화면 중앙에 잠깐 뜨는 나레이션/관찰 문구 (싱글턴). 노크 거절 시 "노크가 거절됐다" 같은
1인칭 관찰 텍스트. 페이드 인(0.3) → 유지(2.5) → 페이드 아웃(0.7). `doc/0121`.

## API

| 메소드 | 설명 |
|---|---|
| `ScreenMessage.Show(string en, string ko)` | `LocalizationManager.T` 로 언어 결정 후 표시 |
| `ScreenMessage.Show(string text)` | 그대로 표시 |

씬에 인스턴스 없으면 경고만 (null-safe).

## 배치

`InGame.unity` HUD Canvas 아래 — 중앙 앵커, `CanvasGroup`(alpha 0), 검정 밴드 BG + Galmuri11 텍스트,
`raycastTarget = false`, HUD 최상단 형제 (`Dialogue_Panel`/`Cursor_Prompt` 위).

## 쓰는 곳

[`KnockEffect`](KnockEffect.md) — 새벽 아닌 노크·거절 손님 노크 시 `refuseMessages` 중 랜덤 하나.

## 관련
[KnockEffect](KnockEffect.md) · [LocalizationManager](LocalizationManager.md) · [`doc/0121`](../doc/0121-knock-refuse-screen-message.md)
