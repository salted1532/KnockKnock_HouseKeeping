using UnityEngine;


[RequireComponent(typeof(CharacterController))]
public class SimpleCharacterController : MonoBehaviour
{
    private CharacterController _characterController;

    [SerializeField] private Transform lookRig;
    [SerializeField] private float moveSpeed = 1;
    [SerializeField] private float lookSensitivity = 1;

    private void Start()
    {
        _characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        _yRotation = lookRig.localEulerAngles.y;
        _xRotation = lookRig.localEulerAngles.x;
    }

    private float _xRotation, _yRotation;

    private void Update()
    {
        var mouseDelta = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();

        _yRotation += mouseDelta.x * lookSensitivity * Time.deltaTime;
        _xRotation -= mouseDelta.y * lookSensitivity * Time.deltaTime;

        _xRotation = Mathf.Clamp(_xRotation, -90f, 90f);


        transform.localRotation = Quaternion.Euler(0F, _yRotation, 0f);
        lookRig.localRotation = Quaternion.Euler(_xRotation, 0F, 0f);


        var moveDelta = GetMoveDelta();

        var direction = transform.TransformDirection(new Vector3(moveDelta.x, 0f, moveDelta.y));

        _characterController.Move(direction * (Time.deltaTime * moveSpeed));
    }

    private Vector2 GetMoveDelta()
    {
        var keyboard = UnityEngine.InputSystem.Keyboard.current;
        var xRead = (keyboard.aKey.isPressed ? -1f : 0f) + (keyboard.dKey.isPressed ? 1f : 0f);
        var yRead = (keyboard.wKey.isPressed ? 1f : 0f) + (keyboard.sKey.isPressed ? -1f : 0f);
        return new Vector2(xRead, yRead);
    }
}