using System;
using UnityEngine;

public enum DoorType
{
    ColdRoomDoor,
    BackRoomDoor,
    RestaurantBackDoor,
    KitchenDoor,
}

[Serializable]
public class DoorClass
{
    public DoorType type;
    public Door door;
}
public class EventManager : MonoBehaviour
{
    public static EventManager Instance { get; private set; }

    public DoorClass[] DoorClasses;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void OpenRandomDoor(DoorType[] types)
    {
        int rand = UnityEngine.Random.Range(0, types.Length);

        foreach (DoorClass dc in DoorClasses)
        {
            if (dc.type == types[rand])
            {
                dc.door.HandleRotation(false);
                break;
            }
        }
    }
}
