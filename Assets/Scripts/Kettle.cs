using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class Kettle : MonoBehaviour, IGrabable
{
    public IGrabable Master => this;
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public Sprite Icon { get => icon; set => icon = value; }
    [SerializeField] private Sprite icon;

    public PlayerManager.HandGrabTypes HandGrabType { get => handGrabType; set => handGrabType = value; }
    [SerializeField] private PlayerManager.HandGrabTypes handGrabType;

    public bool IsThrowable { get => isThrowable; set => isThrowable = value; }
    [SerializeField] private bool isThrowable = true;

    public Transform LeftHandPoint { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public float ThrowMultiplier { get => throwMultiplier; set => throwMultiplier = value; }
    [SerializeField] private float throwMultiplier = 1f;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;
    public Vector3 GrabPositionOffset { get => grabPositionOffset; set => grabPositionOffset = value; }
    [SerializeField] private Vector3 grabPositionOffset = new Vector3(0.4f, 0.1f, 2f);
    public Vector3 GrabRotationOffset { get => grabRotationOffset; set => grabRotationOffset = value; }
    [SerializeField] private Vector3 grabRotationOffset = new Vector3(-5f, -70f, -70f);

    [Space]

    [SerializeField] private Vector3 usePositionOffset;
    [SerializeField] private Vector3 useRotationOffset;
    public bool IsUseable { get => isUseable; set => isUseable = value; }
    [SerializeField] private bool isUseable = true;

    public bool IsGettingPutOnHologram;
    public bool CanGetFocused;

    public AudioClip[] audioClips;
    [Space]
    public float grabSoundVolume = 1f;
    public float grabSoundMinPitch = 0.85f;
    public float grabSoundMaxPitch = 1.15f;
    [Space]
    public float dropSoundVolume = 1f;
    public float dropSoundMinPitch = 0.85f;
    public float dropSoundMaxPitch = 1.15f;
    [Space]
    public float throwSoundVolume = 1f;
    public float throwSoundMinPitch = 0.85f;
    public float throwSoundMaxPitch = 1.15f;
    [Space]
    public float soundCooldown = 0.1f;
    public float throwThreshold = 6f;
    public float dropThreshold = 2f;
    public string FocusTextKey { get => focusTextKey; set => focusTextKey = value; }
    [SerializeField] private string focusTextKey;
    [Space]
    [SerializeField] private ParticleSystem pourParticle;
    [SerializeField] private float timeToPutOnHologram = 0.3f;

    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    public float timeToUse = 0.3f;

    private bool isPlayingParticles;

    private Rigidbody rb;
    private Collider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;

    private Vector3 hologramPos;
    private Quaternion hologramRotation;

    private bool isJustThrowed;
    private bool isJustDropped;

    private Coroutine waterPourCoroutine;

    private float lastSoundTime = 0f;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");

        IsGettingPutOnHologram = false;
        CanGetFocused = true;

        IsGrabbed = false;

        isJustThrowed = false;
        isJustDropped = false;
    }

    public void PutOnHologram(Vector3 hologramPos, Quaternion hologramRotation)
    {
        IsGettingPutOnHologram = true;
        isJustThrowed = false;
        isJustDropped = false;

        NoodleManager.Instance.SetHologramKettle(false);

        ChangeLayer(ungrabableLayer);

        IsGrabbed = false;

        this.hologramPos = hologramPos;
        this.hologramRotation = hologramRotation;

        StartCoroutine(PutOnHologram());
    }

    public void OnHolster()
    {
    }

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(grabbedLayer);

        col.enabled = false;

        SoundManager.Instance.PlaySoundFX(audioClips[0], transform, grabSoundVolume, grabSoundMinPitch, grabSoundMaxPitch);

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(grabLocalRotationOffset);
    }
    public void OnFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed && CanGetFocused)
            ChangeLayer(grabableOutlinedLayer);
    }
    public void OnLoseFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed && CanGetFocused)
            ChangeLayer(grabableLayer);
    }

    public void OnDrop(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.1f);

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

        Invoke("TurnOnCollider", 0.1f);

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

    private void TurnOnCollider()
    {
        col.enabled = true;
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

    private void HandleSoundFX(Collision collision)
    {
        // --- 2. Hýz Hesaplama ---
        // Çarpýþmanýn þiddetini alýyoruz
        float impactForce = collision.relativeVelocity.magnitude;

        // --- 3. Spam Korumasý ve Sessizlik ---
        // Eðer çok yavaþ sürtünüyorsa (dropThreshold altý) veya
        // son sesin üzerinden çok az zaman geçtiyse çýk.
        if (impactForce < dropThreshold || Time.time - lastSoundTime < soundCooldown) return;

        // --- 4. Hýza Göre Ses Seçimi ---
        if (impactForce >= throwThreshold)
        {
            // === FIRLATMA SESÝ (Hýzlý) ===
            SoundManager.Instance.PlaySoundFX(
                audioClips[2],
                transform,
                throwSoundVolume,
                throwSoundMinPitch,
                throwSoundMaxPitch
            );
        }
        else
        {
            // === DÜÞME SESÝ (Yavaþ/Orta) ===
            SoundManager.Instance.PlaySoundFX(
                audioClips[1],
                transform,
                dropSoundVolume,
                dropSoundMinPitch,
                dropSoundMaxPitch
            );
        }

        // Ses çaldýk, zamaný kaydet
        lastSoundTime = Time.time;
    }

    private IEnumerator PutOnHologram()
    {
        rb.isKinematic = true;

        Vector3 startPos = transform.position;
        Quaternion startRotation = transform.rotation;

        float timeElapsed = 0f;
        float rate = 0f;

        while (timeElapsed < timeToPutOnHologram)
        {

            rate = timeElapsed / timeToPutOnHologram;

            transform.position = Vector3.Lerp(startPos, hologramPos, rate);
            transform.rotation = Quaternion.Slerp(startRotation, hologramRotation, rate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = hologramPos;
        transform.rotation = hologramRotation;

        ChangeLayer(ungrabableLayer);

        CanGetFocused = false;
        IsGettingPutOnHologram = false;
    }

    public void OnUseHold()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(usePositionOffset, useRotationOffset, timeToUse);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        if (waterPourCoroutine != null)
        {
            StopCoroutine(waterPourCoroutine);
            waterPourCoroutine = null;
        }

        waterPourCoroutine = StartCoroutine(PourWater());
    }

    public void OnUseRelease()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(grabPositionOffset, grabRotationOffset, timeToUse / 2f);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        if (waterPourCoroutine != null)
        {
            StopCoroutine(waterPourCoroutine);
            waterPourCoroutine = null;
        }

        pourParticle.Stop();
        isPlayingParticles = false;
    }

    private IEnumerator PourWater()
    {
        yield return new WaitForSeconds(timeToUse * 2/3);

        if (!isPlayingParticles)
        {
            pourParticle.Play();
            isPlayingParticles = true;
        }

        waterPourCoroutine = null;

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
