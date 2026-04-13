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

    void Update()
    {
        // Check of toetsenbord aanwezig is
        if (Keyboard.current == null) return;

        bool tPressed = Keyboard.current.tKey.isPressed;

        // Als we T indrukken en we nemen nog niet op -> START
        if (tPressed && !tKeyPressed && !recorder.IsRecording)
        {
            StartAppRecording();
        }
        // Als we T loslaten en we waren aan het opnemen -> STOP
        else if (!tPressed && tKeyPressed && recorder.IsRecording)
        {
            StopAppRecording();
        }

        tKeyPressed = tPressed;
    }

    void StartAppRecording()
    {
        UpdateStatus("Recording...");
        recorder.StartRecording();
    }

    void StopAppRecording()
    {
        UpdateStatus("Processing Audio...");
        
        // Hier roepen we StopRecording aan. 
        // De code tussen { } wordt pas uitgevoerd als de audio klaar is (de Callback).
        recorder.StopRecording((byte[] audioData) => 
        {
            // Nu hebben we de audio data! Stuur naar API.
            apiService.TranscribeAudio(audioData, OnTranscriptionSuccess, UpdateStatus);
        });
    }

    // Deze functie wordt aangeroepen als AssemblyAI klaar is
    void OnTranscriptionSuccess(string text)
    {
        Debug.Log("Tekst ontvangen via Whisper: " + text);
        if (resultText != null) resultText.text = text;

        UpdateStatus("AI beoordeelt logica...");

        // Hier bepalen we waar de speler is. Dit kun je later in je game dynamisch maken op basis van zones!
        string currentRoom = "Server Data Control Room"; 

        // We vragen de LLM om de score
        llmService.EvaluatePlausibility(currentRoom, text, (score, reason) => 
        {
            // Zodra de LLM klaar is (na ~0.5 sec), sturen we de score door naar de spawner
            spawner.ProcessTextAndSpawn(text, score, reason, UpdateStatus);
        });
    }

    void UpdateStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }
}