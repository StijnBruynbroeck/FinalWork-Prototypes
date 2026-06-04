using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.InputSystem; 

public class AutomatedTester : MonoBehaviour
{
    [Header("Verplichte Scripts")]
    public GroqLLMService llmService;
    public AnalyticsManager analytics;

    [Header("De 50 Test Zinnen")]
    public List<string> testZinnen = new List<string>()
    {
        // 20x LOGICAL objects (expected: HIGH score)
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

        // 20x ILLOGICAL objects (expected: LOW score)
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

        // 10x EDGE CASES & typos (tests AI robustness)
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

            llmService.EvaluatePlausibility("Server Data Control Room", zin, (result) =>
            {
                analytics.LogData(zin, result.prefab_name, result.score, 0); 
                isKlaar = true;
            });

            yield return new WaitUntil(() => isKlaar);

            // Rate limit: 4s delay between requests to avoid Cloudflare blocks
            yield return new WaitForSeconds(4f); 
        }

        Debug.Log("<color=green>Test complete! All results saved to CSV.</color>");
        isTesting = false;
    }
}