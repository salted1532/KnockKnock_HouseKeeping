# 0168 – 접객 신분증 확인

## 요청
접객 대화에서 "신분증 제시" 선택지를 눌렀을 때만 ID 오브젝트가 활성화되고, 확인하면 그 손님의 이름·정보가 뜨도록. (노트/Read 상호작용과 구조는 같지만 대화로만 열림)

## 현황
- `Owner's_Motel_Room/ID` (프리팹 `ID.prefab`): Cube + `Interactable(Read)` + `ShowPanelEffect(content = Canvas/ID_image)` + SfxEffect + Outline. **항상 클릭 가능**(게이트 없음).
- `Canvas/ID_image` (sibling index 14, 비활성): 신분증 이미지 600×400 + `Text (TMP)` 자식(200×50, 작음).
- id3 에 이미 `id` 질문 → `id_show` 노드 관례 존재.

## 적용

### 1. 신규 `Assets/My/Scripts/Interaction/ReceptionIdCard.cs`
`ID` Cube 에 부착. `[RequireComponent(Interactable, ShowPanelEffect)]`.
- `Awake`: `Interactable.enabled = false` (아웃라인·클릭 차단)
- `DialogueRunner.OnNodeReached(npc, "id_show")` → `Interactable.enabled = true` + `infoText` 를 `npc.idCard` 로 채움 (name/birthDate, 비면 `DisplayName` / `----.--.--` 폴백. 한/영)
- `DialogueRunner.OnDialogueEnded` → `Interactable.enabled = false` + `ShowPanelEffect.Close()`

### 2. NpcData `idCard` (9명)
전부 비어 있던 `idCard.name` / `idCard.birthDate` 를 채움 (Raymond Teller/1971.03.14 … Nathaniel Ross/1961.02.05). `forged` 는 0 유지 — 위조는 일차 편성에서.

### 3. CSV — `id` 질문 + `id_show` 노드
id 1·2·4·5 (`sample.csv`), 6·7·8·9 (`guests_06-09.csv`) 에 추가 (id3 는 이미 있음). 각 손님별 짧은 대사:
```
{id},Reception,0,Question,id,Ask for identification,신분증을 요구한다,Neutral,"<반응>",...
{id},Reception,0,Question,id,Look at the ID,신분증을 확인한다,,,,id_show,
{id},Reception,0,Question,id,Never mind,됐습니다,,,,,
{id},Reception,0,Node,id_show,,,Neutral,"<확인하는 동안 하는 말>",...
```
`Tools > Dialogue > Import CSV` 재실행 → 294행 / 182노드, goto 검사 통과.

### 4. 씬 배선 (`InGame.unity`)
- `ID` Cube 에 `ReceptionIdCard` 추가, `infoText` = `Canvas/ID_image/Text (TMP)`
- `ID_image/Text` rect 를 카드에 맞게 stretch(패딩 40) + AutoSize(10~34) + TopLeft
- `ID` Cube `Interactable.enabled = false`, `ID_image` 비활성 (저장 상태 정리)

## 검증 (플레이모드, InGame)
```
before:                Interactable.enabled=False  CanInteract=False
after id_show(목사 id9): Interactable.enabled=True   CanInteract=True
  infoText = "성    명\n  Nathaniel Ross\n\n생년월일\n  1961.02.05"
after dialogue end:    Interactable.enabled=False
9명 전원 id질문(choices 2) + id_show 노드 존재. uloop compile 클린.
```
(패널 시각 확인은 에디터 Game 뷰가 이 환경에서 재생 중 리페인트가 안 돼 스크린샷 생략 — 상태·좌표·sibling 순서로 확인.)

## 수정 (버그 2건)
1. **평소 오브젝트가 활성이었음** — `Interactable.enabled` 만 껐어서 Cube 는 계속 보였음. 그리고 이전 setup 스크립트가 `Interactable.enabled=false` 를 **씬에 저장**해버려, id_show 후에도 상호작용 불가였음.
2. **id_show 후에도 클릭 안 됨** — 자식 `Cube` 콜라이더가 **Default 레이어** → `CursorInteractor` 오클루전 마스크에 걸려 `ID` 루트 Interactable 를 가림.

**재설계 (컨트롤러 분리):**
- `ReceptionIdCard` 를 `ID` Cube 에서 떼어 **`Canvas`**(항상 활성)로 옮김. 필드: `idObject`(ID Cube), `infoText`.
- 평소 `idObject.SetActive(false)` — 완전 비활성(안 보임·클릭 안 됨).
- `id_show` → `idObject.SetActive(true)` + 텍스트 채움. `OnDialogueEnded` → 패널 닫고 `SetActive(false)`.
- `ID` Cube: `Interactable.enabled = true` 로 되돌림, 하위 전체 **Interaction 레이어(11)** 로 통일(자식 Cube 콜라이더 오클루전 제거).
- 검증: start `ID.active=False` / id_show 후 `active=True, CanInteract=True`, 카메라 레이가 ID Interactable 에 명중 / `Interact()` → `ID_image.active=True` + 텍스트 / dialogue end → 둘 다 `False`.

## 수정 2 — 사라지는 타이밍
`OnDialogueEnded`(대사 텍스트 끝)에서 치우니 방배정·키 주기 전에 사라졌음. → **`ReceptionManager.CurrentGuest` 폴링**으로 변경: id_show 때 `shownFor = npc`, `Update` 에서 `CurrentGuest != shownFor` 이면 Hide. 즉 그 손님이 체크인/거절로 **퇴장할 때**(CurrentGuest 해제) 사라짐 — 대화 종료 후 방배정·키 단계 내내 유지.
검증: start False / id_show True / 대화종료·체크인대기 중 **True 유지** / 손님 퇴장(CurrentGuest=null) False / 다음 손님 id 안 물음 False.

## 참고 / 후속
- 신분증 정보에 사진(`idCard.photo`)·주소 등 더 넣으려면 `ReceptionIdCard.Populate` + 이미지 스왑 추가.
- **위조 판별**: `idCard.forged` 는 현재 전부 0. 대사에서 "이름/주소가 안 맞는다" 는 힌트만 있음 — 실제 판정 로직·오판 이벤트는 별도(위협의-정체 문서 층1).
- 접객 중 ESC → 신분증 패널이 열려 있으면 `ShowPanelEffect` 가 먼저 닫음(PauseMenu·접객이탈보다 우선, doc/0166).
