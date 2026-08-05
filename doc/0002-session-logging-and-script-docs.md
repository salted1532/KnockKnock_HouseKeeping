# 0002 - 세션 로깅 규칙 적용 + 스크립트 정리

- 날짜: 2026-08-06

## 요청 내용
> Rules폴더 안에있는 규칙을 보고 내가 내리는 명령에 대해서 세션기록을 남기고 현재 프로젝트의 스크립트들을 정리해 둬
> 이 규칙은 매번 지켜져야해

## 조사 내용
- `Rules/README.md`, `Rules/session-logging-rule.md`, `Rules/confirm-before-implementing-rule.md` 확인
  - 세션 로깅 규칙: 모든 요청/결과를 `doc/NNNN-<영문-slug>.md`에 기록, 번호는 세션을 넘어 이어짐
  - 구현 전 확인 규칙: 코드/에셋 변경 요청은 제안서 작성 → 승인 → 적용 순서로 진행 (순수 Q&A/문서화는 대상 아님)
- `doc/`, `Doc/` 폴더 모두 미존재 확인 → 이번이 첫 기록
- `Assets/**/*.cs` 검색 결과 프로젝트 고유 게임 스크립트는 아직 없고, Unity 기본 템플릿 스크립트 2개(`Readme.cs`, `ReadmeEditor.cs`)만 존재
- **이슈 발견**: Windows는 경로 대소문자를 구분하지 않아 규칙에서 말하는 `Doc/`(레퍼런스 문서)와 `doc/`(세션 로그)가 실제로는 동일 폴더로 충돌함 → 사용자에게 확인 후 레퍼런스 문서 폴더명을 `Docs/`(복수형)로 분리하기로 결정

## 변경 내역
- `Docs/Readme.md`, `Docs/ReadmeEditor.md`, `Docs/Overview.md` 신규 작성 — 기존 템플릿 스크립트 2개에 대한 레퍼런스 문서
- `Rules/session-logging-rule.md`, `Rules/README.md` — `Doc/` → `Docs/` 표기 수정 및 Windows 대소문자 충돌 관련 설명 추가
- `doc/0001-write-readme.md` — 직전 세션(README 작성 요청) 소급 기록
- `doc/0002-session-logging-and-script-docs.md` — 본 요청 기록 (이 파일)

## 요약 / 남은 작업
- 세션 로깅 규칙과 구현 전 확인 규칙을 앞으로 매 요청마다 적용
- 새 게임플레이 스크립트가 추가되면 `Docs/`에 스크립트당 문서 1개씩 추가

## 변경된 파일
- `Docs/Readme.md` (신규)
- `Docs/ReadmeEditor.md` (신규)
- `Docs/Overview.md` (신규)
- `Rules/session-logging-rule.md`
- `Rules/README.md`
- `doc/0001-write-readme.md` (신규)
- `doc/0002-session-logging-and-script-docs.md` (신규)
