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

    private int lastSamplePosition = 0;
    
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
        
        // 1. Onthoud waar de microfoon was toen we stopten
        if (Microphone.IsRecording(microphoneDevice))
        {
            // Dit geeft ons het aantal samples dat echt is opgenomen
            lastSamplePosition = Microphone.GetPosition(microphoneDevice);
            Microphone.End(microphoneDevice);
        }
        
        UpdateStatus("Processing...");
        StartCoroutine(ProcessRecording());
    }
    
    IEnumerator ProcessRecording()
    {
        yield return null; // Wacht 1 frame

        if (recordedClip == null) yield break;
        
        // VEILIGHEIDSCHECK: Als lastSamplePosition 0 is (te snel geklikt), pakken we alles
        if (lastSamplePosition <= 0) lastSamplePosition = recordedClip.samples;

        // 1. Haal ALLE data op (dit moet in Unity)
        float[] fullSamples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(fullSamples, 0);
        
        // 2. Maak een nieuwe array die ALLEEN het opgenomen stukje bevat (TRIM)
        // We vermenigvuldigen met channels (meestal 1) voor de zekerheid
        float[] trimmedSamples = new float[lastSamplePosition * recordedClip.channels];
        
        // Kopieer alleen het stuk dat we nodig hebben van 'full' naar 'trimmed'
        System.Array.Copy(fullSamples, trimmedSamples, trimmedSamples.Length);

        Debug.Log($"Originele lengte: {fullSamples.Length}, Getrimde lengte: {trimmedSamples.Length}");

        // 3. Converteren naar WAV bytes met de getrimde array
        byte[] audioData = ConvertToWAV(trimmedSamples, recordedClip.channels, recordedClip.frequency);
        
        // 4. Opruimen
        Destroy(recordedClip);
        recordedClip = null;
        
        // 5. Uploaden
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
        request.SetRequestHeader("Connection", "close");

        #if UNITY_EDITOR
      
        var certHandler = new BypassCertificate();
        request.certificateHandler = certHandler;
        #endif

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
                        ProcessVoiceCommand(status.text);
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

   void ProcessVoiceCommand(string text)
    {
        string command = text.ToLower();
        Debug.Log("Commando analyseren: " + command);

        // STAP 1: Bepaal de Kleur
        // We scannen de zin eerst op kleuren. Standaard is wit.
        Color objectColor = Color.white; 
        
        if (command.Contains("rood") || command.Contains("red")) objectColor = Color.red;
        else if (command.Contains("blauw") || command.Contains("blue")) objectColor = Color.blue;
        else if (command.Contains("groen") || command.Contains("green")) objectColor = Color.green;
        else if (command.Contains("geel") || command.Contains("yellow")) objectColor = Color.yellow;
        else if (command.Contains("zwart") || command.Contains("black")) objectColor = Color.black;

        // STAP 2: Bepaal Fysica (Rigidbody)
        // Standaard zetten we fysica AAN (vallen). 
        // Als we woorden horen als "zweef", "float", "static", zetten we het UIT.
        bool usePhysics = true;

        if (command.Contains("zweef") || command.Contains("float") || 
            command.Contains("statisch") || command.Contains("static") || 
            command.Contains("vast"))
        {
            usePhysics = false;
        }

        // STAP 3: Bepaal de Vorm en maak het object met de gekozen instellingen
        if (command.Contains("box") || command.Contains("cube") || command.Contains("kubus") || command.Contains("vierkant"))
        {
            SpawnObject(PrimitiveType.Cube, objectColor, usePhysics);
            UpdateStatus($"Maakte een {GetColorName(objectColor)} Kubus");
        }
        else if (command.Contains("sphere") || command.Contains("ball") || command.Contains("bal") || command.Contains("bol"))
        {
            SpawnObject(PrimitiveType.Sphere, objectColor, usePhysics);
            UpdateStatus($"Maakte een {GetColorName(objectColor)} Bol");
        }
        else if (command.Contains("capsule") || command.Contains("pil"))
        {
            SpawnObject(PrimitiveType.Capsule, objectColor, usePhysics);
            UpdateStatus($"Maakte een {GetColorName(objectColor)} Capsule");
        }
        else
        {
            UpdateStatus("Geen vorm herkend, probeer: 'Rode kubus' of 'Zwevende bal'");
        }
    }

    // De functie accepteert nu extra parameters: kleur en fysica
    void SpawnObject(PrimitiveType type, Color color, bool usePhysics)
    {
        GameObject obj = GameObject.CreatePrimitive(type);
        
        // 1. Positie instellen (voor de camera)
        if (Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            obj.transform.position = cam.position + (cam.forward * 2f);
            obj.transform.rotation = cam.rotation;
        }
        else
        {
            obj.transform.position = new Vector3(0, 2f, 2f);
        }
        
        // 2. Kleur toepassen
        // We halen de Renderer op en passen de material kleur aan
        var renderer = obj.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material.color = color;
        }

        // 3. Fysica toepassen (alleen als usePhysics true is)
        if (usePhysics)
        {
            Rigidbody rb = obj.AddComponent<Rigidbody>();
            rb.mass = 1.0f; // Standaard gewicht
        }
        
        // Grootte aanpassen
        obj.transform.localScale = Vector3.one * 0.5f; 
    }

    // Een klein hulpje om de naam van de kleur te printen in de UI
    string GetColorName(Color c)
    {
        if (c == Color.red) return "Rode";
        if (c == Color.blue) return "Blauwe";
        if (c == Color.green) return "Groene";
        if (c == Color.yellow) return "Gele";
        if (c == Color.black) return "Zwarte";
        return "Witte";
    }
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