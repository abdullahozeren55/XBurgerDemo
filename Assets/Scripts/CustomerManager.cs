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
        public CustomerController Controller;
    }

    [Header("Scene References")]
    public List<CharacterReference> SceneCharacters;

    // Kasadakiler Listesi
    private readonly List<CustomerController> customersAtCounter = new List<CustomerController>();
    public List<CustomerController> GetCustomersAtCounter() => customersAtCounter;

    // --- EVENTLER ---
    public event System.Action<CustomerController> OnCustomerArrivedAtCounter;
    public event System.Action<CustomerController> OnCustomerLeftCounter;

    // Wave bitti (Sýradaki grup için ScenarioManager'a haber verir)
    public event System.Action OnWaveCompleted;

    // --- KONTROL DEÐÝÞKENLERÝ ---
    private bool isSpawningActive = false;
    private int remainingWaveMembers = 0; // BU DALGADA ÝÞÝ BÝTMEYEN KAÇ KÝÞÝ KALDI?

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // Monitor güncelleme vs. aynen kalýyor
    public void UpdateMonitorWithGroupOrders()
    {
        var customers = GetCustomersAtCounter();
        if (customers == null || customers.Count == 0)
        {
            MonitorManager.Instance.ClearCurrentOrder();
            return;
        }

        List<OrderData> groupOrders = new List<OrderData>();
        foreach (var customer in customers)
        {
            OrderData individualOrder = customer.GetCurrentOrder();
            if (individualOrder != null)
                groupOrders.Add(individualOrder);
        }
        MonitorManager.Instance.SetCurrentOrder(groupOrders);
    }

    public void RegisterCustomerAtCounter(CustomerController customer)
    {
        if (!customersAtCounter.Contains(customer))
        {
            customersAtCounter.Add(customer);
            OnCustomerArrivedAtCounter?.Invoke(customer);
        }
    }

    // --- BURASI KRÝTÝK DEÐÝÞÝKLÝK ---
    public void UnregisterCustomerAtCounter(CustomerController customer)
    {
        if (!customersAtCounter.Contains(customer)) return;

        customersAtCounter.Remove(customer);
        UpdateMonitorWithGroupOrders();

        // Müþteri kasadan ayrýldý
        OnCustomerLeftCounter?.Invoke(customer);

        // Bu gruptan bir kiþi eksildi (ister yemeðini aldý gitti, ister sinirlendi gitti)
        if (remainingWaveMembers > 0)
        {
            remainingWaveMembers--;
        }

        // Kontrol et: Wave bitti mi?
        CheckIfWaveCompleted();
    }

    public bool TryServeTray(Tray tray)
    {
        // Burasý senin orijinal kodun, aynen koruyoruz
        if (customersAtCounter.Count == 0) return false;

        bool isAnyoneWaiting = customersAtCounter.Any(x => x.CurrentState == CustomerState.WaitingForFood);
        if (!isAnyoneWaiting) return false;

        var victim = customersAtCounter
            .Where(x => x.CurrentState == CustomerState.WaitingForFood)
            .OrderBy(x => (int)x.CurrentProfile.ID)
            .FirstOrDefault();

        if (tray.CurrentContent.IsEmpty())
        {
            if (victim != null) victim.OnEmptyTrayReceived();
            return false;
        }

        foreach (var customer in customersAtCounter)
        {
            if (customer.CurrentState == CustomerState.WaitingForFood)
            {
                if (customer.TryReceiveTray(tray)) return true;
            }
        }

        if (victim != null) victim.OnWrongOrderReceived();
        return false;
    }

    // --- YENÝ SPAWN MANTIÐI ---
    public void StartWaveSpawn(CustomerGroupData group)
    {
        // ÖNEMLÝ: Wave baþlarken kaç kiþi bekleyeceðimizi not ediyoruz.
        // Böylece kasaya varmalarý sürse bile "Wave bitti" sanmýyoruz.
        remainingWaveMembers = group.Members.Count;

        StartCoroutine(SpawnGroupRoutine(group));
    }

    private IEnumerator SpawnGroupRoutine(CustomerGroupData group)
    {
        isSpawningActive = true;

        int groupSize = group.Members.Count;
        DiningTable allocatedTable = SeatManager.Instance.FindTableForGroup(groupSize);

        if (allocatedTable == null)
        {
            Debug.LogError($"Grup '{group.GroupName}' için masa yok! Wave iptal.");
            // Masa yoksa bu wave'i iptal et, "bitti" say ki oyun týkanmasýn
            remainingWaveMembers = 0;
            isSpawningActive = false;
            OnWaveCompleted?.Invoke();
            yield break;
        }

        float counterSpacing = 0.8f;
        float totalCounterWidth = (groupSize - 1) * counterSpacing;
        float startCounterX = -totalCounterWidth / 2f;

        float spawnSpacing = 0.6f;
        float totalSpawnWidth = (groupSize - 1) * spawnSpacing;
        float startSpawnX = -totalSpawnWidth / 2f;
        Transform spawnOrigin = WorldManager.Instance.GetSpawnPosition();

        List<CustomerController> currentGroupControllers = new List<CustomerController>();

        for (int i = 0; i < groupSize; i++)
        {
            // Event sistemi duraklattýysa bekle
            while (ScenarioManager.Instance.IsSpawningPaused) yield return null;

            // Ufak bir doðal gecikme (robot gibi ayný anda çýkmasýnlar)
            yield return new WaitForSeconds(Random.Range(0.2f, 0.5f));

            // Tekrar kontrol (Delay sýrasýnda pause gelmiþ olabilir)
            while (ScenarioManager.Instance.IsSpawningPaused) yield return null;

            var assignment = group.Members[i];
            var charRef = SceneCharacters.FirstOrDefault(x => x.ID == assignment.ID);

            if (charRef.Controller != null)
            {
                CustomerController customer = charRef.Controller;
                currentGroupControllers.Add(customer);

                float myCounterOffset = startCounterX + (i * counterSpacing);
                customer.SetCounterOffset(myCounterOffset);

                float mySpawnOffset = startSpawnX + (i * spawnSpacing);

                Vector3 targetSpawnPos = spawnOrigin.position + (spawnOrigin.right * mySpawnOffset);
                UnityEngine.AI.NavMeshHit hit;
                if (UnityEngine.AI.NavMesh.SamplePosition(targetSpawnPos, out hit, 2.0f, UnityEngine.AI.NavMesh.AllAreas))
                    customer.transform.position = hit.position;
                else
                    customer.transform.position = spawnOrigin.position;

                customer.gameObject.SetActive(true);
                customer.AssignTable(allocatedTable);
                customer.Initialize(assignment.ProfileForToday);
            }
        }

        foreach (var member in currentGroupControllers)
            member.SetGroupMembers(currentGroupControllers);

        isSpawningActive = false;

        // Spawn bitti ama wave bitti mi? 
        // HAYIR, çünkü remainingWaveMembers hala > 0 (Daha yemek yiyecekler)
        // O yüzden burada OnWaveCompleted çaðýrmýyoruz, onu UnregisterCustomerAtCounter yapacak.
    }

    private void CheckIfWaveCompleted()
    {
        // 1. Spawn iþlemi bitmiþ olmalý
        // 2. Bu gruptaki herkes kasadan iþlemini bitirip ayrýlmýþ olmalý (remainingWaveMembers == 0)

        if (!isSpawningActive && remainingWaveMembers <= 0)
        {
            OnWaveCompleted?.Invoke();
        }
    }
}