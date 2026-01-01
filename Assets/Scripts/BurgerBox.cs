using UnityEngine;

public class BurgerBox : MonoBehaviour, IGrabable
{
    public IGrabable Master => this;

    [Header("Visuals")]
    [SerializeField] private Transform boxInnerPoint; // Burgerin yerleþeceði iç nokta (Pivot)
    public GameObject topPart; // Kapak

    [Header("Data")]
    public BurgerBoxData data;

    [Header("Lid Settings (Adaptive)")]
    [SerializeField] private float minLidAngle = 32f; // Küçük burgerlerdeki havalý duruþ
    [SerializeField] private float maxLidAngle = 90f; // Dev burgerlerdeki zoraki duruþ

    [Tooltip("Burger bu boydan kýsaysa Min Angle kullanýlýr.")]
    [SerializeField] private float minBurgerHeightLimit = 0.15f; // Bu deðerin altý 32 derece

    [Tooltip("Burger bu boya ulaþýrsa Max Angle (90) olur.")]
    [SerializeField] private float maxBurgerHeightLimit = 0.4f; // Bu deðer ve üstü 90 derece

    // State
    private GameObject containedBurger;
    private bool isBoxFull = false;

    // --- IGrabable Props ---
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public Sprite Icon { get => data.icon[isBoxFull ? 1 : 0]; set { } }
    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;

    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }
    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }
    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }
    public Vector3 GrabLocalPositionOffset { get => data.grabLocalPositionOffset; set => data.grabLocalPositionOffset = value; }
    public Vector3 GrabLocalRotationOffset { get => data.grabLocalRotationOffset; set => data.grabLocalRotationOffset = value; }

    public string FocusTextKey
    {
        get
        {
            return data.focusTextKeys[0]; // "Burger Box"
        }
        set { }
    }

    // References
    private Rigidbody rb;
    private Collider[] allColliders;
    private int grabableLayer, grabableOutlinedLayer, interactableOutlinedRedLayer, ungrabableLayer, grabbedLayer, grabableOutlinedGreenLayer;

    // --- PHYSICS FLAGS (Geri Geldi!) ---
    private bool isJustThrowed;
    private bool isJustDropped;
    private float lastSoundTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        allColliders = GetComponentsInChildren<Collider>();

        // --- YENÝ: TopPart'a Proxy Script Atama ---
        if (topPart != null)
        {
            // Eðer üzerinde BoxChild yoksa ekle ve ayarla
            BoxChild childScript = topPart.GetComponent<BoxChild>();
            if (childScript == null)
            {
                childScript = topPart.AddComponent<BoxChild>();
            }
            childScript.parentBox = this;

            // Eðer TopPart'ýn tag'i "Untagged" ise, raycast'in bulmasý için
            // Layer'ý parent ile ayný yapacaðýz ChangeLayer fonksiyonunda.
        }

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");
        grabableOutlinedGreenLayer = LayerMask.NameToLayer("GrabableOutlinedGreen");

        isJustThrowed = false;
        isJustDropped = false;
    }

    // --- KOMBÝNASYON (FIXED) ---
    public bool TryCombine(IGrabable otherItem)
    {
        if (isBoxFull) return false;

        WholeBurger burger = null;

        // 1. Direkt WholeBurger mi? (Nadiren olur)
        if (otherItem is WholeBurger)
        {
            burger = (WholeBurger)otherItem;
        }
        // 2. ChildBurger mi? (Genelde bu olur çünkü raycast buna çarpar)
        else if (otherItem is ChildBurger)
        {
            burger = ((ChildBurger)otherItem).parentBurger;
        }

        if (burger == null) return false;

        FillBox(burger);
        return true;
    }

    public bool CanCombine(IGrabable otherItem)
    {
        if (isBoxFull) return false;

        // Hem WholeBurger hem ChildBurger kabul et
        if (otherItem is WholeBurger || otherItem is ChildBurger) return true;

        return false;
    }

    private void FillBox(WholeBurger burger)
    {
        isBoxFull = true;

        // 1. Verileri yedekle (Çünkü script yok olacak)
        containedBurger = burger.gameObject;
        float burgerHeight = burger.TotalBurgerHeight;

        // 2. Burgeri paketle (Bu iþlem scriptleri yok eder)
        burger.PackIntoBox(this, boxInnerPoint);

        // 3. Layer'larý MANUEL ayarla (Artýk script yok)
        // Kutu þu an hangi layerdaysa (muhtemelen Grabbed veya Default), burgeri de ona eþitle.
        SetLayerRecursively(containedBurger, grabbedLayer);

        // 4. Kapaðý ayarla
        if (topPart != null)
        {
            float targetAngle = minLidAngle;
            if (burgerHeight > minBurgerHeightLimit)
            {
                float t = Mathf.InverseLerp(minBurgerHeightLimit, maxBurgerHeightLimit, burgerHeight);
                targetAngle = Mathf.Lerp(minLidAngle, maxLidAngle, t);
            }
            topPart.transform.localRotation = Quaternion.Euler(targetAngle, 0f, 0f);
        }

        if (PlayerManager.Instance != null) PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);
        SoundManager.Instance.PlaySoundFX(data.audioClips[3], transform, data.closeSoundVolume, data.closeSoundMinPitch, data.closeSoundMaxPitch);
    }

    // --- IGrabable ---
    public void OnGrab(Transform grabPoint)
    {
        IsGrabbed = true;
        isJustDropped = false;
        isJustThrowed = false;

        rb.isKinematic = true;
        rb.useGravity = false;
        ToggleColliders(false);

        transform.SetParent(grabPoint);
        transform.localPosition = GrabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(GrabLocalRotationOffset);

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);

        ChangeLayer(grabbedLayer);
    }

    public void OnDrop(Vector3 direction, float force)
    {
        ToggleColliders(true);
        IsGrabbed = false;
        transform.SetParent(null);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(direction * force, ForceMode.Impulse);

        // FLAGLER GERÝ GELDÝ
        isJustDropped = true;

        ChangeLayer(ungrabableLayer);
    }

    public void OnThrow(Vector3 direction, float force)
    {
        ToggleColliders(true);
        IsGrabbed = false;
        transform.SetParent(null);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(direction * force, ForceMode.Impulse);

        // FLAGLER GERÝ GELDÝ
        isJustThrowed = true;

        ChangeLayer(ungrabableLayer);
    }

    // --- FOCUS CONTROLS (Fizik durumuna göre) ---
    public void OnFocus()
    {
        if (!isJustDropped && !isJustThrowed)
            ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : grabableOutlinedLayer);
    }

    public void OnLoseFocus()
    {
        if (!isJustDropped && !isJustThrowed)
            ChangeLayer(grabableLayer);
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == grabableOutlinedLayer)
        {
            if (OutlineShouldBeRed) ChangeLayer(interactableOutlinedRedLayer);
            else if (OutlineShouldBeGreen) ChangeLayer(grabableOutlinedGreenLayer);
        }
        else if (gameObject.layer == grabableOutlinedGreenLayer)
        {
            if (OutlineShouldBeRed) ChangeLayer(interactableOutlinedRedLayer);
            else if (!OutlineShouldBeGreen) ChangeLayer(grabableOutlinedLayer);
        }
        else if (gameObject.layer == interactableOutlinedRedLayer)
        {
            if (!OutlineShouldBeRed)
            {
                if (OutlineShouldBeGreen) ChangeLayer(grabableOutlinedGreenLayer);
                else ChangeLayer(grabableOutlinedLayer);
            }
        }
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
        if (topPart != null) topPart.layer = layer;
        if (containedBurger != null)
        {
            SetLayerRecursively(containedBurger, layer);
        }
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;

        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    public void OnHolster() { gameObject.SetActive(false); }
    public void OnUseHold() { }
    public void OnUseRelease() { }

    // --- COLLISION & SOUND (Geri Geldi) ---
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            // Havada süzülürken layer deðiþimi
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
            else if (gameObject.layer == ungrabableLayer)
            {
                ChangeLayer(grabableLayer);
            }

            HandleSoundFX(collision);
        }
    }

    private void HandleSoundFX(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;

        if (impactForce < data.dropThreshold || Time.time - lastSoundTime < data.soundCooldown) return;

        if (impactForce >= data.throwThreshold)
        {
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
            SoundManager.Instance.PlaySoundFX(
                data.audioClips[1],
                transform,
                data.dropSoundVolume,
                data.dropSoundMinPitch,
                data.dropSoundMaxPitch
            );
        }

        lastSoundTime = Time.time;
    }

    private void ToggleColliders(bool state)
    {
        foreach (var c in allColliders)
        {
            if (c != null) c.enabled = state;
        }
    }
}