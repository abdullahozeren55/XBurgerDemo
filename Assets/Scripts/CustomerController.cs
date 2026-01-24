using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening; // Tweenler için þart

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(DialogueSpeaker))]
[RequireComponent(typeof(Animator))]
public class CustomerController : MonoBehaviour, ICustomer, IInteractable
{
    [Header("Runtime Data")]
    [SerializeField] private CustomerProfile currentProfile;
    [SerializeField] private OrderData currentOrder;

    [Header("Settings")]
    [SerializeField] private Transform trayHoldPoint; // Tepsiyi tutacaðý nokta (Göðüs kemiðinin altýnda bir empty obje)
    [SerializeField] private float headLookWeight = 1f; // Kafa çevirme hýzý/aðýrlýðý

    // State Machine
    public CustomerState CurrentState { get; private set; }

    // IInteractable Props
    public PlayerManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes handRigType = PlayerManager.HandRigTypes.Nothing;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;

    public bool CanInteract { get => canInteract; set => canInteract = value; }
    private bool canInteract; // Sadece kasada true olur

    public string FocusTextKey { get => focusTextKey; set => focusTextKey = value; }
    [SerializeField] private string focusTextKey;

    // Components
    private NavMeshAgent agent;
    private Animator anim;
    private DialogueSpeaker dialogueSpeaker;
    private SkinnedMeshRenderer meshRenderer;

    // Hedefler
    private Transform counterPoint;
    private DiningTable assignedTable;
    private Transform mySeatPoint;

