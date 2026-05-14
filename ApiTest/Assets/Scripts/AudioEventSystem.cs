using UnityEngine;
using System;

public static class AudioEventSystem
{
    public static event Action<Vector3, float> OnSoundEmitted;
    
    public static void EmitSound(Vector3 position, float intensity = 1f)
    {
        OnSoundEmitted?.Invoke(position, intensity);
        Debug.Log($"🔊 Geluid gedetecteerd op {position} met intensiteit {intensity}");
    }
}
