using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class Hikmet : MonoBehaviour, ICustomer, IInteractable
{
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;
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

    public ICustomer.Footstep CurrentFootstep { get => currentFootstep; set => currentFootstep = value; }
    [SerializeField] private ICustomer.Footstep currentFootstep;

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

    public PlayerManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;
    public Transform CameraLookAt { get => cameraLookAt; set => cameraLookAt = value; }
    [SerializeField] private Transform cameraLookAt;

    [SerializeField] private Transform restaurantDestination; //destinationToArrive
    [SerializeField] private Transform homeDestination; //destinationToDisappear
    [SerializeField] private Transform facingDirectionTransform;
    [SerializeField] private GameObject[] ordersInRightHand;
    [SerializeField] private GameObject[] ordersInLeftHand;

    public bool TrueDrinkReceived { get => trueDrinkReceived; set => trueDrinkReceived = value; }
    public bool TrueBurgerReceived { get => trueBurgerReceived; set => trueBurgerReceived = value; }
    private bool trueBurgerReceived;
    private bool trueDrinkReceived;

    private Transform currentDestination;

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
    public string FocusTextKey { get => focusTextKey; set => focusTextKey = value; }
    [SerializeField] private string focusTextKey;
    [Space]

    [Header("Hikmet Settings")]
    [SerializeField] private float normalWalkSpeed = 1.2f;
    [SerializeField] private float sadWalkSpeed = 0.8f;
    [SerializeField] private Material hikmetOutfit1Angry;

    private int day;
    private Tween rotateTween;
    private Coroutine currentAnimCoroutine;

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
        day = DayManager.Instance.DayCount;

        TrueBurgerReceived = false;
        TrueDrinkReceived = false;
        
        agent.speed = normalWalkSpeed;

        anim.SetBool("sadWalk", false);
        anim.SetBool("giveOtherBack", false);

        for (int i = 0; i < CustomerDayChanges.Length; i++)
        {
            if (CustomerDayChanges[i].Day == day)
            {
                ApplyDayChanges(CustomerDayChanges[i]);
                break; // daha fazla arama gereksiz
            }
        }

        foreach (GameObject item in ordersInLeftHand)
        {
            item.SetActive(false);
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
                RotateCustomer();
                HandleIdle();
            }
            else
                gameObject.SetActive(false);
        }
    }

    public void HandleFootsteps()
    {
        if (CurrentFootstep == ICustomer.Footstep.STONE)
            SoundManager.Instance.PlayRandomSoundFX(customerData.stoneClips, transform, customerData.FootstepVolume, customerData.FootstepMinPitch, customerData.FootstepMaxPitch);
        else if (CurrentFootstep == ICustomer.Footstep.WOOD)
            SoundManager.Instance.PlayRandomSoundFX(customerData.woodClips, transform, customerData.FootstepVolume, customerData.FootstepMinPitch, customerData.FootstepMaxPitch);
    }

    public void HandleFinishDialogue()
    {
        if (CurrentAction == ICustomer.Action.ReadyToOrder)
        {
            CurrentAction = ICustomer.Action.WaitingForOrder;
            ChangeLayer(interactableLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.WaitingForOrder)
        {
            ChangeLayer(interactableLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.ReceivedFalseBurger)
        {
            if (TrueDrinkReceived)
                CurrentAction = ICustomer.Action.ReceivedTrueDrink;
            else
                CurrentAction = ICustomer.Action.WaitingForOrder;

            ChangeLayer(interactableLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.ReceivedTrueBurger)
        {
            ChangeLayer(interactableLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.ReceivedFalseDrink)
        {
            if (TrueBurgerReceived)
                CurrentAction = ICustomer.Action.ReceivedTrueBurger;
            else
                CurrentAction = ICustomer.Action.WaitingForOrder;

            ChangeLayer(interactableLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.ReceivedTrueDrink)
        {
            ChangeLayer(interactableLayer);
            GameManager.Instance.SetOrderThrowArea(true);
        }
        else if (CurrentAction == ICustomer.Action.GotAnswerD || CurrentAction == ICustomer.Action.NotGotAnswer)
        {
            ChangeLayer(customerLayer);
            skinnedMeshRenderer.material = hikmetOutfit1Angry;
            anim.SetBool("giveOtherBack", true);
            anim.SetTrigger("giveBack");
        }
        else if (CurrentAction == ICustomer.Action.GotAnswerA)
        {
            HandleHeadHome();
        }

    }

    public void ReceiveBurger(BurgerBox burgerBox)
    {
        ChangeLayer(uninteractableLayer);
    }

    public void ReceiveDrink(Drink drink)
    {

        ChangeLayer(uninteractableLayer);

        if (drink.data.drinkType == DrinkType)
            HandleDrinkTrue(drink.data.drinkType);
        else
            HandleDrinkFalse(drink.data.drinkType);
    }

    public void OnInteract()
    {
        if (!CanInteract) return;

        if (CurrentAction == ICustomer.Action.ReadyToOrder)
        {
            anim.SetTrigger("talk");
            DialogueManager.Instance.StartCustomerDialogue(this, beforeOrderDialogueData);
        }

        else
        {
            DialogueManager.Instance.StartCustomerDialogue(this, afterOrderDialogueData);
        }

        ChangeLayer(uninteractableLayer);
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

    public void ChangeLayer(int layer)
    {
        ChangeLayerRecursive(gameObject, layer);
    }

    // Bu da iþi yapan yardýmcý fonksiyon (Private olabilir)
    private void ChangeLayerRecursive(GameObject obj, int newLayer)
    {
        if (null == obj) return;

        // 1. Þu anki objeyi deðiþtir
        obj.layer = newLayer;

        // 2. Çocuklarý gez
        foreach (Transform child in obj.transform)
        {
            if (null == child) continue;

            // DÝKKAT: Burada sadece layer atamak yerine, fonksiyonu tekrar çaðýrýyoruz.
            // Böylece child da kendi içine (torunlara) bakýyor.
            ChangeLayerRecursive(child.gameObject, newLayer);
        }
    }

    private void HandleBurgerTrue()
    {
        TrueBurgerReceived = true;

        if (TrueDrinkReceived)
        {
            ordersInRightHand[0].SetActive(true);
            CurrentAction = ICustomer.Action.ReceivedAllOrder;
            DialogueManager.Instance.StartCustomerDialogue(this, CompleteOrderDialogueData);
        }
        else
        {
            ordersInLeftHand[0].SetActive(true);
            CurrentAction = ICustomer.Action.ReceivedTrueBurger;
            DialogueManager.Instance.StartCustomerDialogue(this, TrueBurgerDialogueData);
        }
    }

    private void HandleBurgerFalse()
    {
        ordersInRightHand[0].SetActive(true);
        CurrentAction = ICustomer.Action.ReceivedFalseBurger;
        anim.SetTrigger("giveBack");
        DialogueManager.Instance.StartCustomerDialogue(this, FalseBurgerDialogueData);
    }

    private void HandleDrinkTrue(GameManager.DrinkTypes drinkType)
    {
        TrueDrinkReceived = true;

        if (TrueBurgerReceived)
        {
            ordersInRightHand[(int)drinkType + 1].SetActive(true);
            CurrentAction = ICustomer.Action.ReceivedAllOrder;
            DialogueManager.Instance.StartCustomerDialogue(this, CompleteOrderDialogueData);
        }
        else
        {
            ordersInLeftHand[(int)drinkType + 1].SetActive(true);
            CurrentAction = ICustomer.Action.ReceivedTrueDrink;
            DialogueManager.Instance.StartCustomerDialogue(this, TrueDrinkDialogueData);
        }
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
        currentDestination = homeDestination;
        agent.enabled = true;
        CurrentAction = ICustomer.Action.GoingToDestination;
    }

    private void HandleSadHeadHome()
    {
        ChangeLayer(customerLayer);
        anim.SetBool("sadWalk", true);
        anim.SetBool("idle", false);
        agent.speed = sadWalkSpeed;
        currentDestination = homeDestination;
        agent.enabled = true;
        CurrentAction = ICustomer.Action.GoingToDestination;
    }

    private void RotateCustomer()
    {
        Vector3 direction = facingDirectionTransform.position - transform.position;
        direction.y = 0f; // Y eksenini yok say
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);

            // Önceki tween'i iptal et (aksi halde üst üste biner)
            rotateTween?.Kill();

            // DOTween ile dönüþ animasyonu
            rotateTween = transform
                .DORotateQuaternion(targetRotation, CustomerData.rotationDuration)
                .SetEase(Ease.OutQuad); // Yumuþak yavaþlama efekti
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
    }

    private void GiveOtherOrderBack()
    {
    }

    public void HandleDialogueAnim(ICustomer.DialogueAnim dialogueAnim, float delay)
    {
        if (currentAnimCoroutine != null)
        {
            StopCoroutine(currentAnimCoroutine);
            currentAnimCoroutine = null;
        }

        currentAnimCoroutine = StartCoroutine(PlayAnimWithDelay(dialogueAnim, delay));
    }

    private IEnumerator PlayAnimWithDelay(ICustomer.DialogueAnim dialogueAnim, float delay)
    {
        yield return new WaitForSeconds(delay);
        
        if (dialogueAnim == ICustomer.DialogueAnim.TALK && !anim.GetCurrentAnimatorStateInfo(0).IsName("Talk"))
            anim.SetTrigger("talk");
    }
}
