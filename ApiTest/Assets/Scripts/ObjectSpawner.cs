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

    public void ProcessTextAndSpawn(string text, int llmScore, string llmReason, System.Action<string> onStatusUpdate)
    {
        string command = text.ToLower();
        
        // Update de UI met de reden van de AI
        onStatusUpdate?.Invoke($"Score: {llmScore}/100. {llmReason}");

        // We triggeren SOWIESO de vijand als de score te laag is, ongeacht of het object gespawnd kon worden!
        if (llmScore < 50 && enemyAI != null)
        {
            Vector3 playerLocation = Camera.main != null ? Camera.main.transform.position : Vector3.zero;
            enemyAI.InvestigateAnomaly(playerLocation, llmScore);
        }

        // 1. Kleur bepalen
        Color objectColor = Color.white;
        bool colorFound = false;
        if (command.Contains("rood") || command.Contains("red")) { objectColor = Color.red; colorFound = true; }
        else if (command.Contains("blauw") || command.Contains("blue")) { objectColor = Color.blue; colorFound = true; }
        else if (command.Contains("groen") || command.Contains("green")) { objectColor = Color.green; colorFound = true; }

        // 2. Object spawnen als het bestaat
        foreach (var item in spawnableObjects)
        {
            foreach (string keyword in item.keywords)
            {
                if (command.Contains(keyword.ToLower()))
                {
                    SpawnPrefab(item.prefab, objectColor, colorFound);
                    return; // Stop de functie, object is gevonden en gespawnd
                }
            }
        }

        // Als we hier zijn aangekomen, is de hele lijst doorzocht en is er geen match gevonden.
        onStatusUpdate?.Invoke("Object niet herkend.");
    } // <--- Dit is de correcte plek voor de afsluitende accolade van ProcessTextAndSpawn

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