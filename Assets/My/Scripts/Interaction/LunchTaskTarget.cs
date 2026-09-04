using UnityEngine;

// 점심 일과 오브젝트(울타리·차 등) 1개에 붙인다. 실제 상태 전환은 같은 오브젝트의
// ChangeObjectEffect 가 담당 — onObjects/offObjects 를 여기에도 그대로 연결하면 끝.
// 울타리(고장→고침 스왑): onObjects=고친 버전, offObjects=고장난 버전.
// 차량 신고(그냥 사라짐): onObjects=비워둠, offObjects=차 오브젝트 자신.
// LunchTasks 가 씬의 모든 LunchTaskTarget 을 합산한다.
public class LunchTaskTarget : MonoBehaviour
{
    [Tooltip("완료 시 켜져 있어야 하는 오브젝트 (ChangeObjectEffect.onObjects 와 동일하게 연결, 비워도 됨)")]
    [SerializeField] private GameObject[] onObjects;
    [Tooltip("완료 시 꺼져 있어야 하는 오브젝트 (ChangeObjectEffect.offObjects 와 동일하게 연결)")]
    [SerializeField] private GameObject[] offObjects;

    public bool IsDone
    {
        get
        {
            if (onObjects != null)
                foreach (var o in onObjects)
                    if (o == null || !o.activeSelf) return false;
            if (offObjects != null)
                foreach (var o in offObjects)
                    if (o != null && o.activeSelf) return false;
            return true;
        }
    }
}
