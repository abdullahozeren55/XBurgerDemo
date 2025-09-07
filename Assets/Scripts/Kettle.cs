using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cookable;

public class Kettle : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public GameManager.GrabTypes GrabType { get => grabType; set => grabType = value; }
    [SerializeField] private GameManager.GrabTypes grabType;

    public float HandLerp { get => handLerp; set => handLerp = value; }
    [SerializeField] private float handLerp;

    public bool IsGettingPutOnHologram;

    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;

    public AudioClip[] audioClips;

    [Header("Regular Settings")]
    [SerializeField] private GameObject hologramPart;
    [SerializeField] private GameObject grabText;
    [SerializeField] private GameObject dropText;
    [Space]
    [SerializeField] private Transform pourInstantiatePoint;
    [SerializeField] private ParticleSystem pourParticlePrefab;
    [SerializeField] private float timeToPutOnHologram = 0.3f;
    private ParticleSystem currentPourPrefab;

    private AudioSource audioSource;
    private Rigidbody rb;
    private Renderer hologramRenderer;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int ungrabableLayer;

    private Vector3 hologramPos;
    private Quaternion hologramRotation;

    private bool isJustThrowed;

    private Coroutine putOnHologramCoroutine;

    private float audioLastPlayedTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        IsGrabbed = false;

        isJustThrowed = false;

        audioLastPlayedTime = 0f;
    }

    private void Update()
    {

        if (HandLerp > 0.7f && (IsGrabbed || isJustThrowed))
        {
            if (currentPourPrefab == null || !currentPourPrefab.isPlaying)
            {
                currentPourPrefab = Instantiate(pourParticlePrefab, pourInstantiatePoint);
            }
        }
        else
        {
            if (currentPourPrefab != null && currentPourPrefab.isPlaying)
            {
                currentPourPrefab.Stop();
                currentPourPrefab = null;
            }
        }

    }

    public void PutOnHologram(Vector3 hologramPos, Quaternion hologramRotation)
    {
        IsGettingPutOnHologram = true;

        hologramPart.SetActive(false);

        audioSource.enabled = false;

        gameObject.layer = ungrabableLayer;

        IsGrabbed = false;
        HandleText(false);

        this.hologramPos = hologramPos;
        this.hologramRotation = hologramRotation;

        putOnHologramCoroutine = StartCoroutine(PutOnHologram());
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

        PlayAudioWithRandomPitch(0);

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        HandleText(true);

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = grabPositionOffset;
        transform.localRotation = Quaternion.Euler(grabRotationOffset);
    }
    public void OnFocus()
    {
        HandleText(true);
        gameObject.layer = grabableOutlinedLayer;
    }
    public void OnLoseFocus()
    {
        HandleText(false);
        gameObject.layer = grabableLayer;
    }

    public void OnDrop(Vector3 direction, float force)
    {
        IsGrabbed = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    public void OnThrow(Vector3 direction, float force)
    {
        IsGrabbed = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustThrowed = true;
    }

    public void SetGrabable()
    {
        gameObject.layer = grabableLayer;
    }

    private void HandleText(bool isFocused)
    {
        if (isFocused)
        {
            grabText.SetActive(!IsGrabbed);
            dropText.SetActive(IsGrabbed);
        }
        else
        {
            if (grabText.activeSelf) grabText.SetActive(false);
            if (dropText.activeSelf) dropText.SetActive(false);
        }
    }

    private void PlayAudioWithRandomPitch(int index)
    {
        audioLastPlayedTime = Time.time;
        audioSource.pitch = Random.Range(0.85f, 1.15f);
        audioSource.PlayOneShot(audioClips[index]);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Door") || collision.gameObject.CompareTag("Customer")))
        {
            if (isJustThrowed)
            {

                PlayAudioWithRandomPitch(2);

                isJustThrowed = false;
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

        putOnHologramCoroutine = null;
    }
}
