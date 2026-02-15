using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(DialogueSpeaker))]
[RequireComponent(typeof(Animator))] // Animasyon þart
public class Clown : MonoBehaviour, ICustomer, IInteractable
{
    public static Clown Instance;

    public bool ShouldBeSad = false;

    [SerializeField] private GameObject bodyGO;
    [SerializeField] private GameObject headGO;
    [Space]
    [SerializeField] private Material sadMat;
    [SerializeField] private GameObject sadCustomers;

    public enum ClownState
    {
        Idle,       // Boþta bekliyor
        Roaming,    // Rastgele bir yere yürüyor
        Performing, // Dans ediyor / Gösteri yapýyor
        Talking,    // Oyuncuyla konuþuyor
        BreakTime,   // (Ýleride) Mola verip yemek yiyeceði zaman
        SadIdle,
    }

    [Header("Settings")]
    [Tooltip("X: Geniþlik (Sað/Sol), Y: Derinlik (Ýleri/Geri)")]
    [SerializeField] private Vector2 roamAreaSize = new Vector2(10f, 10f); // YENÝ AYAR
    [SerializeField] private float minPerformDuration = 5f;
    [SerializeField] private float maxPerformDuration = 10f;
    [SerializeField] private CustomerProfile customerProfile; // ICustomer için gerekli

    [Header("Interaction Data")]
    [SerializeField] private string focusTextKey = "interaction_clown"; // "Palyaço ile konuþ"
    [SerializeField] private DialogueData standardDialogue; // Standart "Naber patron" diyaloðu
    [SerializeField] private DialogueData sadDialogue; // "Yemek yiyorum" diyaloðu

    [Header("References")]
    [SerializeField] private Transform roamCenterPoint; // Dükkanýn ortasý

    public Transform clownSadPointTransform;

    // --- STATE MACHINE ---
    public ClownState CurrentState { get; private set; }

    // Components
    private NavMeshAgent agent;
    private Animator anim;

    // IInteractable Properties
    public PlayerManager.HandRigTypes HandRigType { get; set; } = PlayerManager.HandRigTypes.Nothing;
    public bool OutlineShouldBeRed { get; set; } = false;
    public bool CanInteract { get; set; } = true;
    public string FocusTextKey { get => focusTextKey; set => focusTextKey = value; }

    // ICustomer Property
    CustomerState ICustomer.CurrentState => CustomerState.AtCounter; // Þimdilik hep "Müsait" gibi davranýyor

