using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class CustomerManager : MonoBehaviour
{
    public static CustomerManager Instance { get; private set; }

    [System.Serializable]
    public struct CharacterReference
    {
        public CustomerID ID;
        public CustomerController Controller; // Sahnedeki GameObject referansý
    }

    [Header("Scene References")]
    // Sahnedeki 8 karakteri buraya tek tek sürükleyip ID'lerini seçeceksin
    public List<CharacterReference> SceneCharacters;

    [Header("Current Level")]
    public LevelScenario CurrentScenario; // GameManager burayý her gün deðiþtirecek

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private void Start()
    {
        // Test için Start'ta baþlatýyoruz, normalde GameManager baþlatýr
        if (CurrentScenario != null)
            StartCoroutine(RunScenarioRoutine());
    }

    private IEnumerator RunScenarioRoutine()
    {
        foreach (var groupData in CurrentScenario.Waves)
        {
            // Bekleme süresi (Opsiyonel: Ýster önceki grup bitince, ister süreyle)
            yield return new WaitForSeconds(groupData.DelayAfterPreviousGroup);

            SpawnGroup(groupData);
        }
    }

    private void SpawnGroup(CustomerGroupData group)
    {
        int groupSize = group.Members.Count;

        // 1. Grup için Masa Bul
        DiningTable allocatedTable = SeatManager.Instance.FindTableForGroup(groupSize);

        if (allocatedTable == null)
        {
            Debug.LogError($"Grup '{group.GroupName}' için uygun masa bulunamadý! Rezillik!");
            // Fallback: Belki bekletiriz veya dükkandan kýzýp giderler.
            return;
        }

        // 2. Grubun üyelerini hazýrla ve gönder
        foreach (var assignment in group.Members)
        {
            // ID'den sahnedeki objeyi bul
            var charRef = SceneCharacters.FirstOrDefault(x => x.ID == assignment.ID);

            if (charRef.Controller != null)
            {
                CustomerController customer = charRef.Controller;

                // Pozisyonu resetle (Kapý önüne koy)
                customer.transform.position = WorldManager.Instance.GetSpawnPosition().position;
                customer.gameObject.SetActive(true);

                // Masasýný ata
                customer.AssignTable(allocatedTable);

                // Profilini yükle ve baþlat
                customer.Initialize(assignment.ProfileForToday);
            }
        }
    }
}