using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float mouseSensitivity = 100f;

    private CharacterController controller;
    private Camera firstPersonCamera;
    private Camera thirdPersonCamera;
    private float xRotation = 0f;
    private bool isFirstPerson = true;

    void Start()
    {
        controller = GetComponentInChildren<CharacterController>();
        
        var cameras = GetComponentsInChildren<Camera>();
        string camNames = "";
        foreach (var cam in cameras)
        {
            camNames += cam.gameObject.name + ", ";
            if (cam.gameObject.name.Equals("FirstpersonCamera", System.StringComparison.OrdinalIgnoreCase)) firstPersonCamera = cam;
            else if (cam.gameObject.name.Equals("ThirdpersonCamera", System.StringComparison.OrdinalIgnoreCase)) thirdPersonCamera = cam;
        }
        Debug.Log($"Camera's gevonden op player: [{camNames}] fp={(firstPersonCamera != null ? firstPersonCamera.gameObject.name : "null")} tp={(thirdPersonCamera != null ? thirdPersonCamera.gameObject.name : "null")}");

        Cursor.lockState = CursorLockMode.Locked;
        SwitchCamera(true);
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            isFirstPerson = !isFirstPerson;
            Debug.Log($"P pressed, switching to {(isFirstPerson ? "first" : "third")} person. cams: fp={firstPersonCamera?.name}, tp={thirdPersonCamera?.name}");
            SwitchCamera(isFirstPerson);
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
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            firstPersonCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }

        transform.Rotate(Vector3.up * mouseX);
    }

    public void SwitchCamera(bool firstPerson)
    {
        if (firstPersonCamera != null) firstPersonCamera.enabled = firstPerson;
        if (thirdPersonCamera != null) thirdPersonCamera.enabled = !firstPerson;
    }
}