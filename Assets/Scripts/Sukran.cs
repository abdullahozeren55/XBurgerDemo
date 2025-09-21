using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;

public class Sukran : MonoBehaviour, ICustomer, IInteractable
{
    //WARNING: SUKRAN TRUE DRINK USED FOR HER EPIC SPEAK PART 2 BEFORE SHE ASCENDS. SHE DOES NOT GET DRINK.
    public ICustomer.CustomerDayChangesSegment[] CustomerDayChanges { get => customerDayChanges; set => customerDayChanges = value; }
    [SerializeField] private ICustomer.CustomerDayChangesSegment[] customerDayChanges;
    public CustomerData CustomerData { get => customerData; set => customerData = value; }
    [SerializeField] private CustomerData customerData;

    public ICustomer.Action CurrentAction { get => currentAction; set => currentAction = value; }
    [SerializeField] private ICustomer.Action currentAction;

    public ICustomer.Feeling CurrentFeeling { get => currentFeeling; set => currentFeeling = value; }
    [SerializeField] private ICustomer.Feeling currentFeeling;

    public ICustomer.CustomerName PersonName { get => personName; set => personName = value; }
    [SerializeField] private ICustomer.CustomerName personName;

    public GameManager.BurgerTypes BurgerType { get => burgerType; set => burgerType = value; }
    [SerializeField] private GameManager.BurgerTypes burgerType;

    public GameManager.DrinkTypes DrinkType { get => drinkType; set => drinkType = value; }
    [SerializeField] private GameManager.DrinkTypes drinkType;

    public DialogueData BeforeOrderDialogueData { get => beforeOrderDialogueData; set => beforeOrderDialogueData = value; }
    [SerializeField] private DialogueData beforeOrderDialogueData;

    public DialogueData AfterOrderDialogueData { get => afterOrderDialogueData; set => afterOrderDialogueData = value; }
    [SerializeField] DialogueData afterOrderDialogueData;

    public DialogueData FalseBurgerDialogueData { get => falseBurgerDialogueData; set => falseBurgerDialogueData = value; }
    [SerializeField] DialogueData falseBurgerDialogueData;

    public DialogueData TrueBurgerDialogueData { get => trueBurgerDialogueData; set => trueBurgerDialogueData = value; }
    [SerializeField] DialogueData trueBurgerDialogueData;

    public DialogueData FalseDrinkDialogueData { get => falseDrinkDialogueData; set => falseDrinkDialogueData = value; }
    [SerializeField] DialogueData falseDrinkDialogueData;

    public DialogueData TrueDrinkDialogueData { get => trueDrinkDialogueData; set => trueDrinkDialogueData = value; }
    [SerializeField] DialogueData trueDrinkDialogueData;

    public DialogueData CompleteOrderDialogueData { get => completeOrderDialogueData; set => completeOrderDialogueData = value; }
    [SerializeField] private DialogueData completeOrderDialogueData;

    public DialogueData NotAnsweringDialogueData { get => notAnsweringDialogueData; set => notAnsweringDialogueData = value; }
    [SerializeField] DialogueData notAnsweringDialogueData;

    public DialogueData OptionADialogueData { get => optionADialogueData; set => optionADialogueData = value; }
    [SerializeField] DialogueData optionADialogueData;

    public DialogueData OptionDDialogueData { get => optionDDialogueData; set => optionDDialogueData = value; }
    [SerializeField] DialogueData optionDDialogueData;

    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private GameManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    [SerializeField] DialogueData sukranSelfTalkData;
    [SerializeField] DialogueData finalDialogueData; 

    [SerializeField] private Transform restaurantDestination; //destinationToArrive
    [SerializeField] private Transform homeDestination; //destinationToDisappear
    [SerializeField] private Transform playerTransform;
    [SerializeField] private GameObject[] ordersInRightHand;

    private Transform currentDestination;
    private Coroutine currentRotateCoroutine;

    [Header("Components")]
    private Animator anim;
    private NavMeshAgent agent;
    private Rigidbody rb;
    private AudioSource audioSource;
    private SkinnedMeshRenderer skinnedMeshRenderer;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int uninteractableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int customerLayer;

    [Header("Footstep Parameters")]
    private bool shouldPlayFootstep;
    private Material currentGroundMaterial;
    private AudioClip lastPlayedFootstep;

    [Header("Text Settings")]
    [SerializeField] private GameObject talkWithCustomerText;

    [Header("Þükran Settings")]
    [SerializeField] private float ascendTime = 0.5f;
    [SerializeField] private float flyTime = 2f;
    [SerializeField] private GameObject eyes;

