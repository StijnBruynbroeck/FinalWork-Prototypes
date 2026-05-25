using UnityEngine;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Player Morphing")]
    public Transform playerTransform;
    public GameObject playerVisuals;
    
    private GameObject currentProp;
    private bool isMorphed = false;

    [Header("AI & Logic")]
    public NeuroFilterAI enemyAI; 

    public bool IsMorphed => isMorphed;
    public GameObject CurrentProp => currentProp;

    public void ProcessTextAndSpawn(LLMResult aiResult, System.Action<string> onStatusUpdate)
    {
        onStatusUpdate?.Invoke("Score: " + aiResult.score + "/100. " + aiResult.reason);
        
        string prefabName = string.IsNullOrEmpty(aiResult.prefab_name) ? "none" : aiResult.prefab_name;
        Debug.Log("AI selecteerde object: " + prefabName);

        if (prefabName.ToLower() == "none")
        {
            onStatusUpdate?.Invoke("Transformatie mislukt: Object niet in database.");
            return;
        }

        GameObject loadedPrefab = Resources.Load<GameObject>("Props/" + prefabName);

        if (loadedPrefab != null)
        {
            if (currentProp != null)
            {
                Destroy(currentProp);
            }

            currentProp = Instantiate(loadedPrefab, playerTransform);
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
                    pm.SwitchCamera(false);
                    pm.enabled = false;
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
        else
        {
            onStatusUpdate?.Invoke("Error: Prefab '" + prefabName + "' niet gevonden in de map.");
            Debug.LogError("Resources.Load faalde voor bestand: Props/" + prefabName + ".");
        }
    }

    public void Unmorph()
    {
        if (!isMorphed || currentProp == null) return;

        Destroy(currentProp);
        currentProp = null;

        if (playerVisuals != null)
        {
            Renderer rend = playerVisuals.GetComponentInChildren<Renderer>();
            if (rend != null) rend.enabled = true;
        }

        PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.enabled = true;
            pm.SwitchCamera(true);
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
