using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 100f;

    private CharacterController controller;
    private Camera firstPersonCamera;
    private Camera thirdPersonCamera;
    private Vector3 thirdPersonCameraOffset;
    private float pitch;
    private float yaw;
    private bool isFirstPerson = true;
    private bool morphMode = false;

    void Start()
    {
        controller = GetComponentInChildren<CharacterController>();
        
        var cameras = GetComponentsInChildren<Camera>();
        foreach (var cam in cameras)
        {
            if (cam.gameObject.name.Equals("FirstpersonCamera", System.StringComparison.OrdinalIgnoreCase)) firstPersonCamera = cam;
            else if (cam.gameObject.name.Equals("ThirdpersonCamera", System.StringComparison.OrdinalIgnoreCase)) thirdPersonCamera = cam;
        }

        if (thirdPersonCamera != null)
            thirdPersonCameraOffset = thirdPersonCamera.transform.localPosition;

        Cursor.lockState = CursorLockMode.Locked;
        SwitchCamera(true);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            isFirstPerson = !isFirstPerson;
            SwitchCamera(isFirstPerson);
        }

        if (morphMode)
        {
            HandleMorphLook();
            return;
        }

        float horizontal = 0f;
        float vertical = 0f;

        if (Keyboard.current.wKey.isPressed) vertical = 1f;
        if (Keyboard.current.sKey.isPressed) vertical = -1f;
        if (Keyboard.current.aKey.isPressed) horizontal = -1f;
        if (Keyboard.current.dKey.isPressed) horizontal = 1f;

        float currentSpeed = moveSpeed;
        if (Keyboard.current.shiftKey.isPressed) currentSpeed += moveSpeed * 0.5f;

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * currentSpeed * Time.deltaTime);

        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        if (isFirstPerson && firstPersonCamera != null)
        {
            pitch -= mouseY;
            pitch = Mathf.Clamp(pitch, -90f, 90f);
            firstPersonCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleMorphLook()
    {
        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        yaw += mouseDelta.x * mouseSensitivity * Time.deltaTime;
        pitch -= mouseDelta.y * mouseSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, -30f, 60f);

        if (isFirstPerson && firstPersonCamera != null)
        {
            firstPersonCamera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        else if (!isFirstPerson && thirdPersonCamera != null)
        {
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            thirdPersonCamera.transform.localPosition = orbit * thirdPersonCameraOffset;
            thirdPersonCamera.transform.LookAt(transform.position);
        }
    }

    public void SetMorphMode(bool active)
    {
        morphMode = active;
        if (active)
        {
            isFirstPerson = false;
            pitch = 0f;
            yaw = 0f;
            SwitchCamera(false);
        }
        else
        {
            SwitchCamera(true);
        }
    }

    public void SwitchCamera(bool firstPerson)
    {
        isFirstPerson = firstPerson;
        if (firstPersonCamera != null) firstPersonCamera.enabled = firstPerson;
        if (thirdPersonCamera != null) thirdPersonCamera.enabled = !firstPerson;
    }
}