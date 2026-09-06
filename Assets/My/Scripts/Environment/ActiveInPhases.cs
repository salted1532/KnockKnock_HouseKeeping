using System;
using UnityEngine;

// 지정한 시간대에만 target 들을 켜고, 나머지 시간대엔 끈다.
// 가로등: Evening·Dawn 만 켜고 Morning·Noon 엔 끈다 — street light 프리팹 루트에 부착,
// targets = 램프 메시(Open Cylinder) + Point Light. 전환은 DayPhaseManager.OnPhaseChanged
// (암전 시점) 을 구독해 즉시 적용 — 페이드에 가려짐. (PhaseVisuals 와 같은 패턴)
public class ActiveInPhases : MonoBehaviour
{
    [SerializeField] private DayPhase[] activePhases = { DayPhase.Evening, DayPhase.Dawn };
    [Tooltip("켜고 끌 오브젝트들. 비우면 이 GameObject 의 자식 전부")]
    [SerializeField] private GameObject[] targets;

    private void Start()
    {
        if (DayPhaseManager.Instance != null)
        {
            DayPhaseManager.Instance.OnPhaseChanged += Apply;
            Apply(DayPhaseManager.Instance.Current);
        }
        else Debug.LogWarning("[ActiveInPhases] DayPhaseManager 없음 — 시간대별 토글 안 됨", this);
    }

    private void OnDestroy()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged -= Apply;
    }

    private void Apply(DayPhase phase)
    {
        bool on = Array.IndexOf(activePhases, phase) >= 0;

        if (targets != null && targets.Length > 0)
        {
            foreach (var t in targets)
                if (t != null) t.SetActive(on);
        }
        else
        {
            // targets 미지정: 자식만 토글 (자신을 끄면 이벤트를 못 받음)
            foreach (Transform c in transform) c.gameObject.SetActive(on);
        }
    }
}
