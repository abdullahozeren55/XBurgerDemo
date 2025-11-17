using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Knife : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }
    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public KnifeData data;
    public string FocusTextKey { get => data.focusTextKey; set => data.focusTextKey = value; }
    [Space]
    [SerializeField] private Collider triggerCol; 

    private Rigidbody rb;
    private Collider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;

    private bool isJustThrowed;
    private bool isJustDropped;
    private bool isStuck;

    private Coroutine useCoroutine;

    private void Awake()
    {

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        IsGrabbed = false;

        isJustThrowed = false;
        isJustDropped = false;
        isStuck = false;
    }

    public void OnDrop(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

        if (useCoroutine != null)
        {
            StopCoroutine(useCoroutine);
            useCoroutine = null;
        }

        triggerCol.enabled = false;

        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustDropped = true;
    }

    public void OnFocus()
    {
        if (!isJustDropped && !isJustThrowed)
            gameObject.layer = grabableOutlinedLayer;
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

        if (isStuck)
            Unstick();

        triggerCol.enabled = false;
        col.enabled = false;

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, 1f, 0.85f, 1.15f);

        isJustThrowed = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);
    }

    public void OnLoseFocus()
    {
        if (!isJustDropped && !isJustThrowed)
            gameObject.layer = grabableLayer;
    }

    public void OnThrow(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

        if (useCoroutine != null)
        {
            StopCoroutine(useCoroutine);
            useCoroutine = null;
        }

        triggerCol.enabled = true;

        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

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

    private void TurnOffTriggerCol()
    {
        triggerCol.enabled = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed)
            {
                StickToSurface(collision);

                SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, 1f, 0.85f, 1.15f);

                Invoke("TurnOffTriggerCol", 0.1f);

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
        PlayerManager.Instance.SetPlayerUseHandLerp(data.usePositionOffset, data.useRotationOffset, data.timeToUse);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, true);
        CameraManager.Instance.PlayFOV(data.usingFOV, data.timeToUse);

        triggerCol.enabled = true;

        if (useCoroutine != null)
        {
            StopCoroutine(useCoroutine);
            useCoroutine = null;
        }

        useCoroutine = StartCoroutine(Use(true));
    }

    public void OnUseRelease()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(GrabPositionOffset, GrabRotationOffset, data.timeToUse/2f);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);
        CameraManager.Instance.EndFOV(0f, data.timeToUse/2f);

        triggerCol.enabled = false;

        if (useCoroutine != null)
        {
            StopCoroutine(useCoroutine);
            useCoroutine = null;
        }

        useCoroutine = StartCoroutine(Use(false));
    }

    private IEnumerator Use(bool shouldUse)
    {
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;
        Vector3 startScale = transform.localScale;

        Vector3 endPos = shouldUse ? data.useLocalPositionOffset : data.grabLocalPositionOffset;
        Quaternion endRot = shouldUse ? Quaternion.Euler(data.useLocalRotationOffset) : Quaternion.Euler(data.grabLocalRotationOffset);

        float elapsedTime = 0f;
        float value = 0f;

        while (elapsedTime < data.timeToUse)
        {
            value = elapsedTime / data.timeToUse;

            transform.localPosition = Vector3.Lerp(startPos, endPos, value);
            transform.localRotation = Quaternion.Lerp(startRot, endRot, value);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = endPos;
        transform.localRotation = endRot;

        useCoroutine = null;

    }
}
