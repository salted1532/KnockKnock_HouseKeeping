using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 손님 오브젝트의 이동. 경로(웨이포인트)는 씬 쪽(ReceptionManager)이 넘겨준다 — 프리팹이라 씬 참조를 못 가짐.
// ponytail: NavMesh·경로탐색 없이 웨이포인트 직선 이동. 장애물 회피 필요하면 NavMeshAgent 로 교체.
public class GuestMover : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float speed = 1.3f;
    [SerializeField] private float turnSpeed = 12f;
    [SerializeField] private float arriveDistance = 0.06f;

    [Header("애니메이션")]
    [Tooltip("선택 — 있으면 이동 중 이 bool 파라미터를 true 로")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkBool = "Walking";

    // 즉시 이동 (다음 손님 시작 전 스폰 지점으로).
    public void WarpTo(Transform t)
    {
        if (t != null) transform.SetPositionAndRotation(t.position, t.rotation);
    }

    public IEnumerator WalkThrough(IReadOnlyList<Transform> waypoints)
    {
        if (waypoints == null || waypoints.Count == 0) yield break;
        SetWalking(true);

        foreach (var wp in waypoints)
        {
            if (wp == null) continue;
            while (true)
            {
                Vector3 to = wp.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude <= arriveDistance * arriveDistance) break;

                transform.position += to.normalized * (speed * Time.deltaTime);
                var look = Quaternion.LookRotation(to, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, look, turnSpeed * Time.deltaTime);
                yield return null;
            }
        }

        SetWalking(false);
    }

    private void SetWalking(bool v)
    {
        if (animator != null && !string.IsNullOrEmpty(walkBool))
            animator.SetBool(walkBool, v);
    }
}
