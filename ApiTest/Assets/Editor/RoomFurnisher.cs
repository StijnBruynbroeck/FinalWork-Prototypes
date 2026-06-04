using UnityEngine;
using UnityEditor;

public class RoomFurnisher : EditorWindow
{
    private const string KitchenName = "Kitchen";
    private const string Kitchen2Name = "Kitchen2";
    private static readonly Vector3 BuildingPos = new Vector3(-83.61f, 0f, 269.01f);

    [MenuItem("Tools/Furnish Rooms/Create Kitchen #&k")]
    static void CreateKitchen()
    {
        GameObject existing = GameObject.Find(KitchenName);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Kitchen exists",
                "A Kitchen room already exists in the scene. Delete and recreate?",
                "Yes, delete and recreate", "Cancel"))
                return;
            DestroyImmediate(existing);
        }

        GameObject kitchen = new GameObject(KitchenName);
        kitchen.transform.position = BuildingPos;
        kitchen.transform.rotation = Quaternion.identity;

        PlaceKitchenFurniture(kitchen);

        Undo.RegisterCreatedObjectUndo(kitchen, "Create Kitchen");
        Selection.activeGameObject = kitchen;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("Kitchen created!");
    }

    [MenuItem("Tools/Furnish Rooms/Create Kitchen 2")]
    static void CreateKitchen2()
    {
        GameObject existing = GameObject.Find(Kitchen2Name);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Kitchen 2 exists",
                "A Kitchen 2 room already exists. Delete and recreate?",
                "Yes, delete and recreate", "Cancel"))
                return;
            DestroyImmediate(existing);
        }

        GameObject kitchen = new GameObject(Kitchen2Name);
        kitchen.transform.position = BuildingPos + new Vector3(5f, 0, -4f);
        kitchen.transform.rotation = Quaternion.identity;

        PlaceKitchen2Furniture(kitchen);

        Undo.RegisterCreatedObjectUndo(kitchen, "Create Kitchen 2");
        Selection.activeGameObject = kitchen;
        SceneView.lastActiveSceneView?.FrameSelected();
        Debug.Log("Kitchen 2 created!");
    }

    [MenuItem("Tools/Furnish Rooms/Delete Kitchen")]
    static void DeleteKitchen()
    {
        GameObject kitchen = GameObject.Find(KitchenName);
        if (kitchen == null)
        {
            EditorUtility.DisplayDialog("No Kitchen", "No Kitchen room found in the scene.", "OK");
            return;
        }

        if (EditorUtility.DisplayDialog("Delete Kitchen", "Delete the Kitchen and all its furniture?", "Yes, delete", "Cancel"))
        {
            DestroyImmediate(kitchen);
            Debug.Log("Kitchen deleted.");
        }
    }

    [MenuItem("Tools/Furnish Rooms/Delete Kitchen", true)]
    static bool ValidateDeleteKitchen()
    {
        return GameObject.Find(KitchenName) != null;
    }

    [MenuItem("Tools/Furnish Rooms/Delete Kitchen 2")]
    static void DeleteKitchen2()
    {
        GameObject kitchen = GameObject.Find(Kitchen2Name);
        if (kitchen == null)
        {
            EditorUtility.DisplayDialog("No Kitchen 2", "No Kitchen 2 room found in the scene.", "OK");
            return;
        }

        if (EditorUtility.DisplayDialog("Delete Kitchen 2", "Delete Kitchen 2 and all its furniture?", "Yes, delete", "Cancel"))
        {
            DestroyImmediate(kitchen);
            Debug.Log("Kitchen 2 deleted.");
        }
    }

    [MenuItem("Tools/Furnish Rooms/Delete Kitchen 2", true)]
    static bool ValidateDeleteKitchen2()
    {
        return GameObject.Find(Kitchen2Name) != null;
    }

    private static void PlaceKitchenFurniture(GameObject parent)
    {
        string k = "Assets/Furniture Mega Pack/Prefabs/Kitchen/";
        string c = "Assets/Furniture Mega Pack/Prefabs/Chairs/";
        string t = "Assets/Furniture Mega Pack/Prefabs/Tables/";

        float cz = -1.5f;

        Place(k + "CabinetA01.prefab", parent, new Vector3(-3.6f, 0, cz), Quaternion.identity);
        Place(k + "CabinetA_Sink.prefab", parent, new Vector3(-1.8f, 0, cz), Quaternion.identity);
        Place(k + "CabinetA01.prefab", parent, new Vector3(0, 0, cz), Quaternion.identity);
        Place(k + "GasStove01.prefab", parent, new Vector3(1.8f, 0, cz), Quaternion.identity);
        Place(k + "Refrigerator01.prefab", parent, new Vector3(3.6f, 0, cz), Quaternion.identity);
        Place(k + "KitchenOven01.prefab", parent, new Vector3(-3.6f, 0, cz + 0.7f), Quaternion.identity);
        Place(k + "MicrowaveOven01.prefab", parent, new Vector3(-1.8f, 0.7f, cz), Quaternion.identity);

        Place(t + "Table05.prefab", parent, new Vector3(0, 0, 2.5f), Quaternion.identity);
        Place(c + "Chair06.prefab", parent, new Vector3(-1.1f, 0, 1.6f), Quaternion.Euler(0, 90, 0));
        Place(c + "Chair06.prefab", parent, new Vector3(1.1f, 0, 1.6f), Quaternion.Euler(0, -90, 0));
        Place(c + "Chair06.prefab", parent, new Vector3(-1.1f, 0, 3.4f), Quaternion.Euler(0, 90, 0));
        Place(c + "Chair06.prefab", parent, new Vector3(1.1f, 0, 3.4f), Quaternion.Euler(0, -90, 0));
    }

    private static void PlaceKitchen2Furniture(GameObject parent)
    {
        string k = "Assets/Furniture Mega Pack/Prefabs/Kitchen/";
        string c = "Assets/Furniture Mega Pack/Prefabs/Chairs/";
        string t = "Assets/Furniture Mega Pack/Prefabs/Tables/";

        float cz = -1.5f;

        Place(k + "CabinetB01.prefab", parent, new Vector3(-2.7f, 0, cz), Quaternion.identity);
        Place(k + "CabinetB_Sink.prefab", parent, new Vector3(-0.9f, 0, cz), Quaternion.identity);
        Place(k + "CabinetB01.prefab", parent, new Vector3(3.6f, 0, 0.5f), Quaternion.Euler(0, -90, 0));
        Place(k + "Refrigerator04.prefab", parent, new Vector3(-3.6f, 0, cz), Quaternion.identity);
        Place(k + "KitchenOven04.prefab", parent, new Vector3(3.6f, 0, 2.2f), Quaternion.Euler(0, -90, 0));
        Place(k + "MicrowaveOven04.prefab", parent, new Vector3(-0.9f, 0.7f, cz), Quaternion.identity);

        Place(k + "GasStove04.prefab", parent, new Vector3(0, 0, 1.5f), Quaternion.identity);
        Place(k + "KitchenExhaust01.prefab", parent, new Vector3(0, 1.2f, 1.5f), Quaternion.identity);
        Place(k + "Sink02.prefab", parent, new Vector3(1.5f, 0, 1.5f), Quaternion.identity);

        Place(t + "Table12.prefab", parent, new Vector3(-0.5f, 0, -2.5f), Quaternion.Euler(0, 90, 0));
        Place(c + "Chair12.prefab", parent, new Vector3(-0.5f, 0, -3.7f), Quaternion.Euler(0, 180, 0));
        Place(c + "Chair12.prefab", parent, new Vector3(-0.5f, 0, -1.3f), Quaternion.identity);
        Place(c + "Chair12.prefab", parent, new Vector3(1.2f, 0, -2.5f), Quaternion.Euler(0, -90, 0));
    }

    private static void Place(string assetPath, GameObject parent, Vector3 localPos, Quaternion localRot)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
        {
            Debug.LogWarning("Prefab not found: " + assetPath);
            return;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent.transform);
        if (instance == null)
        {
            Debug.LogWarning("Failed to instantiate: " + assetPath);
            return;
        }

        instance.transform.localPosition = localPos;
        instance.transform.localRotation = localRot;

        Undo.RegisterCreatedObjectUndo(instance, "Place " + instance.name);
    }
}
