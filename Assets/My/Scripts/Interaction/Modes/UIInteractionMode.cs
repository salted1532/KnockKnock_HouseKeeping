using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// 책상 접객 등 "UI 모드": 플레이어를 Player_Anchor 위치/정면으로 이동시켜 화면을 테이블에 고정,
// 마우스 커서 표시, GazeInteractor 대신 CursorInteractor 로 전환. ESC 로 해제.
// 완전 고정은 아니고, 커서를 화면 가장자리로 가져가면 그 방향으로 조금 더 둘러볼 수 있다
// (앵커 정면 기준 yaw/pitch 클램프).
//
// 앵커는 스택으로 쌓인다: 접객 모드(하위) 안에서 모니터 "화면고정"(상위) 을 눌러 들어가고,
// ESC 로 상위만 닫으면 하위(접객)로 복귀. 스택이 비면 완전 종료(플레이어 원위치 복귀).
public class UIInteractionMode : MonoBehaviour
{
    public static UIInteractionMode Instance { get; private set; }

    [Header("플레이어")]
    [SerializeField] private Transform playerRoot;                // PlayerCapsule (yaw + 위치)
    [SerializeField] private Transform cameraPitchPivot;          // PlayerCameraRoot (pitch)
    [SerializeField] private MonoBehaviour firstPersonController;  // StarterAssets.FirstPersonController
    [SerializeField] private CharacterController characterController;
    [SerializeField] private GazeInteractor gazeInteractor;
    [SerializeField] private CursorInteractor cursorInteractor;
    [SerializeField] private GameObject exitHint;                 // "ESC 나가기" UI (선택)
    [SerializeField] private GameObject crosshair;                // 조준점 UI — UI/오버레이 모드 동안 숨김 (선택)
    [SerializeField] private float moveTime = 0.3f;

    [Header("가장자리 둘러보기 (앵커 정면 기준)")]
    [Tooltip("끄면 화면 완전 고정. 켜면 커서를 화면 가장자리로 옮겨 조금 둘러볼 수 있음")]
    [SerializeField] private bool edgeLook = false;
    [SerializeField] private float yawRange = 40f;
    [Tooltip("커서 상단 → 위로 볼 수 있는 최대 각")]
    [SerializeField] private float pitchUpRange = 12f;
    [Tooltip("커서 하단 → 아래로 볼 수 있는 최대 각 (버튼 영역이라 작게)")]
    [SerializeField] private float pitchDownRange = 4f;
    [Tooltip("화면 중앙 이 비율 안에서는 시야가 안 움직임 (0~1)")]
    [SerializeField] private float edgeDeadZone = 0.25f;
    [Tooltip("목표 각도로 수렴하는 속도")]
    [SerializeField] private float lookLerp = 4f;

    public bool Active { get; private set; }
    public int Depth => anchors.Count;   // 쌓인 앵커 수 (0=비활성, 1=접객만, 2=접객+모니터 …)
    public event Action Entered;
    public event Action Exited;          // 완전 종료(스택 비고 Teardown) 시. 접객 세션 정리 등에서 구독

    private readonly Stack<Transform> anchors = new();

    private Vector3 savedPlayerPos;
    private Quaternion savedPlayerRot;
    private Quaternion savedPitch;

    private float baseYaw;    // 앵커 정면 yaw (월드)
    private float curYaw;     // baseYaw 기준 오프셋
    private float curPitch;   // 수평(0) 기준 오프셋
    private bool lookActive;  // 진입 트랜지션이 끝나야 켜짐
    private Coroutine move;

    private void Awake()
    {
        Instance = this;
        if (cursorInteractor != null) cursorInteractor.enabled = false;
        if (exitHint != null) exitHint.SetActive(false);
    }

