using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Player Morphing")]
    public Transform playerTransform; // Sleep Qwinte's Player object hierin!
    public GameObject playerVisuals;  // Sleep de menselijke capsule van de speler hierin!
    
    private GameObject currentProp;   // Houdt bij in welk object we momenteel veranderd zijn

    [Header("AI & Logic")]
     public NeuroFilterAI enemyAI; 

    public void ProcessTextAndSpawn(LLMResult aiResult, System.Action<string> onStatusUpdate)
    {
        // 1. UI Updaten
        onStatusUpdate?.Invoke($"Score: {aiResult.score}/100. {aiResult.reason}");
        
        // 2. Veiligheidscheck op lege JSON velden
        string prefabName = string.IsNullOrEmpty(aiResult.prefab_name) ? "none" : aiResult.prefab_name;
        Debug.Log($"AI selecteerde object: {prefabName}");

        // 3. Controleer of het object geldig is
        if (prefabName.ToLower() == "none")
        {
            onStatusUpdate?.Invoke("Transformatie mislukt: Object niet in database.");
            return;
        }

        // 4. Laad de prefab dynamisch in
        GameObject loadedPrefab = Resources.Load<GameObject>("Props/" + prefabName);

        if (loadedPrefab != null)
        {
            // A. Als we al een prop waren, verwijder die oude prop dan eerst
            if (currentProp != null)
            {
                Destroy(currentProp);
            }

            // B. Spawn de nieuwe prop en maak hem een 'kind' van Qwinte's Player object
            currentProp = Instantiate(loadedPrefab, playerTransform);
            
            // C. Zet de prop netjes in het midden van de speler
            currentProp.transform.localPosition = Vector3.zero;
            currentProp.transform.localRotation = Quaternion.identity;

            // D. START DE HOLOGRAM ANIMATIE! (Dit vervangt de oude harde switch)
            if (playerVisuals != null && currentProp != null)
            {
                // Zet ALLEEN de renderer (het zichtbare model) weer aan, niet het hele object
                Renderer rend = playerVisuals.GetComponentInChildren<Renderer>();
                if (rend != null) rend.enabled = true; 
                
                StartCoroutine(SpeelYouTubeHoloMorphAf(playerVisuals, currentProp));
            }

            

            onStatusUpdate?.Invoke($"Succes! Getransformeerd in {prefabName}!");
            
        }
        else
        {
            onStatusUpdate?.Invoke($"Error: Prefab '{prefabName}' niet gevonden in de map.");
            Debug.LogError($"Resources.Load faalde voor bestand: Props/{prefabName}.");
        }
    }

    private IEnumerator SpeelYouTubeHoloMorphAf(GameObject oldBody, GameObject newProp)
    {
        Renderer oldRend = oldBody.GetComponentInChildren<Renderer>();
        Renderer newRend = newProp.GetComponentInChildren<Renderer>();

        // Start: Speler is zichtbaar (0), nieuwe prop is onzichtbaar/opgelost (1)
        if (oldRend != null) oldRend.material.SetFloat("_DissolveAmount", 0f);
        if (newRend != null) newRend.material.SetFloat("_DissolveAmount", 1f);

        float t = 0f;
        
        // De animatie loop die ongeveer een seconde duurt
        while (t < 1f)
        {
            t += Time.deltaTime * 1.5f; // Dit is de snelheid
            
            if (oldRend != null) oldRend.material.SetFloat("_DissolveAmount", t);
            if (newRend != null) newRend.material.SetFloat("_DissolveAmount", 1f - t);
            
            yield return null;
        }

        // Zorg dat de waarden exact eindigen op hun limiet
        if (oldRend != null) oldRend.material.SetFloat("_DissolveAmount", 1f);
        if (newRend != null) newRend.material.SetFloat("_DissolveAmount", 0f);
if (oldRend != null) oldRend.enabled = false;

PlayerMovement pm = playerTransform.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.SwitchCamera(false); 
        }
    }
}