using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// 책상 접객 등 "UI 모드": 플레이어/카메라를 앵커에 고정, 마우스 커서 표시,
// GazeInteractor 대신 CursorInteractor 로 전환. ESC 로 해제.
public class UIInteractionMode : MonoBehaviour
{
    public static UIInteractionMode Instance { get; private set; }

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private MonoBehaviour firstPersonController; // StarterAssets.FirstPersonController
    [SerializeField] private CharacterController characterController;
    [SerializeField] private GazeInteractor gazeInteractor;
    [SerializeField] private CursorInteractor cursorInteractor;
    [SerializeField] private GameObject exitHint;                 // "ESC 나가기" UI (선택)
    [SerializeField] private float moveTime = 0.3f;

    public bool Active { get; private set; }

    private Vector3 savedCamPos;
    private Quaternion savedCamRot;
    private CursorLockMode savedLock;
    private bool savedCursorVisible;
    private Coroutine move;

    private void Awake()
    {
        Instance = this;
        if (cursorInteractor != null) cursorInteractor.enabled = false;
        if (exitHint != null) exitHint.SetActive(false);
    }

    private void Update()
    {
        if (Active && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            Exit();
    }

    public void Enter(Transform anchor)
    {
        if (Active || anchor == null || cameraTransform == null) return;
        Active = true;

        savedCamPos = cameraTransform.position;
        savedCamRot = cameraTransform.rotation;
        savedLock = Cursor.lockState;
        savedCursorVisible = Cursor.visible;

        if (firstPersonController != null) firstPersonController.enabled = false;
        if (characterController != null) characterController.enabled = false;
        if (gazeInteractor != null) gazeInteractor.Suspended = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (exitHint != null) exitHint.SetActive(true);

        if (move != null) StopCoroutine(move);
        move = StartCoroutine(MoveCamera(anchor.position, anchor.rotation, () =>
        {
            if (cursorInteractor != null) cursorInteractor.enabled = true;
        }));
    }

    public void Exit()
    {
        if (!Active) return;
        Active = false;

        if (cursorInteractor != null) cursorInteractor.enabled = false;
        if (exitHint != null) exitHint.SetActive(false);

        if (move != null) StopCoroutine(move);
        move = StartCoroutine(MoveCamera(savedCamPos, savedCamRot, () =>
        {
            if (characterController != null) characterController.enabled = true;
            if (firstPersonController != null) firstPersonController.enabled = true;
            if (gazeInteractor != null) gazeInteractor.Suspended = false;
            Cursor.lockState = savedLock;
            Cursor.visible = savedCursorVisible;
        }));
    }

    private IEnumerator MoveCamera(Vector3 pos, Quaternion rot, System.Action done)
    {
        Vector3 fromP = cameraTransform.position;
        Quaternion fromR = cameraTransform.rotation;
        float t = 0f;
        while (t < moveTime)
        {
            t += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / moveTime));
            cameraTransform.SetPositionAndRotation(Vector3.Lerp(fromP, pos, k), Quaternion.Slerp(fromR, rot, k));
            yield return null;
        }
        cameraTransform.SetPositionAndRotation(pos, rot);
        done?.Invoke();
    }
}
