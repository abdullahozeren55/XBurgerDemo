using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Noodle : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public bool IsGettingPutOnHologram;

    public NoodleData data;

    [SerializeField] private GameObject hologramPart;
    public string FocusText { get => focusText; set => focusText = value; }
    [SerializeField] private string focusText;
    [Space]

    [Header("Collider Settings")]
    [SerializeField] private Collider[] bodyColliders;
    [SerializeField] private Collider[] closeLidColliders;
    [SerializeField] private Collider[] openLidColliders;

    private SkinnedMeshRenderer skinnedMeshRederer;
    private AudioSource audioSource;
    private Rigidbody rb;
    private Renderer hologramRenderer;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;

    private Vector3 hologramPos;
    private Quaternion hologramRotation;

    private bool isJustThrowed;
    private bool isJustDropped;

    private float audioLastPlayedTime;

    private bool isOpened;


    private void Awake()
    {
        skinnedMeshRederer = GetComponent<SkinnedMeshRenderer>();
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
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        IsGrabbed = false;
        IsGettingPutOnHologram = false;

        isJustThrowed = false;
        isJustDropped = false;

        audioLastPlayedTime = 0f;
    }

    public void PutOnHologram(Vector3 hologramPos, Quaternion hologramRotation)
    {
        IsGettingPutOnHologram = true;

        hologramPart.SetActive(false);

        gameObject.layer = ungrabableLayer;

        IsGrabbed = false;

        this.hologramPos = hologramPos;
        this.hologramRotation = hologramRotation;

        StartCoroutine(PutOnHologram());
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

        SetColliders(false);

        audioSource.enabled = true;

        PlayAudioWithRandomPitch(0);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        foreach (Material material in hologramRenderer.materials)
        {
            Color color = material.color;

            color.a = 40f/255f;

            material.color = color;
        }

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);
    }
    public void OnFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed)
        {
            gameObject.layer = grabableOutlinedLayer;
        }

    }
    public void OnLoseFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed)
        {
            gameObject.layer = grabableLayer;
        }

    }

    public void OnDrop(Vector3 direction, float force)
    {
        SetColliders(true);

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

        isJustDropped = true;
    }

    public void OnThrow(Vector3 direction, float force)
    {
        SetColliders(true);

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

    private void SetColliders(bool value)
    {
        foreach (Collider col in bodyColliders)
        {
            col.enabled = value;
        }

        foreach (Collider col in closeLidColliders)
        {
            col.enabled = value == false ? false : !isOpened;
        }

        foreach (Collider col in openLidColliders)
        {
            col.enabled = value == false ? false : isOpened;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !IsGettingPutOnHologram && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed)
            {

                PlayAudioWithRandomPitch(2);

                gameObject.layer = grabableLayer;

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

        NoodleManager.Instance.SetCurrentNoodle(gameObject);


        IsGettingPutOnHologram = false;
    }

    public void OnUseHold()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(data.usePositionOffset, data.useRotationOffset, data.timeToUse);
        PlayerManager.Instance.SetPlayerLeftUseHandLerp(data.useLeftPositionOffset, data.useLeftRotationOffset);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        skinnedMeshRederer.SetBlendShapeWeight(0, 0F);
    }

    public void OnUseRelease()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(GrabPositionOffset, GrabRotationOffset, data.timeToUse / 2f);
        PlayerManager.Instance.PlayerResetLeftHandLerp();
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

    }
}
