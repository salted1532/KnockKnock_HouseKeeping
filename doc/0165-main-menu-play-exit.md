# 0165 – 메인 메뉴 Play / Exit 버튼 연결

## 요청
MainScene 의 `Canvas` 에 Play·Exit 버튼. Play → InGame 씬, Exit → 게임 종료.

## 현황
- `Assets/Scenes/MainScene.unity` 존재, `Canvas/Play` · `Canvas/Exit` 버튼(onClick 비어 있음).
- Build Settings 엔 `InGame` 만 등록, `MainScene` 없음.

## 적용
1. **신규** `Assets/My/Scripts/Game/MainMenu.cs` — `Play()` = `SceneManager.LoadScene("InGame")`, `Quit()` = `Application.Quit()` (에디터는 `EditorApplication.isPlaying = false`). `playScene` 인스펙터 필드.
2. **Build Settings** — `MainScene`(0), `InGame`(1) 둘 다 enabled.
3. **MainScene 배선** — `Canvas` 에 `MainMenu` 추가, `Canvas/Play`.onClick → `MainMenu.Play`, `Canvas/Exit`.onClick → `MainMenu.Quit`. 씬 저장.

## 검증
- `uloop compile` 클린.
- onClick 리졸브: `Play → MainMenu.Play`, `Exit → MainMenu.Quit`, `playScene=InGame`.
- 플레이모드에서 Play 버튼 Invoke → `activeScene = InGame` 전환 확인.
- Exit 는 표준 패턴이라 코드만 확인(에디터 세션 중 실제 종료 테스트는 생략).
