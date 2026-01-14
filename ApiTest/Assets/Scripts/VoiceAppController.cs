using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

public class VoiceAppController : MonoBehaviour
{
    [Header("Verbindingen (Sleep scripts hierin)")]
    public MicrophoneRecorder recorder;
    public AssemblyAIService apiService;
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
        Debug.Log("Tekst ontvangen: " + text);
        if (resultText != null) resultText.text = text;

        // Stuur de tekst naar de spawner
        spawner.ProcessTextAndSpawn(text, UpdateStatus);
    }

    void UpdateStatus(string status)
    {
        if (statusText != null) statusText.text = status;
    }
}