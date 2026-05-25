using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System;


public class GroqAudioService : MonoBehaviour
{
    private UnityWebRequest activeRequest;
    [Header("Local Whisper Settings")]
    private string localEndpoint = "http://localhost:9090/inference";

    // Zelfde opzet als je oude script, zodat je het makkelijk kan inpluggen!
    public void TranscribeAudio(byte[] audioData, Action<string> onTranscriptionComplete, Action<string> onStatusUpdate)
    {
        StartCoroutine(SendAudioToLocal(audioData, onTranscriptionComplete, onStatusUpdate));
    }

    private IEnumerator SendAudioToLocal(byte[] audioData, Action<string> onComplete, Action<string> onStatus)
    {
        if (audioData == null || audioData.Length < 1000) 
        {
            Debug.LogWarning("Audio was te kort of leeg! Houd de 'T' toets langer ingedrukt.");
            onStatus?.Invoke("Opname te kort.");
            yield break;
        }

        onStatus?.Invoke("Verzenden naar lokale Whisper...");

        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        
        formData.Add(new MultipartFormDataSection("response_format", "json"));
        formData.Add(new MultipartFormDataSection("temperature", "0.0"));
        formData.Add(new MultipartFormDataSection("language", "en"));
        formData.Add(new MultipartFormFileSection("file", audioData, "recording.wav", "audio/wav"));

        using (UnityWebRequest request = UnityWebRequest.Post(localEndpoint, formData))
        {
            request.chunkedTransfer = false;

         yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                onStatus?.Invoke("Transcriptie succesvol!");
                WhisperResponse response = JsonUtility.FromJson<WhisperResponse>(request.downloadHandler.text);
                onComplete?.Invoke(response.text);
            }
            else
            {
                Debug.LogError("Lokale Whisper Error: " + request.error + "\n" + request.downloadHandler.text);
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

[Serializable] 
public class WhisperResponse 
{ 
    public string text; 
}