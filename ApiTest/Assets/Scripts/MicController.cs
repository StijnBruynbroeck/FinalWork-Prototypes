using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MicController : MonoBehaviour
{
    private AudioSource audioSource;
    private string microphoneDevice;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();

        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0]; 
            Debug.Log("Geselecteerde microfoon: " + microphoneDevice);
            
            StartMicrophone();
        }
        else
        {
            Debug.LogError("Geen microfoon gevonden!");
        }
    }

    void Update()
{
    if (Microphone.IsRecording(microphoneDevice))
    {
        float[] samples = new float[128];
        int micPosition = Microphone.GetPosition(microphoneDevice) - 128 + 1;

        if (micPosition < 0) return; 

        audioSource.clip.GetData(samples, micPosition);

        float level = 0;
        foreach (var sample in samples)
        {
            level += Mathf.Abs(sample); 
        }
        level /= 128;

      
        if (level > 0.01f) 
        {
            Debug.Log("Microfoon Volume: " + level * 100); 
        }
    }
}

    void StartMicrophone()
    {
       
        audioSource.clip = Microphone.Start(microphoneDevice, true, 10, 44100);
        
       
        audioSource.loop = true; 
       
        while (!(Microphone.GetPosition(microphoneDevice) > 0)) { }
        
        audioSource.Play(); 
    }

    void OnDisable()
    {
        // Check of we aan het opnemen zijn en stop het netjes
        if (Microphone.IsRecording(microphoneDevice))
        {
            Microphone.End(microphoneDevice);
            Debug.Log("Microfoon opname gestopt.");
        }
    }
}