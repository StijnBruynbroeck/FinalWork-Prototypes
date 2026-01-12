using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
using TMPro;

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
    
    IEnumerator TestAssemblyAIConnection()
    {
        if (statusText != null)
            statusText.text = "Testing AssemblyAI connection...";
        
        using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/account"))
        {
            request.SetRequestHeader("Authorization", apiKey);
            request.SetRequestHeader("Content-Type", "application/json");
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string response = request.downloadHandler.text;
                Debug.Log("AssemblyAI Account Info: " + response);
                
                if (resultText != null)
                    resultText.text = "Press and hold T to record";
                    
                if (statusText != null)
                    statusText.text = "Ready - Hold T to record";
            }
            else
            {
                string error = request.error;
                Debug.LogError("AssemblyAI Error: " + error);
                Debug.LogError("Response Code: " + request.responseCode);
                
                if (resultText != null)
                    resultText.text = "Error: " + error + "\nCode: " + request.responseCode;
                    
                if (statusText != null)
                    statusText.text = "Connection failed";
            }
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T) && !isRecording && Microphone.devices.Length > 0)
        {
            StartRecording();
        }
        else if (Input.GetKeyUp(KeyCode.T) && isRecording)
        {
            StopRecording();
        }
    }
    
    void StartRecording()
    {
        Debug.Log("Starting recording...");
        isRecording = true;
        
        if (statusText != null)
            statusText.text = "Recording... Release T to stop";
        
        recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingLength, recordingFrequency);
    }
    
    void StopRecording()
    {
        Debug.Log("Stopping recording...");
        isRecording = false;
        
        Microphone.End(microphoneDevice);
        
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
        recordedClip.GetData(samples, 0);
        
        byte[] wavData = ConvertToWAV(samples, recordedClip.channels, recordedClip.frequency);
        
        Debug.Log("Sending " + wavData.Length + " bytes to AssemblyAI");
        
        yield return StartCoroutine(SendToAssemblyAI(wavData));
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
            
            yield return request.SendWebRequest();
            
            if (request.result == UnityWebRequest.Result.Success)
            {
                string transcriptResponse = request.downloadHandler.text;
                Debug.Log("Transcript Response: " + transcriptResponse);
                
                string transcriptId = JsonUtility.FromJson<TranscriptResponse>(transcriptResponse).id;
                yield return StartCoroutine(CheckTranscriptionStatus(transcriptId));
            }
            else
            {
                Debug.LogError("Transcription Error: " + request.error);
                if (statusText != null)
                    statusText.text = "Transcription failed: " + request.error;
            }
        }
    }
    
    IEnumerator CheckTranscriptionStatus(string transcriptId)
    {
        string pollUrl = baseUrl + "/transcript/" + transcriptId;
        
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(pollUrl))
            {
                request.SetRequestHeader("Authorization", apiKey);
                
                yield return request.SendWebRequest();
                
                if (request.result == UnityWebRequest.Result.Success)
                {
                    string response = request.downloadHandler.text;
                    TranscriptStatus status = JsonUtility.FromJson<TranscriptStatus>(response);
                    
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
                    yield break;
                }
            }
            
            yield return new WaitForSeconds(2f);
        }
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