    // Layers
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    [Header("Final Scene")]
    [SerializeField] private LayerMask interactableLayersForTheFinalSceneRaycast;
    [SerializeField] private AudioClip tensionSound;
    [SerializeField] private AudioClip horrorSound;
    private bool isSoundPlayed = false;
    private bool isSoundPlayedForCustomers = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        Instance = this;

        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        // NavMesh ayarlarý
        agent.speed = customerProfile.WalkSpeed;
    }

    private void Start()
    {
        // Oyuna baþlar baþlamaz devriyeye çýksýn
        SetState(ClownState.Idle);
    }

    private void Update()
    {
        // Sadece Roaming durumundayken hedefe varýp varmadýðýný kontrol et
        if (CurrentState == ClownState.Roaming)
        {
            if (!agent.pathPending && agent.remainingDistance <= customerProfile.ArrivalDistance)
            {
                // Hedefe vardý, þovu baþlat
                StartCoroutine(PerformRoutine());
            }
        }

        if (ShouldBeSad)
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit, 10f, interactableLayersForTheFinalSceneRaycast))
            {
                if (!isSoundPlayed && hit.collider.CompareTag("Clown"))
                {
                    SoundManager.Instance.PlaySoundFX(tensionSound, transform);
                    isSoundPlayed = true;
                }
                else if (!isSoundPlayedForCustomers && hit.collider.CompareTag("Customer"))
                {
                    SoundManager.Instance.PlaySoundFX(horrorSound, transform);
                    isSoundPlayedForCustomers = true;
                    MenuManager.Instance.FinishTheGame();
                }
            }
        }
    }

    // --- STATE MANAGEMENT ---

    public void SetState(ClownState newState)
    {
        CurrentState = newState;

        switch (newState)
        {
            case ClownState.Roaming:
                anim.SetBool("dance", false);
                anim.SetBool("walk", true);
                agent.isStopped = false;
                GoToRandomPoint();
                break;

            case ClownState.Performing:
                anim.SetBool("walk", false);
                anim.SetBool("dance", true); // Animator'da bu bool olmalý
                agent.isStopped = true;
                break;

            case ClownState.Talking:
                anim.SetBool("walk", false);
                anim.SetBool("dance", false);
                agent.isStopped = true;
                // Yüzünü oyuncuya dönme iþlemi OnInteract'ta yapýlýyor
                break;
            case ClownState.SadIdle:
                gameObject.SetActive(true);
                anim.SetBool("walk", false);
                anim.SetBool("dance", false);
                anim.SetBool("sadIdle", true);
                headGO.GetComponent<SkinnedMeshRenderer>().material = sadMat;
                CanInteract = true;

                // 1. Önce Agent'ý durdur ve yolunu temizle (Garanti olmasý için)
                agent.isStopped = true;
                agent.ResetPath();
                agent.velocity = Vector3.zero;

                // 2. Teleport iþlemi için Warp kullanýyoruz
                // Warp, hem pozisyonu deðiþtirir hem de Agent'ý o NavMesh noktasýna baðlar.
                agent.Warp(clownSadPointTransform.position);

                // 3. Rotasyonu manuel ayarlayabilirsin (Warp rotasyonu deðiþtirmez)
                transform.rotation = clownSadPointTransform.rotation;
                break;
        }
    }

    // --- BEHAVIOR LOGIC ---

    private void GoToRandomPoint()
    {
        Vector3 randomPoint = GetRandomNavMeshPoint();
        agent.SetDestination(randomPoint);
    }

    private Vector3 GetRandomNavMeshPoint()
    {
        Vector3 origin = roamCenterPoint != null ? roamCenterPoint.position : transform.position;

        // --- DEÐÝÞÝKLÝK: EN VE BOY HESABI ---
        // Alanýn merkezinden saða/sola (X) ve ileri/geriye (Z) yarýsý kadar gidebiliriz.
        float halfWidth = roamAreaSize.x / 2f;
        float halfDepth = roamAreaSize.y / 2f;

        float randomX = Random.Range(-halfWidth, halfWidth);
        float randomZ = Random.Range(-halfDepth, halfDepth);

        Vector3 randomPos = origin + new Vector3(randomX, 0, randomZ);
        // ------------------------------------

        NavMeshHit hit;
        // NavMesh üzerinde geçerli bir nokta bul
        if (NavMesh.SamplePosition(randomPos, out hit, 2.0f, NavMesh.AllAreas))
        {
            return hit.position;
        }
        return origin; // Bulamazsa merkeze dönsün
    }

    private IEnumerator PerformRoutine()
    {
        SetState(ClownState.Performing);

        // Rastgele bir süre dans et
        float duration = Random.Range(minPerformDuration, maxPerformDuration);

        // Bu süre boyunca bekle (Eðer araya Diyalog girerse bu routine bozulmasýn diye kontrol ekleyebiliriz)
        float timer = 0;
        while (timer < duration)
        {
            if (CurrentState != ClownState.Performing) yield break; // Diyalog girdiyse iptal et
            timer += Time.deltaTime;
            yield return null;
        }

        // Dans bitti, tekrar yürü
        SetState(ClownState.Roaming);
    }

    // --- IINTERACTABLE IMPLEMENTATION ---

    public void OnInteract()
    {
        if (!CanInteract) return;

        SetState(ClownState.Talking);

        // Oyuncuya Dön
        if (PlayerManager.Instance != null)
        {
            Transform lookPos = PlayerManager.Instance.GetHeadTransform();
            transform.LookAt(lookPos);
        }

        if (ShouldBeSad)
        {
            if (DialogueManager.Instance != null && sadDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(sadDialogue, false, () =>
                {
                    HandleFinishDialogue();
                });
            }
        }
        else
        {
            if (DialogueManager.Instance != null && standardDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(standardDialogue, false, () =>
                {
                    HandleFinishDialogue();
                });
            }
        }
        
    }

    public void HandleFinishDialogue()
    {
        CanInteract = false;
        if (ShouldBeSad)
        {
            sadCustomers.SetActive(true);
        }
        else
        {
            if (EventManager.Instance)
            {
                EventManager.Instance.TurnOnKitchenBlockerTrigger();
            }
        }
        
    }

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

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
        headGO.layer = layer;
        bodyGO.layer = layer;
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
            ChangeLayer(interactableOutlinedRedLayer);
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
            ChangeLayer(interactableOutlinedLayer);
    }


    // --- ICUSTOMER IMPLEMENTATION ---

    public void Initialize(CustomerProfile profile)
    {
    }

    public bool TryReceiveTray(Tray tray)
    {
        // Þimdilik tepsi kabul etmiyor.
        // Ýleride: if (CurrentState == ClownState.BreakTime) { ... }

        // Belki tepsi uzatýnca "Þu an aç deðilim, iþim baþýmdan aþkýn!" der.
        if (DialogueManager.Instance != null)
        {
            // DialogueManager.Instance.StartDialogue(NotHungryDialogue);
        }

        return false;
    }

    public void OnScareEvent()
    {
    }

    public void PlayFootstep()
    {
        // 1. Data ve Manager Kontrolü
        if (customerProfile == null || customerProfile.FootstepSounds == null) return;
        if (SoundManager.Instance == null) return;

        // 2. Zemin Tespiti (LayerMask Kullanarak)
        RaycastHit hit;
        SurfaceType detectedSurface = SurfaceType.Stone; // Varsayýlan

        // Maskeyi Data'dan çek
        LayerMask targetMask = customerProfile.FootstepSounds.GroundLayerMask;

        // Karakterin karnýndan (0.5f yukarý) aþaðýya doðru 1.5 birim ýþýn at.
        // Böylece hafif eðimli yüzeylerde veya merdivenlerde de algýlar.
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out hit, 1.5f, targetMask))
        {
            // Çarptýðýmýz objede SurfaceIdentity var mý?
            SurfaceIdentity surfaceID = hit.collider.GetComponent<SurfaceIdentity>();
            if (surfaceID != null)
            {
                detectedSurface = surfaceID.type;
            }
        }

        // 3. Sesleri Getir
        AudioClip[] clipsToPlay = customerProfile.FootstepSounds.GetClipsForSurface(detectedSurface);

        // 4. Çal (Transform: this.transform)
        if (clipsToPlay != null && clipsToPlay.Length > 0)
        {
            SoundManager.Instance.PlayRandomSoundFX(
                clipsToPlay,
                transform // Ses kaynaðý müþterinin kendisi
            );
        }
    }

    private IEnumerator RecoverFromScare(float delay)
    {
        yield return new WaitForSeconds(delay);
        SetState(ClownState.Roaming);
    }

    // Debug için yarýçapý çiz
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 center = roamCenterPoint != null ? roamCenterPoint.position : transform.position;

        // --- DEÐÝÞÝKLÝK: KUTUYU YENÝ BOYUTLARLA ÇÝZ ---
        // Vector3(Geniþlik, Yükseklik, Derinlik)
        // Yüksekliði sembolik 1 verdik, X ve Z senin ayarýn.
        Gizmos.DrawWireCube(center, new Vector3(roamAreaSize.x, 1f, roamAreaSize.y));
    }
}