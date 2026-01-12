using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using TMPro;
using UnityEngine.InputSystem;

public class ApiCall : MonoBehaviour
{
    private string apiKey = "eb3b47c9928a4faba73a6a2500b97bd5"; // Jouw API Key
    
    // --- FIX 1: DE JUISTE URL ---
    private string baseUrl = "https://api.assemblyai.com/v2"; 
    
    [Header("UI References")]
    public TextMeshProUGUI resultText;
    public TextMeshProUGUI statusText;
    
    [Header("Audio Settings")]
    public int recordingFrequency = 44100;
    public int maxRecordingLength = 30;
    
    private bool isRecording = false;
    private AudioClip recordedClip;
    private string microphoneDevice;
    private bool tKeyPressed = false;
    
    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log("Using microphone: " + microphoneDevice);
            if (statusText != null) statusText.text = "Ready - Hold T to record";
        }
        else
        {
            Debug.LogError("No microphone found!");
            if (statusText != null) statusText.text = "No microphone found!";
        }
    }
    
    void Update()
    {
        if (Keyboard.current != null && !string.IsNullOrEmpty(microphoneDevice))
        {
            bool tPressed = Keyboard.current.tKey.isPressed;
            
            if (tPressed && !tKeyPressed && !isRecording)
            {
                StartRecording();
            }
            else if (!tPressed && tKeyPressed && isRecording)
            {
                StopRecording();
            }
            
            tKeyPressed = tPressed;
        }
    }
    
    void StartRecording()
    {
        if (isRecording) return;
        isRecording = true;
        if (statusText != null) statusText.text = "Recording...";
        
        recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingLength, recordingFrequency);
    }
    
    void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        
        if (Microphone.IsRecording(microphoneDevice))
        {
            Microphone.End(microphoneDevice);
        }
        
        if (statusText != null) statusText.text = "Processing...";
        StartCoroutine(ProcessRecording());
    }
    
    IEnumerator ProcessRecording()
    {
        if (recordedClip == null) yield break;
        
        float[] samples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(samples, 0);
        
        // Convert to WAV bytes
        byte[] audioData = ConvertToWAV(samples, recordedClip.channels, recordedClip.frequency);
        
        // Clean up
        Destroy(recordedClip);
        recordedClip = null;
        
        yield return StartCoroutine(SendToAssemblyAI(audioData));
    }

    // --- FIX 2: DE JUISTE UPLOAD FUNCTIE ---
    IEnumerator SendToAssemblyAI(byte[] audioData)
    {
        if (statusText != null) statusText.text = "Uploading...";

        // We bouwen de URL: https://api.assemblyai.com/v2/upload
        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/upload", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(audioData);
            request.downloadHandler = new DownloadHandlerBuffer();

            request.SetRequestHeader("Authorization", apiKey);
            request.SetRequestHeader("Content-Type", "application/octet-stream");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log("Upload gelukt: " + response);
                
                // Haal de upload_url uit de JSON
                string uploadUrl = JsonUtility.FromJson<UploadResponse>(response).upload_url;
                
                // Start Transcriptie
                yield return StartCoroutine(StartTranscription(uploadUrl));
            }
            else
            {
                Debug.LogError("Upload Fout: " + request.error);
                if (statusText != null) statusText.text = "Error: " + request.error;
            }
        }
    }

    IEnumerator StartTranscription(string audioUrl)
    {
        if (statusText != null) statusText.text = "Transcribing...";
        
        string json = "{\"audio_url\": \"" + audioUrl + "\"}";
        
        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/transcript", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("Authorization", apiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string id = JsonUtility.FromJson<TranscriptResponse>(request.downloadHandler.text).id;
                yield return StartCoroutine(CheckTranscriptionStatus(id));
            }
            else
            {
                Debug.LogError("Transcript Start Fout: " + request.error);
            }
        }
    }

    IEnumerator CheckTranscriptionStatus(string id)
    {
        string pollUrl = baseUrl + "/transcript/" + id;
        
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(pollUrl))
            {
                request.SetRequestHeader("Authorization", apiKey);
                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var status = JsonUtility.FromJson<TranscriptStatus>(request.downloadHandler.text);
                    
                    if (status.status == "completed")
                    {
                        if (resultText != null) resultText.text = status.text;
                        if (statusText != null) statusText.text = "Done!";
                        break;
                    }
                    else if (status.status == "error")
                    {
                        if (statusText != null) statusText.text = "Error: " + status.error;
                        break;
                    }
                }
            }
            yield return new WaitForSeconds(2);
        }
    }

    // HULP FUNCTIES
    byte[] ConvertToWAV(float[] samples, int channels, int frequency)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + samples.Length * 2);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * 2);
            writer.Write((short)(channels * 2));
            writer.Write((short)16);
            writer.Write("data".ToCharArray());
            writer.Write(samples.Length * 2);

            foreach (var sample in samples)
            {
                writer.Write((short)(sample * short.MaxValue));
            }
            return stream.ToArray();
        }
    }
}

// JSON CLASSES
[System.Serializable]
public class UploadResponse { public string upload_url; }

[System.Serializable]
public class TranscriptResponse { public string id; }

[System.Serializable]
public class TranscriptStatus { public string status; public string text; public string error; }