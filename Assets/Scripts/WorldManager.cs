using UnityEngine;

public class WorldManager : MonoBehaviour
{
    // --- HELPER STRUCT ---
    // Müþteriye hem kapýyý hem de duracaðý noktayý paket yapýp veriyoruz
    public struct DoorInfo
    {
        public Door targetDoor;
        public Transform interactionPoint;
    }
    public static WorldManager Instance { get; private set; }

    [Header("Key Locations")]
    public Transform CounterPoint; // Kasanýn önü (Sipariþ verme noktasý)
    public Transform SpawnerPoint; // Müþterilerin belireceði/gireceði kapý önü
    public Transform CounterFacePoint; // Müþterinin vücudunun dönmesi gereken yön (Tezgaha/Player'a dönük)

    [Header("Entrance")]
    public Door FrontDoorRight; // Inspector'dan sahnedeki kapýyý buraya sürükle
    public Door FrontDoorLeft; // Inspector'dan sahnedeki kapýyý buraya sürükle
    public Transform FrontDoorRightEnterPoint; // Dýþarýdaki nokta
    public Transform FrontDoorLeftEnterPoint;  // Dýþarýdaki nokta
    public Transform FrontDoorRightExitPoint;  // Ýçerideki nokta
    public Transform FrontDoorLeftExitPoint;   // Ýçerideki nokta

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    public Transform GetCounterPosition() => CounterPoint;
    public Transform GetSpawnPosition() => SpawnerPoint;
    public Transform GetCounterFacePoint() => CounterFacePoint;

    // Müþteri girerken bunu çaðýracak
    public DoorInfo GetRandomEntryDoor()
    {
        bool isRight = Random.value > 0.5f; // %50 þans
        return new DoorInfo
        {
            targetDoor = isRight ? FrontDoorRight : FrontDoorLeft,
            interactionPoint = isRight ? FrontDoorRightEnterPoint : FrontDoorLeftEnterPoint
        };
    }

    // Müþteri çýkarken bunu çaðýracak
    public DoorInfo GetRandomExitDoor()
    {
        bool isRight = Random.value > 0.5f;
        return new DoorInfo
        {
            targetDoor = isRight ? FrontDoorRight : FrontDoorLeft,
            interactionPoint = isRight ? FrontDoorRightExitPoint : FrontDoorLeftExitPoint
        };
    }
}