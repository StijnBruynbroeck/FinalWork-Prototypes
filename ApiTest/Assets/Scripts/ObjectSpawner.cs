using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct VoiceObject
{
    public string objectName;
    public string[] keywords;
    public GameObject prefab;
}

public class ObjectSpawner : MonoBehaviour
{
    [Header("Mijn ItHappy Objecten")]
    public NeuroFilterAI enemyAI;
    public List<VoiceObject> spawnableObjects;

    public void ProcessTextAndSpawn(LLMResult aiResult, System.Action<string> onStatusUpdate)
    {
        // 1. UI Updaten
        onStatusUpdate?.Invoke($"Score: {aiResult.score}/100. {aiResult.reason}");
        
        // 2. VEILIGHEIDSCHECK: Zorg dat lege (null) AI antwoorden de game niet crashen!
        string prefabName = string.IsNullOrEmpty(aiResult.prefab_name) ? "none" : aiResult.prefab_name;

        Debug.Log($"AI selecteerde object: {prefabName}");

        // 3. Controleer of het geldig is
        if (prefabName.ToLower() == "none")
        {
            onStatusUpdate?.Invoke("Transformatie mislukt: Object niet in database.");
            return;
        }

        // 4. Laad de prefab dynamisch in
        // Zorg dat de AI EXACT de naam (bijv. "OfficeChair") uitspuugt, anders faalt de Load!
        GameObject loadedPrefab = Resources.Load<GameObject>("Props/" + prefabName);

        if (loadedPrefab != null)
        {
            // Object succesvol gevonden in de map! Spawnen maar.
            Instantiate(loadedPrefab, Vector3.zero, Quaternion.identity);
            onStatusUpdate?.Invoke($"Succes! {prefabName} ingeladen.");
        }
        else
        {
            // Error handling als de file niet bestaat of de AI de naam verkeerd spelde
            onStatusUpdate?.Invoke($"Error: Prefab '{prefabName}' niet gevonden in de map.");
            Debug.LogError($"Resources.Load faalde voor bestand: Props/{prefabName}. Controleer de bestandsnaam in Unity!");
        }
    }
    private void SpawnPrefab(GameObject prefab, Color color, bool applyColor)
    {
        if (prefab == null) return;

        GameObject obj = Instantiate(prefab);

        if (Camera.main != null)
        {
            Transform cam = Camera.main.transform;
            obj.transform.position = cam.position + (cam.forward * 2f);
            obj.transform.LookAt(new Vector3(cam.position.x, obj.transform.position.y, cam.position.z));
        }
        else
        {
            obj.transform.position = new Vector3(0, 0, 2f);
        }

        if (obj.GetComponent<Rigidbody>() == null) obj.AddComponent<Rigidbody>();

        if (applyColor)
        {
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers) r.material.color = color;
        }
    }
}