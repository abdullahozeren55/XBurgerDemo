using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Trash : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public TrashData data;

    public string FocusText { get => focusText; set => focusText = value; }
    [SerializeField] private string focusText;
    [Space]


    private AudioSource audioSource;
    private Rigidbody rb;
    private Collider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;

    private bool isJustThrowed;
    private bool isJustDropped;

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

        transform.rotation = Random.rotation;
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

        col.enabled = false;

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, 1f, 0.85f, 1.15f);

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
        if (!isJustDropped && !isJustThrowed)
            gameObject.layer = grabableOutlinedLayer;
    }
    public void OnLoseFocus()
    {
        if (!isJustDropped && !isJustThrowed)
            gameObject.layer = grabableLayer;
    }

    public void OnDrop(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustDropped = true;
    }

    public void OnThrow(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

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

    private void OnDestroy()
    {
        PlayerManager.Instance.ResetPlayerGrab(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed)
            {

                SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, 1f, 0.85f, 1.15f);

                gameObject.layer = grabableLayer;

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

    public void OnUseHold()
    {
        throw new System.NotImplementedException();
    }

    public void OnUseRelease()
    {
        throw new System.NotImplementedException();
    }
}
