using UnityEngine;
using TMPro; // Dit is cruciaal om tegen je TextMeshPro canvas te praten!
using System.Collections;

public class TerminalHacker : MonoBehaviour
{
    [Header("UI Instellingen")]
    public TextMeshProUGUI terminalScherm; // Sleep je Text (TMP) hierin

    [Header("Hacker Instellingen")]
    // Dit is het woord of de zin die je moet zeggen om te hacken
    public string geheimWachtwoord = "alpha protocol"; 

    // Deze functie wordt straks aangeroepen als je in je microfoon praat
    public void ControleerWachtwoord(string gesprokenTekst)
    {
        // Zet alles naar kleine letters, dat voorkomt fouten met hoofdletters
        gesprokenTekst = gesprokenTekst.ToLower();
        string wachtwoordKleineLetters = geheimWachtwoord.ToLower();

        // Check of het geheime woord in jouw gesproken zin zit
        if (gesprokenTekst.Contains(wachtwoordKleineLetters))
        {
            ToegangVerleend();
        }
        else
        {
            ToegangGeweigerd();
        }
    }

    private void ToegangVerleend()
    {
        terminalScherm.text = ">_ WACHTWOORD GEACCEPTEERD.\n>_ SYSTEM UNLOCKED.";
        terminalScherm.color = Color.green; // Kleurt de tekst groen
        
        Debug.Log("HACK SUCCESVOL!");
        // Hier kun je later code toevoegen om een deur te openen of een alarm uit te zetten!
    }

    private void ToegangGeweigerd()
    {
        terminalScherm.text = ">_ ERROR: INVALID CODE.\n>_ TOEGANG GEWEIGERD.";
        terminalScherm.color = Color.red; // Kleurt de tekst rood
        
        // Start een timer om het scherm na 3 seconden weer te resetten
        StartCoroutine(ResetScherm());
    }

    private IEnumerator ResetScherm()
    {
        yield return new WaitForSeconds(3f);
        terminalScherm.text = ">_ SYSTEM LOCKDOWN.\n>_ AWAITING OVERRIDE CODE...";
        terminalScherm.color = new Color(0, 1, 0); // Zet de kleur terug naar je standaard neon-groen
    }
}