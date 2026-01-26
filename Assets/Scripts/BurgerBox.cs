using DG.Tweening;
using System.Collections.Generic; // List için gerekli
using UnityEngine;

public class BurgerBox : MonoBehaviour, IGrabable
{
    public IGrabable Master => this;

    public GameManager.BurgerTypes ContainedBurgerType = GameManager.BurgerTypes.Null;

    [Header("Visuals")]
    [SerializeField] private Transform boxInnerPoint;
    public GameObject topPart;

    [Header("Data")]
    public BurgerBoxData data;

    // State
    private GameObject containedBurger;

    // --- IGrabable Props ---
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public ItemIcon IconData { get => data.iconData[ContainedBurgerType == GameManager.BurgerTypes.Null ? 0 : 1]; set { } }
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
        get { return data.focusTextKeys[(int)ContainedBurgerType]; }
        set { }
    }

    public PlayerManager.HandRigTypes HandRigType { get => data.handRigType; set => data.handRigType = value; }

    // --- LOGIC VARIABLES ---
    [HideInInspector] public Tray currentTray;
    private bool isGettingPutOnTray;

    // References
    private Rigidbody rb;

    // --- DEÐÝÞÝKLÝK 1: Array yerine List kullanýyoruz ---
    private List<Collider> allColliders = new List<Collider>();
    private List<GameObject> containedVisualParts = new List<GameObject>();
    // ----------------------------------------------------

    // Layers
    private int grabableLayer, grabableOutlinedLayer, interactableOutlinedRedLayer, ungrabableLayer, grabbedLayer, grabableOutlinedGreenLayer;

    private bool isJustThrowed;
    private bool isJustDropped;
    private float lastSoundTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        // Baþlangýçta sadece kutunun kendi colliderlarýný ekle
        allColliders.AddRange(GetComponentsInChildren<Collider>());

        if (topPart != null)
        {
            BoxChild childScript = topPart.GetComponent<BoxChild>();
            if (childScript == null) childScript = topPart.AddComponent<BoxChild>();
            childScript.parentBox = this;
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

    // --- YERLEÞME METODU ---
    public void PlaceOnTray(Transform targetSlot, Transform apexTransform, Tray trayRef, int slotIndex)
    {
        isGettingPutOnTray = true;
        isJustDropped = false;
        isJustThrowed = false;
        currentTray = trayRef;

        // Çarpýþmayý Yoksay (Listeye sonradan eklenen burger parçalarý dahil)
        if (currentTray != null)
        {
            ToggleCollisionWithTray(currentTray.GetCollider, true);
        }

        if (PlayerManager.Instance != null && IsGrabbed)
        {
            PlayerManager.Instance.ResetPlayerGrab(this);
        }

        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        // Tüm colliderlarý kapat (Burger dahil)
        ToggleColliders(false);

        transform.SetParent(targetSlot, true);

        // Hedef Hesaplama
        Vector3 baseLocalPos = Vector3.zero;
        Vector3 baseLocalRot = Vector3.zero;

        if (data.slotOffsets != null && slotIndex < data.slotOffsets.Length)
        {
            baseLocalPos = data.slotOffsets[slotIndex].localPosition;
            baseLocalRot = data.slotOffsets[slotIndex].localRotation;
        }

        Quaternion finalTargetRotation = Quaternion.Euler(baseLocalRot);

        // APEX
        Vector3 localApexPos;
        if (apexTransform != null)
        {
            localApexPos = targetSlot.InverseTransformPoint(apexTransform.position);
        }
        else
        {
            localApexPos = baseLocalPos + Vector3.up * 0.15f;
        }

        // DOTween
        Vector3[] pathPoints = new Vector3[] { localApexPos, baseLocalPos };
        Sequence seq = DOTween.Sequence();

        seq.Join(transform.DOLocalPath(pathPoints, 0.2f, PathType.CatmullRom).SetEase(Ease.OutSine));
        seq.Join(transform.DOLocalRotateQuaternion(finalTargetRotation, 0.2f).SetEase(Ease.OutBack));
        seq.Join(transform.DOScale(data.trayLocalScale, 0.2f));

        seq.OnComplete(() =>
        {
            SoundManager.Instance.PlaySoundFX(data.audioClips[4], transform, data.traySoundVolume, data.traySoundMinPitch, data.traySoundMaxPitch);

            transform.localPosition = baseLocalPos;
            transform.localRotation = finalTargetRotation;

            isGettingPutOnTray = false;

            // Tüm colliderlarý aç
            ToggleColliders(true);

            if (currentTray != null) ChangeLayer(currentTray.gameObject.layer);
        });
    }

    // --- GRAB ---
    public void OnGrab(Transform grabPoint)
    {
        // 1. GÜVENLÝK VE TEMÝZLÝK (EKLENEN KISIM)
        // Eðer bu kutu üzerinde çalýþan bir hareket animasyonu (düþme vs.) varsa, 
        // oyuncu tuttuðu an bunu öldürüyoruz. Böylece elde kayma yapmaz.
        transform.DOKill();
        // ----------------------------------------

        IsGrabbed = true;
        isJustDropped = false;
        isJustThrowed = false;

        if (currentTray != null)
        {
            // Çarpýþmayý Geri Aç
            ToggleCollisionWithTray(currentTray.GetCollider, false);

            currentTray.RemoveItem(this);
            currentTray = null;
        }
        isGettingPutOnTray = false;

        // --- YENÝ EKLENEN: YIÐIN KONTROLÜ ---
        // Eðer bu kutu bir yýðýnýn parçasýysa, combine area bunu halletsin.
        if (GameManager.Instance != null && GameManager.Instance.burgerCombineArea != null)
        {
            GameManager.Instance.burgerCombineArea.OnBoxGrabbed(this);
        }
        // ------------------------------------

        rb.isKinematic = true;
        rb.useGravity = false;
        ToggleColliders(false);

        transform.SetParent(grabPoint);
        transform.localPosition = GrabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(GrabLocalRotationOffset);
        transform.localScale = data.grabbedLocalScale;

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);

        ChangeLayer(grabbedLayer);
    }

    // --- YARDIMCI FONKSÝYONLAR ---
    private void ToggleCollisionWithTray(Collider trayCollider, bool ignore)
    {
        if (trayCollider == null || allColliders == null) return;

        // Listeyi döner, artýk burger parçalarý da burada
        foreach (var c in allColliders)
        {
            if (c != null) Physics.IgnoreCollision(c, trayCollider, ignore);
        }
    }

    private void ToggleColliders(bool state)
    {
        foreach (var c in allColliders)
        {
            if (c != null) c.enabled = state;
        }
    }

    // --- KOMBÝNASYON VE FILLBOX ---
    public bool TryCombine(IGrabable otherItem)
    {
        if (ContainedBurgerType != GameManager.BurgerTypes.Null) return false;
        WholeBurger burger = null;
        if (otherItem is WholeBurger) burger = (WholeBurger)otherItem;
        else if (otherItem is ChildBurger) burger = ((ChildBurger)otherItem).parentBurger;

        if (burger == null) return false;
        FillBox(burger);
        return true;
    }

    public bool CanCombine(IGrabable otherItem)
    {
        if (ContainedBurgerType != GameManager.BurgerTypes.Null) return false;
        if (otherItem is WholeBurger || otherItem is ChildBurger) return true;
        return false;
    }

    private void FillBox(WholeBurger burger)
    {
        // 1. ÖNCE REFERANSI AL
        containedBurger = burger.gameObject;

        containedVisualParts.Clear(); // Önce temizle
        containedVisualParts.AddRange(burger.GetVisualParts());

        float burgerHeight = burger.TotalBurgerHeight;

        // 2. PAKETLEME
        burger.PackIntoBox(this, boxInnerPoint);

        Collider[] burgerColliders = containedBurger.GetComponentsInChildren<Collider>();
        if (burgerColliders != null)
        {
            allColliders.AddRange(burgerColliders);
        }

        // --- DÜZELTME BURADA ---

        // ESKÝ KOD (FPC bunu eziyor):
        // SetLayerRecursively(containedBurger, grabbedLayer);

        // YENÝ KOD (ZAMANLAMA HÝLESÝ):
        // FPC'nin "Ungrabable" yapma iþleminin bitmesini beklemek için
        // iþlemi bir sonraki kareye (Next Frame) erteliyoruz.
        // Böylece son sözü biz söylüyoruz.
        // Not: 'gameObject.layer' kullanýyoruz ki o arada kutuyu yere atarsan vs. bug olmasýn, kutu neyse burger de o olsun.

        DOVirtual.DelayedCall(0f, () =>
        {
            if (containedBurger != null)
            {
                // Sadece listeye aldýðýmýz temiz parçalarý boya
                UpdateVisualPartsLayer(gameObject.layer);
            }
        });

        // -----------------------

        if (topPart != null)
        {
            float targetAngle = data.minLidAngle;
            if (burgerHeight > data.minBurgerHeightLimit)
            {
                float t = Mathf.InverseLerp(data.minBurgerHeightLimit, data.maxBurgerHeightLimit, burgerHeight);
                targetAngle = Mathf.Lerp(data.minLidAngle, data.maxLidAngle, t);
            }
            topPart.transform.localRotation = Quaternion.Euler(targetAngle, 0f, 0f);
        }

        if (PlayerManager.Instance != null) PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);
        SoundManager.Instance.PlaySoundFX(data.audioClips[3], transform, data.closeSoundVolume, data.closeSoundMinPitch, data.closeSoundMaxPitch);
    }

    public void OnDrop(Vector3 direction, float force)
    {
        ToggleColliders(true);
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
        ToggleColliders(true);
        IsGrabbed = false;
        transform.SetParent(null);
        rb.isKinematic = false;
        rb.useGravity = true;
        rb.AddForce(direction * force, ForceMode.Impulse);
        isJustThrowed = true;
        ChangeLayer(ungrabableLayer);
    }

    public void OnFocus()
    {
        if (isJustDropped || isJustThrowed || isGettingPutOnTray) return;
        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : grabableOutlinedLayer);
    }

    public void OnLoseFocus()
    {
        if (isJustDropped || isJustThrowed || isGettingPutOnTray) return;
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

        // ESKÝSÝ: if (containedBurger != null) SetLayerRecursively(containedBurger, layer);
        // YENÝSÝ:
        if (containedBurger != null) UpdateVisualPartsLayer(layer);
    }

    private void UpdateVisualPartsLayer(int newLayer)
    {
        if (containedVisualParts == null) return;

        foreach (var part in containedVisualParts)
        {
            if (part != null) part.layer = newLayer;
        }
    }

    public void OnHolster() { gameObject.SetActive(false); }
    public void OnUseHold() { }
    public void OnUseRelease() { }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed) { ChangeLayer(grabableLayer); isJustThrowed = false; }
            else if (isJustDropped) { ChangeLayer(grabableLayer); isJustDropped = false; }
            else if (gameObject.layer == ungrabableLayer) ChangeLayer(grabableLayer);

            HandleSoundFX(collision);
        }
    }

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
}