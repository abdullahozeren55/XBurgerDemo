using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class ChoiceManager : MonoBehaviour
{
    private enum CurrentCoroutine
    {
        NONE,
        PRESSA,
        NOTPRESSA,
        PRESSD,
        NOTPRESSD
    }


    public static ChoiceManager Instance;

    private CurrentCoroutine currentCoroutine;

    [SerializeField] private TMP_Text questionText;
    [SerializeField] private TMP_Text optionAText;
    [SerializeField] private TMP_Text optionDText;
    [SerializeField] private GameObject visualPart;
    [SerializeField] private Image timer;
    [SerializeField] private Volume volume;
    private ColorAdjustments colorAdjust;
    [SerializeField] private float bwFadeInMultiplier = 0.1f;
    [SerializeField] private float bwFadeOutMultiplier = 0.9f;
    [SerializeField] private float slowedDownTimeSpeed = 0.25f;
    [SerializeField] private float cameraFovMultiplier = 2f;
    [SerializeField] private float timeToChoose = 5f;
    [SerializeField] private float timeToPressDown = 0.3f;
    [SerializeField] Color pressDownColor;
    [SerializeField] Vector3 pressDownScale;
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

    private bool isSlowMoEndsStarted;

    private Color startColor;
    private Vector3 startScale;
    private bool isAPressed;
    private bool isDPressed;

    private float bwFadeInTime;
    private float bwFadeOutTime;
    private float originalCameraFov;
    private float changedCameraFov;

    private ICustomer currentCustomer;

    private Tween pressATween;
    private Tween notPressATween;
    private Tween pressDTween;
    private Tween notPressDTween;

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

        isSlowMoEndsStarted = false;

        startScale = optionAText.transform.localScale;
        startColor = optionAText.color;

        currentCustomer = null;

        originalCameraFov = Camera.main.fieldOfView;
        changedCameraFov = originalCameraFov * cameraFovMultiplier;

        visualPart.SetActive(false);

    }

    private void Update()
    {
        if (visualPart.activeSelf)
        {
            isAPressed = Input.GetKey(KeyCode.A);
            isDPressed = Input.GetKey(KeyCode.D);

            if (isAPressed && currentCoroutine != CurrentCoroutine.PRESSA)
            {
                currentCoroutine = CurrentCoroutine.PRESSA;

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
                   .Append(optionAText.transform.DOScale(pressDownScale, timeToPressDown))
                   .Join(optionAText.DOColor(pressDownColor, timeToPressDown))
                   .OnComplete(() =>
                   {
                       FinishChoice();
                       currentCustomer.CurrentAction = ICustomer.Action.GotAnswerA;
                       DialogueManager.Instance.StartCustomerDialogue(currentCustomer, optionADialogueData);
                   });
            }
            else if (!isAPressed && !isDPressed && currentCoroutine == CurrentCoroutine.PRESSA)
            {
                currentCoroutine = CurrentCoroutine.NOTPRESSA;

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
                    .Append(optionAText.transform.DOScale(startScale, timeToPressDown))
                    .Join(optionAText.DOColor(startColor, timeToPressDown));
            }

            if (isDPressed && !isAPressed && currentCoroutine != CurrentCoroutine.PRESSD)
            {
                currentCoroutine = CurrentCoroutine.PRESSD;

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
                   .Append(optionDText.transform.DOScale(pressDownScale, timeToPressDown))
                   .Join(optionDText.DOColor(pressDownColor, timeToPressDown))
                   .OnComplete(() =>
                   {
                       FinishChoice();
                       currentCustomer.CurrentAction = ICustomer.Action.GotAnswerD;
                       DialogueManager.Instance.StartCustomerDialogue(currentCustomer, optionDDialogueData);
                   });
            }
            else if (!isDPressed && !isAPressed && currentCoroutine == CurrentCoroutine.PRESSD)
            {
                currentCoroutine = CurrentCoroutine.NOTPRESSD;

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
                    .Append(optionDText.transform.DOScale(startScale, timeToPressDown))
                    .Join(optionDText.DOColor(startColor, timeToPressDown));
            }

        }
    }

    public void StartTheCustomerChoice(string question, string optionA, string optionD, ICustomer customer, DialogueData optionADialogue, DialogueData optionDDialogue, DialogueData notAnsweringDialogue)
    {
        questionText.text = question;
        optionAText.text = "[A]\n" + optionA;
        optionDText.text = "[D]\n" + optionD;
        bwFadeInTime = timeToChoose * bwFadeInMultiplier;
        bwFadeOutTime = timeToChoose * bwFadeOutMultiplier;
        timer.fillAmount = 1f;

        optionADialogueData = optionADialogue;
        optionDDialogueData = optionDDialogue;
        notAnsweringDialogueData = notAnsweringDialogue;
        currentCustomer = customer;

        currentCoroutine = CurrentCoroutine.NONE;

        visualPart.SetActive(true);

        StartCoroutine(FadeInAndOut());
    }

    private void FinishChoice()
    {
        StopAllCoroutines();
        StartCoroutine(FastOut());

        visualPart.SetActive(false);
    }

    private IEnumerator FastOut()
    {
        float startAlpha = colorAdjust.saturation.value;
        float currentFov = Camera.main.fieldOfView;
        float currentTimeScale = Time.timeScale;

        if (!slowMoEndsSource.isPlaying) slowMoEndsSource.PlayOneShot(slowMoEndsSound);


        float elapsedTime = 0f;
        float fastOutTime = 5f;
        float value = 0f;

        while (elapsedTime < fastOutTime)
        {
            value = elapsedTime / fastOutTime;

            colorAdjust.saturation.value = Mathf.Lerp(startAlpha, 0f, value);
            Camera.main.fieldOfView = Mathf.Lerp(Camera.main.fieldOfView, originalCameraFov, value);
            Time.timeScale = Mathf.Lerp(Time.timeScale, 1f, value);
            if (slowMoBeginSource.isPlaying) slowMoBeginSource.volume = Mathf.Lerp(slowMoBeginSource.volume, 0f, value);
            if (clockTickingSource.isPlaying) clockTickingSource.volume = Mathf.Lerp(clockTickingSource.volume, 0f, value);

            elapsedTime += Time.unscaledDeltaTime;
            yield return null;
        }

        colorAdjust.saturation.value = 0f;

        Camera.main.fieldOfView = originalCameraFov;

        Time.timeScale = 1f;

        if (slowMoBeginSource.isPlaying) slowMoBeginSource.Stop();
        if (clockTickingSource.isPlaying) clockTickingSource.Stop();

        slowMoBeginSource.volume = 1f;
        clockTickingSource.volume = 1f;


    }

    private IEnumerator FadeInAndOut()
    {

        float startCameraFov = Camera.main.fieldOfView;

        slowMoBeginSource.PlayOneShot(slowMoBeginSound);

        float timeElapsed = 0f;
        float value = 0f;

        while (timeElapsed < bwFadeInTime)
        {
            value = timeElapsed / bwFadeInTime;

            colorAdjust.saturation.value = Mathf.Lerp(0f, -100f, value);
            Time.timeScale = Mathf.Lerp(1f, slowedDownTimeSpeed, value);
            Camera.main.fieldOfView = Mathf.Lerp(startCameraFov, changedCameraFov, value);

            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        colorAdjust.saturation.value = -100f;
        Time.timeScale = slowedDownTimeSpeed;
        Camera.main.fieldOfView = changedCameraFov;

        clockTickingSource.PlayOneShot(clockTickingSoundFx);

        startCameraFov = Camera.main.fieldOfView;

        timeElapsed = 0f;
        value = 0f;

        while (timeElapsed < bwFadeOutTime)
        {
            value = timeElapsed / bwFadeOutTime;

            colorAdjust.saturation.value = Mathf.Lerp(-100f, 0f, value);
            Time.timeScale = Mathf.Lerp(slowedDownTimeSpeed, 1f, value);
            Camera.main.fieldOfView = Mathf.Lerp(startCameraFov, originalCameraFov, value);
            timer.fillAmount = Mathf.Lerp(1f, 0f, value);

            if (bwFadeOutTime - timeElapsed < 1f && !isSlowMoEndsStarted)
            {
                slowMoEndsSource.PlayOneShot(slowMoEndsSound);
                isSlowMoEndsStarted = true;
            }

            timeElapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        colorAdjust.saturation.value = 0f;
        Time.timeScale = 1f;
        Camera.main.fieldOfView = originalCameraFov;
        timer.fillAmount = 0f;

        isSlowMoEndsStarted = false;

        FinishChoice();

        currentCustomer.CurrentAction = ICustomer.Action.NotGotAnswer;

        DialogueManager.Instance.StartCustomerDialogue(currentCustomer, notAnsweringDialogueData);

    }
}
