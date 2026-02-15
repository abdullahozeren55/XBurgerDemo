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
    public Cooler Cooler;
    [Space]
    public AudioClip scaryFootstep;
    public Transform scaryFoostepPoint;
    [Space]

    public GameObject kitchenPlayerBlockerTrigger;
    public GameObject kitchenPlayerBlockers;

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

    public void OpenCoolerDoor()
    {
        Cooler.HandleRotation();
    }

    public void PlayScaryFootstep()
    {
        SoundManager.Instance.PlaySoundFX(scaryFootstep, scaryFoostepPoint);
    }

    public void TurnOnKitchenBlockerTrigger()
    {
        if (kitchenPlayerBlockerTrigger != null)
            kitchenPlayerBlockerTrigger.SetActive(true);
    }

    public void MakeKitchenUnleavable()
    {
        kitchenPlayerBlockers.SetActive(true);

        foreach (DoorClass dc in DoorClasses)
        {
            if (dc.type == DoorType.KitchenDoor)
            {
                if (dc.door.isOpened)
                {
                    dc.door.HandleRotation(false);
                }

                dc.door.CanInteract = false;

                break;
            }
        }
    }

    public void PlayRandomHorrorMusic()
    {
        MonitorManager.Instance.PlayRandomHorrorTrack();
        CustomerManager.Instance.DisableAllCustomers();
        Clown.Instance.gameObject.SetActive(false);
        Clown.Instance.ShouldBeSad = true;
    }
}
