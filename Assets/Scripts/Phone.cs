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

    public Vector3 GrabPositionOffset { get => grabPositionOffset; set => grabPositionOffset = value; }
    [SerializeField] private Vector3 grabPositionOffset;

    public Vector3 GrabRotationOffset { get => grabRotationOffset; set => grabRotationOffset = value; }
    [SerializeField] private Vector3 grabRotationOffset;

    public bool IsUseable { get => isUseable; set => isUseable = value; }
    [SerializeField] private bool isUseable = true;

    public string FocusText { get => focusText; set => focusText = value; }
    [SerializeField] private string focusText;
    [Space]
    
    [Space]
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]
    public Vector3 usePositionOffset;
    public Vector3 useRotationOffset;
    [Space]
    public Vector3 useLocalPositionOffset;
    public Vector3 useLocalRotationOffset;
    [Space]
    public float timeToUse = 0.3f;
    [Space]
    public AudioClip[] audioClips;

    private AudioSource audioSource;
    private MeshRenderer meshRenderer;

    private Coroutine useCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        meshRenderer = GetComponent<MeshRenderer>();

        meshRenderer.enabled = false;

        IsGrabbed = false;
    }

    public void OnGrab(Transform grabPoint)
    {
        IsGrabbed = true;

        meshRenderer.enabled = true;

        PlayAudioWithRandomPitch(0);

    }
    public void OnFocus()
    {
    }
    public void OnLoseFocus()
    {
    }

    private void TurnOffMeshRenderer()
    {
        meshRenderer.enabled = false;
        IsGrabbed = false;
    }

    public void OnDrop(Vector3 direction, float force)
    {

        Invoke("TurnOffMeshRenderer", 0.15f);

        if (useCoroutine != null)
        {
            StopCoroutine(useCoroutine);
            useCoroutine = null;
        }

    }

    public void OnThrow(Vector3 direction, float force)
    {
        IsGrabbed = false;

        if (useCoroutine != null)
        {
            StopCoroutine(useCoroutine);
            useCoroutine = null;
        }

        transform.SetParent(null);
    }

    private void PlayAudioWithRandomPitch(int index)
    {
        audioSource.pitch = Random.Range(0.85f, 1.15f);
        audioSource.PlayOneShot(audioClips[index]);
    }

    public void OnUseHold()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(usePositionOffset, useRotationOffset, timeToUse);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        if (useCoroutine != null)
        {
            StopCoroutine(useCoroutine);
            useCoroutine = null;
        }

        useCoroutine = StartCoroutine(Use(true));
    }

    public void OnUseRelease()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(GrabPositionOffset, GrabRotationOffset, timeToUse / 2f);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);


        if (useCoroutine != null)
        {
            StopCoroutine(useCoroutine);
            useCoroutine = null;
        }

        useCoroutine = StartCoroutine(Use(false));
    }

    private IEnumerator Use(bool shouldUse)
    {
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;

        Vector3 endPos = shouldUse ? useLocalPositionOffset : grabLocalPositionOffset;
        Quaternion endRot = shouldUse ? Quaternion.Euler(useLocalRotationOffset) : Quaternion.Euler(grabLocalRotationOffset);

        float elapsedTime = 0f;
        float value = 0f;

        while (elapsedTime < timeToUse)
        {
            value = elapsedTime / timeToUse;

            transform.localPosition = Vector3.Lerp(startPos, endPos, value);
            transform.localRotation = Quaternion.Lerp(startRot, endRot, value);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = endPos;
        transform.localRotation = endRot;

        useCoroutine = null;

    }

    public void OutlineChangeCheck()
    {
        throw new System.NotImplementedException();
    }
}
