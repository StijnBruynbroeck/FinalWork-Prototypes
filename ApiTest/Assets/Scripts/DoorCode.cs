using UnityEngine;
using UnityEngine.UI;
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

        Canvas[] all = FindObjectsOfType<Canvas>(true);
        foreach (Canvas c in all)
        {
            if (c.gameObject.name == "Numpad")
            {
                numpadCanvas = c;
                break;
            }
        }
        if (numpadCanvas != null)
        {
            Debug.Log($"DoorCode: Numpad canvas gevonden, verbergen...");
            SetupNumpadBorder();
            numpadCanvas.gameObject.SetActive(false);
        }
        else
        {
            Debug.LogWarning("DoorCode: Numpad canvas NIET gevonden via FindObjectsOfType!");
        }
    }

    private void SetupNumpadBorder()
    {
        Transform oldBg = numpadCanvas.transform.Find("Background");
        if (oldBg == null) { Debug.LogError("DoorCode: Background not found"); return; }

        if (numpadCanvas.transform.Find("CodeDisplayBorder") != null) return;

        RectTransform oldRt = oldBg.GetComponent<RectTransform>();
        Image oldImg = oldBg.GetComponent<Image>();
        if (oldRt == null || oldImg == null) return;

        int uiLayer = 5;

        GameObject border = new GameObject("CodeDisplayBorder");
        border.layer = uiLayer;
        border.transform.SetParent(numpadCanvas.transform, false);
        RectTransform borderRt = border.AddComponent<RectTransform>();
        borderRt.anchorMin = oldRt.anchorMin;
        borderRt.anchorMax = oldRt.anchorMax;
        borderRt.pivot = oldRt.pivot;
        borderRt.anchoredPosition = oldRt.anchoredPosition;
        borderRt.sizeDelta = oldRt.sizeDelta + new Vector2(4, 4);
        Image borderImg = border.AddComponent<Image>();
        borderImg.sprite = null;
        borderImg.type = Image.Type.Simple;
        borderImg.color = new Color(0, 1f, 0.2549f);

        GameObject fill = new GameObject("CodeDisplayFill");
        fill.layer = uiLayer;
        fill.transform.SetParent(border.transform, false);
        RectTransform fillRt = fill.AddComponent<RectTransform>();
        fillRt.anchorMin = Vector2.zero;
        fillRt.anchorMax = Vector2.one;
        fillRt.offsetMin = new Vector2(2f, 2f);
        fillRt.offsetMax = new Vector2(-2f, -2f);
        Image fillImg = fill.AddComponent<Image>();
        fillImg.sprite = oldImg.sprite;
        fillImg.type = Image.Type.Simple;
        fillImg.color = oldImg.color;

        border.transform.SetSiblingIndex(oldBg.GetSiblingIndex());
        Object.Destroy(oldBg.gameObject);
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
        if (enteredCode.Length < correctCode.Length)
        {
            if (codeDisplayText != null)
            {
                string display = "";
                for (int i = 0; i < enteredCode.Length; i++)
                    display += "*";
                codeDisplayText.text = display;
            }
            return;
        }

        if (enteredCode == correctCode)
        {
            if (codeDisplayText != null)
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
            if (codeDisplayText != null)
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
