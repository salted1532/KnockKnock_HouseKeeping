# 0183 – 머티리얼 관련 자동 규칙 2개 제거

날짜: 2026-09-09
코드/에셋 변경 없음. Claude 자동 메모리 정리만.

## 요청

> 머티리얼 Smoothness 0 / 투명화 스윕 절차 — 이 두 규칙은 빼도 좋을 거 같아

## 처리

Claude 자동 메모리에서 아래 2개 feedback 규칙 삭제:

- `feedback_material-smoothness-default-zero.md` — 새 URP Lit 머티리얼 `_Smoothness`/`_Glossiness` = 0
- `feedback_transparency-sweep-workflow.md` — 새 에셋 팩마다 알파 투명 PNG 찾아 Alpha Clipping 머티리얼 생성/적용 8단계 절차

동반 정리:
- `MEMORY.md` 인덱스에서 두 항목 제거
- `project_hdrp-asset-packs-need-urp-conversion.md`, `project_localization-system.md` 의 `[[…]]` 역링크 제거
- `Rules/` 폴더에는 원래 이 두 규칙 사본이 없어 그대로 둠

## 남는 규칙 (변동 없음)

세션 로깅 규칙, 구현 전 확인 규칙, `Docs/` vs `doc/` 이름 분리, `Assets/My/` 산출물 위치.

이미 0으로 맞춰둔 기존 머티리얼(doc/0005·0006·0008 등)은 그대로 유지 — 규칙만 없어졌지 되돌리지 않음.
