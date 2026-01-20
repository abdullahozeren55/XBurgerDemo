using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SauceBottle : MonoBehaviour, IGrabable
{
    public IGrabable Master => this;
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public ItemIcon IconData { get => data.iconData; set => data.iconData = value; }

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }
    public PlayerManager.HandRigTypes HandRigType { get => data.handRigType; set => data.handRigType = value; }
    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }

    public Transform LeftHandPoint { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public SauceBottleData data;

    public enum SauceType
    {
        Ketchup,
        Mayo,
        Mustard,
        BBQ,
        Buffalo,
        OrangeJuice,
        Lemonade,
        CherryJuice,
        Ayran
    }

    public string FocusTextKey { get => data.focusTextKey; set => data.focusTextKey = value; }
    [Space]
    [SerializeField] private ParticleSystem pourParticle;
    [Space]
    [SerializeField] private SauceType sauceType;

    private bool isPlayingParticles;

    private Rigidbody rb;
    private Collider col;
    private AudioSource audioSource;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;

    private bool isJustThrowed;
    private bool isJustDropped;

    private Coroutine useCoroutine;

    private float lastSoundTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");

        IsGrabbed = false;

        isJustThrowed = false;
        isJustDropped = false;
    }

    public void OnHolster()
    {
    }

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(grabbedLayer);

        col.enabled = false;

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
    public void OnFocus()
    {
        if (!isJustDropped && !isJustThrowed)
            ChangeLayer(grabableOutlinedLayer);
    }
    public void OnLoseFocus()
    {
        if (!isJustDropped && !isJustThrowed)
            ChangeLayer(grabableLayer);
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

        pourParticle.Stop();
        isPlayingParticles = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustDropped = true;

        ChangeLayer(ungrabableLayer);
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

        pourParticle.Stop();
        isPlayingParticles = false;

        transform.SetParent(null);

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

    private void OnDisable()
    {
        OnLoseFocus();
    }

    private void OnDestroy()
    {
        OnLoseFocus();
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
        if (impactForce >= data.throwThreshold)
        {
            // === FIRLATMA SESÝ (Hýzlý) ===
            SoundManager.Instance.PlaySoundFX(
                data.audioClips[2],
                transform,
                data.throwSoundVolume,
                data.throwSoundMinPitch,
                data.throwSoundMaxPitch
            );
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
                ChangeLayer(grabableLayer);

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                ChangeLayer(grabableLayer);

                isJustDropped = false;
            }

            HandleSoundFX(collision);

        }


    }

    public void OnUseHold()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(data.usePositionOffset, data.useRotationOffset, data.timeToUse);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);
        CameraManager.Instance.PlayFOV(data.usingFOV, data.timeToUse);

        if (useCoroutine != null)
        {
            StopCoroutine(useCoroutine);
            useCoroutine = null;
        }

        useCoroutine = StartCoroutine(Use(true));
    }

    public void OnUseRelease()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(GrabPositionOffset, GrabRotationOffset, data.timeToUse / 2f);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);
        CameraManager.Instance.EndFOV(0f, data.timeToUse / 2f);

        pourParticle.Stop();
        audioSource.Stop();
        isPlayingParticles = false;

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

        Vector3 endPos = shouldUse ? data.useLocalPositionOffset : data.grabLocalPositionOffset;
        Quaternion endRot = shouldUse ? Quaternion.Euler(data.useLocalRotationOffset) : Quaternion.Euler(data.grabLocalRotationOffset);

        float elapsedTime = 0f;
        float value = 0f;

        while (elapsedTime < data.timeToUse)
        {
            value = elapsedTime / data.timeToUse;

            transform.localPosition = Vector3.Lerp(startPos, endPos, value);
            transform.localRotation = Quaternion.Lerp(startRot, endRot, value);

            if (shouldUse && !isPlayingParticles && value > 0.6f)
            {
                pourParticle.Play();
                audioSource.Play();
                isPlayingParticles = true;
            }

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
