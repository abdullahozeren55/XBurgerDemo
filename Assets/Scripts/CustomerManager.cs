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

    // --- YENÝ: Kasada Bekleyen Müþteri Referansý ---
    // --- DEÐÝÞÝKLÝK: Tekil deðiþken yerine Liste ---
    private List<CustomerController> customersAtCounter = new List<CustomerController>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;
    }

    private IEnumerator Start() // void yerine IEnumerator
    {
        // Bir kare bekle. Bu, tüm Singletonlarýn hazýr olmasýný garantiler.
        yield return null;

        if (CurrentScenario != null)
            StartCoroutine(RunScenarioRoutine());
    }

    // --- YENÝ: Listeye Ekle/Çýkar ---
    public void RegisterCustomerAtCounter(CustomerController customer)
    {
        if (!customersAtCounter.Contains(customer))
        {
            customersAtCounter.Add(customer);
        }
    }

    public void UnregisterCustomerAtCounter(CustomerController customer)
    {
        if (customersAtCounter.Contains(customer))
        {
            customersAtCounter.Remove(customer);
        }
    }

    // --- YENÝ: Tepsi Kontrol Mantýðý ---
    // Zone'dan çaðrýlýr. "Bu tepsiyi isteyen var mý?"
    public bool TryServeTray(Tray tray)
    {
        if (customersAtCounter.Count == 0) return false;

        // --- FIX 1: ERKEN TESLÝMAT KONTROLÜ ---
        // Kasada müþteri var ama "Yemek Bekleyen" (WaitingForFood) modunda kimse yok mu?
        // Yani hepsi henüz "AtCounter" veya "Ordering" aþamasýnda mý?
        bool isAnyoneWaiting = customersAtCounter.Any(x => x.CurrentState == CustomerState.WaitingForFood);

        if (!isAnyoneWaiting)
        {
            // Henüz sipariþ aþamasýndalar. Tepsiyi görmezden gel.
            // Ceza kesme, diyalog oynatma. Sadece "Henüz deðil" de.
            Debug.Log("Müþteriler henüz sipariþ vermedi. Tepsi iþlem görmüyor.");
            return false;
        }
        // --------------------------------------

        // 2. Yemek bekleyenleri tara
        foreach (var customer in customersAtCounter)
        {
            // Sadece yemek bekleyenlere bak
            if (customer.CurrentState == CustomerState.WaitingForFood)
            {
                if (customer.TryReceiveTray(tray))
                {
                    Debug.Log($"Sipariþ baþarýlý! Alan kiþi: {customer.name}");
                    return true;
                }
            }
        }

        // 3. Buraya geldiysek: 
        // a) Kasada "WaitingForFood" olan en az bir kiþi VAR (Yukarýdaki check'i geçti)
        // b) Ama hiçbiri tepsiyi kabul etmedi (Sipariþ içeriði yanlýþ)

        Debug.Log("Biri yemek bekliyor ama bu tepsiyi istemedi. YANLIÞ SÝPARÝÞ!");

        // --- FIX 2: Sadece yemek bekleyen (maðdur) kiþi konuþsun ---
        // AtCounter'da bekleyen adamýn "Sipariþ yanlýþ" demesi saçma olur.
        var victim = customersAtCounter.FirstOrDefault(x => x.CurrentState == CustomerState.WaitingForFood);

        if (victim != null)
        {
            victim.OnWrongOrderReceived();
        }

        return false;
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