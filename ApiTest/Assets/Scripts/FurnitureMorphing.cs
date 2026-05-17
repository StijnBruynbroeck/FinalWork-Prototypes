using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class FurnitureMorphing : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private Canvas furnitureCanvas;

    [Header("Buttons (5)")]
    [SerializeField] private Button[] morphButtons = new Button[5];

    [Header("Unmorph")]
    [SerializeField] private Button unmorphButton;

    [Header("Furniture Prefabs (5)")]
    [SerializeField] private GameObject[] furniturePrefabs = new GameObject[5];

    [Header("Player")]
    [SerializeField] private GameObject playerObject;

    private GameObject currentFurniture;
    private Camera currentCamera;

    void Start()
    {
        if (playerObject != null)
            currentCamera = playerObject.GetComponentInChildren<Camera>();

        for (int i = 0; i < morphButtons.Length; i++)
        {
            int index = i;
            if (morphButtons[i] != null)
                morphButtons[i].onClick.AddListener(() => {
                    Debug.Log($"Button {index} clicked");
                    MorphInto(index);
                });
        }

        if (unmorphButton != null)
            unmorphButton.onClick.AddListener(Unmorph);
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.gKey.wasPressedThisFrame)
            ToggleCanvas();
    }

    private void ToggleCanvas()
    {
        if (furnitureCanvas == null) return;

        bool isActive = !furnitureCanvas.gameObject.activeSelf;
        furnitureCanvas.gameObject.SetActive(isActive);

        Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isActive;
    }

    private void MorphInto(int index)
    {
        if (index < 0 || index >= furniturePrefabs.Length || furniturePrefabs[index] == null)
            return;

        Vector3 pos = playerObject != null ? playerObject.transform.position : transform.position;
        Quaternion rot = playerObject != null ? playerObject.transform.rotation : transform.rotation;

        if (currentCamera != null)
            currentCamera.transform.SetParent(null);

        if (currentFurniture != null)
            Destroy(currentFurniture);

        currentFurniture = Instantiate(furniturePrefabs[index], pos, rot);

        if (playerObject != null && playerObject.transform.parent != null)
            currentFurniture.transform.SetParent(playerObject.transform.parent);

        if (currentCamera != null)
            currentCamera.transform.SetParent(currentFurniture.transform, true);

        if (playerObject != null)
            playerObject.SetActive(false);

        if (furnitureCanvas != null)
        {
            furnitureCanvas.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void Unmorph()
    {
        if (currentCamera != null && playerObject != null)
            currentCamera.transform.SetParent(playerObject.transform, false);

        if (currentFurniture != null)
            Destroy(currentFurniture);

        if (playerObject != null)
            playerObject.SetActive(true);

        if (furnitureCanvas != null)
        {
            furnitureCanvas.gameObject.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
}