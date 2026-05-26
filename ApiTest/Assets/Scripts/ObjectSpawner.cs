using UnityEngine;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Player Morphing")]
    public Transform playerTransform;
    public GameObject playerVisuals;
    
    private GameObject currentProp;
    private bool isMorphed = false;
    private string currentPrefabName;

    [Header("AI & Logic")]
    public NeuroFilterAI enemyAI; 

    public bool IsMorphed => isMorphed;
    public GameObject CurrentProp => currentProp;
    public string CurrentPrefabName => currentPrefabName;

    public void ProcessTextAndSpawn(LLMResult aiResult, System.Action<string> onStatusUpdate)
    {
        onStatusUpdate?.Invoke("Score: " + aiResult.score + "/100. " + aiResult.reason);
        
        string prefabName = aiResult.prefab_name;
        if (string.IsNullOrEmpty(prefabName))
        {
            onStatusUpdate?.Invoke("Transformatie mislukt: AI gaf geen object naam.");
            return;
        }
        prefabName = prefabName.Trim();
        Debug.Log("AI selecteerde object: " + prefabName);

        if (prefabName.ToLower() == "none")
        {
            onStatusUpdate?.Invoke("Transformatie mislukt: Object niet in database.");
            return;
        }

        GameObject loadedPrefab = Resources.Load<GameObject>("Props/" + prefabName);
        if (loadedPrefab == null)
        {
            onStatusUpdate?.Invoke($"Error: Prefab '{prefabName}' bestaat niet in de Resources map.");
            Debug.LogError($"ObjectSpawner: Prefab 'Props/{prefabName}' niet gevonden, spawn geweigerd.");
            return;
        }

        if (currentProp != null)
            Destroy(currentProp);

        currentProp = Instantiate(loadedPrefab, playerTransform);
        currentPrefabName = prefabName;
        currentProp.transform.localPosition = Vector3.zero;
        currentProp.transform.localRotation = Quaternion.identity;

        if (playerVisuals != null)
        {
            Renderer rend = playerVisuals.GetComponentInChildren<Renderer>();
            if (rend != null) rend.enabled = false;
        }

        if (currentProp != null)
        {
            PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
            if (pm != null)
            {
                pm.enabled = false;
                pm.SetMorphMode(true);
            }

            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null)
            {
                cc.enabled = false;
            }

            isMorphed = true;
        }

        onStatusUpdate?.Invoke("Succes! Getransformeerd in " + prefabName + "!");
    }

    public void Unmorph()
    {
        if (!isMorphed || currentProp == null) return;

        Destroy(currentProp);
        currentProp = null;
        currentPrefabName = null;

        if (playerVisuals != null)
        {
            Renderer rend = playerVisuals.GetComponentInChildren<Renderer>();
            if (rend != null) rend.enabled = true;
        }

        PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.SetMorphMode(false);
            pm.enabled = true;
        }

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        if (cc != null)
        {
            cc.enabled = true;
        }

        isMorphed = false;
        Debug.Log("Unmorph: Terug naar menselijk formulier!");
    }


}
