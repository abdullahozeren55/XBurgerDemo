using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cookable;

public class Knife : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public float HandLerp { get => handLerp; set => handLerp = value; }
    [SerializeField] private float handLerp;

    public KnifeData data;
    [Space]
    [SerializeField] private GameObject grabText;
    [SerializeField] private GameObject dropText;
    [Space]
    [SerializeField] private Vector3 grabPositionOffset;
    [SerializeField] private Vector3 grabRotationOffset;
    [Space]
    [SerializeField] private float throwMultiplier;

    private AudioSource audioSource;
    private Rigidbody rb;
    private Collider col;

    private Vector3 relativePosition;
    private Quaternion relativeRotation;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int ungrabableLayer;

    private bool isJustThrowed;
    private bool isStuck;

    private float audioLastPlayedTime;

    private void Awake()
    {

        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        IsGrabbed = false;

        isJustThrowed = false;
        isStuck = false;

        audioLastPlayedTime = 0f;
    }

    public void OnDrop(Vector3 direction, float force)
    {
        IsGrabbed = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    public void OnFocus()
    {
        HandleText(true);
        gameObject.layer = grabableOutlinedLayer;
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

        if (isStuck)
            Unstick();

        PlayAudioWithRandomPitch(0);

        isJustThrowed = false;

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

    public void OnLoseFocus()
    {
        HandleText(false);
        gameObject.layer = grabableLayer;
    }

    public void OnThrow(Vector3 direction, float force)
    {
        IsGrabbed = false;

        transform.SetParent(null);

        Vector3 throwDirection = Camera.main.transform.forward;
        Quaternion lookRotation = Quaternion.LookRotation(throwDirection);

        // Decompose to Euler angles to modify just Y and Z
        Vector3 euler = lookRotation.eulerAngles;

        // Modify Y and Z as needed
        euler.y += 180f;
        euler.z = 180f;

        // Apply the final rotation
        transform.rotation = Quaternion.Euler(euler);

        rb.useGravity = true;

        rb.AddForce(direction * throwMultiplier * force, ForceMode.Impulse);

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

    private void StickToSurface(Collision collision)
    {
        // Get the surface normal from the collision
        Vector3 surfaceNormal = collision.contacts[0].normal;

        // Define the sharp edge direction of the knife (e.g., forward direction)
        Vector3 sharpEdgeDirection = transform.forward;

        // Calculate the target rotation to align the sharp edge with the surface normal
        Quaternion targetRotation = Quaternion.FromToRotation(sharpEdgeDirection, surfaceNormal) * transform.rotation;

        // Zero out the Z rotation while keeping the X and Y rotations
        targetRotation = Quaternion.Euler(targetRotation.eulerAngles.x, targetRotation.eulerAngles.y, 180f);

        // Apply the rotation to the object
        transform.rotation = targetRotation;

        // Stop the object’s movement
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        // Attach the knife to the surface (set parent to the wall or surface)
        transform.SetParent(collision.transform);
        rb.isKinematic = true;

        // Save the relative position and rotation for possible future calculations
        relativePosition = transform.position - collision.transform.position;  // Relative position
        relativeRotation = Quaternion.Inverse(collision.transform.rotation) * transform.rotation;  // Relative rotation

        // Set the flag that the object is stuck
        isStuck = true;
    }

    private void Unstick()
    {
        transform.SetParent(null);

        rb.isKinematic = false;
        isStuck = false;
    }

    private void PlayAudioWithRandomPitch(int index)
    {
        audioLastPlayedTime = Time.time;
        audioSource.pitch = Random.Range(0.85f, 1.15f);
        audioSource.PlayOneShot(data.audioClips[index]);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Door") || collision.gameObject.CompareTag("Customer")))
        {
            if (isJustThrowed)
            {
                StickToSurface(collision);

                PlayAudioWithRandomPitch(2);

                isJustThrowed = false;
            }
            else if (Time.time > audioLastPlayedTime + 0.1f)
            {
                PlayAudioWithRandomPitch(1);
            }

        }


    }
}
