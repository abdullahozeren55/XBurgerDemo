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
        TalkWithYourselfAfterInteraction,
        TalkWithPhone
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
        Hikmet,
        Phone,
        NPCCustomer0,
        NPCCustomer1,
        NPCCustomer2,
        Metin,
        Nevzat,
        Sukran,
    }
    public static DialogueManager Instance { get; private set; }
    [Space]
    [SerializeField] private FontAtlasData[] fontAtlasDatas;
    [Space]

    public bool IsInDialogue;
    public bool IsInSelfDialogue;
    public bool IsSkipped;
    public bool IsDialogueComplete;

    [SerializeField] private TypewriterByCharacter sinanTextAnim;
    [SerializeField] private TypewriterByCharacter customer0TextAnim;
    [SerializeField] private TypewriterByCharacter customer1TextAnim;
    [SerializeField] private TypewriterByCharacter sinanSelfTalkTextAnim;
    [Space]
    [SerializeField] private AudioClip[] typewriterSounds;
    private float minPitch = 0.85f;
    private float maxPitch = 1.15f;
    private bool pitchDecided;
    private int typewriterIndex;
    [Space]
    [SerializeField] private TMP_Text sinanDialogueText;
    [SerializeField] private TMP_Text customer0DialogueText;
    [SerializeField] private TMP_Text customer1DialogueText;
    [SerializeField] private TMP_Text sinanSelfTalkDialogueText;
    [Space]
    [SerializeField] private ShopSeller shopSeller;

    private DialogueData currentDialogueData;
    private TMP_Text currentDialogueText;
    private TypewriterByCharacter currentTextAnim;
    [Space]
    private float defaultWaitNormal;
    private float defaultWaitMiddle;
    private float defaultWaitLong;

    private int dialogueIndex = 0;
    private TalkType talkType;

    private AudioSource typewriterAudioSource;

    private ICustomer currentCustomer;
    private IInteractable currentInteractable;

    private Coroutine skippingSelfTalkCoroutine;
    private Coroutine showingTextCoroutine;
    private Coroutine showingSelfTextCoroutine;

    private void Awake()
    {
        // Eðer Instance zaten varsa, bu nesneyi yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        typewriterAudioSource = GetComponent<AudioSource>();

        currentCustomer = null;
        currentInteractable = null;

        IsInDialogue = false;
        IsSkipped = false;
        IsDialogueComplete = false;

        defaultWaitNormal = sinanTextAnim.waitForNormalChars;
        defaultWaitMiddle = sinanTextAnim.waitMiddle;
        defaultWaitLong = sinanTextAnim.waitLong;
    }

    private void Update()
    {
        if (IsInDialogue)
        {

            if (InputManager.Instance.PlayerInteract())
            {
                PlayerManager.Instance.SetInteractKeyIsDone(true);

                if (!IsSkipped && !IsDialogueComplete && currentDialogueData.dialogueSegments[dialogueIndex].Skippable)
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
                        else if (talkType == TalkType.TalkWithSeller)
                            EndSellerDialogue();
                        else if (talkType == TalkType.TalkWithPhone)
                            EndPhoneDialogue();
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
        if (currentTextAnim != null)
            currentTextAnim.StartDisappearingText();

        switch (currentDialogueData.dialogueSegments[dialogueIndex].talkingPerson)
        {
            case TalkingPerson.Sinan:

                if (currentDialogueText != sinanDialogueText)
                {
                    currentDialogueText = sinanDialogueText;
                    currentTextAnim = sinanTextAnim;
                }

                break;

            case TalkingPerson.Customer0:

                if (currentDialogueText != customer0DialogueText)
                {
                    currentDialogueText = customer0DialogueText;
                    currentTextAnim = customer0TextAnim;
                }
                
                break;

            case TalkingPerson.Customer1:

                if (currentDialogueText != customer1DialogueText)
                {
                    currentDialogueText = customer1DialogueText;
                    currentTextAnim = customer1TextAnim;
                }
                
                break;
        }

        currentDialogueText.fontStyle = (FontStyles)currentDialogueData.dialogueSegments[dialogueIndex].fontStyle;
    }

    private void HandleDialogue()
    {
        IsSkipped = false;
        IsDialogueComplete = false;

        pitchDecided = false;

        if (IsInSelfDialogue)
        {
            if (skippingSelfTalkCoroutine != null)
            {
                StopCoroutine(skippingSelfTalkCoroutine);
                skippingSelfTalkCoroutine = null;
            }

            sinanSelfTalkTextAnim.StopShowingText();
            sinanSelfTalkTextAnim.StartDisappearingText();

            IsInSelfDialogue = false;
        }

        if (showingTextCoroutine != null)
        {
            StopCoroutine (showingTextCoroutine);
            showingTextCoroutine = null;
        }

        showingTextCoroutine = StartCoroutine(ShowTextWithDelay());

    }

    private void HandleSelfDialogue()
    {
        pitchDecided = false;

        if (showingSelfTextCoroutine != null)
        {
            StopCoroutine(showingSelfTextCoroutine);
            showingSelfTextCoroutine = null;
        }

        showingSelfTextCoroutine = StartCoroutine(ShowSelfTextWithDelay());
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

        PlayerManager.Instance.SetPlayerBasicMovements(false);
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

            ChoiceManager.Instance.StartTheCustomerChoice(LocalizationManager.Instance.GetText(currentDialogueData.QuestionKey),
                                                          LocalizationManager.Instance.GetText(currentDialogueData.OptionAKey),
                                                          LocalizationManager.Instance.GetText(currentDialogueData.OptionDKey),
                                    currentCustomer, currentCustomer.OptionADialogueData, currentCustomer.OptionDDialogueData, currentCustomer.NotAnsweringDialogueData, currentDialogueData.choiceCam);
        }
        else
        {
            CameraManager.Instance.SwitchToFirstPersonCamera();
            PlayerManager.Instance.SetPlayerBasicMovements(true);

            currentCustomer.HandleFinishDialogue();
        }

        IsInDialogue = false;

        currentTextAnim.StartDisappearingText();

        currentCustomer = null;
    }

    public void StartAfterInteractionSelfDialogue(IInteractable interactable, bool shouldBeUninteractable, DialogueData data)
    {
        currentDialogueData = data;

        IsInSelfDialogue = true;

        talkType = TalkType.TalkWithYourselfAfterInteraction;

        currentInteractable = interactable;

        PlayerManager.Instance.ResetPlayerInteract(currentInteractable, shouldBeUninteractable);

        dialogueIndex = 0;

        HandleSelfDialogue();
    }

    private void EndAfterInteractionSelfDialogue()
    {
        if (skippingSelfTalkCoroutine != null)
        {
            StopCoroutine(skippingSelfTalkCoroutine);
            skippingSelfTalkCoroutine = null;
        }

        currentInteractable.HandleFinishDialogue();

        IsInSelfDialogue = false;

        sinanSelfTalkTextAnim.StartDisappearingText();

        currentInteractable = null;
    }

    public void StartSellerDialogue(DialogueData data, bool shouldBeUninteractable)
    {
        currentDialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithSeller;

        PlayerManager.Instance.SetPlayerBasicMovements(false);

        PlayerManager.Instance.ResetPlayerInteract(currentInteractable, shouldBeUninteractable);

        dialogueIndex = 0;

        HandleDialogue();
    }

    private void EndSellerDialogue()
    {
        IsSkipped = false;
        IsDialogueComplete = false;

        shopSeller.HandleFinishDialogue();

        IsInDialogue = false;

        currentTextAnim.StartDisappearingText();

        CameraManager.Instance.SwitchToFirstPersonCamera();
        PlayerManager.Instance.SetPlayerBasicMovements(true);

    }

    public void StartSelfDialogue(DialogueData data)
    {
        currentDialogueData = data;

        IsInSelfDialogue = true;

        talkType = TalkType.TalkWithYourself;

        dialogueIndex = 0;

        HandleSelfDialogue();
    }

    private void EndSelfDialogue()
    {
        if (skippingSelfTalkCoroutine != null)
        {
            StopCoroutine(skippingSelfTalkCoroutine);
            skippingSelfTalkCoroutine = null;
        }

        IsInSelfDialogue = false;

        sinanSelfTalkTextAnim.StartDisappearingText();
    }

    public void StartPhoneDialogue(DialogueData data)
    {
        currentDialogueData = data;

        IsInDialogue = true;

        talkType = TalkType.TalkWithPhone;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        dialogueIndex = 0;

        HandleDialogue();
    }

    private void EndPhoneDialogue()
    {
        IsSkipped = false;
        IsDialogueComplete = false;

        IsInDialogue = false;

        currentTextAnim.StartDisappearingText();

        PhoneManager.Instance.HandleFinishingTheCall();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void StartSelfDialogueInCutscene(DialogueData data)
    {
        currentDialogueData = data;

        IsInSelfDialogue = true;

        talkType = TalkType.TalkWithYourselfInCutscene;

        dialogueIndex = 0;

        HandleSelfDialogue();
    }

    private void EndSelfDialogueInCutscene()
    {
        if (skippingSelfTalkCoroutine != null)
        {
            StopCoroutine(skippingSelfTalkCoroutine);
            skippingSelfTalkCoroutine = null;
        }

        IsInSelfDialogue = false;

        sinanSelfTalkTextAnim.StartDisappearingText();

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

    private void DecideFontTypeForSelfTalk(FontType type)
    {
        for (int i = 0; i < fontAtlasDatas.Length; i++)
        {
            if (type == fontAtlasDatas[i].type)
            {
                if (sinanSelfTalkDialogueText.color != fontAtlasDatas[i].fontColor)
                    sinanSelfTalkDialogueText.color = fontAtlasDatas[i].fontColor;

                break;
            }
        }
    }

    private void SetPitch()
    {
        if (pitchDecided) return;

        minPitch = currentDialogueData.dialogueSegments[dialogueIndex].minAudioPitch;
        maxPitch = currentDialogueData.dialogueSegments[dialogueIndex].maxAudioPitch;

        float pitch = Random.Range(minPitch, maxPitch);

        typewriterAudioSource.pitch = pitch;

        pitchDecided = true;
    }

    private void SelectNextAudio()
    {
        typewriterAudioSource.clip = typewriterSounds[typewriterIndex];

        if (typewriterIndex == typewriterSounds.Length - 1)
            typewriterIndex = 0;
        else
            typewriterIndex++;
    }

    public void PlayTypewriterAudio()
    {
        SelectNextAudio();
        SetPitch();

        typewriterAudioSource.Play();
    }

    private IEnumerator SkipSelfTalk()
    {
        yield return new WaitForSeconds(currentDialogueData.dialogueSegments[dialogueIndex].autoSkipTime);

        dialogueIndex++;

        if (dialogueIndex >= currentDialogueData.dialogueSegments.Length)
        {
            if (talkType == TalkType.TalkWithYourselfInCutscene)
                EndSelfDialogueInCutscene();
            else if (talkType == TalkType.TalkWithYourself)
                EndSelfDialogue();
            else if (talkType == TalkType.TalkWithYourselfAfterInteraction)
                EndAfterInteractionSelfDialogue();
        }
        else
        {
            HandleSelfDialogue();
        }
    }

    private IEnumerator ShowTextWithDelay()
    {
        CameraManager.Instance.SwitchToCamera(currentDialogueData.dialogueSegments[dialogueIndex].cam);

        if (currentDialogueData.dialogueSegments[dialogueIndex].dialogueCamType == DialogueData.DialogueCamType.CHANGEFOV)
            CameraManager.Instance.PlayCurrentCamFOV(currentDialogueData.dialogueSegments[dialogueIndex].TargetFOV,
                                                     currentDialogueData.dialogueSegments[dialogueIndex].FOVDuration,
                                                     currentDialogueData.dialogueSegments[dialogueIndex].FOVEase,
                                                     currentDialogueData.dialogueSegments[dialogueIndex].FOVEaseValue);

        else if (currentDialogueData.dialogueSegments[dialogueIndex].dialogueCamType == DialogueData.DialogueCamType.RESETFOV)
            CameraManager.Instance.EndCurrentCamFOV(currentDialogueData.dialogueSegments[dialogueIndex].FOVDuration,
                                                     currentDialogueData.dialogueSegments[dialogueIndex].FOVEase,
                                                     currentDialogueData.dialogueSegments[dialogueIndex].FOVEaseValue);

        yield return new WaitForSeconds(currentDialogueData.dialogueSegments[dialogueIndex].delay);

        DecideCurrentText();

        RectTransform rect = currentDialogueText.rectTransform;
        rect.anchoredPosition += currentDialogueData.dialogueSegments[dialogueIndex].DialogueOffset;

        if (talkType == TalkType.TalkWithSeller)
            shopSeller.HandleDialogueAnim(currentDialogueData.dialogueSegments[dialogueIndex].dialogueAnim);
        else
            currentCustomer?.HandleDialogueAnim(currentDialogueData.dialogueSegments[dialogueIndex].dialogueAnim, currentDialogueData.dialogueSegments[dialogueIndex].dialogueAnimDelay);

        DecideFontType(currentDialogueData.dialogueSegments[dialogueIndex].fontType);

        string key = currentDialogueData.dialogueSegments[dialogueIndex].DialogueKey;

        currentTextAnim.waitForNormalChars = defaultWaitNormal / currentDialogueData.dialogueSegments[dialogueIndex].typeSpeed;
        currentTextAnim.waitMiddle = defaultWaitMiddle / currentDialogueData.dialogueSegments[dialogueIndex].typeSpeed;
        currentTextAnim.waitLong = defaultWaitLong / currentDialogueData.dialogueSegments[dialogueIndex].typeSpeed;

        if (currentDialogueData.dialogueSegments[dialogueIndex].audioToPlay != null)
            SoundManager.Instance.PlayUISoundFXWithDelay(currentDialogueData.dialogueSegments[dialogueIndex].audioToPlay, currentDialogueData.dialogueSegments[dialogueIndex].audioToPlayVolume, currentDialogueData.dialogueSegments[dialogueIndex].audioToPlayMinPitch, currentDialogueData.dialogueSegments[dialogueIndex].audioToPlayMaxPitch, currentDialogueData.dialogueSegments[dialogueIndex].audioToPlayDelay);

        currentTextAnim.ShowText(LocalizationManager.Instance.GetText(key));
    }

    private IEnumerator ShowSelfTextWithDelay()
    {
        yield return new WaitForSeconds(currentDialogueData.dialogueSegments[dialogueIndex].delay);

        string key = currentDialogueData.dialogueSegments[dialogueIndex].DialogueKey;

        sinanSelfTalkTextAnim.SetTypewriterSpeed(currentDialogueData.dialogueSegments[dialogueIndex].typeSpeed);
        sinanSelfTalkTextAnim.ShowText(LocalizationManager.Instance.GetText(key));

        if (skippingSelfTalkCoroutine != null)
        {
            StopCoroutine(skippingSelfTalkCoroutine);
            skippingSelfTalkCoroutine = null;
        }

        DecideFontTypeForSelfTalk(currentDialogueData.dialogueSegments[dialogueIndex].fontType);

        skippingSelfTalkCoroutine = StartCoroutine(SkipSelfTalk());
    }
}
