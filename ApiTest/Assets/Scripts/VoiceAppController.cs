using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Diagnostics;
using System.Text.RegularExpressions;
using Debug = UnityEngine.Debug;

public class VoiceAppController : MonoBehaviour
{
    public AnalyticsManager analytics;
    private Stopwatch timer = new Stopwatch();
    private string lastSpokenText = "";
    public GroqLLMService llmService;
    public MicrophoneRecorder recorder;
    public GroqAudioService apiService;
    public ObjectSpawner spawner;
    public ZoneDetectionManager zoneDetection;

    [Header("Deur")]
    public DoorController specificDoor;

    [Header("UI")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI statusText;

    [Header("Routing")]
    public float interactieAfstand = 3f;

    [Header("Audio Detection")]
    public float voiceIntensity = 1.5f;

    private bool tKeyPressed = false;
    private float recordingStartTime;

    void Start()
    {
        if (zoneDetection == null)
            zoneDetection = FindObjectOfType<ZoneDetectionManager>();

        if (specificDoor == null)
        {
            Animator[] alleAnimators = FindObjectsOfType<Animator>(true);
            Debug.Log($"Zoeken naar Animator met 'sphere' controller... ({alleAnimators.Length} animators gevonden)");
            foreach (Animator a in alleAnimators)
            {
                Debug.Log($"  Animator op '{a.gameObject.name}', controller={a.runtimeAnimatorController?.name}");
                if (a.runtimeAnimatorController != null && a.runtimeAnimatorController.name.ToLower().Contains("sphere"))
                {
                    specificDoor = a.GetComponent<DoorController>();
                    if (specificDoor == null)
                        specificDoor = a.gameObject.AddComponent<DoorController>();
                    Debug.Log($"Deur gevonden via Animator: '{a.gameObject.name}' (controller: {a.runtimeAnimatorController.name})");
                    break;
                }
            }
        }

        if (specificDoor == null)
        {
            string[] zoekNamen = { "Area1_MeubelShowroom_Door", "deurende", "Deur", "door" };
            foreach (string naam in zoekNamen)
            {
                GameObject obj = GameObject.Find(naam);
                Debug.Log($"Zoek naar '{naam}': {(obj != null ? $"gevonden op {obj.transform.parent?.name ?? "root"}" : "niet gevonden")}");
                if (obj != null)
                {
                    specificDoor = obj.GetComponent<DoorController>();
                    if (specificDoor == null)
                        specificDoor = obj.AddComponent<DoorController>();
                    Debug.Log($"Deur gekozen: '{naam}' (parent: {obj.transform.parent?.name ?? "geen"})");
                    break;
                }
            }
        }

        if (specificDoor == null)
        {
            specificDoor = FindObjectOfType<DoorController>();
            if (specificDoor != null)
                Debug.Log($"Deur gevonden via FindObjectOfType: {specificDoor.name} (parent: {specificDoor.transform.parent?.name})");
        }

        if (specificDoor == null)
            Debug.LogWarning("GEEN deur gevonden! Zet 'specificDoor' handmatig in de Inspector.");
        else
        {
            bool hasAnim = specificDoor.GetComponent<Animator>() != null || specificDoor.GetComponentInParent<Animator>() != null || specificDoor.GetComponentInChildren<Animator>() != null;
            bool hasLegacy = specificDoor.GetComponent<Animation>() != null || specificDoor.GetComponentInParent<Animation>() != null || specificDoor.GetComponentInChildren<Animation>() != null;
            Debug.Log($"specificDoor = '{specificDoor.gameObject.name}', Animator={hasAnim}, Animation={hasLegacy}");
        }
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        // R-toets voor unmorph
        if (Keyboard.current.rKey.wasPressedThisFrame && spawner != null && spawner.IsMorphed)
        {
            spawner.Unmorph();
        }

        bool tPressed = Keyboard.current.tKey.isPressed;

        if (tPressed && !tKeyPressed && !recorder.IsRecording)
        {
            StartAppRecording();
        }
        else if (!tPressed && tKeyPressed && recorder.IsRecording)
        {
            StopAppRecording();
        }

        tKeyPressed = tPressed;
    }

    void StartAppRecording()
    {
        recordingStartTime = Time.time;
        UpdateStatus("<color=red>● Recording...</color>");
        recorder.StartRecording();
        
        // Trigger audio event - vijand hoort je beginnen met praten
        AudioEventSystem.EmitSound(transform.position, voiceIntensity);
    }

    void StopAppRecording()
    {
        float duration = Time.time - recordingStartTime;

        UpdateStatus("Processing Audio...");

        // Trigger audio event - vijand hoort je stoppen met praten
        AudioEventSystem.EmitSound(transform.position, voiceIntensity * 0.5f);

        recorder.StopRecording((byte[] audioData) =>
        {
            timer.Reset();
            timer.Start();
            apiService.TranscribeAudio(audioData, OnTranscriptionSuccess, UpdateStatus);
        });
    }

    void OnTranscriptionSuccess(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        string cleaned = text.Trim();
        lastSpokenText = cleaned;
        if (resultText != null) resultText.text = cleaned;

        // --- 0. PRE-FILTER: reject known noise / garbage transcriptions ---
        string lower = cleaned.ToLower();
        string[] noiseMarkers = { "[blank_audio]", "[inaudible]", "[silence]", "[noise]", "[music]", "[cough]" };
        foreach (var marker in noiseMarkers)
        {
            if (lower.Contains(marker))
            {
                UpdateStatus("Niet verstaan — probeer opnieuw.");
                return;
            }
        }
        if (lower.Length < 4)
        {
            UpdateStatus("Te kort — probeer opnieuw.");
            return;
        }

        // --- 0a. TRANSCRIPTION CORRECTION (fix known Whisper mishearings) ---
        bool corrected = false;
        if (lower.Contains("bye ") && lower.Contains("bass"))
        {
            cleaned = cleaned.Replace("Bye", "").Replace("bass", "bypass").Replace("bye", "bypass");
            corrected = true;
        }
        if (lower.Contains("on morph"))
        {
            cleaned = "unmorph";
            corrected = true;
        }
        if (corrected)
        {
            cleaned = cleaned.Trim().TrimEnd('.');
            lower = cleaned.ToLower();
        }

        // --- 0b. PRE-LLM UNMORPH CHECK (always runs, regardless of morph state) ---
        string[] unmorphPatterns = { "unmorph", "on morph", "unmorpf", "unmorf", "a morph",
                                     "morph back", "change back", "turn back", "revert", "undo", "stop morph" };
        foreach (var pattern in unmorphPatterns)
        {
            if (lower.Contains(pattern))
            {
                if (spawner != null && spawner.IsMorphed)
                {
                    spawner.Unmorph();
                    UpdateStatus("Unmorphed! Terug naar menselijk formulier.");
                }
                else
                {
                    UpdateStatus("Je bent al in menselijke vorm.");
                }
                return;
            }
        }

        // --- 1. DIRECT DEUR COMMANDO ---
        if (RouteerNaarDeur(cleaned)) return;

        // --- 1a. DOOR CODE CHECK (spraakgestuurde code invoer) ---
        if (RouteerNaarCode(cleaned)) return;

        // --- 2. TERMINAL CHECK (actieve terminal waar speler bij staat) ---
        if (RouteerNaarTerminal(cleaned)) return;

        // --- 3. NIEUWE PUZZEL CHECK ---
        VoiceRiddlePuzzle nieuwePuzzel = FindObjectOfType<VoiceRiddlePuzzle>();
        if (nieuwePuzzel != null && nieuwePuzzel.IsInRange && !nieuwePuzzel.IsCompleted)
        {
            nieuwePuzzel.ProcessVoiceInput(cleaned);
            return;
        }

        // --- 4. KAMER DEFINIEREN (dynamisch via zone detection) ---
        string currentRoom = "Server Data Control Room";
        if (zoneDetection != null && zoneDetection.CurrentZone != null)
        {
            var z = zoneDetection.CurrentZone;
            currentRoom = $"Zone: {z.zoneType} (ID: {z.zoneId})";
        }
        Debug.Log($"[VoiceApp] HUIDIGE ZONE CONTEXT: {currentRoom}");

        // --- 5. LLM BEOORDELING ---
        llmService.EvaluatePlausibility(currentRoom, cleaned, (llmResult) =>
        {
            timer.Stop();
            long latencyMs = timer.ElapsedMilliseconds;

            // --- 5a. DEUR ACTIE VAN LLM ---
            if (!string.IsNullOrEmpty(llmResult.door_action) && llmResult.door_action != "none")
            {
                if (specificDoor != null)
                {
                    if (llmResult.door_action == "open")
                    {
                        specificDoor.OpenDoor();
                        UpdateStatus("Deur geopend via AI!");
                    }
                    else if (llmResult.door_action == "close")
                    {
                        specificDoor.CloseDoor();
                        UpdateStatus("Deur gesloten via AI!");
                    }
                }
                return;
            }

            // --- 5b. UNMORPH CHECK ---
            if (llmResult.unmorph)
            {
                if (spawner != null && spawner.IsMorphed)
                {
                    spawner.Unmorph();
                    UpdateStatus("Unmorphed! Terug naar menselijk formulier.");
                }
                else
                {
                    UpdateStatus("Je bent al in menselijke vorm.");
                }
                return;
            }

            // --- 5c. KEYWORD FALLBACK (only when LLM returns nothing — true fallback) ---
            if (string.IsNullOrEmpty(llmResult.prefab_name) || llmResult.prefab_name.ToLower() == "none")
            {
                string fallback = TryKeywordFallback(cleaned);
                if (!string.IsNullOrEmpty(fallback))
                {
                    Debug.Log($"[VoiceApp] LLM returned empty, keyword fallback → {fallback}");
                    llmResult.prefab_name = fallback;
                }
            }

            // --- 5d. VALIDATE PREFAB EXISTS before spawning ---
            if (!string.IsNullOrEmpty(llmResult.prefab_name) && llmResult.prefab_name.ToLower() != "none")
            {
                GameObject prefab = Resources.Load<GameObject>("Props/" + llmResult.prefab_name);
                if (prefab == null)
                {
                    UpdateStatus($"Object '{llmResult.prefab_name}' bestaat niet — geweigerd.");
                    if (analytics != null)
                        analytics.LogData(lastSpokenText, "none", 0, latencyMs);
                    return;
                }
            }

            // --- 5e. SPAWN OBJECT ---
            spawner.ProcessTextAndSpawn(llmResult, UpdateStatus);

            // --- 6. VIJAND MULTIPLIER ---
            EnemyVision vijandZicht = FindObjectOfType<EnemyVision>();
            if (vijandZicht != null)
            {
                vijandZicht.SetLLMScoreMultiplier(llmResult.score);
            }

            // --- 7. ANALYTICS ---
            if (analytics != null)
            {
                analytics.LogData(lastSpokenText, llmResult.prefab_name, llmResult.score, latencyMs);
            }
        });
    }

    bool RouteerNaarTerminal(string text)
    {
        TerminalHacker[] terminals = FindObjectsOfType<TerminalHacker>();
        foreach (var t in terminals)
        {
            if (t.IsActief && t.ControleerWachtwoord(text))
            {
                return true;
            }
        }

        return false;
    }

    bool RouteerNaarPuzzel(string text)
    {
        VoicePuzzle[] puzzels = FindObjectsOfType<VoicePuzzle>();
        foreach (var p in puzzels)
        {
            if (p.IsInBereik && !p.IsVoltooid())
            {
                p.ProbeerOplossing(text);
                return true;
            }
        }

        VoicePuzzle fallback = FindClosestPuzzle();
        if (fallback != null && !fallback.IsVoltooid())
        {
            fallback.ProbeerOplossing(text);
            return true;
        }
        return false;
    }

    bool RouteerNaarDeur(string text)
    {
        string lower = text.ToLower().Trim();

        bool isOpenCmd = lower.Contains("open") && (lower.Contains("deur") || lower.Contains("door") || lower == "open");
        bool isCloseCmd = (lower.Contains("sluit") || lower.Contains("dicht")) && (lower.Contains("deur") || lower.Contains("door"));

        if (isOpenCmd || isCloseCmd)
        {
            if (specificDoor != null)
            {
                if (isOpenCmd)
                {
                    specificDoor.OpenDoor();
                    UpdateStatus($"Deur geopend: {text}");
                }
                else
                {
                    specificDoor.CloseDoor();
                    UpdateStatus($"Deur gesloten: {text}");
                }

                if (analytics != null)
                    analytics.LogData(text, "door_command", isOpenCmd ? 100 : 0, 0);

                return true;
            }
            else
            {
                Debug.LogWarning($"Deurcommando herkend maar 'specificDoor' is null!");
                UpdateStatus("Fout: geen deur gevonden in scene");
            }
        }
        return false;
    }

    bool RouteerNaarCode(string text)
    {
        if (specificDoor == null) return false;

        DoorCode doorCode = specificDoor.GetComponent<DoorCode>();
        if (doorCode == null) return false;

        string lower = text.ToLower().Trim();
        bool hasDigits = Regex.IsMatch(lower, @"\d");
        bool hasNumberWords = false;
        string[] numberWords = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine" };
        foreach (var w in numberWords)
        {
            if (lower.Contains(w)) { hasNumberWords = true; break; }
        }

        if (!hasDigits && !hasNumberWords) return false;

        if (doorCode.ProcessVoiceCode(text))
        {
            UpdateStatus($"Code ingevoerd: {text}");
            return true;
        }
        return false;
    }

    void VerwerkObjectTransformatie(string text)
    {
        string currentRoom = "Server Data Control Room";

        llmService.EvaluatePlausibility(currentRoom, text, (llmResult) =>
        {
            timer.Stop();
            long latencyMs = timer.ElapsedMilliseconds;

            spawner.ProcessTextAndSpawn(llmResult, UpdateStatus);

            if (analytics != null)
            {
                analytics.LogData(lastSpokenText, llmResult.prefab_name, llmResult.score, latencyMs);
            }
        });
    }

    TerminalHacker FindClosestTerminal()
    {
        TerminalHacker[] terminals = FindObjectsOfType<TerminalHacker>();
        TerminalHacker closest = null;
        float minDist = interactieAfstand;

        foreach (var t in terminals)
        {
            float dist = Vector3.Distance(transform.position, t.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = t;
            }
        }
        return closest;
    }

    VoicePuzzle FindClosestPuzzle()
    {
        VoicePuzzle[] puzzels = FindObjectsOfType<VoicePuzzle>();
        VoicePuzzle closest = null;
        float minDist = interactieAfstand;

        foreach (var p in puzzels)
        {
            float dist = Vector3.Distance(transform.position, p.transform.position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = p;
            }
        }
        return closest;
    }

    private string TryKeywordFallback(string text)
    {
        string lower = text.ToLower();
        if (lower.Contains("bed") || lower.Contains("cot") || lower.Contains("bad")) return "Bed01";
        if (lower.Contains("table") || lower.Contains("desk")) return "Table01";
        if (lower.Contains("chair") || lower.Contains("seat") || lower.Contains("stool") || lower.Contains("throne")) return "Chair01";
        if (lower.Contains("office") && (lower.Contains("chair") || lower.Contains("seat"))) return "OfficeChair";
        if (lower.Contains("couch") || lower.Contains("sofa")) return "Sofa01";
        if (lower.Contains("closet") || lower.Contains("cabinet") || lower.Contains("locker") || lower.Contains("wardrobe") || lower.Contains("kast")) return "Closet01";
        if (lower.Contains("bath") || lower.Contains("tub") || lower.Contains("bathtub")) return "BathTub01";
        if (lower.Contains("cushion") || lower.Contains("pillow")) return "Cushion01";
        if (lower.Contains("drawer") || lower.Contains("chest")) return "Drawer01";
        if (lower.Contains("bench") || lower.Contains("bunch")) return "Bench";
        return null;
    }

    void UpdateStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }
}