    private void Update()
    {
        if (!Active) return;

        // 창 포커스가 돌아오면 StarterAssetsInputs.OnApplicationFocus 가 커서를 다시 잠근다.
        // UI 모드 동안은 매 프레임 다시 풀어둔다 (에디터에서 화면 밖 갔다 오면 커서 사라지는 문제).
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // ESC: 노트가 열려 있으면 노트가 먼저 소비. 아니면 한 겹 벗김
        //  - 하위 뷰(모니터 등) → 상위(접객)로 복귀
        //  - 최상위 → 완전 종료 (접객이면 Exited 구독한 ReceptionManager 가 세션 정리)
        if (!ShowPanelEffect.ConsumesEsc
            && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Exit();
            return;
        }

        if (lookActive && edgeLook) EdgeLook();
    }

    // 커서가 화면 가장자리에 가까울수록 그 방향 목표 각도를 키운다. 중앙 데드존이면 0 → 정면으로 복귀.
    private void EdgeLook()
    {
        Vector2 p = Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width, Screen.height) * 0.5f;

        float nx = Mathf.Clamp((p.x / Mathf.Max(1f, Screen.width)) * 2f - 1f, -1f, 1f);
        float ny = Mathf.Clamp((p.y / Mathf.Max(1f, Screen.height)) * 2f - 1f, -1f, 1f);

        float targetYaw = EdgeFactor(nx) * yawRange;
        // ponytail: 커서 위 → 위를 본다고 가정. 플레이 후 반대로 느껴지면 부호만 뒤집기.
        // 상/하 범위 분리 — 아래는 질문 버튼 영역이라 작게.
        float pitchRange = ny >= 0f ? pitchUpRange : pitchDownRange;
        float targetPitch = -EdgeFactor(ny) * pitchRange;

        curYaw = Mathf.Lerp(curYaw, targetYaw, Time.deltaTime * lookLerp);
        curPitch = Mathf.Lerp(curPitch, targetPitch, Time.deltaTime * lookLerp);

