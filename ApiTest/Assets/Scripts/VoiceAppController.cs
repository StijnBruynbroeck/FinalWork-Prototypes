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

        if (specificDoor == null || !specificDoor.gameObject.activeInHierarchy)
        {
            specificDoor = null;
            Animator[] alleAnimators = FindObjectsOfType<Animator>(true);
            Debug.Log($"Zoeken naar Animator met 'sphere' controller... ({alleAnimators.Length} animators gevonden)");
            foreach (Animator a in alleAnimators)
            {
                if (!a.gameObject.activeInHierarchy) continue;
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
                if (obj != null && !obj.activeInHierarchy) continue;
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

        GameObject helpGo = new GameObject("HelpPanelRunner");
        helpGo.transform.SetParent(transform);
        helpGo.AddComponent<HelpPanel>();
    }

    void Update()
    {
        if (Keyboard.current == null) return;

        
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
        
        
        AudioEventSystem.EmitSound(transform.position, voiceIntensity);
    }

    void StopAppRecording()
    {
        float duration = Time.time - recordingStartTime;

        UpdateStatus("Processing Audio...");

        
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

        // Pre-filter: reject known noise transcriptions
        string lower = cleaned.ToLower();
        string[] noiseMarkers = { "[blank_audio]", "[inaudible]", "[silence]", "[noise]", "[music]", "[cough]" };
        foreach (var marker in noiseMarkers)
        {
            if (lower.Contains(marker))
            {
                UpdateStatus("Not understood — try again.");
                return;
            }
        }
        if (lower.Length < 4)
        {
            UpdateStatus("Too short — try again.");
            return;
        }

        // Correction: fix known Whisper mishearings
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

        // Pre-LLM unmorph check (always runs)
        string[] unmorphPatterns = { "unmorph", "on morph", "unmorpf", "unmorf", "a morph",
                                     "morph back", "change back", "turn back", "revert", "undo", "stop morph" };
        foreach (var pattern in unmorphPatterns)
        {
            if (lower.Contains(pattern))
            {
                if (spawner != null && spawner.IsMorphed)
                {
                    spawner.Unmorph();
                    UpdateStatus("Unmorphed! Back to human form.");
                }
                else
                {
                    UpdateStatus("You're already in human form.");
                }
                return;
            }
        }

        // End game check — always available
        if (lower.Contains("end game") || lower.Contains("end the game"))
        {
            EndGameTerminal[] endTerminals = FindObjectsOfType<EndGameTerminal>();
            foreach (var et in endTerminals)
            {
                if (et.TryEndSequence(cleaned))
                {
                    UpdateStatus("Ending game...");
                    return;
                }
            }
            UpdateStatus("No end terminal found in scene.");
            return;
        }

       
        if (RouteerNaarDeur(cleaned)) return;

       
        if (RouteerNaarCode(cleaned)) return;

     
        if (RouteerNaarTerminal(cleaned)) return;

       
        if (RouteerNaarEndTerminal(cleaned)) return;

       
        VoiceRiddlePuzzle nieuwePuzzel = FindObjectOfType<VoiceRiddlePuzzle>();
        if (nieuwePuzzel != null && nieuwePuzzel.IsInRange && !nieuwePuzzel.IsCompleted)
        {
            nieuwePuzzel.ProcessVoiceInput(cleaned);
            return;
        }

      
        string currentRoom = "Server Data Control Room";
        if (zoneDetection != null && zoneDetection.CurrentZone != null)
        {
            var z = zoneDetection.CurrentZone;
            currentRoom = $"Zone: {z.zoneType} (ID: {z.zoneId})";
        }
        Debug.Log($"[VoiceApp] HUIDIGE ZONE CONTEXT: {currentRoom}");

        // LLM evaluation
        llmService.EvaluatePlausibility(currentRoom, cleaned, (llmResult) =>
        {
            timer.Stop();
            long latencyMs = timer.ElapsedMilliseconds;

            // Door action from LLM
            if (!string.IsNullOrEmpty(llmResult.door_action) && llmResult.door_action != "none")
            {
                if (specificDoor != null)
                {
                    if (llmResult.door_action == "open")
                    {
                        specificDoor.OpenDoor();
                        UpdateStatus("Door opened via AI!");
                    }
                    else if (llmResult.door_action == "close")
                    {
                        specificDoor.CloseDoor();
                        UpdateStatus("Door closed via AI!");
                    }
                }
                return;
            }

            // Unmorph check
            if (llmResult.unmorph)
            {
                if (spawner != null && spawner.IsMorphed)
                {
                    spawner.Unmorph();
                    UpdateStatus("Unmorphed! Back to human form.");
                }
                else
                {
                    UpdateStatus("You're already in human form.");
                }
                return;
            }

            // Keyword fallback when LLM returns nothing
            if (string.IsNullOrEmpty(llmResult.prefab_name) || llmResult.prefab_name.ToLower() == "none")
            {
                string fallback = TryKeywordFallback(cleaned);
                if (!string.IsNullOrEmpty(fallback))
                {
                    Debug.Log($"[VoiceApp] LLM returned empty, keyword fallback → {fallback}");
                    llmResult.prefab_name = fallback;
                }
            }

            // Validate prefab exists before spawning
            if (!string.IsNullOrEmpty(llmResult.prefab_name) && llmResult.prefab_name.ToLower() != "none")
            {
                GameObject prefab = Resources.Load<GameObject>("Props/" + llmResult.prefab_name);
                if (prefab == null)
                {
                    UpdateStatus($"Object '{llmResult.prefab_name}' doesn't exist — denied.");
                    if (analytics != null)
                        analytics.LogData(lastSpokenText, "none", 0, latencyMs);
                    return;
                }
            }

            // Spawn object
            spawner.ProcessTextAndSpawn(llmResult, UpdateStatus);

            // Enemy suspicion multiplier
            EnemyVision vijandZicht = FindObjectOfType<EnemyVision>();
            if (vijandZicht != null)
            {
                vijandZicht.SetLLMScoreMultiplier(llmResult.score);
            }

            // Analytics
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

    bool RouteerNaarEndTerminal(string text)
    {
        EndGameTerminal[] terminals = FindObjectsOfType<EndGameTerminal>();
        foreach (var t in terminals)
        {
            if (t.TryEndSequence(text))
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
                    UpdateStatus($"Door opened: {text}");
                }
                else
                {
                    specificDoor.CloseDoor();
                    UpdateStatus($"Door closed: {text}");
                }

                if (analytics != null)
                    analytics.LogData(text, "door_command", isOpenCmd ? 100 : 0, 0);

                return true;
            }
            else
            {
                Debug.LogWarning($"Deurcommando herkend maar 'specificDoor' is null!");
                UpdateStatus("Error: no door found in scene");
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
            UpdateStatus($"Code entered: {text}");
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
        if (lower.Contains("closet") || lower.Contains("locker") || lower.Contains("wardrobe") || lower.Contains("kast")) return "Closet01";
        if (lower.Contains("bath") || lower.Contains("tub") || lower.Contains("bathtub")) return "BathTub01";
        if (lower.Contains("cushion") || lower.Contains("pillow")) return "Cushion01";
        if (lower.Contains("drawer") || lower.Contains("chest")) return "Drawer01";
        if (lower.Contains("bench") || lower.Contains("bunch")) return "Bench";
        if (lower.Contains("toilet") || lower.Contains("wc") || lower.Contains("lavatory")) return "Toilet01";
        if (lower.Contains("sink") || lower.Contains("basin") || lower.Contains("washbasin")) return "WashBasin01";
        if (lower.Contains("shower")) return "Shower01";
        if (lower.Contains("vanity")) return "BathroomVanity01";
        if (lower.Contains("fridge") || lower.Contains("refrigerator") || lower.Contains("freezer")) return "Refrigerator01";
        if (lower.Contains("oven")) return "Oven01";
        if (lower.Contains("stove") || lower.Contains("cooker")) return "Stove01";
        if (lower.Contains("kitchen sink")) return "KitchenSink01";
        if (lower.Contains("microwave")) return "Microwave01";
        if ((lower.Contains("kitchen") && lower.Contains("cabinet")) || lower.Contains("cupboard")) return "KitchenCabinet01";
        if (lower.Contains("cabinet")) return "Closet01";
        return null;
    }

    void UpdateStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }
}
