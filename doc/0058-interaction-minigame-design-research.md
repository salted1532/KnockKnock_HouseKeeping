# 0058 - 상호작용/미니게임 연동 설계안 조사

## 날짜
2026-08-23

## 요청
> 다른 공포게임이나 어드벤처 게임에서 다양한 미니게임들이나 상호작용을 통한 연결을 어떤식으로 처리하는지 알아봐주고 기획폴더 안에 있는 기획서를 읽어보고 현재 게임이 어떤식으로 시스템을 구성하면 좋을지 설계안을 작성해볼래 기능정의서 처럼 기획에다가 문서 만들어주면 돼

## 조사 내용
- `기획/기능정의서.md`, `기획/넉넉하우스키핑.txt` 확인 — SYS-01~12 정의 및 게임 컨셉(Papers Please, That's Not My Neighbor, No I'm Not a Human, This War of Mine 레퍼런스) 확인.
- 현재 코드(`Interactable.cs`, `InventorySystem.cs`, `InteractionOutline.cs`) 확인 — enum-switch 기반 상호작용, 싱글톤+고정배열 인벤토리, 오버레이 개념 없음(OverlayGate 등 미존재)을 확인.
- 기획서가 직접 언급한 4개 레퍼런스 게임의 "상호작용→미니게임→결과반영" 구조를 분석: 진입 트리거와 판정 로직 분리, 오버레이 중 월드 입력 잠금, 결과는 즉시 소비되지 않고 지연 반영, 상위 페이즈 관리자가 통제 — 라는 공통 패턴 4가지를 도출.

## 결과
설계안 문서 신규 작성: `기획/상호작용-미니게임-연동-설계안.md`
- StoryFlags(공유 데이터), OverlayGate(입력 잠금), DayPhaseManager(시간대 상태) 3개 신규 클래스만 추가하고, SYS-04+05(신분증 확인+승인거절)를 하나의 오버레이로, SYS-03+10(접객대화+탐문)을 하나의 DialogueOverlay로 통합하는 구조 제안.
- 구현 순서: StoryFlags/OverlayGate → InteractionOutline 가드 → IDCheckOverlay → DialogueOverlay → DayPhaseManager.

## 남은 작업
- 설계안 승인 여부 확인 후, 승인되면 `StoryFlags`/`OverlayGate`부터 실제 코드로 착수 (코드/에셋 변경이므로 별도 제안서+승인 절차 필요).

## 변경된 파일
- `기획/상호작용-미니게임-연동-설계안.md` (신규)
- `doc/0058-interaction-minigame-design-research.md` (신규, 본 문서)
