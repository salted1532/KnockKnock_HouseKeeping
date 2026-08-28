using System;
using UnityEngine;

// 하루 일과 중 "접객" 파트 관리자. 지금은 세션 상태만 들고 있음 —
// 손님 큐 / 신분증 심사 / 일일 정산은 이 위에 얹는다.
// 접객 세션 = 저녁 단계에 책상 UI 모드에 들어가 있는 동안.
public class ReceptionManager : MonoBehaviour
{
    public static ReceptionManager Instance { get; private set; }

    public bool InSession { get; private set; }
    public event Action OnSessionStarted;
    public event Action OnSessionEnded;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (UIInteractionMode.Instance != null)
        {
            UIInteractionMode.Instance.Entered += HandleEntered;
            UIInteractionMode.Instance.Exited += HandleExited;
        }
        else
        {
            Debug.LogWarning("[ReceptionManager] UIInteractionMode 인스턴스 없음 — 접객 세션 감지 안 됨", this);
        }
    }

    private void OnDestroy()
    {
        if (UIInteractionMode.Instance != null)
        {
            UIInteractionMode.Instance.Entered -= HandleEntered;
            UIInteractionMode.Instance.Exited -= HandleExited;
        }
    }

    private void HandleEntered()
    {
        // 저녁이 아니면 접객이 아닌 다른 UI 모드 (설정 등) — 세션으로 안 침
        var phase = DayPhaseManager.Instance;
        if (phase != null && phase.Current != DayPhase.Evening) return;

        InSession = true;
        OnSessionStarted?.Invoke();
    }

    private void HandleExited()
    {
        if (!InSession) return;
        InSession = false;
        OnSessionEnded?.Invoke();
    }
}
