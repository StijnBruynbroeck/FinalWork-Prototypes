using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class VoiceAppController : MonoBehaviour
{
    public GroqLLMService llmService;
    public MicrophoneRecorder recorder;
    public GroqAudioService apiService;
    public ObjectSpawner spawner;

    [Header("UI")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI statusText;

    private bool tKeyPressed = false;
    private float recordingStartTime; // Nieuw: om de duur te meten

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
            recorder.StopRecording((byte[] audioData) => { /* Doe niets met de data */ });
            UpdateStatus("Opname te kort. Houd 'T' langer ingedrukt.");
            Debug.LogWarning("Opname genegeerd: te kort.");
            return;
        }

        UpdateStatus("Processing Audio...");
        
        recorder.StopRecording((byte[] audioData) => 
        {
            apiService.TranscribeAudio(audioData, OnTranscriptionSuccess, UpdateStatus);
        });
    }

    void OnTranscriptionSuccess(string text)
    {
        // Extra check: als Whisper een leeg resultaat of alleen spaties geeft
        if (string.IsNullOrWhiteSpace(text))
        {
            UpdateStatus("Geen spraak herkend.");
            return;
        }

        Debug.Log("Tekst ontvangen via Whisper: " + text);
        if (resultText != null) resultText.text = text;

        UpdateStatus("AI beoordeelt logica...");

        string currentRoom = "Server Data Control Room"; 

        llmService.EvaluatePlausibility(currentRoom, text, (llmResult) => 
        {
            spawner.ProcessTextAndSpawn(llmResult, UpdateStatus);
        });
    }

    void UpdateStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }
}