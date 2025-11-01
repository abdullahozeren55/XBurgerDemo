using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Phone : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public PlayerManager.HandGrabTypes HandGrabType { get => handGrabType; set => handGrabType = value; }
    [SerializeField] private PlayerManager.HandGrabTypes handGrabType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;

    public int PhoneState; // 0 regular, 1 flashlight

    public Vector3 GrabPositionOffset { get => grabPositionOffsets[PhoneState]; set => grabPositionOffsets[PhoneState] = value; }
    [SerializeField] private Vector3[] grabPositionOffsets;

    public Vector3 GrabRotationOffset { get => grabRotationOffsets[PhoneState]; set => grabRotationOffsets[PhoneState] = value; }
    [SerializeField] private Vector3[] grabRotationOffsets;

    public bool IsUseable { get => isUseable; set => isUseable = value; }
    [SerializeField] private bool isUseable = true;

    public string FocusText { get => focusText; set => focusText = value; }
    [SerializeField] private string focusText;
    [Space]

    [Space]
    public Vector3 usePositionOffset;
    public Vector3 useRotationOffset;
    [Space]
    public float timeToUse = 0.3f;
    [SerializeField] private GameObject phoneUI;
    [SerializeField] private RectTransform phoneUIRectTransform;
    [Space]
    public AudioClip[] audioClips;

    private AudioSource audioSource;
    private MeshRenderer meshRenderer;

    private float lastGrabbedTime;

    private Tween phoneUITween;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.enabled = false;

        lastGrabbedTime = 0f;

        PhoneState = 0;

        IsGrabbed = false;
    }

    public void OnGrab(Transform grabPoint)
    {
        IsGrabbed = true;

        meshRenderer.enabled = true;

        SoundManager.Instance.PlaySoundFX(audioClips[0], transform, 1f, 0.85f, 1.15f);

        lastGrabbedTime = Time.time;

    }
    public void OnFocus()
    {
    }
    public void OnLoseFocus()
    {
    }

    private void TurnOffMeshRenderer()
    {
        if (PhoneManager.Instance.FlashlightIsOn)
            PhoneManager.Instance.HandleFlashlightPowerButton();
        meshRenderer.enabled = false;
        IsGrabbed = false;
    }

    public void OnDrop(Vector3 direction, float force)
    {
        if (Time.time >= lastGrabbedTime + 0.2f)
        {
            Invoke("TurnOffMeshRenderer", 0.15f);
        }
        else
        {
            Invoke("OnDropDelay", 0.2f - (Time.time - lastGrabbedTime));
        }

    }

    private void OnDropDelay()
    {
        OnDrop(Vector3.zero, 0f);
    }

    public void OnThrow(Vector3 direction, float force)
    {
        IsGrabbed = false;

        transform.SetParent(null);
    }

    private void HandlePhoneUI()
    {
        phoneUI.SetActive(true);

        PhoneManager.Instance.IsFocused = true;

        phoneUITween?.Kill();

        phoneUITween = phoneUIRectTransform.DOScale(Vector3.one, timeToUse / 1.2f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        });
    }

    public void FinishPhoneUI()
    {
        phoneUITween?.Kill();

        phoneUITween = phoneUIRectTransform.DOScale(new Vector3(0.75f, 0.75f, 0.75f), timeToUse / 1.2f)
        .SetEase(Ease.OutBack)
        .SetUpdate(true);

        Invoke("FinishPhoneUIP2", timeToUse / 3f);
    }

    private void FinishPhoneUIP2()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        phoneUI.SetActive(false);
        OnUseRelease();
        PlayerManager.Instance.PlayerStopUsingObject();
    }

    public void OnUseHold()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(usePositionOffset, useRotationOffset, timeToUse);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        PlayerManager.Instance.SetPlayerBasicMovements(false);

        CameraManager.Instance.SwitchToCamera(CameraManager.CameraName.PhoneLook);

        Invoke("HandlePhoneUI", timeToUse / 1.2f);
    }

    public void OnUseRelease()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(GrabPositionOffset, GrabRotationOffset, timeToUse / 2f);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        CameraManager.Instance.SwitchToCamera(CameraManager.CameraName.FirstPerson);

        PlayerManager.Instance.SetPlayerBasicMovements(true);
    }

    public void OutlineChangeCheck()
    {
        throw new System.NotImplementedException();
    }
}
