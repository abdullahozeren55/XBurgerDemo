using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FoodPack : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public FoodPackData data;

    public Sprite FocusImage { get => data.focusImage; set => data.focusImage = value; }
    [Space]

    [SerializeField] private Rigidbody[] allRB;
    [SerializeField] private Collider[] allCollider;
    [SerializeField] private Transform[] allTransform;

    private AudioSource audioSource;
    private Rigidbody rb;
    private Collider col;
    private MeshRenderer meshRenderer;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
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
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
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

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);
    }
    public void OnFocus()
    {
        ChangeLayer(grabableOutlinedLayer);
    }
    public void OnLoseFocus()
    {
        ChangeLayer(grabableLayer);
    }

    public void OnDrop(Vector3 direction, float force)
    {
        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.08f);

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    public void OnThrow(Vector3 direction, float force)
    {
        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.08f);

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustThrowed = true;
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == grabableOutlinedLayer && OutlineShouldBeRed)
        {
            ChangeLayer(interactableOutlinedRedLayer);
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            ChangeLayer(grabableOutlinedLayer);
        }
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
        PlayerManager.Instance.ResetPlayerGrab(this);
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
