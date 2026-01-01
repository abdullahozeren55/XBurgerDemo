using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Phone : MonoBehaviour, IGrabable
{
    public IGrabable Master => this;
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public Sprite Icon { get => icon; set => icon = value; }
    [SerializeField] private Sprite icon;

    public PlayerManager.HandGrabTypes HandGrabType { get => handGrabType; set => handGrabType = value; }
    [SerializeField] private PlayerManager.HandGrabTypes handGrabType;

    public bool IsThrowable { get => isThrowable; set => isThrowable = value; }
    [SerializeField] private bool isThrowable = false;

    public Transform LeftHandPoint { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public float ThrowMultiplier { get => throwMultiplier; set => throwMultiplier = value; }
    [SerializeField] private float throwMultiplier = 1f;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;

    public int PhoneState; // 0 regular, 1 flashlight

    public Vector3 GrabPositionOffset { get => grabPositionOffsets[PhoneState]; set => grabPositionOffsets[PhoneState] = value; }
    [SerializeField] private Vector3[] grabPositionOffsets;

    public Vector3 GrabRotationOffset { get => grabRotationOffsets[PhoneState]; set => grabRotationOffsets[PhoneState] = value; }
    [SerializeField] private Vector3[] grabRotationOffsets;

    public bool IsUseable { get => isUseable; set => isUseable = value; }
    [SerializeField] private bool isUseable = true;

    public string FocusTextKey { get => focusTextKey; set => focusTextKey = value; }
    [SerializeField] private string focusTextKey;
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
    [Space]
    public float grabSoundVolume = 1f;
    public float grabSoundMinPitch = 0.85f;
    public float grabSoundMaxPitch = 1.15f;
    [Space]
    public float dropSoundVolume = 1f;
    public float dropSoundMinPitch = 0.85f;
    public float dropSoundMaxPitch = 1.15f;

    private MeshRenderer meshRenderer;

    private float lastGrabbedTime;

    private Tween phoneUITween;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.enabled = false;

        lastGrabbedTime = 0f;

        PhoneState = 0;

        IsGrabbed = false;
    }

    public void OnHolster()
    {
    }

    public void OnGrab(Transform grabPoint)
    {
        IsGrabbed = true;

        meshRenderer.enabled = true;

        SoundManager.Instance.PlaySoundFX(audioClips[0], transform, grabSoundVolume, grabSoundMinPitch, grabSoundMaxPitch);

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
            SoundManager.Instance.PlaySoundFX(audioClips[1], transform, dropSoundVolume, dropSoundMinPitch, dropSoundMaxPitch);
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
        .OnComplete(() =>
        {
            GameManager.Instance.SetCursor(GameManager.CursorType.Hand);
            GameManager.Instance.SetCursorLock(false);
        });
    }

    public void FinishPhoneUI()
    {

        phoneUITween?.Kill();

        phoneUITween = phoneUIRectTransform.DOScale(new Vector3(0.6f, 0.6f, 0.6f), timeToUse / 1.2f)
        .SetEase(Ease.OutBack);

        Invoke("FinishPhoneUIP2", timeToUse / 3f);
    }

    private void FinishPhoneUIP2()
    {
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

        GameManager.Instance.SetCursor(GameManager.CursorType.Default);
        GameManager.Instance.SetCursorLock(true);

        PlayerManager.Instance.SetPlayerBasicMovements(true);
    }

    public void OutlineChangeCheck()
    {
    }

    public void ChangeLayer(int layer)
    {

    }

    public bool TryCombine(IGrabable otherItem)
    {
        return false;
    }

    public bool CanCombine(IGrabable otherItem)
    {
        return false;
    }
}
