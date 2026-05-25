using UnityEngine;
using System.Collections;
using TMPro;

public class TerminalDisplay : MonoBehaviour
{
    [Header("Terminal Scherm")]
    public TerminalUI terminalUI;
    public TextMeshProUGUI fallbackText;

    [Header("Boot Sequence")]
    public string[] bootLines = {
        ">_ INITIALIZING SECURE CHANNEL...",
        ">_ ESTABLISHING ENCRYPTED LINK...",
        ">_ PROTOCOL: SSHv3 // CIPHER: AES-256",
        ">_ AUTHENTICATING USER... ACCEPTED",
        ">_ SYSTEM READY // 0 ERRORS // SECURE"
    };
    public float bootDelayPerLine = 0.6f;

    [Header("Status Bar")]
    public string statusLinks = "SEC > TERMINAL > HACK";
    public string statusRechts = "0 ERRORS // UTF-8 // MASTER";

    [Header("Cursor")]
    public string cursorKarakters = "|/-\\";
    public float cursorSnelheid = 0.15f;

    private Coroutine bootRoutine;
    private Coroutine cursorRoutine;
    private bool isBootVoltooid = false;
    private bool cursorActief = false;
    private string baseText = "";

    void Awake()
    {
        if (terminalUI == null)
            terminalUI = GetComponent<TerminalUI>();
        if (fallbackText == null)
            fallbackText = GetComponentInChildren<TextMeshProUGUI>();
    }

    public void StartBootSequence(System.Action onComplete = null)
    {
        if (bootRoutine != null) StopCoroutine(bootRoutine);
        bootRoutine = StartCoroutine(BootRoutine(onComplete));
    }

    private IEnumerator BootRoutine(System.Action onComplete)
    {
        isBootVoltooid = false;
        StopCursor();

        string volledigeTekst = "";
        for (int i = 0; i < bootLines.Length; i++)
        {
            if (terminalUI != null)
                terminalUI.ToonTekst(volledigeTekst + bootLines[i] + "\n" + FormatStatusBar(), terminalUI.systeemKleur);
            else if (fallbackText != null)
                fallbackText.text = volledigeTekst + bootLines[i] + "\n" + FormatStatusBar();
            yield return new WaitForSeconds(bootDelayPerLine);
            volledigeTekst += bootLines[i] + "\n";
        }

        if (terminalUI != null)
            terminalUI.ToonTekst(volledigeTekst + "\n" + FormatStatusBar());
        else if (fallbackText != null)
            fallbackText.text = volledigeTekst + "\n" + FormatStatusBar();

        isBootVoltooid = true;
        baseText = volledigeTekst;
        StartCursor();

        onComplete?.Invoke();
    }

    public void ToonMainContent(string content)
    {
        StopCursor();
        baseText = content;
        if (terminalUI != null)
            terminalUI.ToonTekst(content + "\n\n" + FormatStatusBar());
        else if (fallbackText != null)
            fallbackText.text = content + "\n\n" + FormatStatusBar();
        StartCursor();
    }

    public void ToonMetCursor(string text, Color? kleur = null)
    {
        StopCursor();
        baseText = text;
        Color c = kleur ?? (terminalUI != null ? terminalUI.systeemKleur : Color.green);
        if (terminalUI != null)
            terminalUI.ToonTekst(text + "\n\n" + FormatStatusBar(), c);
        else if (fallbackText != null)
            fallbackText.text = text + "\n\n" + FormatStatusBar();
        StartCursor();
    }

    public void TypeText(string text, System.Action onComplete = null)
    {
        StopCursor();
        if (terminalUI != null)
        {
            terminalUI.TypeText(text + "\n\n" + FormatStatusBar(), () => {
                baseText = text;
                StartCursor();
                onComplete?.Invoke();
            });
        }
        else if (fallbackText != null)
        {
            fallbackText.text = text + "\n\n" + FormatStatusBar();
            baseText = text;
            StartCursor();
            onComplete?.Invoke();
        }
    }

    private string FormatStatusBar()
    {
        string lijn = new string('\u2500', 45);
        return $"\n{lijn}\n{statusLinks,-30}{statusRechts,30}";
    }

    private void StartCursor()
    {
        if (cursorRoutine != null) StopCoroutine(cursorRoutine);
        cursorActief = true;
        cursorRoutine = StartCoroutine(CursorRoutine());
    }

    private void StopCursor()
    {
        cursorActief = false;
        if (cursorRoutine != null)
        {
            StopCoroutine(cursorRoutine);
            cursorRoutine = null;
        }
    }

    private IEnumerator CursorRoutine()
    {
        int index = 0;
        while (cursorActief)
        {
            if (terminalUI != null || fallbackText != null)
            {
                string cursor = cursorKarakters[index % cursorKarakters.Length].ToString();
                string txt = baseText + "\n\n" + FormatStatusBar() + "\n" + cursor;
                if (terminalUI != null)
                    terminalUI.ToonTekst(txt, terminalUI.systeemKleur);
                else if (fallbackText != null)
                    fallbackText.text = txt;
            }
            index++;
            yield return new WaitForSeconds(cursorSnelheid);
        }
    }

    public void ToonFout(string text)
    {
        StopCursor();
        baseText = text;
        if (terminalUI != null)
            terminalUI.ToonFout(text + "\n\n" + FormatStatusBar());
        else if (fallbackText != null)
        {
            fallbackText.color = Color.red;
            fallbackText.text = text + "\n\n" + FormatStatusBar();
        }
        StartCoroutine(HernoemCursor());
    }

    private IEnumerator HernoemCursor()
    {
        yield return new WaitForSeconds(0.5f);
        if (cursorActief == false)
            StartCursor();
    }

    public void Clear()
    {
        StopCursor();
        baseText = "";
        if (terminalUI != null)
            terminalUI.ToonTekst(">_ " + FormatStatusBar(), terminalUI.wachtKleur);
        else if (fallbackText != null)
            fallbackText.text = ">_ " + FormatStatusBar();
    }

    public bool IsBootVoltooid() => isBootVoltooid;

    public void SetStatusText(string links, string rechts)
    {
        statusLinks = links;
        statusRechts = rechts;
    }
}