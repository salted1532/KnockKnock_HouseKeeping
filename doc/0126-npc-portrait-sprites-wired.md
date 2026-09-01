# 0126 - 숙박객 컨셉 스프라이트를 NpcData 초상화에 연결

## 요청
`Assets/My/image/숙박객/` 안에 각 손님 컨셉 스프라이트를 넣어둠 → 해당 NpcData 필드에 연결.

## 폴더 ↔ NPC id ↔ NpcData

| id | NpcData | 폴더 | 컨셉 |
|---|---|---|---|
| 1 | Npc_.asset | 나그네 | 지친 나그네 |
| 2 | Npc_2.asset | 회사원 | 초조한 회사원(외판원) |
| 3 | Npc_3.asset | 거만한남성 | 거만한 손님 (doc/0124) |
| 4 | Npc_4.asset | 여성 | 경계하는 여자 |
| 5 | Npc_5.asset | 노인 | 수다스러운 노인 |

## 파일명 ↔ NpcData 필드

각 폴더 4장. suffix 키워드로 매칭:
- `neutralPortrait` ← suffix 없음 (`나그네/접객.png`, `회사원/회사원.png`, …)
- `angryPortrait` ← `화남` 포함
- `backPortrait` ← `뒷모습` 포함
- `sidePortrait` ← `옆모습`/`옆` 포함 (화면 왼쪽 향한 그림 기준 — 오른쪽 이동 시 GuestView 가 자동 반전)

파일명 불규칙 있음: 나그네는 `접객*` 접두, 회사원 옆·뒷은 `화사원` 오타, 공백 포함 파일명 등 — 스크립트가 keyword 매칭으로 흡수.

## 처리
- 전 PNG `textureType=Sprite`, `spriteMode=Multiple`(슬라이스 1개 `<name>_0`) — `LoadAllAssetsAtPath().OfType<Sprite>().First()` 로 서브스프라이트 로드.
- id 3 초상화가 이전에 **나그네 스프라이트를 가리키고 있었음** → 거만한남성으로 교정. id 1 은 이미 나그네라 그대로.
- 20장 전부 연결 확인 (로그).

## 상태
2026-08-31 완료. `Npc_*.asset` 5개 저장됨. 컴파일 0에러.
