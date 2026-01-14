using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System;
using System.Net;

public class AssemblyAIService : MonoBehaviour
{
   
    private string apiKey = "fe0d18f4e26d4d44be37680f7e88e7d4"; 
    private string baseUrl = "https://api.assemblyai.com/v2";

    void Start()
    {
        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
    }

    public void TranscribeAudio(byte[] audioData, Action<string> onTranscriptionComplete, Action<string> onStatusUpdate)
    {
        StartCoroutine(UploadAndTranscribe(audioData, onTranscriptionComplete, onStatusUpdate));
    }

    private IEnumerator UploadAndTranscribe(byte[] audioData, Action<string> onComplete, Action<string> onStatus)
    {
        onStatus?.Invoke("Uploading...");
        string uploadUrl = "";

        // 1. UPLOAD
        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/upload", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(audioData);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.chunkedTransfer = false;
            request.SetRequestHeader("Authorization", apiKey.Trim());
            request.SetRequestHeader("Content-Type", "application/octet-stream");
            request.SetRequestHeader("Connection", "close");

            #if UNITY_EDITOR
            request.certificateHandler = new BypassCertificate();
            #endif

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<UploadResponse>(request.downloadHandler.text);
                uploadUrl = response.upload_url;
            }
            else
            {
                onStatus?.Invoke($"Upload Error: {request.error}");
                yield break;
            }
        }

        // 2. START TRANSCRIPT
        onStatus?.Invoke("Transcribing...");
        string transcriptId = "";
        string json = "{\"audio_url\": \"" + uploadUrl + "\", \"language_code\": \"nl\"}";

        using (UnityWebRequest request = new UnityWebRequest(baseUrl + "/transcript", "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Authorization", apiKey.Trim());
            request.SetRequestHeader("Content-Type", "application/json");
            
            #if UNITY_EDITOR
            request.certificateHandler = new BypassCertificate();
            #endif

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                transcriptId = JsonUtility.FromJson<TranscriptResponse>(request.downloadHandler.text).id;
            }
            else
            {
                onStatus?.Invoke("Start Transcript Error");
                yield break;
            }
        }

        // 3. POLL STATUS
        while (true)
        {
            using (UnityWebRequest request = UnityWebRequest.Get(baseUrl + "/transcript/" + transcriptId))
            {
                request.SetRequestHeader("Authorization", apiKey.Trim());
                #if UNITY_EDITOR
                request.certificateHandler = new BypassCertificate();
                #endif

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    var status = JsonUtility.FromJson<TranscriptStatus>(request.downloadHandler.text);
                    onStatus?.Invoke("Status: " + status.status);

                    if (status.status == "completed")
                    {
                        onComplete?.Invoke(status.text);
                        break;
                    }
                    else if (status.status == "error")
                    {
                        onStatus?.Invoke("AI Error: " + status.error);
                        break;
                    }
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }
}

// Data structures
[Serializable] public class UploadResponse { public string upload_url; }
[Serializable] public class TranscriptResponse { public string id; }
[Serializable] public class TranscriptStatus { public string status; public string text; public string error; }

#if UNITY_EDITOR
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData) { return true; }
}
#endif