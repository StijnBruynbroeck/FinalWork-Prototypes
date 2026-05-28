using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class DoorCode : MonoBehaviour
{
    [Header("Numpad Canvas (auto-found if empty)")]
    [SerializeField] private Canvas numpadCanvas;
    [SerializeField] private TMP_Text codeDisplayText;

    [Header("Buttons (auto-found if empty)")]
    [SerializeField] private Button[] numberButtons;
    [SerializeField] private Button cancelButton;
    [SerializeField] private Button correctButton;

    [Header("Door Settings")]
    [SerializeField] private float interactionRange = 3f;

    [Header("Code")]
    [SerializeField] private string correctCode = "12123";

    private string enteredCode = "";
    private bool isLocked = false;

    void Start()
    {
        if (numpadCanvas == null)
            numpadCanvas = GameObject.Find("Numpad")?.GetComponent<Canvas>();

        if (numpadCanvas == null)
        {
            Debug.LogError("DoorCode: Geen Numpad canvas gevonden.");
            enabled = false;
            return;
        }

        if (codeDisplayText == null)
            codeDisplayText = numpadCanvas.GetComponentInChildren<TMP_Text>();

        if (codeDisplayText == null)
            Debug.LogWarning("DoorCode: Geen TMP_Text gevonden in Numpad canvas.");

        if (numberButtons == null || numberButtons.Length == 0)
            FindButtonsByText();

        for (int i = 0; i < numberButtons.Length && i < 10; i++)
        {
            int digit = i;
            if (numberButtons[i] != null)
                numberButtons[i].onClick.AddListener(() => AddDigit(digit));
            else
                Debug.LogWarning($"DoorCode: Geen button gevonden voor cijfer {i}");
        }

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CloseNumpad);

        if (correctButton != null)
            correctButton.onClick.AddListener(RemoveLastDigit);

        numpadCanvas.gameObject.SetActive(false);
    }

    private void FindButtonsByText()
    {
        numberButtons = new Button[10];
        Button[] allButtons = numpadCanvas.GetComponentsInChildren<Button>(true);

        foreach (Button btn in allButtons)
        {
            string btnName = btn.name.ToLower();
            string btnText = btn.GetComponentInChildren<Text>()?.text?.Trim()
                          ?? btn.GetComponentInChildren<TMP_Text>()?.text?.Trim()
                          ?? "";

            Debug.Log($"DoorCode: Button gevonden - name: '{btn.name}', text: '{btnText}'");

            if (btnName.Contains("cancel") || btnText.ToLower() == "cancel" || btnText == "X")
            {
                cancelButton = btn;
                continue;
            }

            if (btnName.Contains("correct") || btnText.ToLower() == "correct"
                || btnText.ToLower() == "confirm" || btnName.Contains("confirm")
                || btnText == "C" || btnText == "<" || btnText == "⌫")
            {
                correctButton = btn;
                continue;
            }

            if (int.TryParse(btnText, out int digit) && digit >= 0 && digit <= 9)
            {
                numberButtons[digit] = btn;
                continue;
            }

            if (btnText.Length == 1 && char.IsDigit(btnText[0]))
            {
                int d = btnText[0] - '0';
                numberButtons[d] = btn;
            }
        }

        Debug.Log($"DoorCode: Cancel={cancelButton?.name}, Correct={correctButton?.name}");
        for (int i = 0; i < 10; i++)
            Debug.Log($"DoorCode: Button[{i}]={(numberButtons[i] != null ? numberButtons[i].name : "NULL")}");
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame && IsPlayerInRange())
        {
            ToggleNumpad();
        }

        if (numpadCanvas != null && numpadCanvas.gameObject.activeSelf && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            CloseNumpad();
        }
    }

    private bool IsPlayerInRange()
    {
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogWarning("DoorCode: Geen Player GameObject gevonden in scene.");
            return false;
        }

        float dist = Vector3.Distance(transform.position, player.transform.position);
        return dist <= interactionRange;
    }

    private void ToggleNumpad()
    {
        bool isActive = !numpadCanvas.gameObject.activeSelf;
        numpadCanvas.gameObject.SetActive(isActive);

        Cursor.lockState = isActive ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isActive;

        if (isActive)
        {
            enteredCode = "";
            isLocked = false;
            UpdateDisplay();
        }
    }

    private void CloseNumpad()
    {
        numpadCanvas.gameObject.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        enteredCode = "";
        isLocked = false;
    }

    private void AddDigit(int digit)
    {
        if (isLocked) return;
        if (enteredCode.Length >= correctCode.Length) return;

        enteredCode += digit.ToString();
        Debug.Log($"DoorCode: ingevoerd='{enteredCode}', correct='{correctCode}', match={enteredCode == correctCode}");
        UpdateDisplay();
    }

    private void RemoveLastDigit()
    {
        if (isLocked) return;
        if (enteredCode.Length == 0) return;

        enteredCode = enteredCode.Substring(0, enteredCode.Length - 1);
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (enteredCode.Length < correctCode.Length)
        {
            string display = "";
            for (int i = 0; i < enteredCode.Length; i++)
                display += "*";
            codeDisplayText.text = display;
            return;
        }

        if (enteredCode == correctCode)
        {
            codeDisplayText.text = "<color=green>Approved</color>";
            Debug.Log("DoorCode: CODE CORRECT!");
            isLocked = true;
        }
        else
        {
            codeDisplayText.text = "<color=red>Wrong Code</color>";
            Debug.Log($"DoorCode: FOUTE code. enteredCode='{enteredCode}' (length={enteredCode.Length}) vs correctCode='{correctCode}' (length={correctCode.Length})");
            isLocked = true;
            Invoke(nameof(ResetCode), 1.5f);
        }
    }

    private void ResetCode()
    {
        enteredCode = "";
        isLocked = false;
        codeDisplayText.text = "";
    }
}
