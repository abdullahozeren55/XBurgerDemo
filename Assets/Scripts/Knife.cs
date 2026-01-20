using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Holder;

public class Knife : MonoBehaviour, IGrabable
{
    public IGrabable Master => this;
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public ItemIcon IconData { get => data.iconData; set => data.iconData = value; }

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }
    public PlayerManager.HandRigTypes HandRigType { get => data.handRigType; set => data.handRigType = value; }
    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }
    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }

    public Transform LeftHandPoint { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }

    public KnifeData data;
    public string FocusTextKey { get => data.focusTextKey; set => data.focusTextKey = value; }
    [Space]
    [SerializeField] private KnifeTrigger triggerSC;
    [SerializeField] private Collider triggerCol;
    [SerializeField] private Transform knifeEdgeTransform;

    private Rigidbody rb;
    private Collider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;

    private bool isJustThrowed;
    private bool isJustDropped;
    private bool isStuck;

    private Coroutine useCoroutine;

    private float lastSoundTime = 0f;
    private bool isStuckAndCantPlayAudioUntilPickedAgain;
    private void Awake()
    {

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");

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

        ChangeLayer(ungrabableLayer);
    }

    public void OnFocus()
    {
        if (!isJustDropped && !isJustThrowed)
            ChangeLayer(grabableOutlinedLayer);
    }

    public void OnHolster()
    {
    }

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(grabbedLayer);

        if (isStuck)
            Unstick();

        triggerCol.enabled = false;
        col.enabled = false;

        isJustThrowed = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);
    }

    public void OnLoseFocus()
    {
        if (!isJustDropped && !isJustThrowed)
            ChangeLayer(grabableLayer);
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
        triggerSC.IsJustThrowed = true;

        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        transform.SetParent(null);

        Vector3 throwDirection = Camera.main.transform.forward;

        // 1. Býçaðýn ana yönünü kameraya çevir
        Quaternion baseRotation = Quaternion.LookRotation(throwDirection);

        // 2. Modelin yamukluðunu düzeltmek için gerekli ofset (Senin Y+180 ve Z=180 dediðin kýsým)
        // Bu deðerler modelin kendisine (Local Space) uygulanýr.
        Quaternion correction = Quaternion.Euler(0, 180f, 180f);

        // 3. Ýkisini çarp (Sýra Önemli: Base * Correction)
        transform.rotation = baseRotation * correction;

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustThrowed = true;

        ChangeLayer(ungrabableLayer);
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

    private void StickToSurface(Collision collision)
    {
        // Get the surface normal from the collision
        Vector3 surfaceNormal = collision.GetContact(0).normal;

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

        Instantiate(data.throwParticles, knifeEdgeTransform.position, Quaternion.identity);

        // Set the flag that the object is stuck
        isStuck = true;
    }

    private void Unstick()
    {
        transform.SetParent(null);

        isStuckAndCantPlayAudioUntilPickedAgain = false;

        rb.isKinematic = false;
        isStuck = false;
    }

    private void TurnOffTriggerSC()
    {
        triggerCol.enabled = false;
        triggerSC.IsJustThrowed = false;
    }

    private void HandleSoundFX(Collision collision)
    {
        // --- 2. Hýz Hesaplama ---
        // Çarpýþmanýn þiddetini alýyoruz
        float impactForce = collision.relativeVelocity.magnitude;

        // --- 3. Spam Korumasý ve Sessizlik ---
        // Eðer çok yavaþ sürtünüyorsa (dropThreshold altý) veya
        // son sesin üzerinden çok az zaman geçtiyse çýk.
        if (impactForce < data.dropThreshold || Time.time - lastSoundTime < data.soundCooldown) return;

        // --- 4. Hýza Göre Ses Seçimi ---
        if (impactForce >= data.throwThreshold || isStuck)
        {
            // === FIRLATMA SESÝ (Hýzlý) ===
            SoundManager.Instance.PlaySoundFX(
                data.audioClips[2],
                transform,
                data.throwSoundVolume,
                data.throwSoundMinPitch,
                data.throwSoundMaxPitch
            );

            if (isStuck)
                isStuckAndCantPlayAudioUntilPickedAgain = true;
        }
        else
        {
            // === DÜÞME SESÝ (Yavaþ/Orta) ===
            SoundManager.Instance.PlaySoundFX(
                data.audioClips[1],
                transform,
                data.dropSoundVolume,
                data.dropSoundMinPitch,
                data.dropSoundMaxPitch
            );
        }

        // Ses çaldýk, zamaný kaydet
        lastSoundTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed)
            {
                if (((1 << collision.gameObject.layer) & data.stabableLayers) != 0)
                    StickToSurface(collision);

                Invoke("TurnOffTriggerSC", 0.1f);

                ChangeLayer(grabableLayer);

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                ChangeLayer(grabableLayer);

                isJustDropped = false;
            }

            if (!isStuckAndCantPlayAudioUntilPickedAgain)
                HandleSoundFX(collision);

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

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
    }

    public bool TryCombine(IGrabable otherItem)
    {
        return false;
    }

    public bool CanCombine(IGrabable otherItem)
    {
        return false;
    }
}
