using UnityEngine;
using System;

public class ZoneDetectionManager : MonoBehaviour
{
    public ZoneData CurrentZone { get; private set; }

    private void OnTriggerEnter(Collider other)
    {
        var zone = other.GetComponent<ZoneData>();
        if (zone == null) return;

        var data = new ZoneEventData
        {
            zoneId = zone.zoneId,
            zoneType = zone.zoneType,
            zoneTag = other.gameObject.tag,
            playerPosition = transform.position,
            timestamp = Time.time,
            eventType = "Enter",
            furnitureCount = zone.furnitureInZone?.Length ?? 0
        };

        CurrentZone = zone;

        Debug.Log($"ENTERED {other.gameObject.name}  |  Type: [{zone.zoneType}]  |  ID: {zone.zoneId}  |  Tag: {other.gameObject.tag}", other.gameObject);

        zone.OnEnter?.Invoke(data);
    }

    private void OnTriggerExit(Collider other)
    {
        var zone = other.GetComponent<ZoneData>();
        if (zone == null) return;

        var data = new ZoneEventData
        {
            zoneId = zone.zoneId,
            zoneType = zone.zoneType,
            zoneTag = other.gameObject.tag,
            playerPosition = transform.position,
            timestamp = Time.time,
            eventType = "Exit"
        };

        if (CurrentZone == zone) CurrentZone = null;

        Debug.Log($"EXITED {other.gameObject.name}  |  Type: [{zone.zoneType}]  |  ID: {zone.zoneId}", other.gameObject);

        zone.OnExit?.Invoke(data);
    }
}

[System.Serializable]
public class ZoneEventData
{
    public string zoneId;
    public string zoneType;
    public string zoneTag;
    public Vector3 playerPosition;
    public float timestamp;
    public string eventType;
    public int furnitureCount;
}
