using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;


public class GroqAudioService : MonoBehaviour
{
    private UnityWebRequest activeRequest;
    [Header("Groq Settings")]
    [Tooltip("Plak hier je Groq API key (begint vaak met gsk_)")]
    public string apiKey = "YOUR_GROQ_API_KEY"; 
    private string groqEndpoint = "https://api.groq.com/openai/v1/audio/transcriptions";

    // Zelfde opzet als je oude script, zodat je het makkelijk kan inpluggen!
    public void TranscribeAudio(byte[] audioData, Action<string> onTranscriptionComplete, Action<string> onStatusUpdate)
    {
        StartCoroutine(SendAudioToGroq(audioData, onTranscriptionComplete, onStatusUpdate));
    }

    private IEnumerator SendAudioToGroq(byte[] audioData, Action<string> onComplete, Action<string> onStatus)
    {
        // --- NIEUWE CHECK: Is er wel audio opgenomen? ---
        if (audioData == null || audioData.Length < 1000) 
        {
            Debug.LogWarning("Audio was te kort of leeg! Houd de 'T' toets langer ingedrukt.");
            onStatus?.Invoke("Opname te kort.");
            yield break; // Stop de functie hier, stuur niets naar de API
        }
        // ------------------------------------------------

        onStatus?.Invoke("Verzenden naar Groq...");

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        
        formData.Add(new MultipartFormDataSection("model", "whisper-large-v3-turbo"));
        
        // 1. DE LOCKDOWN: Zeg keihard dat dit Nederlands is. Geen Koreaans of IJslands meer.
        formData.Add(new MultipartFormDataSection("language", "nl" )); 

        formData.Add(new MultipartFormDataSection("temperature", "0.0"));

        // 2. DE CONTEXT: Dit vertelt de AI welke woorden hij kan verwachten als het onduidelijk is.
        string aiContextPrompt = "Dit is een duidelijk Nederlands commando voor een videogame. Actiewoorden: serverkast, kantoorstoel, wasmachine, plant, koffiemachine, verander, maak.";
formData.Add(new MultipartFormDataSection("prompt", aiContextPrompt));
        
        formData.Add(new MultipartFormFileSection("file", audioData, "recording.wav", "audio/wav"));

        using (UnityWebRequest request = UnityWebRequest.Post(groqEndpoint, formData))
        {
            request.chunkedTransfer = false;
           request.SetRequestHeader("Authorization", "Bearer " + apiKey);
           

         yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onStatus?.Invoke("Transcriptie succesvol!");
                GroqResponse response = JsonUtility.FromJson<GroqResponse>(request.downloadHandler.text);
                onComplete?.Invoke(response.text);
            }
            else
            {
                Debug.LogError("Groq Audio Error: " + request.error + "\n" + request.downloadHandler.text);
                onStatus?.Invoke("Fout bij transcriptie.");
            }
        }
    }

    void OnDisable()
    {
        // Breek de actieve download af als we Unity op Stop zetten
        if (activeRequest != null && !activeRequest.isDone)
        {
            activeRequest.Abort();
            activeRequest.Dispose();
        }
    }
}

// Klein data-structuurtje om het JSON antwoord van Groq te lezen
[Serializable] 
public class GroqResponse 
{ 
    public string text; 
}