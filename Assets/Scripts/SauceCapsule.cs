using DG.Tweening;
using UnityEngine;

public class SauceCapsule : MonoBehaviour, IGrabable
{
    public IGrabable Master => this;
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public Sprite Icon { get => data.icon; set => data.icon = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }
    public PlayerManager.HandRigTypes HandRigType { get => data.handRigType; set => data.handRigType = value; }
    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }

    public Transform LeftHandPoint { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public SauceCapsuleData data;

    // --- YENÝ DEÐÝÞKENLER ---
    private GameObject dummyBlocker; // Etkileþimsiz fizik duvarý
    private bool isStacked = false;  // Üzerimde biri var mý?
    // ------------------------

    public string FocusTextKey { get => data.focusTextKey; set => data.focusTextKey = value; }
    [Space]

    private Rigidbody rb;
    private Collider col;
    private Collider dummyCollider;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;

    [HideInInspector] public bool isJustThrowed;
    [HideInInspector] public bool isJustDropped;
    [HideInInspector] public bool CanBeReceived;

    // --- LOGIC VARIABLES ---
    [HideInInspector] public Tray currentTray;
    private bool isGettingPutOnTray;
    // ----------------------

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

        isJustThrowed = false;
        isJustDropped = false;
        CanBeReceived = true;

        CreateDummyBlocker();
    }

    private void CreateDummyBlocker()
    {
        dummyBlocker = new GameObject("DummyBlocker");
        dummyBlocker.transform.SetParent(transform);
        dummyBlocker.transform.localPosition = Vector3.zero;
        dummyBlocker.transform.localRotation = Quaternion.identity;
        dummyBlocker.transform.localScale = Vector3.one;

        if (col is CapsuleCollider capCol)
        {
            CapsuleCollider dummyCap = dummyBlocker.AddComponent<CapsuleCollider>();
            dummyCap.center = capCol.center;
            dummyCap.radius = capCol.radius;
            dummyCap.height = capCol.height;
            dummyCap.direction = capCol.direction;
            dummyCollider = dummyCap; // <--- KAYDET
        }
        else if (col is BoxCollider boxCol)
        {
            BoxCollider dummyBox = dummyBlocker.AddComponent<BoxCollider>();
            dummyBox.center = boxCol.center;
            dummyBox.size = boxCol.size;
            dummyCollider = dummyBox; // <--- KAYDET
        }

        dummyBlocker.layer = grabbedLayer;
        dummyBlocker.SetActive(false);
    }

    // --- DIÞARIDAN ÇAÐRILACAK FONKSÝYON ---
    public void SetStacked(bool stacked)
    {
        isStacked = stacked;

        if (isStacked)
        {
            // Altta kaldým: Ana colliderý kapat, dummyi aç
            col.enabled = false;
            dummyBlocker.SetActive(true);
        }
        else
        {
            // Özgürüm: Ana colliderý aç, dummyi kapat
            col.enabled = true;
            dummyBlocker.SetActive(false);
        }
    }

    // --- YERLEÞME METODU ---
    public void PlaceOnTray(Transform targetSlot, Transform apexTransform, Tray trayRef, int slotIndex, int stackIndex)
    {
        // 1. State
        isGettingPutOnTray = true;
        isJustDropped = false;
        isJustThrowed = false;
        currentTray = trayRef;

        // Çarpýþmayý Yoksay (ANA COLLIDER)
        if (currentTray != null && col != null)
            Physics.IgnoreCollision(col, currentTray.GetCollider, true);

        // --- FIX: DUMMY COLLIDER DA TEPSÝYLE SAVAÞMASIN ---
        if (currentTray != null && dummyCollider != null)
            Physics.IgnoreCollision(dummyCollider, currentTray.GetCollider, true);
        // --------------------------------------------------

        if (PlayerManager.Instance != null && IsGrabbed)
            PlayerManager.Instance.ResetPlayerGrab(this);

        // 2. Fizik Kapat
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        col.enabled = false;
        if (dummyBlocker != null) dummyBlocker.SetActive(false);

        transform.SetParent(targetSlot, true);

        // 4. Hedef & Randomizasyon
        Vector3 baseLocalPos = Vector3.zero;
        Vector3 baseLocalRot = Vector3.zero;

        if (data.slotOffsets != null && slotIndex < data.slotOffsets.Length)
        {
            baseLocalPos = data.slotOffsets[slotIndex].localPosition;
            baseLocalRot = data.slotOffsets[slotIndex].localRotation;
        }

        // --- STACK OFFSET (YÜKSEKLÝK) ---
        // stackIndex 0 ise tabana, 1 ise offset kadar yukarý.
        // Senin modellerde Z ekseni yükseklik olduðu için Vector3.forward (veya back) kullanacaðýz.
        // Inspector'dan +Z mi -Z mi yukarý bakýyor kontrol et. Genelde Forward +Z'dir.
        Vector3 heightOffset = Vector3.forward * (stackIndex * data.zHeightIncreasePerSauce);

        Vector3 finalTargetPos = baseLocalPos + heightOffset;
        // --------------------------------

        // Randomizasyon (X Ekseninde - Kendi etrafýnda)
        float randomX = Random.Range(0f, 360f);
        Vector3 finalTargetEuler = new Vector3(randomX, baseLocalRot.y, baseLocalRot.z);
        Quaternion finalTargetRotation = Quaternion.Euler(finalTargetEuler);

        // 5. APEX (Yüksekliði hesaba kat)
        Vector3 localApexPos;
        if (apexTransform != null)
        {
            localApexPos = targetSlot.InverseTransformPoint(apexTransform.position);
            // Apex'i de biraz yükseltelim ki üstteki kapsül alttakinin içinden geçmesin
            localApexPos += heightOffset;
        }
        else
        {
            localApexPos = finalTargetPos + Vector3.up * 0.15f;
        }

        // 6. DOTween
        Vector3[] pathPoints = new Vector3[] { localApexPos, finalTargetPos };
        Sequence seq = DOTween.Sequence();

        seq.Join(transform.DOLocalPath(pathPoints, 0.2f, PathType.CatmullRom).SetEase(Ease.OutSine));
        seq.Join(transform.DOLocalRotateQuaternion(finalTargetRotation, 0.2f).SetEase(Ease.OutBack));
        seq.Join(transform.DOScale(data.trayLocalScale, 0.2f)); // Varsa tray scale

        seq.OnComplete(() =>
        {
            transform.localPosition = finalTargetPos;
            transform.localRotation = finalTargetRotation;

            isGettingPutOnTray = false;

            if (!isStacked)
            {
                col.enabled = true;
            }
            else
            {
                // Stacked isem dummy açýk olmalý
                dummyBlocker.SetActive(true);
            }

            if (currentTray != null) ChangeLayer(currentTray.gameObject.layer);
        });
    }

    public void OnHolster() { }

    // --- GRAB (Updated) ---
    public void OnGrab(Transform grabPoint)
    {
        SetStacked(false);

        IsGrabbed = true;
        CanBeReceived = true;
        isJustDropped = false;
        isJustThrowed = false;

        // --- TEPSÝDEN AYRILMA ---
        if (currentTray != null)
        {
            // Çarpýþmayý Geri Aç (ANA COLLIDER)
            if (col != null) Physics.IgnoreCollision(col, currentTray.GetCollider, false);

            // --- FIX: DUMMY COLLIDER ÝÇÝN DE AÇ ---
            if (dummyCollider != null) Physics.IgnoreCollision(dummyCollider, currentTray.GetCollider, false);
            // --------------------------------------

            currentTray.RemoveItem(this);
            currentTray = null;
        }
        isGettingPutOnTray = false;
        // ------------------------

        ChangeLayer(grabbedLayer);

        col.enabled = false; // Eldeyken collider kapalý (Standartýn)

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true; // Güvenlik için Kinematic

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);

        // Scale Reset
        transform.localScale = data.grabbedLocalScale;
    }

    public void OnFocus()
    {
        if (isJustDropped || isJustThrowed || isGettingPutOnTray) return;
        ChangeLayer(grabableOutlinedLayer);
    }
    public void OnLoseFocus()
    {
        if (isJustDropped || isJustThrowed || isGettingPutOnTray) return;
        ChangeLayer(grabableLayer);
    }

    public void OnDrop(Vector3 direction, float force)
    {
        col.enabled = true;
        IsGrabbed = false;
        transform.SetParent(null);

        rb.isKinematic = false;
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

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);
        isJustThrowed = true;
        ChangeLayer(ungrabableLayer);
    }

    // ... (Kalan kýsýmlar: ChangeLayer, OutlineCheck, Collision, SoundFX AYNEN KALSIN) ...

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
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

    private void OnDisable() { OnLoseFocus(); }
    private void OnDestroy() { OnLoseFocus(); }

    private void HandleSoundFX(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < data.dropThreshold || Time.time - lastSoundTime < data.soundCooldown) return;

        if (impactForce >= data.throwThreshold)
            SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, data.throwSoundVolume, data.throwSoundMinPitch, data.throwSoundMaxPitch);
        else
            SoundManager.Instance.PlaySoundFX(data.audioClips[1], transform, data.dropSoundVolume, data.dropSoundMinPitch, data.dropSoundMaxPitch);
        lastSoundTime = Time.time;
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed) { ChangeLayer(grabableLayer); isJustThrowed = false; }
            else if (isJustDropped) { ChangeLayer(grabableLayer); isJustDropped = false; }

            HandleSoundFX(collision);
        }
    }

    public void OnUseHold() { throw new System.NotImplementedException(); }
    public void OnUseRelease() { throw new System.NotImplementedException(); }
    public bool TryCombine(IGrabable otherItem) { return false; }
    public bool CanCombine(IGrabable otherItem) { return false; }
}