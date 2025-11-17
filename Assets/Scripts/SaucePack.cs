using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaucePack : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public Vector3 GrabPositionOffset { get => grabPositionOffset; set => grabPositionOffset = value; }
    [SerializeField] private Vector3 grabPositionOffset = new Vector3(0.4f, 0.1f, 2f);
    public Vector3 GrabRotationOffset { get => grabRotationOffset; set => grabRotationOffset = value; }
    [SerializeField] private Vector3 grabRotationOffset = new Vector3(-5f, -70f, -70f);

    public bool IsUseable { get => isUseable; set => isUseable = value; }
    [SerializeField] private bool isUseable = true;

    public bool IsGettingPutOnHologram;
    public bool CanGetFocused;

    public NoodleData data;
    public string FocusTextKey { get => focusTextKey; set => focusTextKey = value; }
    [SerializeField] private string focusTextKey;
    [Space]

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

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        IsGrabbed = false;
        IsGettingPutOnHologram = false;
        CanGetFocused = true;

        isJustThrowed = false;
        isJustDropped = false;
    }

    public void PutOnHologram(Vector3 hologramPos, Quaternion hologramRotation)
    {
        IsGettingPutOnHologram = true;
        isJustThrowed = false;
        isJustDropped = false;

        NoodleManager.Instance.SetHologramSaucePack(false);

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

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, 1f, 0.85f, 1.15f);

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        NoodleManager.Instance.SetHologramSaucePack(true);

        IsGrabbed = true;

        NoodleManager.Instance.SetCurrentSaucePack(gameObject);

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabRotationOffset);
    }
    public void OnFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed && CanGetFocused)
        {
            gameObject.layer = grabableOutlinedLayer;
        }

    }
    public void OnLoseFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed && CanGetFocused)
        {
            gameObject.layer = grabableLayer;
        }

    }

    public void OnDrop(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

        transform.SetParent(null);

        NoodleManager.Instance.SetHologramSaucePack(false);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustDropped = true;
    }

    public void OnThrow(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

        transform.SetParent(null);

        NoodleManager.Instance.SetHologramSaucePack(false);

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

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !IsGettingPutOnHologram)
        {
            if (collision.gameObject.CompareTag("Noodle"))
            {
                NoodleManager.Instance.AddSauceToWater();
            }
            else if (!collision.gameObject.CompareTag("Player"))
            {
                if (isJustThrowed)
                {
                    gameObject.layer = grabableLayer;

                    SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, 1f, 0.85f, 1.15f);

                    isJustThrowed = false;
                }
                else if (isJustDropped)
                {
                    gameObject.layer = grabableLayer;
                    
                    SoundManager.Instance.PlaySoundFX(data.audioClips[1], transform, 1f, 0.85f, 1.15f);

                    isJustDropped = false;
                }
            }
            
        }
    }

    private void OnDestroy()
    {
        PlayerManager.Instance.ResetPlayerGrab(this);
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

        CanGetFocused = false;
        IsGettingPutOnHologram = false;
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
