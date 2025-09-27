using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;

public class Nevzat : MonoBehaviour, ICustomer, IInteractable
{
    //WARNING: NEVZAT CANT HAVE FALSE BURGER OR DRINK. HE ACCEPTS ALL. HIS WRONG BURGER DIALOGUE BAR IS USED FOR FIRST TIME TALK. WRONG DRINK IS USED FOR BAD LEAVING. COMPLETE USED FOR PICKLE BURGER.
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

    public Image FocusImage { get => focusImage; set => focusImage = value; }
    [SerializeField] private Image focusImage;
    [Space]

    [Header("Nevzat Settings")]
    private GameObject nevzatSpecialColliderGO;

    [Header("Push Player Settings")]
    [SerializeField] private Transform rayPointForPushingPlayer;

    private int day;

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

        StartPathFollow(restaurantDestination);
    }

    private void OnEnable()
    {
        day = GameManager.Instance.DayCount;

        for (int i = 0; i < CustomerDayChanges.Length; i++)
        {
            if (CustomerDayChanges[i].Day == day)
            {
                ApplyDayChanges(CustomerDayChanges[i]);
                break; // daha fazla arama gereksiz
            }
        }

        foreach (GameObject item in ordersInRightHand)
        {
            item.SetActive(false);
        }
    }

    private void LateUpdate()
    {
        if (currentAction == ICustomer.Action.GoingToDestination)
        {
            HandlePathFollow(currentDestination);
            HandleFootsteps();
            HandlePushingPlayer();
        }
        else if (currentAction != ICustomer.Action.ReceivedFalseDrink)
            RotateCustomer();
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
        if (CurrentAction == ICustomer.Action.ReadyToTalk)
        {
            agent.enabled = true;
            anim.SetBool("idle", false);
            CurrentAction = ICustomer.Action.GoingToDestination;
        }
        else if (CurrentAction == ICustomer.Action.ReadyToOrder)
        {
            CurrentAction = ICustomer.Action.WaitingForOrder;
            ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.GotAnswerA)
        {
            CurrentAction = ICustomer.Action.WaitingForOrder;
            ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.WaitingForOrder)
        {
            ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.ReceivedFalseBurger)
        {
            CurrentAction = ICustomer.Action.WaitingForOrder;
            ChangeLayer(interactableLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.ReceivedTrueBurger)
        {
            HandleHeadHome();
        }
        else if (CurrentAction == ICustomer.Action.ReceivedFalseDrink)
        {
            anim.SetBool("seriousIdle", false);
            agent.enabled = true;
            CurrentAction = ICustomer.Action.GoingToDestination;
        }
        else if (CurrentAction == ICustomer.Action.ReceivedTrueDrink)
        {
            HandleHeadHome();
        }
        else if (CurrentAction == ICustomer.Action.NotGotAnswer)
        {
            nevzatSpecialColliderGO.SetActive(true);
            HandleHeadHome();
        }
        else if (CurrentAction == ICustomer.Action.GotAnswerD)
        {
            HandleHeadHome();
        }

        GameManager.Instance.ChangePlayerCanMove(true);

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

        if (IsOnlyPicklesInside(burgerBox.allBurgerIngredientTypes))
            HandleBurgerFalse();
        else
            HandleBurgerTrue();
    }

    public void ReceiveDrink(Drink drink)
    {
        ChangeLayer(uninteractableLayer);
        
        HandleDrinkTrue(drink.data.drinkType);
    }

    public void OnInteract()
    {
        ChangeLayer(uninteractableLayer);

        if (CurrentAction == ICustomer.Action.ReadyToOrder)
        {
            anim.SetTrigger("talk");
            DialogueManager.Instance.StartCustomerDialogue(this, beforeOrderDialogueData);
        }

        else
        {
            DialogueManager.Instance.StartCustomerDialogue(this, afterOrderDialogueData);
        }
    }

    public void OnFocus()
    {
        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }

    public void OnLoseFocus()
    {
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
        CurrentAction = ICustomer.Action.ReceivedTrueBurger;
        DialogueManager.Instance.StartCustomerDialogue(this, TrueBurgerDialogueData);
    }

    private void HandleBurgerFalse()
    {
        ordersInRightHand[0].SetActive(true);
        CurrentAction = ICustomer.Action.ReceivedFalseBurger;
        anim.SetTrigger("giveBack");
        DialogueManager.Instance.StartCustomerDialogue(this, CompleteOrderDialogueData); //CompleteOrderDialogueData is used for pickle sandwich dialogue.
    }

    private void HandleDrinkTrue(GameManager.DrinkTypes drinkType)
    {
        ordersInRightHand[(int)drinkType + 1].SetActive(true);
        CurrentAction = ICustomer.Action.ReceivedTrueDrink;
        DialogueManager.Instance.StartCustomerDialogue(this, TrueDrinkDialogueData);
    }

    private void HandleHeadHome()
    {
        ChangeLayer(customerLayer);
        anim.SetBool("idle", false);
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

    private void ApplyDayChanges(ICustomer.CustomerDayChangesSegment changes)
    {
        skinnedMeshRenderer.material = changes.Material;
        BurgerType = changes.BurgerType;
        DrinkType = changes.DrinkType;

        BeforeOrderDialogueData = changes.BeforeOrderDialogueData;
        AfterOrderDialogueData = changes.AfterOrderDialogueData;
        FalseBurgerDialogueData = changes.FalseBurgerDialogueData;
        TrueBurgerDialogueData = changes.TrueBurgerDialogueData;
        FalseDrinkDialogueData = changes.FalseDrinkDialogueData;
        TrueDrinkDialogueData = changes.TrueDrinkDialogueData;
        CompleteOrderDialogueData = changes.CompleteOrderDialogueData;
        NotAnsweringDialogueData = changes.NotAnsweringDialogueData;
        OptionADialogueData = changes.OptionADialogueData;
        OptionDDialogueData = changes.OptionDDialogueData;
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

    private void FirstMet()
    {
        CurrentAction = ICustomer.Action.ReadyToTalk;
        agent.enabled = false;
        HandleFootsteps();
        anim.SetTrigger("wave");
        anim.SetBool("idle", true);
        DialogueManager.Instance.StartCustomerDialogue(this, falseBurgerDialogueData);
    }

    private void BadLeaving()
    {
        CurrentAction = ICustomer.Action.ReceivedFalseDrink;
        agent.enabled = false;
        HandleFootsteps();
        anim.SetBool("seriousIdle", true);
        DialogueManager.Instance.StartCustomerDialogue(this, falseDrinkDialogueData);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("NevzatTrigger"))
        {
            if (day == 1)
            {
                if (currentDestination == restaurantDestination)
                {
                    nevzatSpecialColliderGO = other.gameObject;
                    nevzatSpecialColliderGO.SetActive(false);
                    Invoke("FirstMet", 2f);
                }
                else
                {
                    nevzatSpecialColliderGO.SetActive(false);
                    Invoke("BadLeaving", 1f);
                }

            }
                
        }
    }
}
