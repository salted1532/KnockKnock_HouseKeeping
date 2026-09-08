# 0184 – README 갱신 규칙 추가

날짜: 2026-09-09
코드/에셋 변경 없음. 규칙(자동 메모리 + `Rules/` 사본) 추가.

## 요청 (원문)

> 새로운 규칙을 하나 추가할게. 내가 Readme 파일 갱신하라고 하면 그때 작동하는 규칙인데
> Readme파일 갱신 규칙: 새로 추가된 스크립트가 있을 경우 해당 스크립트에 대한 md파일을 Docs폴더
> 안에 만들고 프로젝트 구조에도 갱신하며 각 스크립트 문서와 연결까지 해준다. 나머지 기능적인
> 부분과 기획적인 부분 갱신과 로드맵 갱신도 진행한다.
> 지금까지 Readme 갱신했던 방식으로 연결해서 규칙을 지정하면 될 거 같아.

## 처리

`doc/0080`·`0101`·`0143`·`0158`·`0181` 에서 반복해온 README 갱신 방식을 규칙으로 고정.
트리거 = 사용자의 명시적 "Readme 갱신" 요청 (상시 자동 아님).

절차:
1. 직전 README 갱신 로그 찾아 그 이후 doc/커밋 분량 파악
2. 신규 게임플레이 스크립트마다 `Docs/<Script>.md` 생성 + `Docs/Overview.md` 표 + `README.md`
   핵심 스크립트 표 링크 (= "각 스크립트 문서와 연결")
3. README 본문 — 기능적 부분(핵심 루프·행동 카테고리·mermaid·구현 완료·수정 필요) +
   기획적 부분(게임 소개·시간대별 콘텐츠·기획 문서 링크) + 로드맵
4. mermaid 렌더 검증 + `git diff --stat`
5. `doc/NNNN-readme-*.md` 세션 로그

## 변경된 파일

- `~/.claude/.../memory/feedback_readme-refresh-rule.md` (신규) + `MEMORY.md` 인덱스
- `Rules/readme-refresh-rule.md` (신규, 프로젝트 내 사본) + `Rules/README.md` 목록

## 현재 규칙 전체

| 규칙 | 트리거 | 위치 |
|---|---|---|
| 세션 로깅 | 상시 | `Rules/session-logging-rule.md` |
| 구현 전 확인 | 상시 (코드/에셋 변경 시) | `Rules/confirm-before-implementing-rule.md` |
| README 갱신 | "Readme 갱신" 요청 시 | `Rules/readme-refresh-rule.md` |
| Docs/ vs doc/ 이름 분리 | 상시 (폴더/파일명) | 메모리만 |
| Assets/My/ 산출물 위치 | 상시 (프리팹·머티리얼 생성) | 메모리만 |
