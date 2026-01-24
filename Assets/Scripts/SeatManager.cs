using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class SeatManager : MonoBehaviour
{
    public static SeatManager Instance { get; private set; }

    [SerializeField] private List<DiningTable> allTables;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;

        // Sahnedeki tüm masalarý otomatik bul (Opsiyonel)
        if (allTables == null || allTables.Count == 0)
            allTables = FindObjectsOfType<DiningTable>().ToList();
    }

    public DiningTable FindTableForGroup(int groupSize)
    {
        // 1. Kapasitesi yeten ve rezerve edilmemiþ masalarý bul
        var possibleTables = allTables
            .Where(t => t.CanAccommodateGroup(groupSize))
            .ToList();

        if (possibleTables.Count == 0) return null; // Yer yok!

        // 2. En "uygun" masayý seç.
        // Optimizasyon: 2 kiþilik grup için 6 kiþilik masayý ziyan etmeyelim.
        // Kapasitesi grup boyutuna en yakýn olaný seçelim.
        var bestTable = possibleTables
            .OrderBy(t => t.TotalCapacity) // Küçükten büyüðe sýrala (önce 2 kiþilik, sonra 4...)
            .First();

        bestTable.ReserveTable();
        return bestTable;
    }
}