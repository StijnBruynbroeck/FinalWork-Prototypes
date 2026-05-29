using UnityEngine;
using TMPro;
using System.Text.RegularExpressions;

public class DoorCode : MonoBehaviour
{
    [Header("Door Controller (auto-found if empty)")]
    [SerializeField] private DoorController doorController;

    [Header("UI")]
    [SerializeField] private TMP_Text codeDisplayText;

    [Header("Trigger Settings")]
    [SerializeField] private Vector3 triggerSize = new Vector3(3, 3, 3);

    [Header("Code")]
    [SerializeField] private string correctCode = "12123";

    private Canvas numpadCanvas;
    private string enteredCode = "";
    private bool hasBeenUnlocked = false;
    private bool playerInRange = false;

    private static readonly string[] DigitWords = {
        "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine"
    };

    void Start()
    {
        if (doorController == null)
            doorController = GetComponent<DoorController>();
        if (doorController == null)
            doorController = GetComponentInParent<DoorController>();

        SetupTrigger();

        if (codeDisplayText == null)
            codeDisplayText = GetComponentInChildren<TMP_Text>(true);

        numpadCanvas = GameObject.Find("Numpad")?.GetComponent<Canvas>();
        if (numpadCanvas != null)
        {
            Debug.Log($"DoorCode: Numpad canvas gevonden, verbergen...");
            numpadCanvas.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("DoorCode: Numpad canvas NIET gevonden via GameObject.Find!");
        }
    }

    private void SetupTrigger()
    {
        BoxCollider trigger = GetComponent<BoxCollider>();
        bool wasAdded = trigger == null;
        if (wasAdded)
            trigger = gameObject.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.size = triggerSize;
        trigger.center = new Vector3(0, 1, 0);
        Debug.Log($"DoorCode: Trigger setup - {(wasAdded ? "nieuwe BoxCollider toegevoegd" : "bestaande BoxCollider gevonden")}, size={trigger.size}, isTrigger={trigger.isTrigger}");
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"DoorCode: OnTriggerEnter met: {other.gameObject.name}, tag: {other.tag}");
        if (!other.CompareTag("Player")) return;
        playerInRange = true;
        ToonCanvas();
        if (doorController != null && !doorController.IsLocked())
            doorController.OpenDoor();
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player")) return;
        playerInRange = false;
        VerbergCanvas();
        if (doorController != null && doorController.IsOpen())
            doorController.CloseDoor();
    }

    void Update()
    {
        if (numpadCanvas == null) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) return;

        float dist = Vector3.Distance(transform.position, player.transform.position);
        bool shouldShow = dist < 4f;

        if (shouldShow && !playerInRange)
        {
            playerInRange = true;
            ToonCanvas();
        }
        else if (!shouldShow && playerInRange)
        {
            playerInRange = false;
            VerbergCanvas();
        }
    }

    void ToonCanvas()
    {
        if (numpadCanvas != null)
        {
            numpadCanvas.gameObject.SetActive(true);
            Debug.Log("DoorCode: Numpad canvas getoond!");
        }
    }

    void VerbergCanvas()
    {
        if (numpadCanvas != null)
        {
            numpadCanvas.gameObject.SetActive(false);
            Debug.Log("DoorCode: Numpad canvas verborgen!");
        }
    }

    public bool ProcessVoiceCode(string spokenText)
    {
        if (!playerInRange || hasBeenUnlocked) return false;

        string digits = ExtractDigits(spokenText.ToLower().Trim());
        if (string.IsNullOrEmpty(digits)) return false;

        enteredCode = "";
        foreach (char c in digits)
        {
            if (enteredCode.Length >= correctCode.Length) break;
            enteredCode += c;
        }

        CheckCode();
        return true;
    }

    private string ExtractDigits(string text)
    {
        string digits = "";

        MatchCollection digitMatches = Regex.Matches(text, @"\d");
        if (digitMatches.Count > 0)
        {
            foreach (Match m in digitMatches)
                digits += m.Value;
            return digits;
        }

        string[] words = text.Split(' ');
        foreach (string word in words)
        {
            string cleaned = word.Trim('.', ',', '!', '?');
            for (int i = 0; i < DigitWords.Length; i++)
            {
                if (cleaned == DigitWords[i])
                {
                    digits += i.ToString();
                    break;
                }
            }
        }

        return digits;
    }

    private void CheckCode()
    {
        if (codeDisplayText == null) return;

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
            hasBeenUnlocked = true;

            if (doorController != null)
            {
                doorController.UnlockDoor();
                doorController.OpenDoor();
            }
        }
        else
        {
            codeDisplayText.text = "<color=red>Wrong Code</color>";
            Invoke(nameof(ResetCode), 1.5f);
        }
    }

    private void ResetCode()
    {
        enteredCode = "";
        if (codeDisplayText != null)
            codeDisplayText.text = "";
    }
}
