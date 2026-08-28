using System;
using UnityEngine;
using UnityEngine.InputSystem;

// 저녁 "접객" 파트 관리자.
// 저녁으로 전환되면(암전 시점) 접객 자리로 플레이어를 옮기고 세션을 연다.
// 세션 종료(EndSession)는 그날 숙박객을 전부 처리했을 때 손님 시스템이 호출한다.
//   ponytail: 손님 시스템(SYS-03~06/09) 전까지는 —
//     K   = EndSession (정상 종료 → 새벽으로 페이드)
//     ESC = UIInteractionMode 가 처리. 하위 뷰(모니터)면 그것만 닫고, 접객 레벨이면
//           완전 종료 → Exited 구독으로 세션만 정리 (하루 전환 없음, 테스트용).
public class ReceptionManager : MonoBehaviour
{
    public static ReceptionManager Instance { get; private set; }

    [Tooltip("접객 시 플레이어가 앉을 위치/정면 (접객 테이블의 Player_Anchor)")]
    [SerializeField] private Transform receptionAnchor;
    [SerializeField] private bool debugEndKey = true;   // 디버그: K 로 정상 종료(→새벽)

    public bool InSession { get; private set; }
    public event Action OnSessionStarted;
    public event Action OnSessionEnded;

    private void Awake() => Instance = this;

    private void Start()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged += HandlePhase;
        else
            Debug.LogWarning("[ReceptionManager] DayPhaseManager 없음 — 접객 세션 시작 안 됨", this);

        if (UIInteractionMode.Instance != null)
            UIInteractionMode.Instance.Exited += HandleUIExit;
    }

    private void OnDestroy()
    {
        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.OnPhaseChanged -= HandlePhase;
        if (UIInteractionMode.Instance != null)
            UIInteractionMode.Instance.Exited -= HandleUIExit;
    }

    private void Update()
    {
        if (debugEndKey && InSession && Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
            EndSession();
    }

    private void HandlePhase(DayPhase phase)
    {
        if (phase == DayPhase.Evening && !InSession) BeginSession();
    }

    private void BeginSession()
    {
        InSession = true;

        if (UIInteractionMode.Instance != null)
        {
            if (receptionAnchor == null)
                Debug.LogWarning("[ReceptionManager] receptionAnchor 미할당 — 접객 자리로 이동 못 함", this);
            UIInteractionMode.Instance.Enter(receptionAnchor);
        }

        OnSessionStarted?.Invoke();
    }

    // UI 모드가 완전히 닫혔을 때 (ESC 로 접객 레벨까지 빠져나온 경우 등). 하루 전환은 하지 않음.
    private void HandleUIExit()
    {
        if (!InSession) return;
        InSession = false;
        OnSessionEnded?.Invoke();
        Debug.Log("[ReceptionManager] 접객 모드 탈출 — 세션 정리 (하루 전환 없음)", this);
    }

    // 그날 접객 완료 → 세션 닫고 새벽으로.
    public void EndSession()
    {
        if (!InSession) return;
        InSession = false;

        if (UIInteractionMode.Instance != null)
            UIInteractionMode.Instance.ExitAll();

        OnSessionEnded?.Invoke();

        if (DayPhaseManager.Instance != null)
            DayPhaseManager.Instance.Advance();   // 저녁 → 새벽 (페이드)
    }
}
