using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEditor.Experimental.GraphView.GraphView;

public class SaucePack : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public GameManager.GrabTypes GrabType { get => grabType; set => grabType = value; }
    [SerializeField] private GameManager.GrabTypes grabType;

    public float HandLerp { get => handLerp; set => handLerp = value; }
    [SerializeField] private float handLerp;

    public bool IsGettingPutOnHologram;

    public NoodleData data;

    [SerializeField] private GameObject hologramPart;
    [SerializeField] private GameObject grabText;
    [SerializeField] private GameObject dropText;
    [SerializeField] private Kettle kettle;

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
        hologramRenderer = hologramPart.GetComponent<Renderer>();

        foreach (Material material in hologramRenderer.materials)
        {
            Color color = material.color;

            color.a = 0f;

            material.color = color;
        }

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        IsGrabbed = false;
        IsGettingPutOnHologram = false;

        isJustThrowed = false;

        audioLastPlayedTime = 0f;
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

        audioSource.enabled = true;

        PlayAudioWithRandomPitch(0);

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        foreach (Material material in hologramRenderer.materials)
        {
            Color color = material.color;

            color.a = 40f / 255f;

            material.color = color;
        }

        IsGrabbed = true;

        HandleText(true);

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabRotationOffset);
    }
    public void OnFocus()
    {
        if (!IsGettingPutOnHologram)
        {
            HandleText(true);
            gameObject.layer = grabableOutlinedLayer;
        }

    }
    public void OnLoseFocus()
    {
        if (!IsGettingPutOnHologram)
        {
            HandleText(false);
            gameObject.layer = grabableLayer;
        }

    }

    public void OnDrop(Vector3 direction, float force)
    {
        IsGrabbed = false;

        transform.SetParent(null);

        foreach (Material material in hologramRenderer.materials)
        {
            Color color = material.color;

            color.a = 0f;

            material.color = color;
        }

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    public void OnThrow(Vector3 direction, float force)
    {
        IsGrabbed = false;

        transform.SetParent(null);

        foreach (Material material in hologramRenderer.materials)
        {
            Color color = material.color;

            color.a = 0f;

            material.color = color;
        }

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustThrowed = true;
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
        audioSource.PlayOneShot(data.audioClips[index]);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !IsGettingPutOnHologram && (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Door") || collision.gameObject.CompareTag("Customer")))
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

        while (timeElapsed < data.timeToPutOnHologram)
        {

            rate = timeElapsed / data.timeToPutOnHologram;

            transform.position = Vector3.Lerp(startPos, hologramPos, rate);
            transform.rotation = Quaternion.Slerp(startRotation, hologramRotation, rate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = hologramPos;
        transform.rotation = hologramRotation;

        kettle.SetGrabable();

        IsGettingPutOnHologram = false;

        putOnHologramCoroutine = null;
    }
}
