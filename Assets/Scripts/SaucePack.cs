using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SaucePack : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public Vector3 GrabPositionOffset { get => grabPositionOffset; set => grabPositionOffset = value; }
    [SerializeField] private Vector3 grabPositionOffset = new Vector3(0.4f, 0.1f, 2f);
    public Vector3 GrabRotationOffset { get => grabRotationOffset; set => grabRotationOffset = value; }
    [SerializeField] private Vector3 grabRotationOffset = new Vector3(-5f, -70f, -70f);

    public bool IsGettingPutOnHologram;

    public NoodleData data;

    [SerializeField] private GameObject hologramPart;
    public Sprite FocusImage { get => focusImage; set => focusImage = value; }
    [SerializeField] private Sprite focusImage;
    [Space]

    private AudioSource audioSource;
    private Rigidbody rb;
    private Collider col;
    private Renderer hologramRenderer;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
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
        col = GetComponent<Collider>();
        hologramRenderer = hologramPart.GetComponent<Renderer>();

        foreach (Material material in hologramRenderer.materials)
        {
            Color color = material.color;

            color.a = 0f;

            material.color = color;
        }

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
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

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabRotationOffset);
    }
    public void OnFocus()
    {
        if (!IsGettingPutOnHologram)
        {
            gameObject.layer = grabableOutlinedLayer;
        }

    }
    public void OnLoseFocus()
    {
        if (!IsGettingPutOnHologram)
        {
            gameObject.layer = grabableLayer;
        }

    }

    public void OnDrop(Vector3 direction, float force)
    {
        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.08f);

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

        Invoke("TurnOnCollider", 0.08f);

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

    private void OnTriggerEnter(Collider other)
    {
        if (!IsGrabbed && !IsGettingPutOnHologram && other.CompareTag("Water"))
        {
            NoodleManager.Instance.AddSauceToWater();
            Destroy(gameObject);
        }
    }

    private void TurnOnCollider()
    {
        col.enabled = true;
    }

    private void OnDestroy()
    {
        GameManager.Instance.ResetPlayerGrab(this);
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

        NoodleManager.Instance.kettle.SetGrabable();

        IsGettingPutOnHologram = false;

        putOnHologramCoroutine = null;
    }

    public void OnUseHold()
    {
        throw new System.NotImplementedException();
    }

    public void OnUseRelease()
    {
        throw new System.NotImplementedException();
    }
}
