using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem; // Belangrijk voor jullie project!

public class AutomatedTester : MonoBehaviour
{
    [Header("Verplichte Scripts")]
    public GroqLLMService llmService;
    public AnalyticsManager analytics;

    [Header("De 50 Test Zinnen")]
    public List<string> testZinnen = new List<string>()
    {
        // --- 20x LOGISCHE OBJECTEN (Verwachte score: HOOG) ---
        "Verander me in een serverkast.",
        "Ik wil een bureaustoel zijn.",
        "Maak van mij een archiefkast.",
        "Verander me in een grote brandblusser.",
        "Ik ben een patchkast.",
        "Maak me een koffiemachine.",
        "Verander me in een metalen prullenbak.",
        "Ik wil een verrijdbaar whiteboard zijn.",
        "Maak me een stapel serverblades.",
        "Verander me in een zware UPS batterij.",
        "Ik ben een waterkoeler.",
        "Maak van mij een gereedschapskist.",
        "Ik wil een dikke rol netwerkkabels zijn.",
        "Verander me in een beveiligingscamera.",
        "Maak me een bureau met een monitor erop.",
        "Ik wil een grote industriële ventilator zijn.",
        "Verander me in een stapel kartonnen dozen.",
        "Ik ben een netwerkrouter.",
        "Verander me in een laptop op een karretje.",
        "Maak van mij een metalen stellingkast.",

        // --- 20x ONLOGISCHE OBJECTEN (Verwachte score: LAAG) ---
        "Ik ben een roze eenhoorn.",
        "Verander me in een baksteen.",
        "Maak me een middeleeuws zwaard.",
        "Ik wil een palmboom zijn.",
        "Verander me in een opblaasbare krokodil.",
        "Maak van mij een zandkasteel.",
        "Ik ben een groot piratenschip.",
        "Verander me in een kampvuur.",
        "Maak me een stoomtrein.",
        "Ik wil een voetbal zijn.",
        "Verander me in een boze grizzlybeer.",
        "Maak van mij een hete luchtballon.",
        "Ik ben een draaiende wasmachine.",
        "Verander me in een straatlantaarn.",
        "Maak me een akoestische gitaar.",
        "Ik wil een achtbaan zijn.",
        "Verander me in een opblaasbaar zwembad.",
        "Maak van mij een trampoline.",
        "Ik ben een rode tractor.",
        "Verander me in een iglo van ijs.",

        // --- 10x EDGE CASES & TYPFOUTEN (Test de AI robuustheid) ---
        "Uh, wacht, ik weet het niet, doe maar iets.",
        "stúl",
        "Mag ik een ehhh, koffiemachine zijn?",
        "Verander me in zo'n kantoordinges om op te zitten.",
        "Maak me een serfkurkast.", 
        "Verander me in... wacht, komt er iemand aan?",
        "Ik wil een [onverstaanbaar] zijn.",
        "Pff, maak me maar gewoon onzichtbaar ofzo.",
        "Kan ik veranderen in de vloer zelf?",
        "Geef me de vorm van een buraeustuel."
    };

    private bool isTesting = false;

    void Update()
    {
        // Start de test als we op F12 drukken en we nog niet aan het testen zijn
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame && !isTesting)
        {
            StartCoroutine(RunTestBatch());
        }
    }

    private IEnumerator RunTestBatch()
    {
        isTesting = true;
        Debug.Log($"<color=cyan>Start automatische A/B test voor {testZinnen.Count} zinnen...</color>");

        foreach (string zin in testZinnen)
        {
            bool isKlaar = false;

            // We sturen de zin direct naar het LLM 'Brein', we slaan Whisper (audio) dus over!
            llmService.EvaluatePlausibility("Server Data Control Room", zin, (result) =>
            {
                // We loggen de data direct in je CSV! (Tijd staat op 0 omdat we audio overslaan)
                analytics.LogData(zin, result.prefab_name, result.score, 0); 
                isKlaar = true;
            });

            // Wacht net zolang tot de API een antwoord heeft gegeven
            yield return new WaitUntil(() => isKlaar);

            // CRITIQUE BEVEILIGING: Wacht 2 seconden voordat je de volgende zin stuurt.
            // Als je dit niet doet, blokkeert de Cloudflare-beveiliging van Groq je direct voor spam!
            yield return new WaitForSeconds(4f); 
        }

        Debug.Log("<color=green>Test compleet! Al je zinnen staan nu netjes in het CSV bestand.</color>");
        isTesting = false;
    }
}