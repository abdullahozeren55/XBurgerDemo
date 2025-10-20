using Febucci.UI;
using Febucci.UI.Core;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    [System.Serializable]
    public class FontAtlasData
    {
        public FontType type;
        public Color fontColor;
    }
    public enum TalkType
    {
        TalkWithCustomer,
        TalkWithSeller,
        TalkWithYourself,
        TalkWithMascott,
        TalkWithYourselfInCutscene,
        TalkWithYourselfAfterInteraction
    }

    public enum TalkingPerson
    {
        Sinan,
        Customer0,
        Customer1,
    }

    public enum FontType
    {
        Default,
        Sinan,
        Hikmet
    }
    public static DialogueManager Instance { get; private set; }
    [Space]
    [SerializeField] private FontAtlasData[] fontAtlasDatas;
    [Space]

    public bool IsInDialogue;
    public bool IsSkipped;
    public bool IsDialogueComplete;

    [Space]
    [SerializeField] private FirstPersonController firstPersonController;
    [Space]
    [SerializeField] private TypewriterCore sinanTextAnim;
    [SerializeField] private TypewriterCore customer0TextAnim;
    [SerializeField] private TypewriterCore customer1TextAnim;
    [Space]
    [SerializeField] private AudioClip defaultDialogueAudio;
    [Space]
    [SerializeField] private TMP_Text sinanDialogueText;
    [SerializeField] private TMP_Text customer0DialogueText;
    [SerializeField] private TMP_Text customer1DialogueText;
    [Space]
    [SerializeField] private ShopSeller shopSeller;

    private DialogueData currentDialogueData;
    private TMP_Text currentDialogueText;
    private TypewriterCore currentTextAnim;

    private int dialogueIndex = 0;
    private TalkType talkType;

    private AudioSource audioSource;

    private KeyCode skipKey;

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
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();

        skipKey = firstPersonController.interactKey;

        currentCustomer = null;
        currentInteractable = null;

        IsInDialogue = false;
        IsSkipped = false;
        IsDialogueComplete = false;
    }

    private void Update()
    {
        if (IsInDialogue)
        {

            if (Input.GetKeyDown(skipKey))
            {
                if (!IsSkipped && !IsDialogueComplete)
                {
                    sinanTextAnim.SkipTypewriter();
                    customer0TextAnim.SkipTypewriter();
                    customer1TextAnim.SkipTypewriter(); //WE SKIP ALL INDIVIDUALLY BECAUSE SKIP MAKES IT FINISH IMMEDIATELY DOESNT MATTER IF ITS SHOWING OR DISAPPEARING. SO THEY ALL IMMEDIATELY FINISHES. IF WE ONLY USE CURRENTTEXTANIM DISAPPEARING OCCURS NORMALLY AND STAYS UNNECESSERILY.
                    IsSkipped = true;
                }
                else if (IsSkipped || IsDialogueComplete)
                {
                    dialogueIndex++;

                    if (dialogueIndex >= currentDialogueData.dialogueSegments.Length)
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
                    else
                    {
                        HandleDialogue();
                    }
                }

            }
            
        }
    }

    private void DecideCurrentText()
    {
        switch (currentDialogueData.dialogueSegments[dialogueIndex].talkingPerson)
        {
            case TalkingPerson.Sinan:

                if (currentDialogueText != sinanDialogueText)
                {
                    if (currentDialogueText != null)
                        currentTextAnim.StartDisappearingText();

                    currentDialogueText = sinanDialogueText;
                    currentTextAnim = sinanTextAnim;
                }

                break;

            case TalkingPerson.Customer0:

                if (currentDialogueText != customer0DialogueText)
                {
                    if (currentDialogueText != null)
                        currentTextAnim.StartDisappearingText();

                    currentDialogueText = customer0DialogueText;
                    currentTextAnim = customer0TextAnim;
                }
                
                break;

            case TalkingPerson.Customer1:

                if (currentDialogueText != customer1DialogueText)
                {
                    if (currentDialogueText != null)
                        currentTextAnim.StartDisappearingText();

                    currentDialogueText = customer1DialogueText;
                    currentTextAnim = customer1TextAnim;
                }
                
                break;
        }
    }

    private void HandleDialogue()
    {
        IsSkipped = false;
        IsDialogueComplete = false;

        DecideCurrentText();

        CameraManager.Instance.SwitchToCamera(currentDialogueData.dialogueSegments[dialogueIndex].cam);

        RectTransform rect = currentDialogueText.rectTransform;
        rect.anchoredPosition += currentDialogueData.dialogueSegments[dialogueIndex].DialogueOffset;

        DecideFontType(currentDialogueData.dialogueSegments[dialogueIndex].fontType);
        currentDialogueText.SetText(currentDialogueData.dialogueSegments[dialogueIndex].DialogueToPrint);

    }

    public void SetIsDialogueComplete(bool value)
    {
        IsDialogueComplete = value;
    }

    public void StartCustomerDialogue(ICustomer customer, DialogueData data)
    {
        GameManager.Instance.SetOrderThrowArea(false);
        currentDialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithCustomer;

        firstPersonController.CanMove = false;
        firstPersonController.CanUseHeadbob = false;
        dialogueIndex = 0;

        currentCustomer = customer;

        HandleDialogue();
    }

    private void EndCustomerDialogue()
    {
        IsSkipped = false;
        IsDialogueComplete = false;

        if (currentDialogueData.type == DialogueData.DialogueType.ENDSWITHACHOICE)
        {

            ChoiceManager.Instance.StartTheCustomerChoice(currentDialogueData.question, currentDialogueData.optionA, currentDialogueData.optionD,
                                    currentCustomer, currentCustomer.OptionADialogueData, currentCustomer.OptionDDialogueData, currentCustomer.NotAnsweringDialogueData, currentDialogueData.choiceCam);
        }
        else
        {
            currentCustomer.HandleFinishDialogue();
        }

        IsInDialogue = false;

        currentTextAnim.StartDisappearingText();

        firstPersonController.CanUseHeadbob = true;
    }

    public void StartAfterInteractionSelfDialogue(IInteractable interactable, DialogueData data)
    {
        currentDialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithYourselfAfterInteraction;

        firstPersonController.CanMove = false;
        firstPersonController.CanUseHeadbob = false;
        dialogueIndex = 0;

        currentInteractable = interactable;
    }

    private void EndAfterInteractionSelfDialogue()
    {

        currentInteractable.HandleFinishDialogue();

        IsInDialogue = false;

        firstPersonController.CanUseHeadbob = true;
    }

    public void StartSellerDialogue(DialogueData data)
    {
        currentDialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithSeller;

        firstPersonController.CanMove = false;
        firstPersonController.CanUseHeadbob = false;
        dialogueIndex = 0;
    }

    private void EndSellerDialogue()
    {
        shopSeller.HandleFinishDialogue();
        IsInDialogue = false;

        firstPersonController.CanUseHeadbob = true;

    }

    public void StartSelfDialogue(DialogueData data)
    {
        currentDialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithYourself;

        firstPersonController.CanMove = false;
        firstPersonController.CanUseHeadbob = false;
        dialogueIndex = 0;

    }

    private void EndSelfDialogue()
    {
        IsInDialogue = false;

        firstPersonController.CanUseHeadbob = true;

        firstPersonController.CanMove = true;
    }

    public void StartSelfDialogueInCutscene(DialogueData data)
    {
        currentDialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithYourselfInCutscene;

        firstPersonController.CanMove = false;
        firstPersonController.CanUseHeadbob = false;
        dialogueIndex = 0;
    }

    private void EndSelfDialogueInCutscene()
    {
        IsInDialogue = false;

        firstPersonController.CanUseHeadbob = true;

        if (currentDialogueData.type == DialogueData.DialogueType.ENDSWITHACUTSCENE)
        {
            CutsceneManager.Instance.StopCutscene();
            CutsceneManager.Instance.PlayCutscene(currentDialogueData.cutsceneType);
        }
    }

    private void DecideFontType(FontType type)
    {
        for (int i = 0; i < fontAtlasDatas.Length; i++)
        {
            if (type == fontAtlasDatas[i].type)
            {
                if (currentDialogueText.color != fontAtlasDatas[i].fontColor)
                    currentDialogueText.color = fontAtlasDatas[i].fontColor;

                break;
            }
        }
        
    }

    private void SetRandomPitch()
    {
        float pitch = Random.Range(1, 1.2f);

        audioSource.pitch = pitch;
    }
}
