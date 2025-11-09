using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;
using static ICustomer;
using static UnityEngine.GraphicsBuffer;
using static UnityEngine.Rendering.DebugUI;

public class NpcCustomer : MonoBehaviour, ICustomer, IInteractable
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

    [Header("Footstep Parameters")]
    private bool shouldPlayFootstep;
    private Material currentGroundMaterial;
    private AudioClip lastPlayedFootstep;

    public string FocusText { get => focusText; set => focusText = value; }
    [SerializeField] private string focusText;

    public Transform CameraLookAt { get => cameraLookAt; set => cameraLookAt = value; }
    [SerializeField] private Transform cameraLookAt;

    [Header("Push Player Settings")]
    [SerializeField] private Transform rayPointForPushingPlayer;

    [Header("Other Settings")]
    [SerializeField] private float giveOrderBackMultiplier = 1f;

    private int day;
    private Tween rotateTween;
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

        TrueBurgerReceived = false;
        TrueDrinkReceived = false;

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
            HandleFootsteps();
            HandlePushingPlayer();
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
            PlayerManager.Instance.SetPlayerPushedByCustomer(true);
            PlayerManager.Instance.MovePlayer(transform.forward * CustomerData.pushForce * Time.deltaTime);
        }
        else
        {
            PlayerManager.Instance.SetPlayerPushedByCustomer(false);
        }
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
        else if (CurrentAction == ICustomer.Action.ReceivedAllOrder)
        {
            HandleHeadHome();
        }

    }

    public void ReceiveBurger(BurgerBox burgerBox)
    {
        ChangeLayer(uninteractableLayer);

        if (burgerBox.burgerType == BurgerType)
            HandleBurgerTrue();
        else
            HandleBurgerFalse();
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

        ChangeLayer(uninteractableLayer);

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
        if (!CanInteract) return;

        ChangeLayer(interactableOutlinedLayer);
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
        gameObject.layer = layer;
        skinnedMeshRenderer.gameObject.layer = layer;

        foreach (GameObject item in ordersInRightHand)
        {
            item.layer = layer;
        }

        foreach (GameObject item in ordersInLeftHand)
        {
            item.layer = layer;
        }
    }

    public void HandleDialogueAnim(DialogueAnim dialogueAnim)
    {
        if (dialogueAnim == DialogueAnim.TALK && !anim.GetCurrentAnimatorStateInfo(0).IsName("Talk") && CurrentAction != ICustomer.Action.ReceivedFalseDrink && CurrentAction != ICustomer.Action.ReceivedFalseBurger)
            anim.SetTrigger("talk");
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

    private void ChangeShouldPlayFootstep() => shouldPlayFootstep = !shouldPlayFootstep;

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
        if (ordersInRightHand[0].activeSelf)
        {
            GameManager.Instance.CustomerGiveBackBurger(ordersInRightHand[0].transform, customerData.throwForce * transform.forward * giveOrderBackMultiplier);
            ordersInRightHand[0].SetActive(false);
        }
        else
        {
            GameManager.Instance.CustomerGiveBackDrink(ordersInRightHand[1].transform, customerData.throwForce * transform.forward * giveOrderBackMultiplier);
            ordersInRightHand[1].SetActive(false);
            ordersInRightHand[2].SetActive(false);
            ordersInRightHand[3].SetActive(false);
        }

    }
}
