using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopSeller : MonoBehaviour, IInteractable
{
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;
    private int grabableLayer;

    [SerializeField] private DialogueData firstDialogueData;
    [SerializeField] private DialogueData secondDialogueData;
    [SerializeField] private DialogueData noodleBuyDialogueData;
    [SerializeField] private DialogueData noodleBuyDialoguePartTwoData;
    [SerializeField] private float waitingTime = 1f;
    [Space]
    [SerializeField] private GameObject talkWithSellerText;
    [SerializeField] private GameObject storeBlocker;
    [Space]
    [SerializeField] private AudioClip sellerJumpscareSound;


    [HideInInspector] public bool isNoodlePlaced;
    [HideInInspector] public Noodle noodle;

    private bool isNoodleBought;
    private bool isJumpscared;
    private bool firstTalkIsFinished;

    private Animator anim;
    private AudioSource audioSource;

    public GameManager.HandRigTypes HandRigType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    private void Awake()
    {
        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        anim = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();

        isNoodlePlaced = false;
        isNoodleBought = false;
        isJumpscared = false;
        firstTalkIsFinished = false;
    }
    public void OnFocus()
    {
        gameObject.layer = OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer;
        talkWithSellerText.SetActive(true);
    }

    public void OnInteract()
    {
        talkWithSellerText.SetActive(false);

        if (isNoodlePlaced)
        {
            storeBlocker.SetActive(false);
            isNoodleBought = true;
            DialogueManager.Instance.StartSellerDialogue(noodleBuyDialogueData);
            noodle.OnLoseFocus();
        }  
        else if (!firstTalkIsFinished)
            DialogueManager.Instance.StartSellerDialogue(firstDialogueData);
        else
            DialogueManager.Instance.StartSellerDialogue(secondDialogueData);
    }

    public void OnLoseFocus()
    {
        gameObject.layer = interactableLayer;
        talkWithSellerText.SetActive(false);
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
        {
            gameObject.layer = interactableOutlinedRedLayer;
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            gameObject.layer = interactableOutlinedLayer;
        }
    }

    public void HandleFinishDialogue()
    {
        if (isJumpscared)
            StartSitBack();
        else if (isNoodleBought)
            StartJumpscareTalk();
    }

    private void StartJumpscareTalk()
    {
        gameObject.layer = uninteractableLayer;
        audioSource.PlayOneShot(sellerJumpscareSound);
        anim.SetBool("idle", true);

        isJumpscared = true;

        Invoke("StartPartTwoDialogue", waitingTime);

    }

    private void StartSitBack()
    {
        gameObject.layer = uninteractableLayer;
        anim.SetBool("idle", false);
        anim.SetBool("sit", true);
    }

    private void StartPartTwoDialogue()
    {
        DialogueManager.Instance.StartSellerDialogue(noodleBuyDialoguePartTwoData);
    }
}
