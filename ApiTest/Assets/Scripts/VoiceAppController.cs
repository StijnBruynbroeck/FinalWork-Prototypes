using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using System.Diagnostics;

public class VoiceAppController : MonoBehaviour
{
    public AnalyticsManager analytics; 
    private Stopwatch timer = new Stopwatch();
    private string lastSpokenText = ""; 
    public GroqLLMService llmService;
    public MicrophoneRecorder recorder;
    public GroqAudioService apiService;
    public ObjectSpawner spawner;

    [Header("UI")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI statusText;

    private bool tKeyPressed = false;
    private float recordingStartTime; 

    void Update()
    {
        if (Keyboard.current == null) return;

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
        recordingStartTime = Time.time; // Sla de starttijd op
        UpdateStatus("<color=red>● Recording...</color>"); // Visuele indicator (rood bolletje)
        recorder.StartRecording();
    }

    void StopAppRecording()
    {
        float duration = Time.time - recordingStartTime;

        // BEVEILIGING: Als de opname korter is dan 0.8 seconden, negeren we het.
        // Dit voorkomt dat 'klikjes' of ruis als hallucinaties (Koreaans) worden vertaald.
        if (duration < 0.8f)
        {
            recorder.StopRecording((byte[] audioData) => 
        {
            timer.Reset();
            timer.Start(); // START DE KLOK!
            apiService.TranscribeAudio(audioData, OnTranscriptionSuccess, UpdateStatus);
        }); 
        }

        UpdateStatus("Processing Audio...");
        
        recorder.StopRecording((byte[] audioData) => 
        {
            apiService.TranscribeAudio(audioData, OnTranscriptionSuccess, UpdateStatus);
        });
    }

    void OnTranscriptionSuccess(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return;

        lastSpokenText = text; // Onthoud wat Whisper hoorde

        TerminalHacker actieveTerminal = FindObjectOfType<TerminalHacker>();
        if (actieveTerminal != null)
        {
            // We sturen de 'text' (jouw gesproken woorden) naar het scherm
            actieveTerminal.ControleerWachtwoord(text); 
        }
        
        string currentRoom = "Server Data Control Room"; 

        llmService.EvaluatePlausibility(currentRoom, text, (llmResult) => 
        {
            timer.Stop(); // STOP DE KLOK!
            long latencyMs = timer.ElapsedMilliseconds; // Dit is je harde data!

            // Spawn het object
            spawner.ProcessTextAndSpawn(llmResult, UpdateStatus);

            // Sla de data op in je CSV
            if (analytics != null)
            {
                analytics.LogData(lastSpokenText, llmResult.prefab_name, llmResult.score, latencyMs);
            }
        });
    }

    void UpdateStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }
}