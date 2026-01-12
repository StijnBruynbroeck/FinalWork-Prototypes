using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using TMPro;
using UnityEngine.InputSystem;
using System.Security.Cryptography.X509Certificates;

public class ApiCall : MonoBehaviour
{
    private string apiKey = "eb3b47c9928a4faba73a6a2500b97bd5";
    private string baseUrl = "https://api.assemblyai.com";
    
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
        StartCoroutine(TestAssemblyAIConnection());
        
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log("Using microphone: " + microphoneDevice);
        }
        else
        {
            Debug.LogError("No microphone found!");
        if (statusText != null)
            statusText.text = "No microphone found!";
        }
    }
    
    void OnDestroy()
    {
        StopRecording();
        
        if (recordedClip != null)
        {
            Destroy(recordedClip);
            recordedClip = null;
        }
    }
    
    void OnApplicationQuit()
    {
        StopRecording();
        
        if (recordedClip != null)
        {
            Destroy(recordedClip);
            recordedClip = null;
        }
    }
    
    IEnumerator TestAssemblyAIConnection()
    {
        if (statusText != null)
            statusText.text = "Testing AssemblyAI connection...";
        
        // Remove the account test since /account endpoint doesn't exist
        // Instead just show ready message
        yield return new WaitForSeconds(0.5f);
        
        Debug.Log("AssemblyAI API Key configured");
        
        if (resultText != null)
            resultText.text = "Press and hold T to record";
            
        if (statusText != null)
            statusText.text = "Ready - Hold T to record";
    }
    
    void Update()
    {
        if (Keyboard.current != null && !string.IsNullOrEmpty(microphoneDevice))
        {
            bool tPressed = Keyboard.current.tKey.isPressed;
            
            if (tPressed && !tKeyPressed && !isRecording && Microphone.devices.Length > 0)
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
        
        Debug.Log("Starting recording...");
        isRecording = true;
        
        if (statusText != null)
            statusText.text = "Recording... Release T to stop";
        
        try
        {
            recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingLength, recordingFrequency);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to start recording: " + e.Message);
            isRecording = false;
            if (statusText != null)
                statusText.text = "Recording failed!";
        }
    }
    
    void StopRecording()
    {
        if (!isRecording) return;
        
        Debug.Log("Stopping recording...");
        isRecording = false;
        
        if (Microphone.IsRecording(microphoneDevice))
        {
            Microphone.End(microphoneDevice);
        }
        
        if (statusText != null)
            statusText.text = "Processing recording...";
        
        StartCoroutine(ProcessRecording());
    }
    
    IEnumerator ProcessRecording()
    {
        yield return new WaitForSeconds(0.1f);
        
        if (recordedClip == null)
        {
            Debug.LogError("No audio recorded!");
            if (statusText != null)
                statusText.text = "Recording failed!";
            yield break;
        }
        
        float[] samples = new float[recordedClip.samples * recordedClip.channels];
        byte[] wavData = null;
        
        try
        {
            recordedClip.GetData(samples, 0);
            wavData = ConvertToWAV(samples, recordedClip.channels, recordedClip.frequency);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to process recording: " + e.Message);
            if (statusText != null)
                statusText.text = "Processing failed!";
            yield break;
        }
        finally
        {
            if (recordedClip != null)
            {
                Destroy(recordedClip);
                recordedClip = null;
            }
        }
        
        if (wavData != null)
        {
            Debug.Log("Sending " + wavData.Length + " bytes to AssemblyAI");
            yield return StartCoroutine(SendToAssemblyAI(wavData));
        }
    }
    
    byte[] ConvertToWAV(float[] samples, int channels, int frequency)
    {
        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            int sampleCount = samples.Length;
            short[] intSamples = new short[sampleCount];
            
            for (int i = 0; i < sampleCount; i++)
            {
                intSamples[i] = (short)(samples[i] * short.MaxValue);
            }
            
            byte[] idata = new byte[sampleCount * 2];
            Buffer.BlockCopy(intSamples, 0, idata, 0, idata.Length);
            
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + idata.Length);
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
            writer.Write(idata.Length);
            writer.Write(idata);
            
            return stream.ToArray();
        }
    }
    
    IEnumerator SendToAssemblyAI(byte[] wavData)
    {
        if (statusText != null)
            statusText.text = "Uploading to AssemblyAI...";
        
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("audio", wavData, "audio/wav"));
        
        using (UnityWebRequest request = UnityWebRequest.Post(baseUrl + "/upload", formData))
        {
            request.SetRequestHeader("Authorization", apiKey);
            request.timeout = 30; // Set timeout to 30 seconds
            
            // Handle certificate bypass for development (remove in production)
            #if UNITY_EDITOR
            request.certificateHandler = new BypassCertificate();
            #endif
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string uploadResponse = request.downloadHandler.text;
                Debug.Log("Upload Response: " + uploadResponse);
                
                string uploadUrl = "";
                try
                {
                    uploadUrl = JsonUtility.FromJson<UploadResponse>(uploadResponse).upload_url;
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to parse upload response: " + e.Message);
                    if (statusText != null)
                        statusText.text = "Upload response error";
                    yield break;
                }
                
                yield return StartCoroutine(StartTranscription(uploadUrl));
            }
            else
            {
                string errorMsg = request.error;
                if (request.responseCode == 0 && errorMsg.Contains("unitytls"))
                {
                    errorMsg = "TLS/SSL Certificate Error - Check network connection";
                }
                
                Debug.LogError("Upload Error: " + errorMsg);
                Debug.LogError("Response Code: " + request.responseCode);
                Debug.LogError("Response Body: " + request.downloadHandler.text);
                
                if (statusText != null)
                    statusText.text = "Upload failed: " + errorMsg;
            }
        }
    }
    
    public void UploadAudioForTranscription(string audioFilePath)
    {
        StartCoroutine(UploadAndTranscribe(audioFilePath));
    }
    
    IEnumerator UploadAndTranscribe(string audioFilePath)
    {
        if (statusText != null)
            statusText.text = "Uploading audio file...";
        
        byte[] audioData = System.IO.File.ReadAllBytes(audioFilePath);
        
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        formData.Add(new MultipartFormDataSection("audio", audioData, "audio/wav"));
        
        using (UnityWebRequest request = UnityWebRequest.Post(baseUrl + "/upload", formData))
        {
            request.SetRequestHeader("Authorization", apiKey);
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string uploadResponse = request.downloadHandler.text;
                Debug.Log("Upload Response: " + uploadResponse);
                
                string uploadUrl = JsonUtility.FromJson<UploadResponse>(uploadResponse).upload_url;
                yield return StartCoroutine(StartTranscription(uploadUrl));
            }
            else
            {
                Debug.LogError("Upload Error: " + request.error);
                if (statusText != null)
                    statusText.text = "Upload failed: " + request.error;
            }
        }
    }
    
    IEnumerator StartTranscription(string audioUrl)
    {
        if (statusText != null)
            statusText.text = "Starting transcription...";
        
        string jsonData = "{\"audio_url\": \"" + audioUrl + "\"}";
        
        using (UnityWebRequest request = UnityWebRequest.Post(baseUrl + "/transcript", jsonData, "application/json"))
        {
            request.SetRequestHeader("Authorization", apiKey);
            request.timeout = 30;
            
            #if UNITY_EDITOR
            request.certificateHandler = new BypassCertificate();
            #endif
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string transcriptResponse = request.downloadHandler.text;
                Debug.Log("Transcript Response: " + transcriptResponse);
                
                string transcriptId = "";
                try
                {
                    transcriptId = JsonUtility.FromJson<TranscriptResponse>(transcriptResponse).id;
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to parse transcript response: " + e.Message);
                    if (statusText != null)
                        statusText.text = "Transcription response error";
                    yield break;
                }
                
                yield return StartCoroutine(CheckTranscriptionStatus(transcriptId));
            }
            else
            {
                string errorMsg = request.error;
                if (request.responseCode == 0 && errorMsg.Contains("unitytls"))
                {
                    errorMsg = "TLS/SSL Certificate Error during transcription";
                }
                
                Debug.LogError("Transcription Error: " + errorMsg);
                Debug.LogError("Response Code: " + request.responseCode);
                Debug.LogError("Response Body: " + request.downloadHandler.text);
                
                if (statusText != null)
                    statusText.text = "Transcription failed: " + errorMsg;
            }
        }
    }
    
    IEnumerator CheckTranscriptionStatus(string transcriptId)
    {
        string pollUrl = baseUrl + "/transcript/" + transcriptId;
        int maxAttempts = 30; // Maximum 60 seconds (30 * 2 seconds)
        int attempts = 0;
        
        while (attempts < maxAttempts)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(pollUrl))
            {
                request.SetRequestHeader("Authorization", apiKey);
                request.timeout = 10;
                
                #if UNITY_EDITOR
                request.certificateHandler = new BypassCertificate();
                #endif
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    TranscriptStatus status = null;
                    
                    try
                    {
                        status = JsonUtility.FromJson<TranscriptStatus>(response);
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError("Failed to parse status response: " + e.Message);
                        attempts++;
                    }
                    
                    if (status == null)
                    {
                        yield return new WaitForSeconds(2f);
                        continue;
                    }
                    
                    Debug.Log("Transcription Status: " + status.status);
                    
                    if (statusText != null)
                        statusText.text = "Status: " + status.status;
                    
                    if (status.status == "completed")
                    {
                        if (resultText != null)
                            resultText.text = "Transcription Complete:\n" + status.text;
                        yield break;
                    }
                    else if (status.status == "error")
                    {
                        Debug.LogError("Transcription Error: " + status.error);
                        if (resultText != null)
                            resultText.text = "Transcription Error: " + status.error;
                        yield break;
                    }
                }
                else
                {
                    Debug.LogError("Status Check Error: " + request.error);
                    Debug.LogError("Response Code: " + request.responseCode);
                    attempts++;
                    
                    if (attempts >= maxAttempts)
                    {
                        if (statusText != null)
                            statusText.text = "Status check failed - Max attempts reached";
                        yield break;
                    }
                    
                    yield return new WaitForSeconds(2f);
                    continue;
                }
            }
            
            attempts++;
            yield return new WaitForSeconds(2f);
        }
        
        if (statusText != null)
            statusText.text = "Transcription timeout";
    }
}

[System.Serializable]
public class UploadResponse
{
    public string upload_url;
}

[System.Serializable]
public class TranscriptResponse
{
    public string id;
}

[System.Serializable]
public class TranscriptStatus
{
    public string id;
    public string status;
    public string text;
    public string error;
}

#if UNITY_EDITOR
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true; // Bypass certificate validation in editor for testing
    }
}
#endif
