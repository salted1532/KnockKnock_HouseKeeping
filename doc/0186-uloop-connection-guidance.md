# Unity uloop 연결 방법 안내

날짜: 2026-09-11

## 요청

Unity에 uloop를 MCP처럼 연결할 수 있는지 및 방법 확인.

## 조사 및 답변

- 프로젝트에 Unity CLI Loop 2.2.0 설치 및 MCP server.bundle.js 존재 확인.
- Node.js v24.21.0 확인.
- 설치 패키지 README의 MCP/CLI 연결 절차 및 Codex 공식 MCP 문서 확인.
- Unity Window > Unity CLI Loop > Settings에서 MCP 모드를 선택하고 대상 도구 자동 설정 가능.
- CLI 방식은 CLI 모드에서 Install CLI, Codex 대상 Install Skills로 구성. 패키지 문서는 CLI 방식을 권장.
- 현재 대화에 uloop MCP 도구는 노출되지 않음. 실제 연결 설정 변경이나 연결 검증은 수행하지 않음.

## 변경 파일

- 이 세션 로그만 추가. 게임 코드·에셋·Codex 설정 변경 없음.