    // Layers
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>(); // "GetComponentInChildren" yerine direkt component (Rootta varsa)
        dialogueSpeaker = GetComponent<DialogueSpeaker>();
        meshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        // Baþlangýçta etkileþim kapalý
        CanInteract = false;
    }

    public void Initialize(CustomerProfile profile)
    {
        currentProfile = profile;

        if (meshRenderer && profile.SkinMaterial)
            meshRenderer.material = profile.SkinMaterial;

        agent.speed = profile.WalkSpeed;

        var randomSelection = profile.PossibleOrders[Random.Range(0, profile.PossibleOrders.Count)];
        currentOrder = randomSelection.Order;

        counterPoint = WorldManager.Instance.GetCounterPosition();

        GoToState(CustomerState.Entering);
    }

    private void Update()
    {
        switch (CurrentState)
        {
            case CustomerState.Entering:
                // Kasaya yaklaþtý mý?
                if (!agent.pathPending && agent.remainingDistance < 0.2f)
                {
                    RotateBodyToCounter(); // Vücudu sabitle
                    GoToState(CustomerState.AtCounter);
                }
                break;

            case CustomerState.AtCounter:
                // Idle animasyonu oynuyor.
                // Interaction bekleniyor.
                break;

            case CustomerState.Ordering:
                // Diyalog oynuyor... 
                // HandleFinishDialogue çaðrýlana kadar buradayýz.
                break;

            case CustomerState.WaitingForFood:
                // Bekliyor...
                // Kafa takibi OnAnimatorIK içinde yapýlýyor.
                break;

            case CustomerState.MovingToSeat:
                if (mySeatPoint == null && assignedTable != null)
                {
                    mySeatPoint = assignedTable.GetSeatForCustomer(this);
                }

                if (mySeatPoint != null)
                {
                    agent.SetDestination(mySeatPoint.position);

                    if (!agent.pathPending && agent.remainingDistance < 0.2f)
                    {
                        // Koltuða vardý
                        GoToState(CustomerState.Eating);
                    }
                }
                break;

            case CustomerState.Eating:
                // Yeme animasyonu...
                // Ýleride "Yeme bitti, kalk git" mantýðý buraya eklenecek.
                break;
        }
    }

    // --- ANÝMASYON IK (HEAD TRACKING) ---
    private void OnAnimatorIK(int layerIndex)
    {
        // Sadece belirli durumlarda kafayý çevirsin
        if (CurrentState == CustomerState.AtCounter ||
            CurrentState == CustomerState.Ordering ||
            CurrentState == CustomerState.WaitingForFood)
        {
            if (PlayerManager.Instance != null)
            {
                // Bakýlacak hedef (Player'ýn kamerasý)
                Transform target = PlayerManager.Instance.GetHeadTransform(); // PlayerManager'a bu metodu eklemelisin!

                if (target != null)
                {
                    // Aðýrlýðý ayarla (1 = Tam bakýþ, 0 = Hiç)
                    anim.SetLookAtWeight(1f, 0.2f, 1f, 0.5f, 0.7f);
                    anim.SetLookAtPosition(target.position);
                }
            }
        }
        else
        {
            // Diðer durumlarda kafayý serbest býrak
            anim.SetLookAtWeight(0);
        }
    }

    private void RotateBodyToCounter()
    {
        Transform faceTarget = WorldManager.Instance.GetCounterFacePoint();
        if (faceTarget != null)
        {
            // NavMesh rotasyonunu kapatýp manuel döndürüyoruz ki titremesin
            agent.updateRotation = false;
            transform.DORotateQuaternion(faceTarget.rotation, 0.5f).SetEase(Ease.OutSine);
        }
    }

    // --- IINTERACTABLE ---
    public void OnInteract()
    {
        if (!CanInteract) return;

        if (CurrentState == CustomerState.AtCounter)
        {
            GoToState(CustomerState.Ordering);

            // Sipariþi bul ve diyaloðu baþlat
            var selectedOrderSet = currentProfile.PossibleOrders.FirstOrDefault(x => x.Order == currentOrder);

            if (selectedOrderSet.OrderDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(selectedOrderSet.OrderDialogue);
            }
            else
            {
                // Diyalog yoksa direkt sipariþi vermiþ sayalým (Test için)
                Debug.LogWarning("Müþterinin bu sipariþ için diyaloðu yok!");
                HandleFinishDialogue();
            }
        }
    }

    // --- TEPSÝ ALMA ---
    public bool TryReceiveTray(Tray tray)
    {
        if (CurrentState != CustomerState.WaitingForFood) return false;

        bool isOrderCorrect = OrderValidator.Validate(tray, currentOrder);

        if (isOrderCorrect)
        {
            // 1. Tepsiyi Kap
            GrabTray(tray);

            // 2. Teþekkür et
            if (currentProfile.CorrectOrderDialogue)
                DialogueManager.Instance.StartDialogue(currentProfile.CorrectOrderDialogue);
            else
                HandleFinishDialogue(); // Diyalog yoksa direkt git

            return true;
        }
        else
        {
            // Yanlýþ Sipariþ
            if (currentProfile.WrongOrderDialogue)
                DialogueManager.Instance.StartDialogue(currentProfile.WrongOrderDialogue);

            // Yanlýþta tepsiyi almýyoruz, geri dönüyoruz.
            return false;
        }
    }

    private void GrabTray(Tray tray)
    {
        // Tray scriptindeki "OnGrab" benzeri cleanup iþlemleri
        // Ama oyuncu deðil NPC aldýðý için manual yapýyoruz.

        // Fiziði kapat
        var trayRb = tray.GetComponent<Rigidbody>();
        if (trayRb) trayRb.isKinematic = true;

        var trayCol = tray.GetComponent<Collider>();
        if (trayCol) trayCol.enabled = false;

        // Ebeveyn deðiþtir (Chest Point)
        tray.transform.SetParent(trayHoldPoint != null ? trayHoldPoint : transform);

        // Yumuþak geçiþ (Snap)
        tray.transform.DOLocalMove(Vector3.zero, 0.5f).SetEase(Ease.OutBack);
        tray.transform.DOLocalRotate(Vector3.zero, 0.5f);

        // Eðer Animation Rigging (TwoBoneIK) kullanýyorsan, burada Target'ý tepsiye çekmen gerekir.
        // anim.SetIKPositionWeight(...) veya Rig Builder weight = 1
    }

    // --- DÝYALOG BÝTÝÞÝ (DialogueManager çaðýrýr) ---
    public void HandleFinishDialogue()
    {
        // Hangi aþamadaydýk?
        if (CurrentState == CustomerState.Ordering)
        {
            // Sipariþi verdi, þimdi beklemeye geçiyor
            GoToState(CustomerState.WaitingForFood);
        }
        else if (CurrentState == CustomerState.WaitingForFood) // Teþekkür diyaloðu bitti
        {
            // Yemeðe gidiyor
            GoToState(CustomerState.MovingToSeat);
        }
        else if (CurrentState == CustomerState.Eating)
        {
            // Yemek bitti diyaloðu (varsa) -> Eve git
            GoToState(CustomerState.Leaving); // Leaving state'i eklersen
        }
    }

    private void GoToState(CustomerState newState)
    {
        CurrentState = newState;

        // State'e özel ayarlar
        switch (newState)
        {
            case CustomerState.AtCounter:
                CanInteract = true;
                ChangeLayer(interactableLayer); // Layer aç
                agent.isStopped = true; // Dur
                anim.SetBool("Idle", true); // Animasyon
                break;

            case CustomerState.Ordering:
                CanInteract = false; // Konuþurken tekrar týklanmasýn
                ChangeLayer(uninteractableLayer);
                break;

            case CustomerState.WaitingForFood:
                CanInteract = false; // Tepsi beklerken etkileþim yok (Tepsi triggerý hariç)
                ChangeLayer(uninteractableLayer);
                // Burada "OrderThrowArea" aktif edilecek (GameManager üzerinden)
                // GameManager.Instance.SetOrderThrowArea(true);
                break;

            case CustomerState.MovingToSeat:
                agent.isStopped = false;
                agent.updateRotation = true; // Yürürken rotasyonu geri aç
                anim.SetBool("Idle", false);
                anim.SetBool("Walking", true); // Yürüme animasyonu
                break;

            case CustomerState.Eating:
                agent.isStopped = true;
                anim.SetBool("Walking", false);
                anim.SetBool("Sitting", true);
                // Rotasyonu koltuða göre ayarla (Seat scriptinden gelen veriyle)
                break;
        }
    }

    // --- UTILS ---
    public void OnFocus()
    {
        if (!CanInteract) return;
        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }

    public void OnLoseFocus()
    {
        if (!CanInteract) return;
        ChangeLayer(interactableLayer);
    }

    public void OutlineChangeCheck()
    {
        if (meshRenderer.gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
            ChangeLayer(interactableOutlinedRedLayer);
        else if (meshRenderer.gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
            ChangeLayer(interactableOutlinedLayer);
    }

    public void ChangeLayer(int layer)
    {
        if (meshRenderer) meshRenderer.gameObject.layer = layer;
    }

    public void AssignTable(DiningTable table)
    {
        assignedTable = table;
    }

    public void OnScareEvent()
    {
        // Korku eventi (Zýplama, Baðýrma vs.)
    }
}