    [Header("Glow Settings")]
    [SerializeField] private Color emissionColor = Color.white;
    [SerializeField] private float targetGlow = 5f;
    [SerializeField] private float approachSpeed = 2f;
    [SerializeField] private float pulseRange = 1f;
    [SerializeField] private float pulseSpeed = 2f;

    [Header("Push Player Settings")]
    [SerializeField] private Transform rayPointForPushingPlayer;

    private float[] currentGlows;
    private bool reachedTarget = false;
    private float pingPongTime = 0f;
    private Material[] glowMats;
    private bool shouldGlow = false;

    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        agent = GetComponent<NavMeshAgent>();
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        customerLayer = LayerMask.NameToLayer("Customer");

        foreach (GameObject item in ordersInRightHand)
        {
            item.SetActive(false);
        }

        GetGlowMaterials();

        StartPathFollow(restaurantDestination);
    }

    private void LateUpdate()
    {
        if (currentAction == ICustomer.Action.GoingToDestination)
        {
            HandlePathFollow(currentDestination);
            HandleFootsteps();
            HandlePushingPlayer();
        }
        else
            RotateCustomer();

        if (shouldGlow)
            HandleGlow();
    }

    private void GetGlowMaterials()
    {
        glowMats = new Material[3];
        glowMats[0] = eyes.transform.GetChild(0).GetComponent<Renderer>().material;
        glowMats[1] = eyes.transform.GetChild(1).GetComponent<Renderer>().material;
        glowMats[2] = ordersInRightHand[0].GetComponent<Renderer>().material;

        foreach (var mat in glowMats)
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", Color.black);
        }

        currentGlows = new float[glowMats.Length];
    }

    private void HandleGlow()
    {

        if (!reachedTarget)
        {
            bool allReached = true;

            for (int i = 0; i < glowMats.Length; i++)
            {
                currentGlows[i] = Mathf.MoveTowards(currentGlows[i], targetGlow, Time.deltaTime * approachSpeed);
                glowMats[i].SetColor("_EmissionColor", emissionColor * currentGlows[i]);

                if (!Mathf.Approximately(currentGlows[i], targetGlow))
                    allReached = false;
            }

            if (allReached)
            {
                reachedTarget = true;
                pingPongTime = 0f;
            }
        }
        else
        {
            pingPongTime += Time.deltaTime * pulseSpeed;

            for (int i = 0; i < glowMats.Length; i++)
            {
                float glow = targetGlow + Mathf.Sin(pingPongTime) * pulseRange;
                glowMats[i].SetColor("_EmissionColor", emissionColor * glow);
            }
        }
    }

    public void HandleIdle()
    {
        GameManager.Instance.SetCurrentCustomer(this);

        anim.SetBool("idle", true);
        agent.enabled = false;
        rb.isKinematic = true;

        ChangeLayer(interactableLayer);
    }

    public void StartPathFollow(Transform destination)
    {
        currentDestination = destination;
        CurrentAction = ICustomer.Action.GoingToDestination;
    }

    public void HandlePathFollow(Transform destination)
    {
        agent.destination = destination.position;

        if (Vector3.Distance(transform.position, destination.position) < CustomerData.minDistance)
        {
            if (currentDestination == restaurantDestination)
            {
                CurrentAction = ICustomer.Action.ReadyToOrder;
                HandleIdle();
            }
            else
                gameObject.SetActive(false);
        }
    }

    public void HandleFootsteps()
    {
        FindCurrentGroundMaterial();

        if (shouldPlayFootstep)
        {
            if (currentGroundMaterial != null)
            {
                if (currentGroundMaterial.name.Contains("Wood"))
                    PlayFootstep(CustomerData.woodClips);
                else if (currentGroundMaterial.name.Contains("Metal"))
                    PlayFootstep(CustomerData.metalClips);
                else if (currentGroundMaterial.name.Contains("Grass"))
                    PlayFootstep(CustomerData.grassClips);
                else if (currentGroundMaterial.name.Contains("Stone"))
                    PlayFootstep(CustomerData.stoneClips);
                else if (currentGroundMaterial.name.Contains("Tile"))
                    PlayFootstep(CustomerData.tileClips);
                else if (currentGroundMaterial.name.Contains("Gravel"))
                    PlayFootstep(CustomerData.gravelClips);

                ChangeShouldPlayFootstep();
            }
        }
    }

    private void PlayFootstep(AudioClip[] audioClips)
    {
        var audio = audioClips[Random.Range(0, audioClips.Length - 1)];

        if (audio == lastPlayedFootstep)
            PlayFootstep(audioClips);
        else
        {
            audioSource.PlayOneShot(audio);
            lastPlayedFootstep = audio;
        }
    }

    private void FindCurrentGroundMaterial()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, CustomerData.rayDistance, CustomerData.groundTypeLayers))
        {
            // Step 1: Get material (existing)
            if (hit.collider.TryGetComponent<MeshRenderer>(out var renderer))
            {
                var materials = renderer.materials;
                var index = hit.triangleIndex;
                var mesh = hit.transform.GetComponent<MeshFilter>().mesh;
                var subMeshIndex = GetSubMeshIndex(mesh, index);
                currentGroundMaterial = materials[subMeshIndex];
            }

            agent.updatePosition = false;

            // Step 2: Adjust NPC Y-position to match hit point
            Vector3 currentPos = transform.position;
            currentPos.y = hit.point.y;
            transform.position = currentPos;

            agent.updatePosition = true;
        }
    }

    private int GetSubMeshIndex(Mesh mesh, int triangleIndex)
    {
        if (!mesh.isReadable) return 0;

        var triangleCounter = 0;
        for (var subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; subMeshIndex++)
        {
            var indexCount = mesh.GetSubMesh(subMeshIndex).indexCount;
            triangleCounter += indexCount / 3;
            if (triangleIndex < triangleCounter) return subMeshIndex;
        }

        return 0;
    }

    private void HandlePushingPlayer()
    {
        Ray ray = new Ray(rayPointForPushingPlayer.position, transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, customerData.rayDistanceForPushingPlayer, customerData.playerLayer))
        {
            GameManager.Instance.SetPlayerPushedByCustomer(true);
            GameManager.Instance.MovePlayer(transform.forward * CustomerData.pushForce * Time.deltaTime);
        }
        else
        {
            GameManager.Instance.SetPlayerPushedByCustomer(false);
        }
    }

    public void HandleFinishDialogue()
    {
        if (CurrentAction == ICustomer.Action.ReadyToOrder)
        {
            CurrentAction = ICustomer.Action.WaitingForOrder;
            ChangeLayer(uninteractableLayer);
            Invoke("StartSelfTalk", 1f);
        }
        else if (CurrentAction == ICustomer.Action.WaitingForOrder)
        {
            ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.ReceivedFalseBurger)
        {
            HandleHeadHome();
        }
        else if (CurrentAction == ICustomer.Action.ReceivedTrueBurger)
        {
            ChangeLayer(uninteractableLayer);
            eyes.SetActive(true);
            shouldGlow = true;
            Invoke("StartTrueBurgerPartTwo", 2f);
        }
        else if (CurrentAction == ICustomer.Action.ReceivedTrueDrink)
        {
            anim.SetTrigger("float");
            StartCoroutine(StartFloating());
        }
        else if (CurrentAction == ICustomer.Action.ReceivedFalseDrink)
        {
            CurrentAction = ICustomer.Action.WaitingForOrder;
            ChangeLayer(interactableLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.ReceivedAllOrder)
        {
            StartCoroutine(Ascend());
        }

        GameManager.Instance.ChangePlayerCanMove(true);

    }

    private void StartSelfTalk()
    {
        DialogueManager.Instance.StartSelfDialogue(sukranSelfTalkData);
        ChangeLayer(interactableLayer);
        GameManager.Instance.SetOrderThrowArea(true);
    }

    private void StartTrueBurgerPartTwo()
    {
        CurrentAction = ICustomer.Action.ReceivedTrueDrink;
        DialogueManager.Instance.StartCustomerDialogue(this, trueDrinkDialogueData);
    }

    private bool IsOnlyPicklesInside(List<BurgerIngredientData.IngredientType> ingredients)
    {
        if (ingredients.Count <= 2) return false; // Only buns, no filling

        for (int i = 1; i < ingredients.Count - 1; i++)
        {
            if (ingredients[i] != BurgerIngredientData.IngredientType.PICKLE)
            {
                return false;
            }
        }
        return true;
    }

    public void ReceiveBurger(BurgerBox burgerBox)
    {
        ChangeLayer(uninteractableLayer);
        HandleText(false);

        if (IsOnlyPicklesInside(burgerBox.allBurgerIngredientTypes))
            HandleBurgerTrue();
        else
            HandleBurgerFalse();
    }

    public void ReceiveDrink(Drink drink)
    {
        ChangeLayer(uninteractableLayer);
        HandleText(false);
        
        HandleDrinkFalse(drink.data.drinkType);
    }

    public void OnInteract()
    {
        ChangeLayer(uninteractableLayer);
        HandleText(false);

        if (CurrentAction == ICustomer.Action.ReadyToOrder)
        {
            DialogueManager.Instance.StartCustomerDialogue(this, beforeOrderDialogueData);
        }

        else
        {
            DialogueManager.Instance.StartCustomerDialogue(this, afterOrderDialogueData);
        }
    }

    public void OnFocus()
    {
        HandleText(true);
        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }

    public void OnLoseFocus()
    {
        HandleText(false);
        ChangeLayer(interactableLayer);
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
        {
            ChangeLayer(interactableOutlinedRedLayer);
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            ChangeLayer(interactableOutlinedLayer);
        }
    }

    private void HandleText(bool isFocused)
    {
        if (isFocused)
            talkWithCustomerText.SetActive(true);
        else
            talkWithCustomerText.SetActive(false);
    }

    private void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
        skinnedMeshRenderer.gameObject.layer = layer;

        foreach (GameObject item in ordersInRightHand)
        {
            item.layer = layer;
        }
    }

    private void HandleBurgerTrue()
    {
        ordersInRightHand[0].SetActive(true);
        anim.SetTrigger("carryBox");
        CurrentAction = ICustomer.Action.ReceivedTrueBurger;
        DialogueManager.Instance.StartCustomerDialogue(this, TrueBurgerDialogueData);
    }

    private void HandleBurgerFalse()
    {
        ordersInRightHand[0].SetActive(true);
        CurrentAction = ICustomer.Action.ReceivedFalseBurger;
        anim.SetTrigger("giveBack");
        DialogueManager.Instance.StartCustomerDialogue(this, FalseBurgerDialogueData);
    }

    private void HandleDrinkFalse(GameManager.DrinkTypes drinkType)
    {
        ordersInRightHand[(int)drinkType + 1].SetActive(true);
        CurrentAction = ICustomer.Action.ReceivedFalseDrink;
        anim.SetTrigger("giveBack");
        DialogueManager.Instance.StartCustomerDialogue(this, FalseDrinkDialogueData);
    }

    private void HandleHeadHome()
    {
        ChangeLayer(customerLayer);
        anim.SetBool("idle", false);
        anim.SetBool("walkAway", true);
        currentDestination = homeDestination;
        agent.enabled = true;
        CurrentAction = ICustomer.Action.GoingToDestination;
    }

    private void ChangeShouldPlayFootstep() => shouldPlayFootstep = !shouldPlayFootstep;

    private void RotateCustomer()
    {
        Vector3 direction = playerTransform.position - transform.position;
        direction.y = 0f; // Ignore vertical difference
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, CustomerData.rotationSpeed * Time.deltaTime);
        }
    }

    private void GiveOrderBack()
    {
        if (ordersInRightHand[0].activeSelf)
        {
            GameManager.Instance.CustomerGiveBackBurger(ordersInRightHand[0].transform, customerData.throwForce * (transform.forward + (transform.up * 2f)));
            ordersInRightHand[0].SetActive(false);
        }
        else
        {
            GameManager.Instance.CustomerGiveBackDrink(ordersInRightHand[1].transform, customerData.throwForce * (transform.forward + (transform.up * 2f)));
            ordersInRightHand[1].SetActive(false);
            ordersInRightHand[2].SetActive(false);
            ordersInRightHand[3].SetActive(false);
        }

    }

    private void StartFinalDialogue()
    {
        CurrentAction = ICustomer.Action.ReceivedAllOrder;

        DialogueManager.Instance.StartCustomerDialogue(this, finalDialogueData);
    }

    private IEnumerator StartFloating()
    {
        Vector3 startingPosition = transform.position;
        Vector3 targetPosition = new Vector3(startingPosition.x, startingPosition.y + 0.7f, startingPosition.z);

        float elapsedTime = 0f;

        while (elapsedTime < flyTime)
        {
            transform.position = Vector3.Lerp(startingPosition, targetPosition, elapsedTime / flyTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
    }

    private IEnumerator Ascend()
    {
        Vector3 startingScale = transform.localScale;
        Vector3 targetScale = new Vector3(0f, startingScale.y, startingScale.z);

        float elapsedTime = 0f;

        while (elapsedTime < ascendTime)
        {
            transform.localScale = Vector3.Lerp(startingScale, targetScale, elapsedTime / ascendTime);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localScale = targetScale;

        Destroy(gameObject);
    }
}
