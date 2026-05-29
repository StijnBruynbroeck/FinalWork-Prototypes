using UnityEngine;
using System.Collections;
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
            onStatusUpdate?.Invoke("Transform failed: AI gave no object name.");
            return;
        }
        prefabName = prefabName.Trim();
        Debug.Log("AI selecteerde object: " + prefabName);

        if (prefabName.ToLower() == "none")
        {
            onStatusUpdate?.Invoke("Transform failed: Object not in database.");
            return;
        }

        GameObject loadedPrefab = Resources.Load<GameObject>("Props/" + prefabName);
        if (loadedPrefab == null)
        {
            onStatusUpdate?.Invoke($"Error: Prefab '{prefabName}' not found in Resources.");
            Debug.LogError($"ObjectSpawner: Prefab 'Props/{prefabName}' niet gevonden, spawn geweigerd.");
            return;
        }

        if (currentProp != null)
            Destroy(currentProp);

        Vector3 spawnPos = playerTransform.position;
        currentProp = Instantiate(loadedPrefab, spawnPos, Quaternion.identity);
        currentPrefabName = prefabName;

        StartCoroutine(SnapToGround(currentProp));

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

        onStatusUpdate?.Invoke("Success! Transformed into " + prefabName + "!");
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

    private IEnumerator SnapToGround(GameObject obj)
    {
        yield return null;

        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) yield break;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        float pivotToBottom = obj.transform.position.y - bounds.min.y;

        CharacterController cc = playerTransform.GetComponent<CharacterController>();
        float charHalfHeight = cc != null ? cc.height * 0.5f : 1f;
        float charRadius = cc != null ? cc.radius : 0.5f;
        Vector3 rayOrigin = playerTransform.position - new Vector3(0, charHalfHeight + charRadius + 0.1f, 0);

        RaycastHit hit;
        float groundY;

        if (Physics.Raycast(rayOrigin, Vector3.down, out hit, 10f))
        {
            groundY = hit.point.y;
            Debug.Log($"SnapToGround: raycast hit {hit.collider.name} at Y={hit.point.y}");
        }
        else
        {
            groundY = 0f;
            Debug.Log($"SnapToGround: raycast missed, fallback to Y=0");
        }

        obj.transform.position = new Vector3(
            obj.transform.position.x,
            groundY + pivotToBottom,
            obj.transform.position.z
        );
        Debug.Log($"SnapToGround: groundY={groundY}, pivotToBottom={pivotToBottom}, finalY={obj.transform.position.y}");
    }
}
