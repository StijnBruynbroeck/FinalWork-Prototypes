using UnityEngine;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class TerminalHacker : MonoBehaviour
{
    public enum TerminalFase
    {
        Inactief,
        Boot,
        HackMinigame,
        HackVoltooid,
        WachtOpTelling,
        TellingVoltooid
    }

    [Header("UI Instellingen")]
    public TextMeshProUGUI terminalScherm;
    public TerminalUI terminalUI;
    public TerminalDisplay terminalDisplay;

    [Header("Hack Minigame")]
    public HackMinigame hackMinigame;

    [Header("Multi-Step Hack Instellingen")]
    public string[] hackSteps = {
        "initiate connection",
        "authentication code alpha seven",
        "override security"
    };
    private int huidigeStap = 0;

    [Header("Fuzzy Matching")]
    public float fuzzyTolerantie = 0.35f;

    [Header("Deur Integratie")]
    public DoorController verbondenDeur;

    [Header("Meubel Telling")]
    public FurnitureCounter furnitureCounter;

    [Header("Audio Feedback")]
    public AudioClip successSound;
    public AudioClip failSound;
    public AudioClip completeSound;
    private AudioSource audioSource;

    private bool isActief = false;
    private TerminalFase huidigeFase = TerminalFase.Inactief;
    private bool bootVoltooid = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        TextMeshProUGUI gevondenInScene = GetComponentInChildren<TextMeshProUGUI>();
        if (terminalScherm == null || (gevondenInScene != null && terminalScherm != gevondenInScene))
            terminalScherm = gevondenInScene;

        if (terminalScherm != null)
        {
            if (terminalUI == null)
            {
                terminalUI = terminalScherm.GetComponent<TerminalUI>();
                if (terminalUI == null)
                    terminalUI = terminalScherm.gameObject.AddComponent<TerminalUI>();
            }
            terminalUI.StelTekstVeldIn(terminalScherm);
            Debug.Log($"[HACK] terminalScherm = '{terminalScherm.name}', tekst = '{terminalScherm.text}', gameObject active = {terminalScherm.gameObject.activeInHierarchy}");
        }
        else
        {
            Debug.LogError($"[TerminalHacker] terminalScherm is NULL op '{gameObject.name}'. " +
                "Sleep een TextMeshProUGUI naar de terminalScherm field in de Inspector!");
        }

        if (terminalDisplay == null)
        {
            terminalDisplay = GetComponent<TerminalDisplay>();
            if (terminalDisplay == null && terminalScherm != null)
                terminalDisplay = terminalScherm.GetComponentInChildren<TerminalDisplay>();
        }

        if (terminalDisplay != null && terminalUI != null)
            terminalDisplay.terminalUI = terminalUI;

        if (hackMinigame == null)
        {
            hackMinigame = GetComponent<HackMinigame>();
            if (hackMinigame == null)
                hackMinigame = gameObject.AddComponent<HackMinigame>();
        }

        if (furnitureCounter == null)
        {
            furnitureCounter = GetComponent<FurnitureCounter>();
            if (furnitureCounter == null)
                furnitureCounter = gameObject.AddComponent<FurnitureCounter>();
        }

        if (furnitureCounter != null && furnitureCounter.verbondenDeur == null)
            furnitureCounter.verbondenDeur = verbondenDeur;

        ResetTerminal();
    }

    void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[HACK] OnTriggerEnter: {other.name} (tag={other.tag}) - huidigeFase={huidigeFase}");
        if (other.CompareTag("Player"))
        {
            isActief = true;
            if (terminalScherm != null)
                terminalScherm.enabled = true;

            if (huidigeFase == TerminalFase.Inactief)
            {
                StartBootSequence();
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isActief = false;
        }
    }

    private void StartBootSequence()
    {
        Debug.Log($"[HACK] StartBootSequence! display={terminalDisplay?.name ?? "null"}, ui={terminalUI?.name ?? "null"}, scherm={terminalScherm?.name ?? "null"}");
        huidigeFase = TerminalFase.Boot;
        if (terminalDisplay != null)
        {
            terminalDisplay.SetStatusText(
                $"SEC > TERMINAL > NODE_{gameObject.name}",
                "0 ERRORS // UTF-8 // MASTER"
            );
            terminalDisplay.StartBootSequence(() => {
                bootVoltooid = true;
                StartHackMinigame();
            });
        }
        else if (terminalUI != null)
        {
            StartCoroutine(BootSequenceFallback());
        }
    }

    private IEnumerator BootSequenceFallback()
    {
        string[] bootLines = {
            ">_ INITIALIZING SECURE CHANNEL...",
            ">_ ESTABLISHING ENCRYPTED LINK...",
            ">_ PROTOCOL: SSHv3 // CIPHER: AES-256",
            ">_ AUTHENTICATING USER... ACCEPTED",
            ">_ SYSTEM READY // 0 ERRORS // SECURE"
        };
        string volledigeTekst = "";
        foreach (var line in bootLines)
        {
            volledigeTekst += line + "\n";
            terminalUI.ToonTekst(volledigeTekst + "\n" + new string('\u2500', 45), terminalUI.systeemKleur);
            yield return new WaitForSeconds(0.6f);
        }
        bootVoltooid = true;
        StartHackMinigame();
    }

    private void StartHackMinigame()
    {
        huidigeFase = TerminalFase.HackMinigame;
        if (hackMinigame != null)
        {
            hackMinigame.StartGame();
            ToonHackGrid();
        }
        else
        {
            ToonLegacyHackPrompt();
        }
    }

    private void ToonHackGrid()
    {
        if (hackMinigame == null) return;
        string grid = hackMinigame.FormatWoordGrid();
        string text = ">_ SECURITY OVERRIDE PROTOCOL ACTIVE\n>_ IDENTIFY PASSWORD\n\n" + grid;

        if (terminalDisplay != null)
            terminalDisplay.ToonMetCursor(text);
        else if (terminalUI != null)
            terminalUI.ToonTekst(text);
    }

    private void ToonLegacyHackPrompt()
    {
        if (terminalUI != null)
        {
            string text = $">_ SYSTEM LOCKDOWN\n>_ AWAITING INITIATION\n>_ STEP 1: {hackSteps[0]}";
            terminalUI.ToonTekst(text, terminalUI.systeemKleur);
        }
        else if (terminalScherm != null)
        {
            terminalScherm.text = $">_ SYSTEM LOCKDOWN\n>_ AWAITING INITIATION\n>_ STEP 1: {hackSteps[0]}";
            terminalScherm.color = new Color(0, 1, 0);
        }
    }

    public bool ControleerWachtwoord(string gesprokenTekst)
    {
        if (!isActief) return false;

        switch (huidigeFase)
        {
            case TerminalFase.HackMinigame:
                return VerwerkHackMinigame(gesprokenTekst);

            case TerminalFase.WachtOpTelling:
                VerwerkTelling(gesprokenTekst);
                return true;

            case TerminalFase.Boot:
            case TerminalFase.Inactief:
                return false;

            case TerminalFase.HackVoltooid:
            case TerminalFase.TellingVoltooid:
                if (terminalDisplay != null)
                    terminalDisplay.ToonMetCursor(">_ SYSTEM ALREADY COMPROMISED\n>_ DOOR UNLOCKED");
                return true;
        }
        return false;
    }

    private bool VerwerkHackMinigame(string text)
    {
        if (hackMinigame == null)
        {
            VerwerkLegacyStap(text);
            return true;
        }

        if (hackMinigame.IsVoltooid()) return true;

        string lower = text.ToLower().Trim();

        string dudResult = hackMinigame.CheckDudRemover(lower);
        if (dudResult != null)
        {
            if (dudResult == "DUD_REMOVED")
            {
                PlaySound(successSound);
                if (terminalDisplay != null)
                    terminalDisplay.ToonMetCursor(">_ DUD PATTERN DETECTED // REMOVING FAKE ENTRIES...\n\n" + hackMinigame.FormatWoordGrid());
                else
                    ToonHackGrid();
            }
            else if (dudResult == "ATTEMPT_RESTORED")
            {
                PlaySound(successSound);
                if (terminalDisplay != null)
                    terminalDisplay.ToonMetCursor(">_ RESET TOKEN FOUND // ATTEMPT RESTORED\n\n" + hackMinigame.FormatWoordGrid());
                else
                    ToonHackGrid();
            }
            return true;
        }

        int result = hackMinigame.SelecteerWoord(text);

        if (result == -2)
        {
            if (terminalUI != null)
                terminalUI.ToonFout($">_ INVALID WORD: \"{text}\"\n>_ WORD NOT FOUND IN DATABASE");
            PlaySound(failSound);
            return false;
        }

        if (result == 100)
        {
            HackSuccess();
            return true;
        }

        if (result == -3)
        {
            HackFailed();
            return true;
        }

        if (result >= 0 && hackMinigame != null)
        {
            string correctWoord = hackMinigame.CorrectWoord();
            string feedback = $">_ ACCESS DENIED\n>_ {text.ToUpper()} = {result}/{correctWoord.Length} MATCH\n>_ TRIES LEFT: {hackMinigame.PogingenOver()}/{hackMinigame.MaxPogingen()}\n\n{hackMinigame.FormatWoordGrid()}";

            if (terminalDisplay != null)
                terminalDisplay.ToonMetCursor(feedback, terminalUI != null ? terminalUI.foutKleur : Color.red);
            else if (terminalUI != null)
                terminalUI.ToonFout(feedback);

            PlaySound(failSound);
        }
        return true;
    }

    private void VerwerkTelling(string text)
    {
        if (furnitureCounter == null || furnitureCounter.IsVoltooid())
        {
            if (furnitureCounter != null && furnitureCounter.IsVoltooid())
            {
                if (terminalDisplay != null)
                    terminalDisplay.ToonMetCursor(">_ DOOR ALREADY UNLOCKED", terminalUI != null ? terminalUI.toegangKleur : Color.green);
            }
            return;
        }

        int result = furnitureCounter.VerwerkInput(text);

        if (result == 100)
        {
            huidigeFase = TerminalFase.TellingVoltooid;
            string successText = ">_ ACCESS GRANTED // DECRYPTION SUCCESSFUL\n>_ ALL COUNTS VERIFIED\n>_ DOOR UNLOCKED";

            if (terminalDisplay != null)
                terminalDisplay.ToonMetCursor(successText, terminalUI != null ? terminalUI.toegangKleur : Color.green);
            else if (terminalUI != null)
                terminalUI.ToonToegang(successText);

            PlaySound(completeSound);
            StartCoroutine(ResetNaarLockdown(10f));
        }
        else
        {
            string feedback = furnitureCounter.GetFeedback(result);

            if (terminalDisplay != null)
                terminalDisplay.ToonMetCursor(feedback, terminalUI != null ? terminalUI.foutKleur : Color.red);
            else if (terminalUI != null)
                terminalUI.ToonFout(feedback);

            PlaySound(failSound);
        }
    }

    private void HackSuccess()
    {
        huidigeFase = TerminalFase.HackVoltooid;
        PlaySound(completeSound);

        string successText = ">_ ACCESS GRANTED\n>_ SYSTEM OVERRIDE SUCCESSFUL\n>_ DECRYPTING DATA...";

        if (terminalDisplay != null)
            terminalDisplay.ToonMetCursor(successText, terminalUI != null ? terminalUI.toegangKleur : Color.green);
        else if (terminalUI != null)
            terminalUI.ToonToegang(successText);

        StartCoroutine(ToonClueNaHack());
    }

    private IEnumerator ToonClueNaHack()
    {
        yield return new WaitForSeconds(2.5f);

        if (furnitureCounter != null)
        {
            string clueText = furnitureCounter.GetClueText();
            if (terminalDisplay != null)
                terminalDisplay.ToonMetCursor(clueText);
            else if (terminalUI != null)
                terminalUI.ToonTekst(clueText);

            huidigeFase = TerminalFase.WachtOpTelling;
        }
        else
        {
            if (verbondenDeur != null)
            {
                verbondenDeur.UnlockDoor();
                verbondenDeur.OpenDoor();
            }

            string done = ">_ HACK COMPLETE\n>_ ALL SYSTEMS OVERRIDDEN\n>_ DOOR UNLOCKED";
            if (terminalDisplay != null)
                terminalDisplay.ToonMetCursor(done, terminalUI != null ? terminalUI.toegangKleur : Color.green);
            else if (terminalUI != null)
                terminalUI.ToonToegang(done);

            StartCoroutine(ResetNaarLockdown(5f));
        }
    }

    private void HackFailed()
    {
        PlaySound(failSound);
        string failText = ">_ SECURITY LOCKDOWN ACTIVATED\n>_ TOO MANY FAILED ATTEMPTS\n>_ SYSTEM RESETTING...";

        if (terminalDisplay != null)
            terminalDisplay.ToonFout(failText);
        else if (terminalUI != null)
            terminalUI.ToonFout(failText);

        StartCoroutine(ResetNaarLockdown(4f));
    }

    private void VerwerkLegacyStap(string gesprokenTekst)
    {
        gesprokenTekst = Normaliseer(gesprokenTekst);
        string verwachteCode = Normaliseer(hackSteps[huidigeStap]);

        if (ZelfdeAls(gesprokenTekst, verwachteCode))
        {
            StapVoltooid();
        }
        else
        {
            ToegangGeweigerd();
        }
    }

    private bool ZelfdeAls(string input, string verwacht)
    {
        if (input.Contains(verwacht) || input == verwacht)
            return true;

        int afstand = Levenshtein(input, verwacht);
        float maxAfstand = Mathf.Max(verwacht.Length * fuzzyTolerantie, 3);
        return afstand <= maxAfstand;
    }

    private string Normaliseer(string tekst)
    {
        tekst = tekst.ToLower().Trim();
        tekst = tekst.Replace("0", "nul");
        tekst = tekst.Replace("1", "een");
        tekst = tekst.Replace("2", "twee");
        tekst = tekst.Replace("3", "drie");
        tekst = tekst.Replace("4", "vier");
        tekst = tekst.Replace("5", "vijf");
        tekst = tekst.Replace("6", "zes");
        tekst = tekst.Replace("7", "zeven");
        tekst = tekst.Replace("8", "acht");
        tekst = tekst.Replace("9", "negen");
        tekst = tekst.Replace("alfa", "alpha");
        tekst = tekst.Replace("inici", "initie");
        tekst = tekst.Replace("negú", "negen");
        tekst = tekst.Replace("negu", "negen");
        tekst = tekst.Replace("negou", "negen");
        tekst = tekst.Replace("verbending", "verbinding");
        tekst = tekst.Replace("verbindingen", "verbinding");
        tekst = tekst.Replace("beveiligeng", "beveiliging");
        tekst = Regex.Replace(tekst.Normalize(System.Text.NormalizationForm.FormD), @"\p{M}", "");
        tekst = Regex.Replace(tekst, @"\s+", " ");
        return tekst.Trim();
    }

    private int Levenshtein(string a, string b)
    {
        int lenA = a.Length;
        int lenB = b.Length;
        int[,] matrix = new int[lenA + 1, lenB + 1];
        for (int i = 0; i <= lenA; i++) matrix[i, 0] = i;
        for (int j = 0; j <= lenB; j++) matrix[0, j] = j;
        for (int i = 1; i <= lenA; i++)
        {
            for (int j = 1; j <= lenB; j++)
            {
                int cost = (a[i - 1] == b[j - 1]) ? 0 : 1;
                matrix[i, j] = Mathf.Min(
                    Mathf.Min(matrix[i - 1, j] + 1, matrix[i, j - 1] + 1),
                    matrix[i - 1, j - 1] + cost
                );
            }
        }
        return matrix[lenA, lenB];
    }

    private void StapVoltooid()
    {
        PlaySound(successSound);
        huidigeStap++;

        if (huidigeStap >= hackSteps.Length)
        {
            HackVoltooid();
        }
        else
        {
            if (terminalUI != null)
                terminalUI.ToonTekst($">_ STEP {huidigeStap}/{hackSteps.Length} COMPLETE\n>_ PROCEEDING...\n\n>_ AWAITING: {hackSteps[huidigeStap]}", terminalUI.systeemKleur);
            else if (terminalScherm != null)
            {
                terminalScherm.color = Color.green;
                terminalScherm.text = $">_ STEP {huidigeStap}/{hackSteps.Length} COMPLETE\n>_ PROCEEDING...\n\n>_ AWAITING: {hackSteps[huidigeStap]}";
            }
            Debug.Log($"Hack stap {huidigeStap}/{hackSteps.Length} voltooid!");
        }
    }

    private void HackVoltooid()
    {
        PlaySound(completeSound);
        if (terminalUI != null)
            terminalUI.ToonToegang(">_ HACK COMPLETE\n>_ ALL STEPS VERIFIED\n>_ SYSTEM OVERRIDE SUCCESSFUL\n>_ SECURITY DISABLED");
        else if (terminalScherm != null)
        {
            terminalScherm.color = Color.green;
            terminalScherm.text = ">_ HACK COMPLETE\n>_ ALL STEPS VERIFIED\n>_ SYSTEM OVERRIDE SUCCESSFUL\n>_ SECURITY DISABLED";
        }

        if (verbondenDeur != null)
        {
            verbondenDeur.UnlockDoor();
            verbondenDeur.OpenDoor();
        }

        Debug.Log("HACK VOLTOOID - ALLE SYSTEMEN GEÖVERRIDE!");
        StartCoroutine(ResetNaarLockdown(5f));
    }

    private void ToegangGeweigerd()
    {
        PlaySound(failSound);
        if (terminalUI != null)
                terminalUI.ToonFout(">_ ERROR: INVALID CODE\n>_ ACCESS DENIED\n>_ RESETTING SEQUENCE...");
        else if (terminalScherm != null)
        {
            terminalScherm.color = Color.red;
            terminalScherm.text = ">_ ERROR: INVALID CODE\n>_ ACCESS DENIED\n>_ RESETTING SEQUENCE...";
        }

        Debug.LogWarning("Hack mislukt! Sequence gereset.");
        StartCoroutine(ResetScherm());
    }

    private IEnumerator ResetScherm()
    {
        yield return new WaitForSeconds(2.5f);
        huidigeStap = 0;
        ResetTerminal();
    }

    private IEnumerator ResetNaarLockdown(float delay)
    {
        yield return new WaitForSeconds(delay);
        huidigeFase = TerminalFase.Inactief;
        bootVoltooid = false;
        huidigeStap = 0;
        if (hackMinigame != null) hackMinigame.ResetGame();
        if (furnitureCounter != null) furnitureCounter.ResetPuzzle();
        ResetTerminal();
    }

    private void ResetTerminal()
    {
        huidigeFase = TerminalFase.Inactief;
        bootVoltooid = false;

        string lockdownText = ">_ SYSTEM LOCKDOWN\n>_ AWAITING INITIATION\n>_ SYSTEM READY";

        if (terminalScherm != null)
        {
            terminalScherm.text = lockdownText;
            terminalScherm.color = new Color(0, 1, 0);
            terminalScherm.enabled = true;
            Debug.Log($"[HACK] ResetTerminal: text gezet op '{lockdownText}', enabled={terminalScherm.enabled}, gameObject active={terminalScherm.gameObject.activeInHierarchy}");
        }

        if (terminalDisplay != null)
            terminalDisplay.Clear();
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }

    public void ForceerReset()
    {
        StopAllCoroutines();
        huidigeStap = 0;
        huidigeFase = TerminalFase.Inactief;
        bootVoltooid = false;
        if (hackMinigame != null) hackMinigame.ResetGame();
        if (furnitureCounter != null) furnitureCounter.ResetPuzzle();
        ResetTerminal();
    }

    public bool IsActief => isActief;
    public TerminalFase GetHuidigeFase() => huidigeFase;
    public bool IsVoltooid() => huidigeFase == TerminalFase.HackVoltooid || huidigeFase == TerminalFase.TellingVoltooid;
}
