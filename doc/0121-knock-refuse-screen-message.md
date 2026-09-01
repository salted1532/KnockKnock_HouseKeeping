# 0121 - 노크 거절 시 화면 중앙 관찰 문구 (영/한)

## 요청
노크 거절 시 화면 중앙에 "노크가 거절된 것 같다", "깊이 잠든 것 같다", "응답이 없다" 등 문구 출력. 영/한 모두.

## 구현

### 신규 `UI/ScreenMessage.cs` (싱글턴)
화면 중앙 임시 나레이션/관찰 문구. 페이드 인(0.3) → 유지(2.5) → 페이드 아웃(0.7).
- `ScreenMessage.Show(en, ko)` — `LocalizationManager.T` 로 언어 결정
- `ScreenMessage.Show(text)` — 그대로
- 씬에 인스턴스 없으면 경고만.

### 씬: `InGame.unity` HUD Canvas 에 `ScreenMessage` 오브젝트
- 루트: 중앙 앵커, `anchoredPosition (0, 40)`, `CanvasGroup`(alpha 0)
- 자식 `BG`: 검정 밴드 860×84 @ alpha 0.5 (가독성)
- 자식 `Text`: Galmuri11 SDF, 42pt, 중앙 정렬, 흰색 + drop shadow, `raycastTarget=false`
- HUD 최하단 형제 삭제 → 맨 위에 그려짐. `Dialogue_Panel`/`Cursor_Prompt` 위.

### `KnockEffect.cs` 수정
- `[Serializable] struct Flavor { string en; string ko; }`
- `Flavor[] refuseMessages` — 기본 3개 (거절된 것 같다 / 깊이 잠든 것 같다 / 응답이 없다)
- `float refuseReadTime = 2.5f`
- 거절 분기: `refuseMessages` 에서 랜덤 1개 → `ScreenMessage.Show(f.en, f.ko)` → `dawnPanel` 있으면 기존 CSV `refuse` 노드도 재생 / 없으면 `refuseReadTime` 만큼 대기 → 화면고정 해제.

## 검증
Play 에서 `ScreenMessage.Show("There is no answer.", "응답이 없다.")` → 화면 중앙에 밴드 + "응답이 없다." 정상 표시 확인 (스크린샷).

## 상태
2026-08-31 완료. 컴파일 0에러. 실제 노크 거절 흐름 검증은 doc/0118 배선 후.
