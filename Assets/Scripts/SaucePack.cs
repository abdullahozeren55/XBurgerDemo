using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SaucePack : MonoBehaviour, IGrabable
{
    public IGrabable Master => this;
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public Sprite Icon { get => data.icon; set => data.icon = value; }

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }

    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }

    public Transform LeftHandPoint { get => leftHandPoint; set => leftHandPoint = value; }
    [SerializeField] private Transform leftHandPoint;

    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }
    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;
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
    private int grabbedLayer;

    private Vector3 hologramPos;
    private Quaternion hologramRotation;

    private bool isJustThrowed;
    private bool isJustDropped;

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

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);

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
            ChangeLayer(grabableOutlinedLayer);
        }

    }
    public void OnLoseFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed && CanGetFocused)
        {
            ChangeLayer(grabableLayer);
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

        ChangeLayer(ungrabableLayer);
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
