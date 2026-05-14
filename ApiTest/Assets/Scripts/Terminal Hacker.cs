using UnityEngine;
using TMPro;
using System.Collections;
using System.Text.RegularExpressions;

public class TerminalHacker : MonoBehaviour
{
    [Header("UI Instellingen")]
    public TextMeshProUGUI terminalScherm;

    [Header("Multi-Step Hack Instellingen")]
    public string[] hackSteps = {
        "initieer verbinding",
        "authenticatiecode alpha zeven",
        "override beveiliging"
    };
    private int huidigeStap = 0;

    [Header("Fuzzy Matching")]
    [Tooltip("Max toegestane Levenshtein-afstand als fractie van de woordlengte (0.0 = exact, 0.3 = 30% fout)")]
    public float fuzzyTolerantie = 0.35f;

    [Header("Deur Integratie")]
    public DoorController verbondenDeur;

    [Header("Audio Feedback")]
    public AudioClip successSound;
    public AudioClip failSound;
    public AudioClip completeSound;
    private AudioSource audioSource;

    private bool isActief = false;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
        }

        ResetTerminal();
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isActief = true;
            if (terminalScherm != null)
                terminalScherm.enabled = true;
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isActief = false;
        }
    }

    public void ControleerWachtwoord(string gesprokenTekst)
    {
        if (!isActief) return;

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

        // Cijfers naar woorden
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

        // Veelgemaakte fouten door Whisper
        tekst = tekst.Replace("alfa", "alpha");
        tekst = tekst.Replace("inici", "initie");
        tekst = tekst.Replace("negú", "negen");
        tekst = tekst.Replace("negu", "negen");
        tekst = tekst.Replace("negou", "negen");
        tekst = tekst.Replace("verbending", "verbinding");
        tekst = tekst.Replace("verbindingen", "verbinding");
        tekst = tekst.Replace("beveiligeng", "beveiliging");

        // Diakritische tekens verwijderen (é, ë, è → e, etc.)
        tekst = Regex.Replace(tekst.Normalize(System.Text.NormalizationForm.FormD), @"\p{M}", "");

        // Overbodige spaties
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
            if (terminalScherm != null)
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

        if (terminalScherm != null)
        {
            terminalScherm.color = Color.green;
            terminalScherm.text = $">_ HACK COMPLETE\n>_ ALL {hackSteps.Length} STEPS VERIFIED\n>_ SYSTEM OVERRIDE SUCCESSFUL\n>_ SECURITY DISABLED";
        }

        if (verbondenDeur != null)
        {
            verbondenDeur.UnlockDoor();
            verbondenDeur.OpenDoor();
        }

        Debug.Log("HACK VOLTOOID - ALLE SYSTEMEN GEÖVERRIDE!");

        StartCoroutine(ResetNaarLockdown());
    }

    private void ToegangGeweigerd()
    {
        PlaySound(failSound);

        if (terminalScherm != null)
        {
            terminalScherm.color = Color.red;
            terminalScherm.text = ">_ ERROR: INVALID CODE\n>_ TOEGANG GEWEIGERD\n>_ RESETTING SEQUENCE...";
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

    private IEnumerator ResetNaarLockdown()
    {
        yield return new WaitForSeconds(5f);
        huidigeStap = 0;
        ResetTerminal();
    }

    private void ResetTerminal()
    {
        if (terminalScherm != null)
        {
            terminalScherm.text = $">_ SYSTEM LOCKDOWN\n>_ AWAITING INITIATION\n>_ STEP 1: {hackSteps[0]}";
            terminalScherm.color = new Color(0, 1, 0);
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    public void ForceerReset()
    {
        StopAllCoroutines();
        huidigeStap = 0;
        ResetTerminal();
    }

    public bool IsActief => isActief;
    public int GetHuidigeStap() => huidigeStap;
    public int GetTotaalStappen() => hackSteps.Length;
    public bool IsVoltooid() => huidigeStap >= hackSteps.Length;
}
