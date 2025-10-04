using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public enum TalkType
    {
        TalkWithCustomer,
        TalkWithSeller,
        TalkWithYourself,
        TalkWithMascott,
        TalkWithYourselfInCutscene,
        TalkWithYourselfAfterInteraction
    }
    public static DialogueManager Instance { get; private set; }

    public bool IsInDialogue;

    [Space]
    [SerializeField] private FirstPersonController firstPersonController;
    [SerializeField] private AudioClip defaultDialogueAudio;
    [Space]
    [SerializeField] private TMP_Text personText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float dialogueTextNormalYValue = -66f;
    [SerializeField] private float dialogueTextSelfTalkYValue = -33f;
    [Space]
    [SerializeField] private ShopSeller shopSeller;
    private RectTransform dialogueRectTransform;

    private DialogueData dialogueData;
    private GameObject visualPart;

    private int dialogueIndex = 0;

    private bool playingDialogue;
    private bool skip;
    private TalkType talkType;

    private AudioSource audioSource;

    private KeyCode skipKey;

    private float coroutineTimeBeforeSkip = 0.15f;
    private float currentCoroutineTime;

    private ICustomer currentCustomer;
    private IInteractable currentInteractable;

    private void Awake()
    {
        // Eðer Instance zaten varsa, bu nesneyi yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        dialogueRectTransform = dialogueText.GetComponent<RectTransform>();

        visualPart = GetComponentInChildren<Image>().gameObject;
        visualPart.SetActive(false);

        audioSource = GetComponent<AudioSource>();

        skipKey = firstPersonController.interactKey;

        currentCustomer = null;
        currentInteractable = null;

        IsInDialogue = false;
    }

    private void Update()
    {
        if (visualPart.activeSelf)
        {
            if (currentCoroutineTime > 0f)
            {
                currentCoroutineTime -= Time.deltaTime;
            }

            if (Input.GetKeyDown(skipKey))
            {
                if (dialogueIndex >= dialogueData.dialogueSegments.Length)
                {
                    if (talkType == TalkType.TalkWithCustomer)
                        EndCustomerDialogue();
                    else if (talkType == TalkType.TalkWithYourselfAfterInteraction)
                        EndAfterInteractionSelfDialogue();
                    else if (talkType == TalkType.TalkWithSeller)
                        EndSellerDialogue();
                    else if (talkType == TalkType.TalkWithYourself)
                        EndSelfDialogue();
                    else if (talkType == TalkType.TalkWithYourselfInCutscene)
                        EndSelfDialogueInCutscene();
                }

                else if (!playingDialogue)
                {
                    StartCoroutine(PlayDialogue(dialogueData.dialogueSegments[dialogueIndex]));
                }
                else
                {
                    if (dialogueData.dialogueSegments[dialogueIndex].Skippable && currentCoroutineTime <= 0f)
                    {
                        skip = true;
                    }
                }
            }
            
        }
    }

    public void StartCustomerDialogue(ICustomer customer, DialogueData data)
    {
        GameManager.Instance.SetOrderThrowArea(false);
        dialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithCustomer;

        firstPersonController.CanMove = false;
        firstPersonController.CanUseHeadbob = false;
        dialogueIndex = 0;

        currentCustomer = customer;

        currentCoroutineTime = coroutineTimeBeforeSkip;

        visualPart.SetActive(true);

        StartCoroutine(PlayDialogue(dialogueData.dialogueSegments[dialogueIndex]));
    }

    private void EndCustomerDialogue()
    {

        if (dialogueData.type == DialogueData.DialogueType.ENDSWITHACHOICE)
        {
            visualPart.SetActive(false);

            ChoiceManager.Instance.StartTheCustomerChoice(dialogueData.question, dialogueData.optionA, dialogueData.optionD,
                                    currentCustomer, currentCustomer.OptionADialogueData, currentCustomer.OptionDDialogueData, currentCustomer.NotAnsweringDialogueData, dialogueData.choiceCam);
        }
        else
        {
            currentCustomer.HandleFinishDialogue();
        }

        IsInDialogue = false;

        firstPersonController.CanUseHeadbob = true;

        visualPart.SetActive(false);
    }

    public void StartAfterInteractionSelfDialogue(IInteractable interactable, DialogueData data)
    {
        dialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithYourselfAfterInteraction;

        firstPersonController.CanMove = false;
        firstPersonController.CanUseHeadbob = false;
        dialogueIndex = 0;

        currentInteractable = interactable;

        currentCoroutineTime = coroutineTimeBeforeSkip;

        visualPart.SetActive(true);

        StartCoroutine(PlayDialogue(dialogueData.dialogueSegments[dialogueIndex]));
    }

    private void EndAfterInteractionSelfDialogue()
    {

        currentInteractable.HandleFinishDialogue();

        IsInDialogue = false;

        firstPersonController.CanUseHeadbob = true;

        visualPart.SetActive(false);
    }

    public void StartSellerDialogue(DialogueData data)
    {
        dialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithSeller;

        firstPersonController.CanMove = false;
        firstPersonController.CanUseHeadbob = false;
        dialogueIndex = 0;

        currentCoroutineTime = coroutineTimeBeforeSkip;

        visualPart.SetActive(true);

        StartCoroutine(PlayDialogue(dialogueData.dialogueSegments[dialogueIndex]));
    }

    private void EndSellerDialogue()
    {
        shopSeller.HandleFinishDialogue();
        IsInDialogue = false;

        firstPersonController.CanUseHeadbob = true;

        visualPart.SetActive(false);
    }

    public void StartSelfDialogue(DialogueData data)
    {
        dialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithYourself;

        firstPersonController.CanMove = false;
        firstPersonController.CanUseHeadbob = false;
        dialogueIndex = 0;

        currentCoroutineTime = coroutineTimeBeforeSkip;

        visualPart.SetActive(true);

        StartCoroutine(PlayDialogue(dialogueData.dialogueSegments[dialogueIndex]));
    }

    private void EndSelfDialogue()
    {
        IsInDialogue = false;

        firstPersonController.CanUseHeadbob = true;

        visualPart.SetActive(false);


        firstPersonController.CanMove = true;
    }

    public void StartSelfDialogueInCutscene(DialogueData data)
    {
        dialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithYourselfInCutscene;

        firstPersonController.CanMove = false;
        firstPersonController.CanUseHeadbob = false;
        dialogueIndex = 0;

        currentCoroutineTime = coroutineTimeBeforeSkip;

        visualPart.SetActive(true);

        StartCoroutine(PlayDialogue(dialogueData.dialogueSegments[dialogueIndex]));
    }

    private void EndSelfDialogueInCutscene()
    {
        IsInDialogue = false;

        firstPersonController.CanUseHeadbob = true;

        visualPart.SetActive(false);

        if (dialogueData.type == DialogueData.DialogueType.ENDSWITHACUTSCENE)
        {
            CutsceneManager.Instance.StopCutscene();
            CutsceneManager.Instance.PlayCutscene(dialogueData.cutsceneType);
        }
    }

    private void SetRandomPitch()
    {
        float pitch = Random.Range(1, 1.2f);

        audioSource.pitch = pitch;
    }

    private IEnumerator PlayDialogue(DialogueData.DialogueSegment segment)
    {
        playingDialogue = true;
        
        CameraManager.Instance.SwitchToCamera(segment.cam);

        SetRandomPitch();

        if (segment.audioClip != null)
        {
            audioSource.PlayOneShot(segment.audioClip);
        }
        else
        {
            audioSource.PlayOneShot(defaultDialogueAudio);
        }
            

        dialogueText.SetText(string.Empty);
        personText.SetText(segment.PersonName);

        float delay = 1f / segment.LettersPerSecond;

        for (int i = 0; i < segment.DialogueToPrint.Length; i++)
        {
            if (skip)
            {
                dialogueText.SetText(segment.DialogueToPrint);
                skip = false;
                break;
            }

            string chunkToAdd = string.Empty;
            chunkToAdd += segment.DialogueToPrint[i];

            if (segment.DialogueToPrint[i] == ' ' && i < segment.DialogueToPrint.Length - 1)
            {
                chunkToAdd = segment.DialogueToPrint.Substring(i, 2);
                i++;
            }

            dialogueText.text += chunkToAdd;
            yield return new WaitForSeconds(delay);
        }

        playingDialogue = false;

        audioSource.Stop();

        dialogueIndex++;
    }
}
