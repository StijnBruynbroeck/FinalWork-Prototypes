using UnityEngine;
using UnityEngine.InputSystem;

public class MorphCamera : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private float verticalClampMin = -30f;
    [SerializeField] private float verticalClampMax = 60f;

    private Camera firstPersonCamera;
    private Camera thirdPersonCamera;
    private Vector3 initialThirdPersonOffset;
    private float pitch;
    private float yaw;
    private bool isFirstPerson = false;
    private bool isActive = false;

    void Start()
    {
        var cameras = GetComponentsInChildren<Camera>();
        foreach (var cam in cameras)
        {
            if (cam.gameObject.name.Equals("FirstpersonCamera", System.StringComparison.OrdinalIgnoreCase))
                firstPersonCamera = cam;
            else if (cam.gameObject.name.Equals("ThirdpersonCamera", System.StringComparison.OrdinalIgnoreCase))
                thirdPersonCamera = cam;
        }

        if (thirdPersonCamera != null)
            initialThirdPersonOffset = thirdPersonCamera.transform.localPosition;

        UpdateCameraState();
    }

    void Update()
    {
        if (!isActive) return;

        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            isFirstPerson = !isFirstPerson;
            UpdateCameraState();
        }

        if (Mouse.current == null) return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        float mouseX = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        float mouseY = mouseDelta.y * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, verticalClampMin, verticalClampMax);

        if (isFirstPerson && firstPersonCamera != null)
        {
            firstPersonCamera.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
        }
        else if (!isFirstPerson && thirdPersonCamera != null)
        {
            // Orbit camera around player
            Quaternion orbit = Quaternion.Euler(pitch, yaw, 0f);
            thirdPersonCamera.transform.localPosition = orbit * initialThirdPersonOffset;
            thirdPersonCamera.transform.LookAt(transform.position);
        }
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (active)
        {
            isFirstPerson = false;
            pitch = 0f;
            yaw = 0f;
        }
        UpdateCameraState();
    }

    private void UpdateCameraState()
    {
        if (firstPersonCamera != null) firstPersonCamera.enabled = isActive && isFirstPerson;
        if (thirdPersonCamera != null) thirdPersonCamera.enabled = isActive && !isFirstPerson;
    }
}
