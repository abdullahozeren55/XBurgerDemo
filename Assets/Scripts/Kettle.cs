using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class Kettle : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public PlayerManager.HandGrabTypes HandGrabType { get => handGrabType; set => handGrabType = value; }
    [SerializeField] private PlayerManager.HandGrabTypes handGrabType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public Vector3 GrabPositionOffset { get => grabPositionOffset; set => grabPositionOffset = value; }
    [SerializeField] private Vector3 grabPositionOffset = new Vector3(0.4f, 0.1f, 2f);
    public Vector3 GrabRotationOffset { get => grabRotationOffset; set => grabRotationOffset = value; }
    [SerializeField] private Vector3 grabRotationOffset = new Vector3(-5f, -70f, -70f);

    [Space]

    [SerializeField] private Vector3 usePositionOffset;
    [SerializeField] private Vector3 useRotationOffset;
    public bool IsUseable { get => isUseable; set => isUseable = value; }
    [SerializeField] private bool isUseable = true;

    public bool IsGettingPutOnHologram;
    public bool CanGetFocused;

    public AudioClip[] audioClips;
    public string FocusText { get => focusText; set => focusText = value; }
    [SerializeField] private string focusText;
    [Space]
    [SerializeField] private ParticleSystem pourParticle;
    [SerializeField] private float timeToPutOnHologram = 0.3f;

    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    public float timeToUse = 0.3f;

    private bool isPlayingParticles;

    private Rigidbody rb;
    private Collider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;

    private Vector3 hologramPos;
    private Quaternion hologramRotation;

    private bool isJustThrowed;
    private bool isJustDropped;

    private Coroutine waterPourCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        IsGettingPutOnHologram = false;
        CanGetFocused = true;

        IsGrabbed = false;

        isJustThrowed = false;
        isJustDropped = false;
    }

    public void PutOnHologram(Vector3 hologramPos, Quaternion hologramRotation)
    {
        IsGettingPutOnHologram = true;
        isJustThrowed = false;
        isJustDropped = false;

        NoodleManager.Instance.SetHologramKettle(false);

        gameObject.layer = ungrabableLayer;

        IsGrabbed = false;

        this.hologramPos = hologramPos;
        this.hologramRotation = hologramRotation;

        StartCoroutine(PutOnHologram());
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

        col.enabled = false;

        SoundManager.Instance.PlaySoundFX(audioClips[0], transform, 1f, 0.85f, 1.15f);

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(grabLocalRotationOffset);
    }
    public void OnFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed && CanGetFocused)
            gameObject.layer = grabableOutlinedLayer;
    }
    public void OnLoseFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed && CanGetFocused)
            gameObject.layer = grabableLayer;
    }

    public void OnDrop(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.1f);

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustDropped = true;
    }

    public void OnThrow(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.1f);

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustThrowed = true;
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == grabableOutlinedLayer && OutlineShouldBeRed)
        {
            gameObject.layer = interactableOutlinedRedLayer;
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            gameObject.layer = grabableOutlinedLayer;
        }
    }

    private void TurnOnCollider()
    {
        col.enabled = true;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed)
            {
                gameObject.layer = grabableLayer;

                SoundManager.Instance.PlaySoundFX(audioClips[2], transform, 1f, 0.85f, 1.15f);

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                gameObject.layer = grabableLayer;

                SoundManager.Instance.PlaySoundFX(audioClips[1], transform, 1f, 0.85f, 1.15f);

                isJustDropped = false;
            }

        }


    }

    private IEnumerator PutOnHologram()
    {
        rb.isKinematic = true;

        Vector3 startPos = transform.position;
        Quaternion startRotation = transform.rotation;

        float timeElapsed = 0f;
        float rate = 0f;

        while (timeElapsed < timeToPutOnHologram)
        {

            rate = timeElapsed / timeToPutOnHologram;

            transform.position = Vector3.Lerp(startPos, hologramPos, rate);
            transform.rotation = Quaternion.Slerp(startRotation, hologramRotation, rate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = hologramPos;
        transform.rotation = hologramRotation;

        gameObject.layer = ungrabableLayer;

        CanGetFocused = false;
        IsGettingPutOnHologram = false;
    }

    public void OnUseHold()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(usePositionOffset, useRotationOffset, timeToUse);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        if (waterPourCoroutine != null)
        {
            StopCoroutine(waterPourCoroutine);
            waterPourCoroutine = null;
        }

        waterPourCoroutine = StartCoroutine(PourWater());
    }

    public void OnUseRelease()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(grabPositionOffset, grabRotationOffset, timeToUse / 2f);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        if (waterPourCoroutine != null)
        {
            StopCoroutine(waterPourCoroutine);
            waterPourCoroutine = null;
        }

        pourParticle.Stop();
        isPlayingParticles = false;
    }

    private IEnumerator PourWater()
    {
        yield return new WaitForSeconds(timeToUse * 2/3);

        if (!isPlayingParticles)
        {
            pourParticle.Play();
            isPlayingParticles = true;
        }

        waterPourCoroutine = null;

    }
}
