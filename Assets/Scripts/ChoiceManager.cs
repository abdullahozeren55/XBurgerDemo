using Cinemachine;
using DG.Tweening;
using Febucci.UI.Core;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class ChoiceManager : MonoBehaviour
{
    private enum CurrentTween
    {
        NONE,
        PRESSA,
        PRESSD,
        NOTPRESSA,
        NOTPRESSD
    }


    public static ChoiceManager Instance;

    public bool IsInChoice;

    private CurrentTween currentATween;
    private CurrentTween currentDTween;

    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text optionAText;
    [SerializeField] private TMP_Text optionDText;
    [Space]
    [SerializeField] private TypewriterCore questionTextAnim;
    [SerializeField] private TypewriterCore optionATextAnim;
    [SerializeField] private TypewriterCore optionDTextAnim;
    [Space]
    [SerializeField] private Image timerImage;
    [SerializeField] private Image timerBackgroundImage;
    [SerializeField] private Image keyboardAImage;
    [SerializeField] private Image keyboardDImage;
    [Space]
    [SerializeField] private Sprite[] keyboardASprites; //0 not pressed, 1 pressed
    [SerializeField] private Sprite[] keyboardDSprites;
    [Space]
    [SerializeField] private Volume volume;
    private ColorAdjustments colorAdjust;
    [SerializeField] private float fadeTime = 0.5f;
    [SerializeField] private float slowedDownTimeSpeed = 0.25f;
    [SerializeField] private float cameraFovMultiplier = 2f;
    [SerializeField] private float timeToChoose = 5f;
    [SerializeField] private float timeToPressDown = 0.3f;
    [Space]
    [SerializeField] Color optionPressDownColor;
    [SerializeField] Vector3 optionPressDownScale;
    [Space]
    [SerializeField] private AudioSource slowMoBeginSource;
    [SerializeField] private AudioSource slowMoEndsSource;
    [SerializeField] private AudioSource clockTickingSource;
    [SerializeField] private AudioClip slowMoBeginSound;
    [SerializeField] private AudioClip slowMoEndsSound;
    [SerializeField] private AudioClip clockTickingSoundFx;

    [Header("Dialogue Parameters")]
    [SerializeField] private DialogueData optionADialogueData;
    [SerializeField] private DialogueData optionDDialogueData;
    [SerializeField] private DialogueData notAnsweringDialogueData;

    private Color timerImageStartColor;
    private Color timerBackgroundImageStartColor;
    private Color keyboardImageStartColor;

    private Color optionStartColor;
    private Vector3 optionStartScale;

    private bool isAPressed;
    private bool isDPressed;
    private bool isInAnim;

    private float originalCameraFov;
    private float changedCameraFov;
    private CinemachineVirtualCamera virtualCamera;

    private ICustomer currentCustomer;

    private Tween pressATween;
    private Tween notPressATween;
    private Tween pressDTween;
    private Tween notPressDTween;

    private Coroutine fadeInAndOutCoroutine;

    private void Awake()
    {
        // Eğer Instance zaten varsa, bu nesneyi yok et
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        volume.profile.TryGet(out colorAdjust);

        optionStartScale = optionAText.transform.localScale;
        optionStartColor = optionAText.color;

        currentCustomer = null;

        IsInChoice = false;

        timerImageStartColor = timerImage.color;
        timerBackgroundImageStartColor = timerBackgroundImage.color;
        keyboardImageStartColor = keyboardAImage.color;

        timerImage.color = Color.clear;
        timerBackgroundImage.color = Color.clear;
        keyboardAImage.color = Color.clear;
        keyboardDImage.color = Color.clear;

        questionText.text = null;
        optionAText.text = null;
        optionDText.text = null;

    }

    private void Update()
    {
        if (IsInChoice && !isInAnim)
        {
            isAPressed = Input.GetKey(KeyCode.A);
            isDPressed = Input.GetKey(KeyCode.D);

            HandleKeyboardImages();

            if (isAPressed && !isDPressed && currentATween != CurrentTween.PRESSA)
            {
                currentATween = CurrentTween.PRESSA;

                if (pressATween != null)
                {
                    pressATween.Kill();
                    pressATween = null;
                }

                if (notPressATween != null)
                {
                    notPressATween.Kill();
                    notPressATween = null;
                }

                pressATween = DOTween.Sequence()
                   .Append(keyboardAImage.transform.DOScale(optionPressDownScale, timeToPressDown))
                   .Join(keyboardAImage.DOColor(optionPressDownColor, timeToPressDown))
                   .Join(optionAText.DOColor(optionPressDownColor, timeToPressDown))
                   .OnComplete(() =>
                   {
                       FinishChoice(ICustomer.Action.GotAnswerA);
                   }).SetUpdate(true);
            }
            else if (!isAPressed && currentATween != CurrentTween.NOTPRESSA)
            {
                currentATween = CurrentTween.NOTPRESSA;

                if (pressATween != null)
                {
                    pressATween.Kill();
                    pressATween = null;
                }

                if (notPressATween != null)
                {
                    notPressATween.Kill();
                    notPressATween = null;
                }

                notPressATween = DOTween.Sequence()
                    .Append(keyboardAImage.transform.DOScale(optionStartScale, timeToPressDown/5f))
                    .Join(keyboardAImage.DOColor(optionStartColor, timeToPressDown)).SetUpdate(true)
                    .Join(optionAText.DOColor(optionStartColor, timeToPressDown)).SetUpdate(true);
            }

            if (isDPressed && !isAPressed && currentDTween != CurrentTween.PRESSD)
            {
                currentDTween = CurrentTween.PRESSD;

                if (pressDTween != null)
                {
                    pressDTween.Kill();
                    pressDTween = null;
                }

                if (notPressDTween != null)
                {
                    notPressDTween.Kill();
                    notPressDTween = null;
                }

                pressDTween = DOTween.Sequence()
                   .Append(keyboardDImage.transform.DOScale(optionPressDownScale, timeToPressDown))
                   .Join(keyboardDImage.DOColor(optionPressDownColor, timeToPressDown))
                   .Join(optionDText.DOColor(optionPressDownColor, timeToPressDown))
                   .OnComplete(() =>
                   {
                       FinishChoice(ICustomer.Action.GotAnswerD);
                   }).SetUpdate(true);
            }
            else if (!isDPressed && currentDTween != CurrentTween.NOTPRESSD)
            {
                currentDTween = CurrentTween.NOTPRESSD;

                if (pressDTween != null)
                {
                    pressDTween.Kill();
                    pressDTween = null;
                }

                if (notPressDTween != null)
                {
                    notPressDTween.Kill();
                    notPressDTween = null;
                }

                notPressDTween = DOTween.Sequence()
                    .Append(keyboardDImage.transform.DOScale(optionStartScale, timeToPressDown/5f))
                    .Join(keyboardDImage.DOColor(optionStartColor, timeToPressDown)).SetUpdate(true)
                    .Join(optionDText.DOColor(optionStartColor, timeToPressDown)).SetUpdate(true);
            }

        }
    }

    private void HandleKeyboardImages()
    {
        if (isAPressed && !isDPressed && keyboardAImage.sprite != keyboardASprites[1])
        {
            keyboardAImage.sprite = keyboardASprites[1];
        }
        else if (isDPressed && !isAPressed && keyboardDImage.sprite != keyboardDSprites[1])
        {
            keyboardDImage.sprite = keyboardDSprites[1];
        }

        if (!isAPressed && keyboardAImage.sprite != keyboardASprites[0])
            keyboardAImage.sprite = keyboardASprites[0];

        if (!isDPressed && keyboardDImage.sprite != keyboardDSprites[0])
            keyboardDImage.sprite = keyboardDSprites[0];
    }

    public void StartTheCustomerChoice
        (
        string question,
        string optionA,
        string optionD,
        ICustomer customer,
        DialogueData optionADialogue,
        DialogueData optionDDialogue,
        DialogueData notAnsweringDialogue,
        CameraManager.CameraName camToSwitch
        )
    {
        questionTextAnim.ShowText(question);
        optionATextAnim.ShowText(optionA);
        optionDTextAnim.ShowText(optionD);

        timerImage.fillAmount = 1f;

        optionADialogueData = optionADialogue;
        optionDDialogueData = optionDDialogue;
        notAnsweringDialogueData = notAnsweringDialogue;
        currentCustomer = customer;

        currentATween = CurrentTween.NONE;
        currentDTween = CurrentTween.NONE;

        CameraManager.Instance.SwitchToCamera(camToSwitch);

        virtualCamera = CameraManager.Instance.GetCamera();

        originalCameraFov = virtualCamera.m_Lens.FieldOfView;
        changedCameraFov = originalCameraFov * cameraFovMultiplier;

        IsInChoice = true;

        fadeInAndOutCoroutine = StartCoroutine(FadeInAndOut());
    }

    private void FinishChoice(ICustomer.Action action)
    {
        if (isInAnim) return;
        isInAnim = true;

        IsInChoice = false;

        isAPressed = false;
        isDPressed = false;

        pressATween?.Kill();
        notPressATween?.Kill();
        pressDTween?.Kill();
        notPressDTween?.Kill();

        if (fadeInAndOutCoroutine != null)
        {
            StopCoroutine(fadeInAndOutCoroutine);
            fadeInAndOutCoroutine = null;
        }
        StartCoroutine(FastOut(action));     
    }

    private IEnumerator FastOut(ICustomer.Action action)
    {
        float currentFov = virtualCamera.m_Lens.FieldOfView;
        float currentTimeScale = Time.timeScale;

        Color timerImageColor = timerImage.color;
        Color timerBackgroundImageColor = timerBackgroundImage.color;
        Color keyboardAImageColor = keyboardAImage.color;
        Color keyboardDImageColor = keyboardDImage.color;

        if (!slowMoEndsSource.isPlaying) slowMoEndsSource.PlayOneShot(slowMoEndsSound);

        questionTextAnim.StartDisappearingText();
        optionATextAnim.StartDisappearingText();
        optionDTextAnim.StartDisappearingText();

        float elapsedTime = 0f;
        float value = 0f;

        while (elapsedTime < fadeTime)
        {
            value = elapsedTime / fadeTime;

            colorAdjust.saturation.value = Mathf.Lerp(-100f, 0f, value);
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(currentFov, originalCameraFov, value);
            Time.timeScale = Mathf.Lerp(Time.timeScale, 1f, value);
            if (slowMoBeginSource.isPlaying) slowMoBeginSource.volume = Mathf.Lerp(slowMoBeginSource.volume, 0f, value);
            if (clockTickingSource.isPlaying) clockTickingSource.volume = Mathf.Lerp(clockTickingSource.volume, 0f, value);

            timerImage.color = Color.Lerp(timerImageColor, Color.clear, value);
            timerBackgroundImage.color = Color.Lerp(timerBackgroundImageColor, Color.clear, value);
            keyboardAImage.color = Color.Lerp(keyboardAImageColor, Color.clear, value);
            keyboardDImage.color = Color.Lerp(keyboardDImageColor, Color.clear, value);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        colorAdjust.saturation.value = 0f;
        virtualCamera.m_Lens.FieldOfView = originalCameraFov;
        Time.timeScale = 1f;

        if (slowMoBeginSource.isPlaying) slowMoBeginSource.Stop();
        if (clockTickingSource.isPlaying) clockTickingSource.Stop();

        slowMoBeginSource.volume = 1f;
        clockTickingSource.volume = 1f;

        timerImage.color = Color.clear;
        timerBackgroundImage.color = Color.clear;
        keyboardAImage.color = Color.clear;
        keyboardDImage.color = Color.clear;

        yield return new WaitForSecondsRealtime(0.05f);

        currentCustomer.CurrentAction = action;

        isInAnim = false;

        if (action == ICustomer.Action.GotAnswerA)
            DialogueManager.Instance.StartCustomerDialogue(currentCustomer, optionADialogueData);
        else if (action == ICustomer.Action.GotAnswerD)
            DialogueManager.Instance.StartCustomerDialogue(currentCustomer, optionDDialogueData);

    }

    private IEnumerator FadeInAndOut()
    {

        isInAnim = true;

        float startCameraFov = virtualCamera.m_Lens.FieldOfView;

        Color timerImageColor = timerImage.color;
        Color timerBackgroundImageColor = timerBackgroundImage.color;
        Color keyboardAImageColor = keyboardAImage.color;
        Color keyboardDImageColor = keyboardDImage.color;

        slowMoBeginSource.PlayOneShot(slowMoBeginSound);

        float timeElapsed = 0f;
        float value = 0f;

        while (timeElapsed < fadeTime)
        {
            value = timeElapsed / fadeTime;

            colorAdjust.saturation.value = Mathf.Lerp(0f, -100f, value);
            Time.timeScale = Mathf.Lerp(1f, slowedDownTimeSpeed, value);
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(startCameraFov, changedCameraFov, value);

            timerImage.color = Color.Lerp(timerImageColor, timerImageStartColor, value);
            timerBackgroundImage.color = Color.Lerp(timerBackgroundImageColor, timerBackgroundImageStartColor, value);
            keyboardAImage.color = Color.Lerp(keyboardAImageColor, keyboardImageStartColor, value);
            keyboardDImage.color = Color.Lerp(keyboardDImageColor, keyboardImageStartColor, value);

            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        colorAdjust.saturation.value = -100f;
        Time.timeScale = slowedDownTimeSpeed;
        virtualCamera.m_Lens.FieldOfView = changedCameraFov;

        timerImage.color = timerImageStartColor;
        timerBackgroundImage.color = timerBackgroundImageStartColor;
        keyboardAImage.color = keyboardImageStartColor;
        keyboardDImage.color = keyboardImageStartColor;

        isInAnim = false;

        clockTickingSource.PlayOneShot(clockTickingSoundFx);

        timeElapsed = 0f;
        value = 0f;

        while (timeElapsed < timeToChoose)
        {
            value = timeElapsed / timeToChoose;

            timerImage.fillAmount = Mathf.Lerp(1f, 0f, value);

            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        timerImage.fillAmount = 0f;

        IsInChoice = false;

        IsInChoice = false;

        isAPressed = false;
        isDPressed = false;

        bool pressAIsAboveThreshold = (pressATween != null && pressATween.ElapsedPercentage() > 0.4f) && (pressDTween == null || pressDTween.ElapsedPercentage() < 0.4f);
        bool pressDIsAboveThreshold = (pressDTween != null && pressDTween.ElapsedPercentage() > 0.4f) && (pressATween == null || pressATween.ElapsedPercentage() < 0.4f);

        pressATween.Kill();
        notPressATween.Kill();
        pressDTween.Kill();
        notPressDTween.Kill();

        questionTextAnim.StartDisappearingText();
        optionATextAnim.StartDisappearingText();
        optionDTextAnim.StartDisappearingText();

        isInAnim = true;

        timeElapsed = 0f;
        value = 0f;

        timerImageColor = timerImage.color;
        timerBackgroundImageColor = timerBackgroundImage.color;
        keyboardAImageColor = keyboardAImage.color;
        keyboardDImageColor = keyboardDImage.color;

        slowMoEndsSource.PlayOneShot(slowMoEndsSound);

        while (timeElapsed < fadeTime)
        {
            value = timeElapsed / fadeTime;

            colorAdjust.saturation.value = Mathf.Lerp(-100f, 0f, value);
            Time.timeScale = Mathf.Lerp(slowedDownTimeSpeed, 1f, value);
            virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(startCameraFov, originalCameraFov, value);

            timerImage.color = Color.Lerp(timerImageColor, Color.clear, value);
            timerBackgroundImage.color = Color.Lerp(timerBackgroundImageColor, Color.clear, value);
            keyboardAImage.color = Color.Lerp(keyboardAImageColor, Color.clear, value);
            keyboardDImage.color = Color.Lerp(keyboardDImageColor, Color.clear, value);

            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        colorAdjust.saturation.value = 0f;
        Time.timeScale = 1f;
        virtualCamera.m_Lens.FieldOfView = originalCameraFov;

        timerImage.color = Color.clear;
        timerBackgroundImage.color = Color.clear;
        keyboardAImage.color = Color.clear;
        keyboardDImage.color = Color.clear;

        if (pressAIsAboveThreshold)
        {
            currentCustomer.CurrentAction = ICustomer.Action.GotAnswerA;
            DialogueManager.Instance.StartCustomerDialogue(currentCustomer, optionADialogueData);
        }
        else if (pressDIsAboveThreshold)
        {
            currentCustomer.CurrentAction = ICustomer.Action.GotAnswerD;
            DialogueManager.Instance.StartCustomerDialogue(currentCustomer, optionDDialogueData);
        }
        else
        {
            currentCustomer.CurrentAction = ICustomer.Action.NotGotAnswer;
            DialogueManager.Instance.StartCustomerDialogue(currentCustomer, notAnsweringDialogueData);
        }

            

    }
}
