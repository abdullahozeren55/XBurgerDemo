using DG.Tweening;
using System.Collections.Generic; // List için gerekli
using UnityEngine;

public class Toy : MonoBehaviour, IGrabable
{
    public IGrabable Master => this;
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    [Header("Visual Parts")]
    [SerializeField] private GameObject[] childParts; // Inspector'dan atanacak parçalar

    public ItemIcon IconData { get => data.iconData; set => data.iconData = value; }

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

    public ToyData data;

    public string FocusTextKey { get => data.focusTextKey; set => data.focusTextKey = value; }
    [Space]

    private Rigidbody rb;
    private Collider col; // Ana collider

    // --- YENÝ: Tüm colliderlarý tutan liste ---
    private List<Collider> allColliders = new List<Collider>();

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;

    [HideInInspector] public bool isJustThrowed;
    [HideInInspector] public bool isJustDropped;

    // --- LOGIC VARIABLES ---
    [HideInInspector] public Tray currentTray;
    private bool isGettingPutOnTray;
    // ----------------------

    private float lastSoundTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // --- COLLIDERLARI TOPLA ---
        allColliders.Clear();

        // 1. Ana collider varsa ekle
        if (col != null) allColliders.Add(col);

        // 2. Child parçalardaki colliderlarý ekle
        if (childParts != null)
        {
            foreach (var part in childParts)
            {
                if (part != null)
                {
                    Collider c = part.GetComponent<Collider>();
                    if (c != null) allColliders.Add(c);
                }
            }
        }
        // -------------------------

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");

        IsGrabbed = false;

        isJustThrowed = false;
        isJustDropped = false;
    }

    // --- YERLEÞME METODU ---
    public void PlaceOnTray(Transform targetSlot, Transform apexTransform, Tray trayRef, int slotIndex)
    {
        // 1. State
        isGettingPutOnTray = true;
        isJustDropped = false;
        isJustThrowed = false;
        currentTray = trayRef;

        // --- FIX: TÜM PARÇALARIN TEPSÝYLE ÇARPIÞMASINI KAPAT ---
        if (currentTray != null)
        {
            ToggleCollisionWithTray(currentTray.GetCollider, true);
        }
        // -------------------------------------------------------

        if (PlayerManager.Instance != null && IsGrabbed)
            PlayerManager.Instance.ResetPlayerGrab(this);

        // 2. Fizik Kapat
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        // --- FIX: TÜM COLLIDERLARI KAPAT ---
        ToggleAllColliders(false);

        transform.SetParent(targetSlot, true);

        // 4. Hedef & Randomizasyon
        Vector3 baseLocalPos = Vector3.zero;
        Vector3 baseLocalRot = Vector3.zero;

        if (data.slotOffsets != null && slotIndex < data.slotOffsets.Length)
        {
            baseLocalPos = data.slotOffsets[slotIndex].localPosition;
            baseLocalRot = data.slotOffsets[slotIndex].localRotation;
        }

        Quaternion finalTargetRotation = Quaternion.Euler(baseLocalRot);

        // 5. APEX
        Vector3 localApexPos;
        if (apexTransform != null)
        {
            localApexPos = targetSlot.InverseTransformPoint(apexTransform.position);
        }
        else
        {
            localApexPos = baseLocalPos + Vector3.up * 0.15f;
        }

        // 6. DOTween
        Vector3[] pathPoints = new Vector3[] { localApexPos, baseLocalPos };
        Sequence seq = DOTween.Sequence();

        seq.Join(transform.DOLocalPath(pathPoints, 0.2f, PathType.CatmullRom).SetEase(Ease.OutSine));
        seq.Join(transform.DOLocalRotateQuaternion(finalTargetRotation, 0.2f).SetEase(Ease.OutBack));
        seq.Join(transform.DOScale(data.trayLocalScale, 0.2f)); // Varsa tray scale

        seq.OnComplete(() =>
        {
            transform.localPosition = baseLocalPos;
            transform.localRotation = finalTargetRotation;

            isGettingPutOnTray = false;

            // --- FIX: TÜM COLLIDERLARI AÇ ---
            ToggleAllColliders(true);

            if (currentTray != null) ChangeLayer(currentTray.gameObject.layer);
        });
    }

    public void OnHolster() { }

    // --- GRAB (Updated) ---
    public void OnGrab(Transform grabPoint)
    {
        IsGrabbed = true;
        isJustDropped = false;
        isJustThrowed = false;

        // --- TEPSÝDEN AYRILMA ---
        if (currentTray != null)
        {
            // --- FIX: TÜM PARÇALARIN ÇARPIÞMASINI GERÝ AÇ ---
            ToggleCollisionWithTray(currentTray.GetCollider, false);

            currentTray.RemoveItem(this);
            currentTray = null;
        }
        isGettingPutOnTray = false;
        // ------------------------

        ChangeLayer(grabbedLayer);

        // --- FIX: TÜM COLLIDERLARI KAPAT ---
        ToggleAllColliders(false);

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
        // --- FIX: TÜM COLLIDERLARI AÇ ---
        ToggleAllColliders(true);

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
        // --- FIX: TÜM COLLIDERLARI AÇ ---
        ToggleAllColliders(true);

        IsGrabbed = false;
        transform.SetParent(null);

        rb.isKinematic = false;
        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);
        isJustThrowed = true;
        ChangeLayer(ungrabableLayer);
    }

    // --- Kalan kýsýmlar: ChangeLayer, OutlineCheck, Collision, SoundFX AYNEN KALSIN ---

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;

        // --- FIX: CHILD PARÇALARIN DA LAYERINI DEÐÝÞTÝR ---
        if (childParts != null)
        {
            foreach (var part in childParts)
            {
                if (part != null) part.layer = layer;
            }
        }
    }

    // --- YENÝ YARDIMCI FONKSÝYONLAR ---

    private void ToggleAllColliders(bool state)
    {
        foreach (var c in allColliders)
        {
            if (c != null) c.enabled = state;
        }
    }

    private void ToggleCollisionWithTray(Collider trayCollider, bool ignore)
    {
        if (trayCollider == null) return;

        // Tüm normal parçalar için
        foreach (var c in allColliders)
        {
            if (c != null) Physics.IgnoreCollision(c, trayCollider, ignore);
        }
    }
    // ---------------------------------

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