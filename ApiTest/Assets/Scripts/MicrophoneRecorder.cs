using UnityEngine;
using System.IO;
using System.Collections;
using System;

public class MicrophoneRecorder : MonoBehaviour
{
    [Header("Audio Settings")]
    public int recordingFrequency = 16000;
    public int maxRecordingLength = 30;

    private AudioClip recordedClip;
    private string microphoneDevice;
    private int lastSamplePosition = 0;
    private bool isRecording = false;

    public bool IsRecording => isRecording;

    void Start()
    {
        if (Microphone.devices.Length > 0)
        {
            microphoneDevice = Microphone.devices[0];
            Debug.Log("Microfoon: " + microphoneDevice);
        }
        else
        {
            Debug.LogError("Geen microfoon gevonden!");
        }
    }

    public void StartRecording()
    {
        if (isRecording) return;
        isRecording = true;
        lastSamplePosition = 0;
        
        recordedClip = Microphone.Start(microphoneDevice, false, maxRecordingLength, recordingFrequency);
    }

    public void StopRecording(Action<byte[]> onAudioReady)
    {
        if (!isRecording) return;
        isRecording = false;

        if (Microphone.IsRecording(microphoneDevice))
        {
            lastSamplePosition = Microphone.GetPosition(microphoneDevice);
            Microphone.End(microphoneDevice);
        }

        StartCoroutine(ProcessAudio(onAudioReady));
    }

    private IEnumerator ProcessAudio(Action<byte[]> onAudioReady)
    {
        yield return null; 

        if (recordedClip == null || lastSamplePosition <= 0)
        {
                if(recordedClip != null) lastSamplePosition = recordedClip.samples;
            else 
            {
                Debug.LogError("Opname mislukt");
                yield break;
            }
        }

        // Audio trimmen
        float[] fullSamples = new float[recordedClip.samples * recordedClip.channels];
        recordedClip.GetData(fullSamples, 0);
        
        float[] trimmedSamples = new float[lastSamplePosition * recordedClip.channels];
        Array.Copy(fullSamples, trimmedSamples, trimmedSamples.Length);

        // Converteren
        byte[] audioData = ConvertToWAV(trimmedSamples, recordedClip.channels, recordedClip.frequency);
        
        Destroy(recordedClip);
        recordedClip = null;

        onAudioReady?.Invoke(audioData);
    }

    private byte[] ConvertToWAV(float[] samples, int channels, int frequency)
    {
        float[] mono;
        int finalChannels;
        if (channels > 1)
        {
            int sampleCount = samples.Length / channels;
            mono = new float[sampleCount];
            for (int i = 0; i < sampleCount; i++)
            {
                float sum = 0f;
                for (int c = 0; c < channels; c++)
                    sum += samples[i * channels + c];
                mono[i] = sum / channels;
            }
            finalChannels = 1;
        }
        else
        {
            mono = samples;
            finalChannels = channels;
        }

        using (MemoryStream stream = new MemoryStream())
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write("RIFF".ToCharArray());
            writer.Write(36 + mono.Length * 2);
            writer.Write("WAVE".ToCharArray());
            writer.Write("fmt ".ToCharArray());
            writer.Write(16);
            writer.Write((short)1);
            writer.Write((short)finalChannels);
            writer.Write(frequency);
            writer.Write(frequency * finalChannels * 2);
            writer.Write((short)(finalChannels * 2));
            writer.Write((short)16);
            writer.Write("data".ToCharArray());
            writer.Write(mono.Length * 2);

            foreach (var sample in mono) writer.Write((short)(sample * short.MaxValue));
            return stream.ToArray();
        }
    }

    void OnDisable()
    {
        if (isRecording && microphoneDevice != null)
        {
            if (Microphone.IsRecording(microphoneDevice))
            {
                Microphone.End(microphoneDevice);
                Debug.Log("Microfoon netjes afgesloten door OnDisable.");
            }
            isRecording = false;
        }
    }
}