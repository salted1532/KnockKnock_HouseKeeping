using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 손님 오브젝트의 이동. 경로(웨이포인트)는 씬 쪽(ReceptionManager)이 넘겨준다 — 프리팹이라 씬 참조를 못 가짐.
// ponytail: NavMesh·경로탐색 없이 웨이포인트 직선 이동. 장애물 회피 필요하면 NavMeshAgent 로 교체.
public class GuestMover : MonoBehaviour
{
    [Header("이동")]
    [SerializeField] private float speed = 1.3f;
    [SerializeField] private float arriveDistance = 0.06f;

    [Header("바라보는 방향")]
    [Tooltip("손님이 항상 유지할 y 회전. 접객 중 플레이어는 고정이라 걷는 방향과 무관하게 이 각도로 플레이어를 쳐다봄")]
    [SerializeField] private float faceYaw = -180f;

    [Header("애니메이션")]
    [Tooltip("선택 — 있으면 이동 중 이 bool 파라미터를 true 로")]
    [SerializeField] private Animator animator;
    [SerializeField] private string walkBool = "Walking";

    [Tooltip("옆모습 전환용. 비우면 자식에서 찾음")]
    [SerializeField] private GuestView view;

    // 접객 일시정지(ESC) — true 면 이동 코루틴이 그 자리에 멈춘다. ReceptionManager 가 토글.
    public bool Frozen { get; set; }

    private void Reset()
    {
        animator = GetComponentInChildren<Animator>();
        view = GetComponentInChildren<GuestView>();
    }

    private void Awake()
    {
        if (view == null) view = GetComponentInChildren<GuestView>();
    }

    // 즉시 이동 (다음 손님 시작 전 스폰 지점으로). 회전은 항상 플레이어 쪽 고정.
    public void WarpTo(Transform t)
    {
        if (t != null) transform.SetPositionAndRotation(t.position, Quaternion.Euler(0f, faceYaw, 0f));
    }

    // facings[i] = waypoints[i] 로 걷는 동안의 바라보는 방향 (없거나 짧으면 Auto).
    // onArrive(i) = waypoints[i] 에 도착한 직후 호출 (출입문 열기 등). i=0 은 시작점이라 거의 즉시.
    public IEnumerator WalkThrough(IReadOnlyList<Transform> waypoints,
                                   IReadOnlyList<GuestView.Facing> facings = null,
                                   Action<int> onArrive = null)
    {
        if (waypoints == null || waypoints.Count == 0) yield break;
        SetWalking(true);

        var facing = Quaternion.Euler(0f, faceYaw, 0f);
        for (int i = 0; i < waypoints.Count; i++)
        {
            var wp = waypoints[i];
            if (wp == null) continue;
            var legFacing = facings != null && i < facings.Count ? facings[i] : GuestView.Facing.Auto;
            while (true)
            {
                if (Frozen)   // 일시정지 — 그 자리에 멈춤
                {
                    SetWalking(false);
                    while (Frozen) yield return null;
                    SetWalking(true);
                }

                Vector3 to = wp.position - transform.position;
                to.y = 0f;
                if (to.sqrMagnitude <= arriveDistance * arriveDistance) break;

                Vector3 dir = to.normalized;
                transform.position += dir * (speed * Time.deltaTime);
                transform.rotation = facing;   // 걷는 방향과 무관하게 항상 플레이어 쪽
                if (view != null) view.SetWalkFacing(legFacing, dir);
                yield return null;
            }
            onArrive?.Invoke(i);
        }

        SetWalking(false);
        if (view != null) view.EndSide();
    }

    private void SetWalking(bool v)
    {
        if (animator != null && !string.IsNullOrEmpty(walkBool))
            animator.SetBool(walkBool, v);
    }
}
