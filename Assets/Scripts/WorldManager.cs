using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public static WorldManager Instance { get; private set; }

    [Header("Key Locations")]
    public Transform CounterPoint; // Kasanýn önü (Sipariþ verme noktasý)
    public Transform ExitPoint;    // Dükkan çýkýþý (Eve dönüþ)
    public Transform SpawnerPoint; // Müþterilerin belireceði/gireceði kapý önü
    public Transform CounterFacePoint; // Müþterinin vücudunun dönmesi gereken yön (Tezgaha/Player'a dönük)

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    public Transform GetCounterPosition() => CounterPoint;
    public Transform GetExitPosition() => ExitPoint;
    public Transform GetSpawnPosition() => SpawnerPoint;
    public Transform GetCounterFacePoint() => CounterFacePoint;
}