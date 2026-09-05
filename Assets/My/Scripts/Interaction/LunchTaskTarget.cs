using UnityEngine;

// 점심 일과 오브젝트(울타리·차 등) 1개에 붙인다. 완료 연출은 선택 사항 — 필요하면 같은 오브젝트의
// ChangeObjectEffect 로 onObjects/offObjects 를 스왑하면 되고(울타리: 고장→고침), 아무것도
// 안 바꿔도 된다(차량 신고: 신고만 하고 오브젝트는 그대로 둔다, 삭제/비활성화 금지).
// 완료 여부는 상호작용 성공(Interactable.Interacted) 1회로 기록한다. InteractionCondition 이라
// 완료 후엔 Interactable.CanInteract 가 false 가 되어 상호작용·프롬프트·아웃라인이 시간대와
// 무관하게 자동으로 막힌다. LunchTasks 가 씬의 모든 LunchTaskTarget 을 합산해 HUD 에 표시하고,
// 아침이 되면 ResetForNewDay 로 재활용한다.
[RequireComponent(typeof(Interactable))]
public class LunchTaskTarget : InteractionCondition
{
    private bool done;

    public bool IsDone => done;
    public override bool IsMet => !done;

    private void Awake() => GetComponent<Interactable>().Interacted += () => done = true;

    // 새 날 아침이 되면 다시 상호작용 가능하도록 초기화 (LunchTasks 가 호출).
    // ChangeObjectEffect 가 있으면(울타리 등 시각 연출이 있는 경우) 원래 상태로도 되돌린다.
    public void ResetForNewDay()
    {
        done = false;
        GetComponent<ChangeObjectEffect>()?.ResetToOff();
    }
}
