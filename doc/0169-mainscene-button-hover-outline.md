# 0169 – MainScene 메뉴 버튼 호버 시 글자 외곽선 + 색 노란색

## 요청
MainScene 메인화면의 `Play`/`Exit` 버튼(이미지 끄고 글자만 보임)에 마우스 호버 피드백:
호버하면 **TMP 글자색 + 외곽선색이 같은 노란색**으로 (초기 요청은 외곽선만 → 후속으로 글자색도 추가).

## 현황 (조사)
- `Canvas/Play`, `Canvas/Exit` = `Image`(alpha 0, raycastTarget on = 클릭 히트박스) + `Button`(Transition=ColorTint, TargetGraphic=그 Image → 투명이라 피드백 안 보임)
- 각 버튼 자식 `Text (TMP)` = `Galmuri11 SDF Material` 공유 (외곽선 검정 0.25, doc/0155)
- `MainMenu` 스크립트가 `Canvas` 에, `Play`/`Exit` onClick → `MainMenu.Play()/Quit()`
- **MainScene 에 `EventSystem` 이 없음** → 지금은 클릭·호버 **아무것도 안 됨**. InGame 은 `EventSystem` + `InputSystemUIInputModule` 사용 (프로젝트 = Input System 전용, `activeInputHandler:1`)

## 작업

### 1. MainScene 에 EventSystem 추가 (필수 선행)
- `GameObject > UI > Event System` 메뉴로 생성 (프로젝트 입력 백엔드에 맞는 `InputSystemUIInputModule` 자동 부착)

### 2. 신규 `Assets/My/Scripts/UI/HoverTextOutline.cs` (~35줄)
```csharp
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

// 호버 시 자식 TMP 텍스트의 글자색 + 외곽선색을 hoverColor 로. label 비우면 자식 자동 검색.
// 외곽선은 fontMaterial 게터로 만든 텍스트 전용 인스턴스 머티리얼에만 적용 → 공유 머티리얼 무변경.
public class HoverTextOutline : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private TMP_Text label;
    [SerializeField] private Color hoverColor = Color.yellow;

    private Color normalFace = Color.white;
    private Color normalOutline = Color.black;

    private void Awake()
    {
        if (label == null) label = GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            normalFace = label.color;
            normalOutline = label.fontSharedMaterial.GetColor(ShaderUtilities.ID_OutlineColor);
        }
    }

    private void OnDisable() => Apply(normalFace, normalOutline);

    public void OnPointerEnter(PointerEventData _) => Apply(hoverColor, hoverColor);
    public void OnPointerExit(PointerEventData _) => Apply(normalFace, normalOutline);

    private void Apply(Color face, Color outline)
    {
        if (label == null) return;
        label.color = face;
        label.fontMaterial.SetColor(ShaderUtilities.ID_OutlineColor, outline);
    }
}
```

### 3. 배선
- `Canvas/Play`, `Canvas/Exit` 각각에 `HoverTextOutline` 부착 (label 은 Awake 자동 검색, 그대로 둠)
- `uloop compile` 검증

## 대안 (안 씀)
버튼 Transition=ColorTint + TargetGraphic 을 텍스트로, Highlighted=노랑 → 코드 0줄이지만 face 만 바뀌고 외곽선은 그대로. 외곽선까지 노랗게 하려면 스크립트 필요.

## 결과 (구현·플레이모드 검증 완료)
- 신규 `HoverTextOutline.cs` (~35줄). 최종 동작:
  - 호버 시 `label.color`(글자 face) + 인스턴스 머티리얼 `_OutlineColor` 둘 다 `hoverColor`(노랑)
  - `label.fontMaterial` 게터로 **텍스트별 인스턴스 머티리얼** 생성 → 공유 머티리얼(`Galmuri11 SDF Material`) 무변경
  - `normalFace`/`normalOutline` 은 Awake 에서 현재값 캡처(하드코딩 X), Exit·OnDisable 에 원복
- `EventSystem` (+`InputSystemUIInputModule`, `DefaultInputActions` 자동 할당) MainScene 에 생성 — **이게 없어서 그동안 버튼 클릭조차 안 됐음** (doc/0165 는 코드 Invoke 로만 테스트).
- `Canvas/Play`, `Canvas/Exit` 에 `HoverTextOutline` 부착 (`label` 비움 → 런타임 자동 검색, `hoverColor` = 노랑).
- `uloop compile` 에러 0. 플레이모드: `OnPointerEnter` → face·outline `RGBA(1, 0.922, 0.016, 1)`, `OnPointerExit` → 흰색 원복, 공유 머티리얼 불변. 스크린샷: 호버한 "Play" 만 완전 노랑, "Exit" 흰색 유지.

## 수정 파일
- `Assets/My/Scripts/UI/HoverTextOutline.cs` (신규, ~30줄)
- `Assets/Scenes/MainScene.unity` (EventSystem 추가, Play·Exit 에 HoverTextOutline)
