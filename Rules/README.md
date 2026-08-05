# Rules

이 폴더는 Claude Code가 이 프로젝트에서 작업할 때 매 요청마다 확인하고 따르는 규칙 목록입니다.
원본은 Claude Code의 영구 메모리(`~/.claude/projects/.../memory/`)에 있으며, 이 폴더는 프로젝트 내에서
바로 확인할 수 있도록 만든 사본/요약본입니다.

- [session-logging-rule.md](session-logging-rule.md) — 세션 로깅 규칙
- [confirm-before-implementing-rule.md](confirm-before-implementing-rule.md) — 구현 전 확인 규칙

## 폴더 규칙 참고
- `doc/` (소문자) — 세션 로그, `NNNN-<영문-slug>.md`
- `Docs/` (복수형) — 스크립트별 레퍼런스 문서
- Windows는 대소문자를 구분하지 않으므로 `Doc/`(대문자, 단수)는 사용하지 않음 (`doc/`와 충돌)
