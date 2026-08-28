using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 책상 접객 등 "UI 모드": 플레이어를 Player_Anchor 위치/정면으로 이동시켜 화면을 테이블에 고정,
// 마우스 커서 표시, GazeInteractor 대신 CursorInteractor 로 전환. ESC 로 해제.
// 완전 고정은 아니고, 커서를 화면 가장자리로 가져가면 그 방향으로 조금 더 둘러볼 수 있다
// (앵커 정면 기준 yaw/pitch 클램프).
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
    [SerializeField] private float moveTime = 0.3f;

    [Header("가장자리 둘러보기 (앵커 정면 기준)")]
    [Tooltip("끄면 화면 완전 고정. 켜면 커서를 화면 가장자리로 옮겨 조금 둘러볼 수 있음")]
    [SerializeField] private bool edgeLook = false;
    [SerializeField] private float yawRange = 40f;
    [SerializeField] private float pitchRange = 25f;
    [Tooltip("화면 중앙 이 비율 안에서는 시야가 안 움직임 (0~1)")]
    [SerializeField] private float edgeDeadZone = 0.25f;
    [Tooltip("목표 각도로 수렴하는 속도")]
    [SerializeField] private float lookLerp = 4f;

    public bool Active { get; private set; }
    public event Action Entered;
    public event Action Exited;

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

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
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

    public void Enter(Transform anchor)
    {
        if (Active || anchor == null || playerRoot == null)
        {
            if (playerRoot == null) Debug.LogWarning("[UIInteractionMode] playerRoot 미할당 — 이동 안 됨", this);
            if (anchor == null) Debug.LogWarning("[UIInteractionMode] anchor 가 null — EnterUIModeEffect.anchor 확인", this);
            return;
        }
        Active = true;

        if (characterController == null)
            Debug.LogWarning("[UIInteractionMode] characterController 미할당 — CC가 안 꺼져서 플레이어가 앵커로 안 감", this);
        if (firstPersonController == null)
            Debug.LogWarning("[UIInteractionMode] firstPersonController 미할당", this);
        Debug.Log($"[UIInteractionMode] Enter: '{playerRoot.name}' {playerRoot.position} → '{anchor.name}' {anchor.position}", this);

        savedPlayerPos = playerRoot.position;
        savedPlayerRot = playerRoot.rotation;
        savedPitch = cameraPitchPivot != null ? cameraPitchPivot.localRotation : Quaternion.identity;

        if (firstPersonController != null) firstPersonController.enabled = false;
        if (characterController != null) characterController.enabled = false;   // 끈 뒤에야 transform 이동 가능
        if (gazeInteractor != null) gazeInteractor.Suspended = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (exitHint != null) exitHint.SetActive(true);

        baseYaw = anchor.eulerAngles.y;   // 수평 정면만 사용 (pitch/roll 무시)
        curYaw = 0f;
        curPitch = 0f;
        lookActive = false;

        Entered?.Invoke();

        if (move != null) StopCoroutine(move);
        move = StartCoroutine(Transition(
            anchor.position, Quaternion.Euler(0f, baseYaw, 0f), Quaternion.identity,
            () =>
            {
                if (cursorInteractor != null) cursorInteractor.enabled = true;
                lookActive = true;
            }));
    }

    public void Exit()
    {
        if (!Active) return;
        Active = false;
        lookActive = false;

        if (cursorInteractor != null) cursorInteractor.enabled = false;
        if (exitHint != null) exitHint.SetActive(false);

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
