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
    public List<VoiceObject> spawnableObjects;

    public void ProcessTextAndSpawn(string text, System.Action<string> onStatusUpdate)
    {
        string command = text.ToLower();
        Debug.Log("Analyseren: " + command);

        // 1. Kleur bepalen
        Color objectColor = Color.white;
        bool colorFound = false;

        if (command.Contains("rood") || command.Contains("red")) { objectColor = Color.red; colorFound = true; }
        else if (command.Contains("blauw") || command.Contains("blue")) { objectColor = Color.blue; colorFound = true; }
        else if (command.Contains("groen") || command.Contains("green")) { objectColor = Color.green; colorFound = true; }
        else if (command.Contains("geel") || command.Contains("yellow")) { objectColor = Color.yellow; colorFound = true; }
        else if (command.Contains("zwart") || command.Contains("black")) { objectColor = Color.black; colorFound = true; }

        // 2. Zoeken in lijst
        foreach (var item in spawnableObjects)
        {
            foreach (string keyword in item.keywords)
            {
                if (command.Contains(keyword.ToLower()))
                {
                    SpawnPrefab(item.prefab, objectColor, colorFound);
                    onStatusUpdate?.Invoke($"Spawned: {item.objectName}");
                    return;
                }
            }
        }

        onStatusUpdate?.Invoke("Object niet herkend.");
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