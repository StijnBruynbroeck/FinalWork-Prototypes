using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.IO;
using TMPro;
using UnityEngine.InputSystem;
using System;
using System.Net; 

public class ApiCall : MonoBehaviour
{
    // 🔥 BELANGRIJK: VERVANG DEZE KEY, DE OUDE IS NU ONLINE ZICHTBAAR VOOR IEDEREEN
    private string apiKey = "c786b69a434f4bdb90be39b94ed56a14"; // <-- Maak zsm een nieuwe aan op AssemblyAI!
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
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        if (Microphone.devices.Length > 0)
        {
            
            microphoneDevice = Microphone.devices[0];
            Debug.Log("Microfoon gevonden: " + microphoneDevice);
            UpdateStatus("Ready - Hold T to record");
        }
        else
        {
            Debug.LogError("Geen microfoon gevonden!");
            UpdateStatus("No microphone found!");
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
        UpdateStatus("Recording...");
        
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
        
        UpdateStatus("Processing...");
        StartCoroutine(ProcessRecording());
    }
    
    IEnumerator ProcessRecording()
    {
        yield return new WaitForSeconds(0.1f);

        if (recordedClip == null) yield break;
        
        float[] samples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(samples, 0);
        
        byte[] audioData = ConvertToWAV(samples, recordedClip.channels, recordedClip.frequency);
        
        Destroy(recordedClip);
        recordedClip = null;
        
        yield return StartCoroutine(SendToAssemblyAI(audioData));
    }

   IEnumerator SendToAssemblyAI(byte[] audioData)
    {
        UpdateStatus("Uploading...");
        Debug.Log($"Uploading {audioData.Length} bytes...");

        if (audioData.Length == 0)
        {
            Debug.LogError("Audio data is leeg.");
            yield break;
        }

        string uploadEndpoint = baseUrl + "/upload";
        
        // We maken het request object aan ZONDER 'using' statement om GC errors te vermijden
        UnityWebRequest request = new UnityWebRequest(uploadEndpoint, "POST");
        
        // Setup
        request.uploadHandler = new UploadHandlerRaw(audioData);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.chunkedTransfer = false; 
        request.SetRequestHeader("Authorization", apiKey.Trim());
        request.SetRequestHeader("Content-Type", "application/octet-stream");

        #if UNITY_EDITOR
        // We slaan de certificaat handler op in een variabele
        var certHandler = new BypassCertificate();
        request.certificateHandler = certHandler;
        #endif

        // We gebruiken een try/finally blok om zeker te weten dat we netjes opruimen
        try 
        {
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<UploadResponse>(request.downloadHandler.text);
                Debug.Log("Upload gelukt! URL: " + response.upload_url);
                yield return StartCoroutine(StartTranscription(response.upload_url));
            }
            else
            {
                Debug.LogError($"Upload Error: {request.error} | {request.downloadHandler.text}");
                UpdateStatus($"Upload Failed: {request.error}");
            }
        }
        finally
        {
            // Handmatig opruimen. Dit voorkomt vaak de "Invalid GC Handle" error
            if (request != null)
            {
                request.Dispose();
            }
        }
    }

    IEnumerator StartTranscription(string audioUrl)
    {
        UpdateStatus("Transcribing...");
        
        string json = "{\"audio_url\": \"" + audioUrl + "\"}";
        string transcriptEndpoint = baseUrl + "/transcript";
        
        using (UnityWebRequest request = new UnityWebRequest(transcriptEndpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            request.SetRequestHeader("Authorization", apiKey);
            request.SetRequestHeader("Content-Type", "application/json");

            #if UNITY_EDITOR
            request.certificateHandler = new BypassCertificate();
            #endif

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string id = JsonUtility.FromJson<TranscriptResponse>(request.downloadHandler.text).id;
                yield return StartCoroutine(CheckTranscriptionStatus(id));
            }
            else
            {
                Debug.LogError("Transcript Start Fout: " + request.error);
                UpdateStatus("Transcript Error");
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
                
                #if UNITY_EDITOR
                request.certificateHandler = new BypassCertificate();
                #endif

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var status = JsonUtility.FromJson<TranscriptStatus>(request.downloadHandler.text);
                    
                    UpdateStatus("Status: " + status.status);

                    if (status.status == "completed")
                    {
                        if (resultText != null) resultText.text = status.text;
                        Debug.Log("Transcriptie compleet: " + status.text);
                        break;
                    }
                    else if (status.status == "error")
                    {
                        UpdateStatus("Error: " + status.error);
                        Debug.LogError("AssemblyAI Error: " + status.error);
                        break;
                    }
                }
            }
            yield return new WaitForSeconds(2);
        }
    }

    // --- DE ONTBREKENDE FUNCTIE ---
    // Dit is de functie die "missing" was in jouw vorige code
    void UpdateStatus(string msg)
    {
        if (statusText != null) 
        {
            statusText.text = msg;
        }
    }

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

// JSON HELPERS
[System.Serializable]
public class UploadResponse { public string upload_url; }
[System.Serializable]
public class TranscriptResponse { public string id; }
[System.Serializable]
public class TranscriptStatus { public string status; public string text; public string error; }

#if UNITY_EDITOR
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}
#endif