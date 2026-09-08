# 0180 – MoneyHud "$" 기호 초록색

## 요청
Wallet HUD 에서 `$` 텍스트만 초록색으로.

## 수정 (`Assets/My/Scripts/UI/MoneyHud.cs`)
`Show(int balance)` 의 텍스트를 TMP 리치텍스트로:
`$"<color=#3CB043>$</color>{balance:N0}"` — `$` 만 초록(`#3CB043`), 숫자는 기본색 유지.
색상은 `const string DollarColor`.

## 검증
`uloop compile` 클린. (TMP `richText` 기본 on)

## 참고
- 초록 톤 바꾸려면 `MoneyHud.DollarColor` 헥스.
- 숫자까지 초록으로 하려면 `<color>` 를 전체로 감싸면 됨.
