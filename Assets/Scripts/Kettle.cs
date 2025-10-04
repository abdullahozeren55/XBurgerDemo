using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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

    [SerializeField] private Vector3 stabPositionOffset = new Vector3(0.2f, 0.3f, 1.5f);
    [SerializeField] private Vector3 stabRotationOffset = new Vector3(2.5f, -70f, -100f);
    public bool IsUseable { get => isUseable; set => isUseable = value; }
    [SerializeField] private bool isUseable = true;

    [HideInInspector] public bool IsGettingPutOnHologram;

    public AudioClip[] audioClips;

    [Header("Regular Settings")]
    [SerializeField] private GameObject hologramPart;
    public Sprite FocusImage { get => focusImage; set => focusImage = value; }
    [SerializeField] private Sprite focusImage;
    [Space]
    [SerializeField] private ParticleSystem pourParticle;
    [SerializeField] private float timeToPutOnHologram = 0.3f;

    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]
    public Vector3 stabLocalPositionOffset;
    public Vector3 stabLocalRotationOffset;
    [Space]
    public float timeToStab = 0.3f;

    private bool isPlayingParticles;

    private AudioSource audioSource;
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

    private float audioLastPlayedTime;

    private Coroutine stabCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        IsGrabbed = false;

        isJustThrowed = false;
        isJustDropped = false;

        audioLastPlayedTime = 0f;
    }

    public void PutOnHologram(Vector3 hologramPos, Quaternion hologramRotation)
    {
        IsGettingPutOnHologram = true;

        hologramPart.SetActive(false);

        audioSource.enabled = false;

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

        PlayAudioWithRandomPitch(0);

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
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed)
            gameObject.layer = grabableOutlinedLayer;
    }
    public void OnLoseFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed)
            gameObject.layer = grabableLayer;
    }

    public void OnDrop(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.1f);

        if (stabCoroutine != null)
        {
            StopCoroutine(stabCoroutine);
            stabCoroutine = null;
        }

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

        if (stabCoroutine != null)
        {
            StopCoroutine(stabCoroutine);
            stabCoroutine = null;
        }

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

    public void SetGrabable()
    {
        gameObject.layer = grabableLayer;
    }

    private void PlayAudioWithRandomPitch(int index)
    {
        audioLastPlayedTime = Time.time;
        audioSource.pitch = Random.Range(0.85f, 1.15f);
        audioSource.PlayOneShot(audioClips[index]);
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

                PlayAudioWithRandomPitch(2);

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                gameObject.layer = grabableLayer;

                if (Time.time > audioLastPlayedTime + 0.1f)
                    PlayAudioWithRandomPitch(1);

                isJustDropped = false;
            }
            else if (Time.time > audioLastPlayedTime + 0.1f)
            {
                PlayAudioWithRandomPitch(1);
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
        NoodleManager.Instance.currentSaucePackGO.layer = grabableLayer;

        IsGettingPutOnHologram = false;
    }

    public void OnUseHold()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(stabPositionOffset, stabRotationOffset, timeToStab);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        if (stabCoroutine != null)
        {
            StopCoroutine(stabCoroutine);
            stabCoroutine = null;
        }

        stabCoroutine = StartCoroutine(Stab(true));
    }

    public void OnUseRelease()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(grabPositionOffset, grabRotationOffset, timeToStab / 2f);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        pourParticle.Stop();
        isPlayingParticles = false;

        if (stabCoroutine != null)
        {
            StopCoroutine(stabCoroutine);
            stabCoroutine = null;
        }

        stabCoroutine = StartCoroutine(Stab(false));
    }

    private IEnumerator Stab(bool shouldStab)
    {
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;

        Vector3 endPos = shouldStab ? stabLocalPositionOffset : grabLocalPositionOffset;
        Quaternion endRot = shouldStab ? Quaternion.Euler(stabLocalRotationOffset) : Quaternion.Euler(grabLocalRotationOffset);

        float elapsedTime = 0f;
        float value = 0f;

        while (elapsedTime < timeToStab)
        {
            value = elapsedTime / timeToStab;

            transform.localPosition = Vector3.Lerp(startPos, endPos, value);
            transform.localRotation = Quaternion.Lerp(startRot, endRot, value);

            if (shouldStab && !isPlayingParticles && value > 0.6f)
            {
                pourParticle.Play();
                isPlayingParticles = true;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = endPos;
        transform.localRotation = endRot;

        stabCoroutine = null;

    }
}
