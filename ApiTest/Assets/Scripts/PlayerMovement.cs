using UnityEngine;
using UnityEngine.InputSystem; // Deze is cruciaal voor het nieuwe systeem!

public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed = 5f;
    // Let op: Ik heb deze iets verlaagd omdat de nieuwe Mouse.delta grotere getallen teruggeeft dan de oude GetAxis
    public float mouseSensitivity = 10f; 

    private CharacterController controller;
    private Camera firstPersonCamera;
    private Camera thirdPersonCamera;
    private float xRotation = 0f;
    private bool isFirstPerson = true;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        
        var cameras = GetComponentsInChildren<Camera>();
        foreach (var cam in cameras)
        {
            if (cam.gameObject.name == "FirstPersonCamera") firstPersonCamera = cam;
            else if (cam.gameObject.name == "ThirdPersonCamera") thirdPersonCamera = cam;
        }

        Cursor.lockState = CursorLockMode.Locked;
        SwitchCamera(true);
    }

    void Update()
    {
        // Veiligheidscheck of toetsenbord en muis wel aangesloten zijn
        if (Keyboard.current == null || Mouse.current == null) return;

        // Switchen tussen First en Third person met 'P'
        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            isFirstPerson = !isFirstPerson;
            SwitchCamera(isFirstPerson);
        }

        float horizontal = 0f;
        float vertical = 0f;

        // WASD Movement
        if (Keyboard.current.wKey.isPressed) vertical = 1f;
        if (Keyboard.current.sKey.isPressed) vertical = -1f;
        if (Keyboard.current.aKey.isPressed) horizontal = -1f;
        if (Keyboard.current.dKey.isPressed) horizontal = 1f;

        float currentSpeed = moveSpeed;
        if (Keyboard.current.leftShiftKey.isPressed) currentSpeed += moveSpeed * 0.5f;

        Vector3 move = transform.right * horizontal + transform.forward * vertical;
        controller.Move(move * currentSpeed * Time.deltaTime);

        // Muis input lezen
        float mouseX = Mouse.current.delta.x.ReadValue() * mouseSensitivity * Time.deltaTime;
        float mouseY = Mouse.current.delta.y.ReadValue() * mouseSensitivity * Time.deltaTime;

        if (isFirstPerson)
        {
            xRotation -= mouseY;
            xRotation = Mathf.Clamp(xRotation, -90f, 90f);

            if (firstPersonCamera != null)
            {
                firstPersonCamera.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
            }
            transform.Rotate(Vector3.up * mouseX);
        }
        else
        {
            transform.Rotate(Vector3.up * mouseX);
        }
    }

    public void SwitchCamera(bool firstPerson)
    {
        isFirstPerson = firstPerson; // Houd de status synchroon
        
        if (firstPersonCamera != null) firstPersonCamera.enabled = firstPerson;
        if (thirdPersonCamera != null) thirdPersonCamera.enabled = !firstPerson;
    }
}