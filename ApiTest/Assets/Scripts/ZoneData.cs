using UnityEngine;
using System;

public class ZoneData : MonoBehaviour
{
    public string zoneId;
    public string zoneType;
    public GameObject[] furnitureInZone;

    public Action<ZoneEventData> OnEnter;
    public Action<ZoneEventData> OnExit;
}
