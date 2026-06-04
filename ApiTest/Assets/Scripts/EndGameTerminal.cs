using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class EndGameTerminal : MonoBehaviour
{
    [Header("Terminal UI")]
    public TextMeshProUGUI terminalScherm;
    public TerminalUI terminalUI;
    public TerminalDisplay terminalDisplay;

    [Header("Fade Settings")]
    public Image fadeOverlay;
    public float fadeDuration = 2f;
    public float displayDelay = 3f;

    [Header("End Messages")]
    public string[] endLines = {
        ">_ END SEQUENCE INITIATED...",
        ">_ ALL SYSTEMS COMPROMISED",
        ">_ MISSION COMPLETE",
        ">_ SHUTTING DOWN..."
    };

    [Header("Voice Command")]
    public string triggerPhrase = "end game";

    [Header("Audio")]
    public AudioClip successSound;
    public AudioClip completeSound;
    private AudioSource audioSource;

    private bool hasEnded = false;
    private bool isActief = false;
    private bool hintShown = false;

    public bool IsActief => isActief;
    public bool HasEnded => hasEnded;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        TextMeshProUGUI gevonden = GetComponentInChildren<TextMeshProUGUI>();
        if (terminalScherm == null)
            terminalScherm = gevonden;

        if (terminalScherm != null)
        {
            if (terminalUI == null)
            {
                terminalUI = terminalScherm.GetComponent<TerminalUI>();
                if (terminalUI == null)
                    terminalUI = terminalScherm.gameObject.AddComponent<TerminalUI>();
            }
            terminalUI.StelTekstVeldIn(terminalScherm);
        }

        if (terminalDisplay == null)
        {
            terminalDisplay = GetComponent<TerminalDisplay>();
            if (terminalDisplay == null && terminalScherm != null)
                terminalDisplay = terminalScherm.GetComponentInChildren<TerminalDisplay>();
        }

        if (terminalDisplay != null && terminalUI != null)
            terminalDisplay.terminalUI = terminalUI;

        if (fadeOverlay == null)
        {
            GameObject overlay = new GameObject("EndFadeOverlay");
            overlay.transform.SetParent(null);
            Canvas canvas = overlay.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 999;
            overlay.AddComponent<CanvasScaler>();
            overlay.AddComponent<GraphicRaycaster>();
            fadeOverlay = overlay.AddComponent<Image>();
            fadeOverlay.color = new Color(0, 0, 0, 0);
            fadeOverlay.raycastTarget = false;
            RectTransform rt = fadeOverlay.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(Screen.width, Screen.height);
        }

        ResetTerminal();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !hasEnded)
        {
            isActief = true;
            if (terminalScherm != null)
                terminalScherm.enabled = true;

            if (!hintShown)
            {
                hintShown = true;
                ShowHint();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            isActief = false;
    }

    private void ShowHint()
    {
        string hint = $">_ TERMINAL ACCESS GRANTED\n>_ TO END THE MISSION, SAY: \"{triggerPhrase.ToUpper()}\"";
        if (terminalDisplay != null)
        {
            terminalDisplay.SetStatusText(
                $"SEC > TERMINAL > {gameObject.name}",
                "0 ERRORS // STANDBY"
            );
            terminalDisplay.StartBootSequence(() => {
                terminalDisplay.ToonMetCursor(hint, terminalUI != null ? terminalUI.systeemKleur : Color.green);
            });
        }
        else if (terminalUI != null)
        {
            terminalUI.ToonTekst(hint, terminalUI.systeemKleur);
        }
    }

    public bool TryEndSequence(string spokenText)
    {
        if (hasEnded) return false;

        string lower = spokenText.ToLower().Trim();
        if (lower.Contains(triggerPhrase.ToLower()))
        {
            hasEnded = true;
            StartCoroutine(ShowEndMessages());
            return true;
        }

        if (terminalUI != null)
            terminalUI.ToonFout($">_ INVALID COMMAND: \"{spokenText}\"\n>_ TO END, SAY: \"{triggerPhrase.ToUpper()}\"");
        return false;
    }

    private IEnumerator ShowEndMessages()
    {
        PlaySound(successSound);

        string volledigeTekst = "";
        foreach (string line in endLines)
        {
            volledigeTekst += line + "\n";
            if (terminalDisplay != null)
                terminalDisplay.ToonMetCursor(volledigeTekst);
            else if (terminalUI != null)
                terminalUI.ToonTekst(volledigeTekst, terminalUI.toegangKleur);
            yield return new WaitForSeconds(1f);
        }

        volledigeTekst += "\n>_ THANK YOU FOR PLAYING";
        if (terminalDisplay != null)
            terminalDisplay.ToonMetCursor(volledigeTekst, Color.cyan);
        else if (terminalUI != null)
            terminalUI.ToonTekst(volledigeTekst, Color.cyan);

        PlaySound(completeSound);

        yield return new WaitForSeconds(displayDelay);

        StartCoroutine(FadeToBlack());
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeOverlay == null) yield break;

        fadeOverlay.raycastTarget = true;
        float elapsed = 0f;
        Color c = fadeOverlay.color;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            fadeOverlay.color = new Color(0, 0, 0, Mathf.Lerp(0, 1, t));
            yield return null;
        }

        fadeOverlay.color = new Color(0, 0, 0, 1);

        yield return new WaitForSeconds(1f);

        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    private void ResetTerminal()
    {
        string text = ">_ SYSTEM STANDBY\n>_ AWAITING FINAL COMMAND";
        if (terminalScherm != null)
        {
            terminalScherm.text = text;
            terminalScherm.color = new Color(0, 1, 0);
            terminalScherm.enabled = true;
        }
        if (terminalDisplay != null)
            terminalDisplay.Clear();
    }
}
