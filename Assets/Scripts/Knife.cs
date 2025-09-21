using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Knife : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    public Vector3 GrabPositionOffset { get => grabPositionOffset; set => grabPositionOffset = value; }
    [SerializeField] private Vector3 grabPositionOffset = new Vector3(0.4f, 0.1f, 2f);
    public Vector3 GrabRotationOffset { get => grabRotationOffset; set => grabRotationOffset = value; }
    [SerializeField] private Vector3 grabRotationOffset = new Vector3(-5f, -70f, -70f);

    [Space]

    [SerializeField] private Vector3 stabPositionOffset = new Vector3(0.2f, 0.3f, 1.5f);
    [SerializeField] private Vector3 stabRotationOffset = new Vector3(2.5f, -70f, -100f);

    [Space]

    private bool isGrabbed;

    public KnifeData data;
    [Space]
    [SerializeField] private GameObject grabText;
    [SerializeField] private GameObject dropText;
    [Space]
    [SerializeField] private float throwMultiplier;
    [Space]
    [SerializeField] private Collider triggerCol; 

    private AudioSource audioSource;
    private Rigidbody rb;
    private Collider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int ungrabableLayer;

    private bool isJustThrowed;
    private bool isStuck;

    private float audioLastPlayedTime;

    private Coroutine stabCoroutine;

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

        if (stabCoroutine != null)
        {
            StopCoroutine(stabCoroutine);
            stabCoroutine = null;
        }

        transform.localScale = data.grabScaleOffset;
        GameManager.Instance.SetPlayerIsUsingItemXY(false, false);

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

        triggerCol.enabled = false;

        PlayAudioWithRandomPitch(0);

        isJustThrowed = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        HandleText(true);

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabRotationOffset);
        transform.localScale = data.grabScaleOffset;
    }

    public void OnLoseFocus()
    {
        HandleText(false);
        gameObject.layer = grabableLayer;
    }

    public void OnThrow(Vector3 direction, float force)
    {
        IsGrabbed = false;

        if (stabCoroutine != null)
        {
            StopCoroutine(stabCoroutine);
            stabCoroutine = null;
        }

        triggerCol.enabled = true;

        transform.localScale = data.grabScaleOffset;
        GameManager.Instance.SetPlayerIsUsingItemXY(false, false);

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

                triggerCol.enabled = false;

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
        GameManager.Instance.SetPlayerAnimBool("stabRight", true);
        GameManager.Instance.SetPlayerUseHandLerp(stabPositionOffset, stabRotationOffset, data.timeToStab);
        GameManager.Instance.SetPlayerIsUsingItemXY(false, true);

        triggerCol.enabled = true;

        if (stabCoroutine != null)
        {
            StopCoroutine(stabCoroutine);
            stabCoroutine = null;
        }

        stabCoroutine = StartCoroutine(Stab(true));
    }

    public void OnUseRelease()
    {
        GameManager.Instance.SetPlayerAnimBool("stabRight", false);
        GameManager.Instance.SetPlayerUseHandLerp(grabPositionOffset, grabRotationOffset, data.timeToStab/2f);
        GameManager.Instance.SetPlayerIsUsingItemXY(false, false);

        triggerCol.enabled = false;

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
        Vector3 startScale = transform.localScale;

        Vector3 endPos = shouldStab ? data.stabPositionOffset : data.grabPositionOffset;
        Quaternion endRot = shouldStab ? Quaternion.Euler(data.stabRotationOffset) : Quaternion.Euler(data.grabRotationOffset);
        Vector3 endScale = shouldStab ? data.stabScaleOffset : data.grabScaleOffset;

        float elapsedTime = 0f;
        float value = 0f;

        while (elapsedTime < data.timeToStab)
        {
            value = elapsedTime / data.timeToStab;

            transform.localPosition = Vector3.Lerp(startPos, endPos, value);
            transform.localRotation = Quaternion.Lerp(startRot, endRot, value);
            transform.localScale = Vector3.Lerp(startScale, endScale, value);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = endPos;
        transform.localRotation = endRot;
        transform.localScale = endScale;

        stabCoroutine = null;

    }
}
