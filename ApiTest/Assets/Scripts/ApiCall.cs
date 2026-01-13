using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic; // BELANGRIJK: Nodig voor List<>
using System.IO;
using TMPro;
using UnityEngine.InputSystem;
using System;
using System.Net; 

// DIT IS HET BLOKJE DAT JE STRAKS IN DE INSPECTOR ZIET
[System.Serializable]
public struct VoiceObject
{
    public string objectName;      // Naam voor jezelf (bijv. "Bank")
    public string[] keywords;      // Woorden: "bank", "sofa", "couch"
    public GameObject prefab;      // Sleep hier je ItHappy prefab in
}

public class ApiCall : MonoBehaviour
{
    // 🔥 PAS OP: VRAAG EEN NIEUWE KEY AAN BIJ ASSEMBLYAI, DEZE IS GELEKT!
    private string apiKey = "c786b69a434f4bdb90be39b94ed56a14"; 
    private string baseUrl = "https://api.assemblyai.com/v2"; 
    
    // --- NIEUW: HIER KOMEN JE PREFABS IN TE STAAN ---
    [Header("Mijn ItHappy Objecten")]
    public List<VoiceObject> spawnableObjects; 

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
    private int lastSamplePosition = 0;
    
    void Start()
    {
        // Netwerk beveiliging fix voor Unity
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
        lastSamplePosition = 0;
        UpdateStatus("Recording...");
        
        recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingLength, recordingFrequency);
    }
    
    void StopRecording()
    {
        if (!isRecording) return;
        isRecording = false;
        
        if (Microphone.IsRecording(microphoneDevice))
        {
            lastSamplePosition = Microphone.GetPosition(microphoneDevice);
            Microphone.End(microphoneDevice);
        }
        
        UpdateStatus("Processing...");
        StartCoroutine(ProcessRecording());
    }
    
    IEnumerator ProcessRecording()
    {
        yield return null; 

        if (recordedClip == null) yield break;
        
        if (lastSamplePosition <= 0) lastSamplePosition = recordedClip.samples;

        // Audio trimmen (zodat het bestand klein blijft)
        float[] fullSamples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(fullSamples, 0);
        
        float[] trimmedSamples = new float[lastSamplePosition * recordedClip.channels];
        Array.Copy(fullSamples, trimmedSamples, trimmedSamples.Length);

        Debug.Log($"Audio getrimd: {fullSamples.Length} -> {trimmedSamples.Length}");

        byte[] audioData = ConvertToWAV(trimmedSamples, recordedClip.channels, recordedClip.frequency);
        
        Destroy(recordedClip);
        recordedClip = null;
        
        yield return StartCoroutine(SendToAssemblyAI(audioData));
    }

    IEnumerator SendToAssemblyAI(byte[] audioData)
    {
        UpdateStatus("Uploading...");
        
        if (audioData.Length == 0) yield break;

        string uploadEndpoint = baseUrl + "/upload";
        
        using (UnityWebRequest request = new UnityWebRequest(uploadEndpoint, "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(audioData);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // Instellingen tegen Error 55
            request.chunkedTransfer = false; 
            request.SetRequestHeader("Authorization", apiKey.Trim());
            request.SetRequestHeader("Content-Type", "application/octet-stream");
            request.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) Chrome/91.0.4472.124 Safari/537.36");
            request.SetRequestHeader("Connection", "close");

            #if UNITY_EDITOR
            request.certificateHandler = new BypassCertificate();
            #endif

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<UploadResponse>(request.downloadHandler.text);
                Debug.Log("Upload gelukt! URL: " + response.upload_url);
                yield return StartCoroutine(StartTranscription(response.upload_url));
            }
            else
            {
                Debug.LogError($"Upload Error: {request.error}");
                UpdateStatus($"Upload Failed: {request.error}");
            }
        }
    }

    IEnumerator StartTranscription(string audioUrl)
    {
        UpdateStatus("Transcribing...");
        
        // Taal op Nederlands gezet (werkt beter voor nl spraak)
        string json = "{\"audio_url\": \"" + audioUrl + "\", \"language_code\": \"nl\"}";
        string transcriptEndpoint = baseUrl + "/transcript";
        
        using (UnityWebRequest request = new UnityWebRequest(transcriptEndpoint, "POST"))
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
                request.SetRequestHeader("Authorization", apiKey.Trim());
                
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
                        
                        // HIER STARTEN WE DE LOGICA
                        ProcessVoiceCommand(status.text);
                        break;
                    }
                    else if (status.status == "error")
                    {
                        UpdateStatus("Error: " + status.error);
                        break;
                    }
                }
            }
            yield return new WaitForSeconds(1f);
        }
    }

    // --- DE NIEUWE LOGICA VOOR JOUW LIJST ---
    void ProcessVoiceCommand(string text)
    {
        string command = text.ToLower();
        Debug.Log("Commando analyseren: " + command);

        // 1. Kleur bepalen
        Color objectColor = Color.white;
        bool colorFound = false;
        
        if (command.Contains("rood") || command.Contains("red")) { objectColor = Color.red; colorFound = true; }
        else if (command.Contains("blauw") || command.Contains("blue")) { objectColor = Color.blue; colorFound = true; }
        else if (command.Contains("groen") || command.Contains("green")) { objectColor = Color.green; colorFound = true; }
        else if (command.Contains("geel") || command.Contains("yellow")) { objectColor = Color.yellow; colorFound = true; }
        else if (command.Contains("zwart") || command.Contains("black")) { objectColor = Color.black; colorFound = true; }

        // 2. Zoeken in jouw Inspector Lijst
        bool itemFound = false;

        foreach (var item in spawnableObjects)
        {
            // Check alle keywords die jij bij dit object hebt gezet
            foreach (string keyword in item.keywords)
            {
                if (command.Contains(keyword.ToLower()))
                {
                    SpawnItHappyObject(item.prefab, objectColor, colorFound);
                    UpdateStatus($"Spawned: {item.objectName}");
                    itemFound = true;
                    return; // Stop met zoeken, we hebben hem!
                }
            }
        }

        if (!itemFound)
        {
            UpdateStatus("Object niet herkend. Staat het in de lijst?");
        }
    }

    void SpawnItHappyObject(GameObject prefab, Color color, bool applyColor)
    {
        if (prefab == null) return;

        // Maak object aan
        GameObject obj = Instantiate(prefab);
        
        // Zet positie (voor camera)
        if (Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            // Zet 2 meter voor de camera
            Vector3 spawnPos = cam.position + (cam.forward * 2f);
            
            // Zorg dat hij niet in de lucht zweeft als je naar boven kijkt (reset Y naar vloer niveau indien gewenst)
            // Maar voor nu zetten we hem gewoon voor je neus:
            obj.transform.position = spawnPos; 
            
            // Draai object naar jou toe
            obj.transform.LookAt(new Vector3(cam.position.x, obj.transform.position.y, cam.position.z));
        }
        else
        {
            obj.transform.position = new Vector3(0, 0, 2f);
        }

        // Fysica toevoegen
        if (obj.GetComponent<Rigidbody>() == null) obj.AddComponent<Rigidbody>();
        
        // Kleur toepassen op ItHappy prefabs (die hebben vaak meerdere onderdelen)
        if (applyColor)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.material.color = color;
            }
        }
    }

    void UpdateStatus(string msg)
    {
        if (statusText != null) statusText.text = msg;
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

            foreach (var sample in samples) writer.Write((short)(sample * short.MaxValue));
            return stream.ToArray();
        }
    }
}

// JSON HELPERS
[System.Serializable] public class UploadResponse { public string upload_url; }
[System.Serializable] public class TranscriptResponse { public string id; }
[System.Serializable] public class TranscriptStatus { public string status; public string text; public string error; }

#if UNITY_EDITOR
public class BypassCertificate : CertificateHandler
{
    protected override bool ValidateCertificate(byte[] certificateData)
    {
        return true;
    }
}
#endif