        if (playerRoot != null)
            playerRoot.rotation = Quaternion.Euler(0f, baseYaw + curYaw, 0f);
        if (cameraPitchPivot != null)
            cameraPitchPivot.localRotation = Quaternion.Euler(curPitch, 0f, 0f);
    }

    // 데드존 밖 부분만 0..1 로 재매핑 (부호 유지)
    private float EdgeFactor(float n)
    {
        float a = Mathf.Abs(n);
        if (a <= edgeDeadZone) return 0f;
        return Mathf.Sign(n) * (a - edgeDeadZone) / Mathf.Max(0.0001f, 1f - edgeDeadZone);
    }

    // anchor 뷰로 진입. 이미 UI 모드면 그 위에 쌓는다 (접객 → 모니터). 같은 앵커 재진입은 무시.
    public void Enter(Transform anchor)
    {
        if (anchor == null || playerRoot == null)
        {
            if (playerRoot == null) Debug.LogWarning("[UIInteractionMode] playerRoot 미할당 — 이동 안 됨", this);
            if (anchor == null) Debug.LogWarning("[UIInteractionMode] anchor 가 null — EnterUIModeEffect.anchor 확인", this);
            return;
        }
        if (anchors.Count > 0 && anchors.Peek() == anchor) return;   // 이미 이 뷰

        if (!Active)   // 첫 진입 — 플레이어 상태 저장 + 모드 셋업
        {
            Active = true;

            if (characterController == null)
                Debug.LogWarning("[UIInteractionMode] characterController 미할당 — CC가 안 꺼져서 플레이어가 앵커로 안 감", this);
            if (firstPersonController == null)
                Debug.LogWarning("[UIInteractionMode] firstPersonController 미할당", this);

            savedPlayerPos = playerRoot.position;
            savedPlayerRot = playerRoot.rotation;
            savedPitch = cameraPitchPivot != null ? cameraPitchPivot.localRotation : Quaternion.identity;

            if (firstPersonController != null) firstPersonController.enabled = false;
            if (characterController != null) characterController.enabled = false;   // 끈 뒤에야 transform 이동 가능
            if (gazeInteractor != null) gazeInteractor.Suspended = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (exitHint != null) exitHint.SetActive(true);
            if (crosshair != null) crosshair.SetActive(false);

            Entered?.Invoke();
        }

        Debug.Log($"[UIInteractionMode] Enter → '{anchor.name}' (depth {anchors.Count + 1})", this);
        anchors.Push(anchor);
        MoveToAnchor(anchor);
    }

    private void MoveToAnchor(Transform anchor)
    {
        baseYaw = anchor.eulerAngles.y;   // 수평 정면만 사용 (pitch/roll 무시)
        curYaw = 0f;
        curPitch = 0f;
        lookActive = false;

        if (move != null) StopCoroutine(move);
        move = StartCoroutine(Transition(
            anchor.position, Quaternion.Euler(0f, baseYaw, 0f), Quaternion.identity,
            () =>
            {
                if (cursorInteractor != null) cursorInteractor.enabled = true;
                lookActive = true;
            }));
    }

    // 이동 없이 플레이어만 정지 + 커서 표시 (노트 등 오버레이 UI 용). Enter 와 달리 앵커 이동/전환 없음.
    // 실제 UI 모드(Active)가 돌고 있으면 무시 — 그쪽이 이미 관리 중.
    public void FreezeForOverlay(bool on)
    {
        if (Active) return;
        if (firstPersonController != null) firstPersonController.enabled = !on;
        if (gazeInteractor != null) gazeInteractor.Suspended = on;
        if (crosshair != null) crosshair.SetActive(!on);
        Cursor.lockState = on ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = on;
    }

    // 한 단계 뒤로. 하위 뷰가 남아 있으면 그 뷰로 복귀, 스택이 비면 완전 종료.
    public void Exit()
    {
        if (!Active || anchors.Count == 0) return;
        anchors.Pop();

        if (anchors.Count > 0)
        {
            MoveToAnchor(anchors.Peek());   // 상위 뷰(접객 등)로 복귀 — 모드 유지
            return;
        }
        Teardown();
    }

    // 모든 레벨을 닫고 완전 종료 (접객 세션 종료 등에서 호출).
    public void ExitAll()
    {
        if (!Active) return;
        anchors.Clear();
        Teardown();
    }

    private void Teardown()
    {
        Active = false;
        lookActive = false;

        if (cursorInteractor != null) cursorInteractor.enabled = false;
        if (exitHint != null) exitHint.SetActive(false);
        if (crosshair != null) crosshair.SetActive(true);

        Exited?.Invoke();

        if (move != null) StopCoroutine(move);
        move = StartCoroutine(Transition(
            savedPlayerPos, savedPlayerRot, savedPitch,
            () =>
            {
                if (characterController != null) characterController.enabled = true;
                if (firstPersonController != null) firstPersonController.enabled = true;
                if (gazeInteractor != null) gazeInteractor.Suspended = false;
                Cursor.lockState = CursorLockMode.Locked;   // UI 모드에서 나오면 항상 게임플레이 = 락 + 숨김
                Cursor.visible = false;
            }));
    }

    private IEnumerator Transition(Vector3 pos, Quaternion yaw, Quaternion pitch, Action done)
    {
        Vector3 fromP = playerRoot.position;
        Quaternion fromYaw = playerRoot.rotation;
        Quaternion fromPitch = cameraPitchPivot != null ? cameraPitchPivot.localRotation : Quaternion.identity;
        float t = 0f;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / moveTime));
            playerRoot.SetPositionAndRotation(Vector3.Lerp(fromP, pos, k), Quaternion.Slerp(fromYaw, yaw, k));
            if (cameraPitchPivot != null)
                cameraPitchPivot.localRotation = Quaternion.Slerp(fromPitch, pitch, k);
            yield return null;
        }
        playerRoot.SetPositionAndRotation(pos, yaw);
        if (cameraPitchPivot != null) cameraPitchPivot.localRotation = pitch;
        done?.Invoke();
    }
}
