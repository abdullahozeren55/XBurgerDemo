using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FoodPack : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    public Vector3 GrabPositionOffset { get => grabPositionOffset; set => grabPositionOffset = value; }
    [SerializeField] private Vector3 grabPositionOffset = new Vector3(0.4f, 0.1f, 2f);
    public Vector3 GrabRotationOffset { get => grabRotationOffset; set => grabRotationOffset = value; }
    [SerializeField] private Vector3 grabRotationOffset = new Vector3(-5f, -70f, -70f);

    private bool isGrabbed;

    public FoodPackData data;

    [SerializeField] private GameObject grabText;
    [SerializeField] private GameObject dropText;

    [SerializeField] private Rigidbody[] allRB;
    [SerializeField] private Collider[] allCollider;
    [SerializeField] private Transform[] allTransform;

    private AudioSource audioSource;
    private Rigidbody rb;
    private Collider col;
    private MeshRenderer meshRenderer;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int ungrabableLayer;

    private bool isJustThrowed;

    private float audioLastPlayedTime;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        IsGrabbed = false;

        isJustThrowed = false;

        audioLastPlayedTime = 0f;
    }

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(ungrabableLayer);

        col.enabled = false;

        PlayAudioWithRandomPitch(0);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        HandleText(true);

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabRotationOffset);
    }
    public void OnFocus()
    {
        HandleText(true);
        ChangeLayer(grabableOutlinedLayer);
    }
    public void OnLoseFocus()
    {
        HandleText(false);
        ChangeLayer(grabableLayer);
    }

    public void OnDrop(Vector3 direction, float force)
    {
        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.05f);

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    public void OnThrow(Vector3 direction, float force)
    {
        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.05f);

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustThrowed = true;
    }

    public void Open()
    {
        gameObject.layer = ungrabableLayer;
        meshRenderer.enabled = false;
        col.enabled = false;

        for (int i = 0; i < allTransform.Length; i++)
        {
            if (data.haveWholeIngredient)
            {
                allTransform[i].GetComponent<WholeIngredient>().HandlePackOpening();
            }
            // Generate a random force direction and magnitude
            Vector3 randomForce = new Vector3(
                Random.Range(-0.5f, 0.5f), // Random x direction
                Random.Range(0.5f, 1f), // Random y direction
                Random.Range(-0.5f, 0.5f)  // Random z direction
            ).normalized * Random.Range(data.minForce, data.maxForce); // Apply random magnitude

            allRB[i].isKinematic = false;
            allRB[i].AddForce(randomForce, ForceMode.Impulse);

            allCollider[i].enabled = true;
            allTransform[i].SetParent(null);
        }

        Instantiate(data.destroyParticle, transform.position, Quaternion.Euler(transform.rotation.x - 90f, transform.rotation.y + 90f, transform.rotation.z + 90f));

        Destroy(gameObject);
    }

    private void ChangeLayer(int layer)
    {
        gameObject.layer = layer;

        foreach (Transform tr in allTransform)
        {
            tr.gameObject.layer = layer;
        }
    }

    private void TurnOnCollider()
    {
        col.enabled = true;
    }

    private void OnDestroy()
    {
        HandleText(false);
        GameManager.Instance.ResetPlayerGrab(this);
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
        audioSource.pitch = Random.Range(0.6f, 0.8f);
        audioSource.PlayOneShot(data.audioClips[index]);
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

    public void OnUseHold()
    {
        throw new System.NotImplementedException();
    }

    public void OnUseRelease()
    {
        throw new System.NotImplementedException();
    }
}
