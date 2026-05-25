using UnityEngine;
using TMPro;
using System.Collections;

public class VoicePuzzle : MonoBehaviour
{
    [Header("Puzzel Instellingen")]
    public string[] oplossingCodes;
    public string puzzelTitel = "FIREWALL OVERRIDE";
    public string hintTekst = "Tip: Zoek de authenticatiecode in de server logs.";

    [Header("UI")]
    public TextMeshProUGUI puzzelScherm;
    public TextMeshProUGUI hintScherm;

    [Header("Beloning")]
    public DoorController verbondenDeur;
    public GameObject beloningObject;

    private int huidigeStap = 0;
    private bool isVoltooid = false;
    private bool isInBereik = false;

    public bool IsInBereik => isInBereik;

    void Start()
    {
        if (puzzelScherm != null)
            puzzelScherm.text = ">_ " + puzzelTitel + "\n>_ AWAITING INPUT...\n\n" + hintTekst;

        if (hintScherm != null)
            hintScherm.text = "";

        if (oplossingCodes == null || oplossingCodes.Length == 0)
        {
            oplossingCodes = new string[] { "alpha zeven negen" };
        }
    }

    public void ProbeerOplossing(string gesprokenTekst)
    {
        if (isVoltooid) return;

        gesprokenTekst = gesprokenTekst.ToLower().Trim();
        string juisteCode = oplossingCodes[huidigeStap].ToLower();

        if (gesprokenTekst.Contains(juisteCode) || gesprokenTekst == juisteCode)
        {
            StapVoltooid();
        }
        else
        {
            ToegangGeweigerd(gesprokenTekst);
        }
    }

    private void StapVoltooid()
    {
        huidigeStap++;

        if (huidigeStap >= oplossingCodes.Length)
        {
            PuzzelVoltooid();
        }
        else
        {
            if (puzzelScherm != null)
                puzzelScherm.text = $">_ STEP {huidigeStap}/{oplossingCodes.Length} COMPLETE\n>_ AWAITING INPUT...\n\n{hintTekst}";

            Debug.Log($"Puzzel stap {huidigeStap} voltooid!");
        }
    }

    private void PuzzelVoltooid()
    {
        isVoltooid = true;

        if (puzzelScherm != null)
        {
            puzzelScherm.color = Color.green;
            puzzelScherm.text = $">_ {puzzelTitel}\n>_ ACCESS GRANTED\n>_ ALL {oplossingCodes.Length} STEPS COMPLETE\n>_ SYSTEM UNLOCKED";
        }

        if (verbondenDeur != null)
            verbondenDeur.UnlockDoor();

        if (beloningObject != null)
            beloningObject.SetActive(true);

        Debug.Log("PUZZEL VOLTOOID!");
    }

    private void ToegangGeweigerd(string input)
    {
        if (puzzelScherm != null)
        {
            puzzelScherm.color = Color.red;
            puzzelScherm.text = $">_ ERROR: INVALID CODE\n>_ INPUT: \"{input}\"\n>_ TOEGANG GEWEIGERD";
        }

        StartCoroutine(ResetPuzzelKleur());
    }

    private IEnumerator ResetPuzzelKleur()
    {
        yield return new WaitForSeconds(2f);

        if (puzzelScherm != null)
        {
            puzzelScherm.color = new Color(0, 1, 0);
            puzzelScherm.text = $">_ {puzzelTitel}\n>_ STEP {huidigeStap + 1}/{oplossingCodes.Length}\n>_ AWAITING INPUT...\n\n{hintTekst}";
        }
    }

    public bool IsVoltooid() => isVoltooid;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInBereik = true;
            if (!isVoltooid && puzzelScherm != null)
            {
                puzzelScherm.text = $">_ {puzzelTitel}\n>_ STEP {huidigeStap + 1}/{oplossingCodes.Length}\n>_ AWAITING INPUT...\n\n{hintTekst}";
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isInBereik = false;
            if (!isVoltooid && puzzelScherm != null)
            {
                puzzelScherm.text = $">_ {puzzelTitel}\n>_ DISCONNECTED";
            }
        }
    }
}
