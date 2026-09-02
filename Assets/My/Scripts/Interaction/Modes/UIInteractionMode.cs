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
    [SerializeField] private GameObject exitHint;                 // "Backspace 길게 눌러 나가기" UI (선택)
    [SerializeField] private GameObject crosshair;                // 조준점 UI — UI/오버레이 모드 동안 숨김 (선택)
    [SerializeField] private float moveTime = 0.3f;

    [Header("나가기 (ESC 아님 — 실수 방지)")]
    [Tooltip("이 키를 exitHoldTime 만큼 누르고 있어야 화면고정에서 빠져나온다. 접객/새벽 대화 공통")]
    [SerializeField] private Key exitKey = Key.Backspace;
    [SerializeField] private float exitHoldTime = 0.5f;

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

    private float exitHeld;   // exitKey 를 누르고 있은 시간

    // 나가기 홀드 진행도 0~1 (exitHint 의 채움 게이지 등에서 쓸 수 있음)
    public float ExitProgress => exitHoldTime > 0f ? Mathf.Clamp01(exitHeld / exitHoldTime) : 0f;

    // 오버레이(노트 읽기·페이드 전환)로 플레이어가 정지된 상태. FreezeForOverlay 가 토글.
    public bool FrozenForOverlay { get; private set; }
    // 화면고정이든 오버레이든 플레이어 이동이 잠겨 있나 (발소리 등 이동 연출 게이트용).
    public bool MovementLocked => Active || FrozenForOverlay;

    // 이 앵커가 현재 최상위 뷰인가 (토글 진입/해제 판정용).
    public bool IsTopAnchor(Transform t) => anchors.Count > 0 && anchors.Peek() == t;
    public event Action Entered;
    public event Action Exited;          // 완전 종료(스택 비고 Teardown) 시. 접객 세션 정리 등에서 구독

    private readonly Stack<Transform> anchors = new();
    private readonly Stack<float> anchorLookScales = new();   // 앵커별 가장자리 둘러보기 배율 (0=완전고정, 1=기본)
    private readonly Stack<bool> anchorEscExits = new();      // 이 앵커는 ESC 한 번으로 나가나 (모니터=true, 접객·대화=false)
    private float CurLookScale => anchorLookScales.Count > 0 ? anchorLookScales.Peek() : 1f;

    // 최상위 뷰가 ESC 로 나가는 뷰인가 (모니터 화면고정 등). 아니면 exitKey 홀드로만 나감.
    private bool TopEscExits => anchorEscExits.Count > 0 && anchorEscExits.Peek();

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

        bool topEscExits = TopEscExits;

        // 나가기 안내(Backspace 홀드)는 exitKey 로 나가는 뷰에서만 (모니터처럼 ESC 로 나가는 뷰에선 숨김)
        if (exitHint != null && exitHint.activeSelf != !topEscExits)
            exitHint.SetActive(!topEscExits);

        // 노트(ShowPanelEffect)가 열려 있으면 그쪽이 ESC 로 먼저 처리하므로 여기선 손 안 댐
        bool noteEatsEsc = ShowPanelEffect.ConsumesEsc;

        // 모니터 등 ESC 로 나가는 뷰: ESC 한 번에 한 겹 pop (노트 Esc 처럼)
        if (topEscExits && !noteEatsEsc
            && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            Exit();
            return;
        }

        // 접객·새벽 대화: ESC 아님. exitKey(기본 Backspace) 를 exitHoldTime 동안 눌러야 한 겹 벗김.
        var exitCtrl = Keyboard.current != null ? Keyboard.current[exitKey] : null;
        if (!topEscExits && !noteEatsEsc && exitCtrl != null && exitCtrl.isPressed)
        {
            exitHeld += Time.deltaTime;
            if (exitHeld >= exitHoldTime)
            {
                exitHeld = 0f;
                Exit();
                return;
            }
        }
        else exitHeld = 0f;

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

        float s = CurLookScale;   // 현재 앵커의 둘러보기 배율 (노크 = 좁게)
        float targetYaw = EdgeFactor(nx) * yawRange * s;
        // ponytail: 커서 위 → 위를 본다고 가정. 플레이 후 반대로 느껴지면 부호만 뒤집기.
        // 상/하 범위 분리 — 아래는 질문 버튼 영역이라 작게.
        float pitchRange = (ny >= 0f ? pitchUpRange : pitchDownRange) * s;
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
    // 기본 = 연출용(ESC 로 못 나감, exitKey 홀드로만). 모니터처럼 가볍게 보다 마는 뷰는 escExits:true.
    public void Enter(Transform anchor) => Enter(anchor, 1f, false);
    public void Enter(Transform anchor, float lookScale) => Enter(anchor, lookScale, false);

    // lookScale: 가장자리 둘러보기 배율 (0 = 완전 고정, 1 = 기본). 노크는 좁게 (0.2~0.3 권장).
    // escExits: true 면 이 뷰는 ESC 한 번으로 나감(모니터 등). false(기본) 면 exitKey 홀드로만 (접객·노크·연출).
    public void Enter(Transform anchor, float lookScale, bool escExits)
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

        Debug.Log($"[UIInteractionMode] Enter → '{anchor.name}' (depth {anchors.Count + 1}, esc={escExits})", this);
        anchors.Push(anchor);
        anchorLookScales.Push(Mathf.Clamp01(lookScale));
        anchorEscExits.Push(escExits);
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
        FrozenForOverlay = on;
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
        exitHeld = 0f;   // 홀드 소비 — 눌린 채로 다음 레벨까지 연속으로 벗겨지지 않게
        anchors.Pop();
        if (anchorLookScales.Count > 0) anchorLookScales.Pop();
        if (anchorEscExits.Count > 0) anchorEscExits.Pop();

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
        anchorLookScales.Clear();
        anchorEscExits.Clear();
        Teardown();
    }

    private void Teardown()
    {
        Active = false;
        lookActive = false;
        exitHeld = 0f;

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
