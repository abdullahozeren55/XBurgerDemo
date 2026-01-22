using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class Holder : MonoBehaviour, IGrabable
{
    public enum HolderIngredient
    {
        Empty,
        Fries,
        OnionRing,
        SmileyFries,
        DinoNuggets,
        Nuggets,
        CheeseSticks,
        CrispyChicken
    }

    [System.Serializable]
    public struct VisualMapping
    {
        public HolderIngredient type;
        public GameObject visualObject;
    }

    [Header("Visual References")]
    [SerializeField] private List<VisualMapping> visualMappings;

    [Header("Data & IGrabable")]
    [SerializeField] private HolderData data;

    // --- IGrabable Properties ---
    public IGrabable Master => this;

    public HolderIngredient CurrentIngredient => currentIngredientType;
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public ItemIcon IconData { get => data.iconData[(int)currentIngredientType]; set => data.iconData[(int)currentIngredientType] = value; }
    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }
    public PlayerManager.HandRigTypes HandRigType { get => data.handRigType; set => data.handRigType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;

    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }
    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }
    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public string FocusTextKey
    {
        get => data.focusTextKeys[(int)currentIngredientType];
        set => data.focusTextKeys[(int)currentIngredientType] = value;
    }

    private HolderIngredient currentIngredientType = HolderIngredient.Empty;

    // --- LOGIC VARIABLES ---
    [HideInInspector] public Tray currentTray;
    private bool isGettingPutOnTray;
    // isOnTray silindi (Gerek yok)

    private bool isJustDropped;
    private bool isJustThrowed;

    // --- References ---
    private Rigidbody rb;
    private Collider col;

    // Layers
    private int grabableLayer;
    private int ungrabableLayer;
    private int grabbedLayer;
    private int grabableOutlinedLayer;
    private int grabableOutlinedGreenLayer;
    private int interactableOutlinedRedLayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        UpdateVisuals();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        grabableOutlinedGreenLayer = LayerMask.NameToLayer("GrabableOutlinedGreen");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
    }

    // --- YENÝ YERLEÞME METODU (DRINKCUP MANTIÐI) ---
    public void PlaceOnTray(Transform targetSlot, Transform apexTransform, Tray trayRef, int slotIndex)
    {
        // 1. State Ayarla
        isGettingPutOnTray = true;
        isJustDropped = false;
        isJustThrowed = false;
        currentTray = trayRef;

        // Çarpýþmayý yoksay (Tepsiyle kavga etmesin)
        if (currentTray != null && col != null)
        {
            Physics.IgnoreCollision(col, currentTray.GetCollider, true);
        }

        // Player elinden düþür
        if (PlayerManager.Instance != null && IsGrabbed)
        {
            PlayerManager.Instance.ResetPlayerGrab(this);
        }

        // 2. Fizik Kapat
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        col.enabled = false; // Yerleþirken tamamen kapalý kalsýn

        // 3. Parent Ata (Dünya pozisyonunu koru: true)
        transform.SetParent(targetSlot, true);

        // 4. HEDEF OFFSET VE RANDOMÝZASYON
        Vector3 baseLocalPos = Vector3.zero;
        Vector3 baseLocalRot = Vector3.zero;

        // Data'dan veriyi çek
        if (data.slotOffsets != null && slotIndex < data.slotOffsets.Length)
        {
            baseLocalPos = data.slotOffsets[slotIndex].localPosition;
            baseLocalRot = data.slotOffsets[slotIndex].localRotation;
        }

        Quaternion finalTargetRotation = Quaternion.Euler(baseLocalRot);
        // -----------------------------

        // 5. APEX Hesabý (Havadan gelme)
        Vector3 localApexPos;
        if (apexTransform != null)
        {
            localApexPos = targetSlot.InverseTransformPoint(apexTransform.position);
        }
        else
        {
            localApexPos = baseLocalPos + Vector3.up * 0.15f;
        }

        // 6. DOTween Sequence
        Vector3[] pathPoints = new Vector3[] { localApexPos, baseLocalPos };
        Sequence seq = DOTween.Sequence();

        // Hareket (Kavisli)
        seq.Join(transform.DOLocalPath(pathPoints, 0.2f, PathType.CatmullRom).SetEase(Ease.OutSine));

        // Dönüþ (Quaternion - Kýsa Yol)
        seq.Join(transform.DOLocalRotateQuaternion(finalTargetRotation, 0.2f).SetEase(Ease.OutBack));

        // Scale (Garantiye al)
        seq.Join(transform.DOScale(data.trayLocalScale, 0.2f));

        seq.OnComplete(() =>
        {
            // Floating point hatalarýný temizle
            transform.localPosition = baseLocalPos;
            transform.localRotation = finalTargetRotation;

            isGettingPutOnTray = false;

            // Collider'ý aç (IgnoreCollision sayesinde tepsiyle çarpýþmaz, ama Raycast tutar)
            col.enabled = true;

            // Layer'ý tepsiye uydur
            if (currentTray != null) ChangeLayer(currentTray.gameObject.layer);
        });
    }

    // --- IGrabable Standartlarý ---

    public void OnGrab(Transform grabPoint)
    {
        IsGrabbed = true;

        ChangeLayer(grabbedLayer);

        // --- TEPSÝDEN AYRILMA ---
        if (currentTray != null)
        {
            // Çarpýþmayý Geri Aç
            if (col != null) Physics.IgnoreCollision(col, currentTray.GetCollider, false);

            currentTray.RemoveItem(this);
            currentTray = null;
        }

        isGettingPutOnTray = false;
        // ------------------------

        rb.isKinematic = true;
        rb.useGravity = false;
        col.enabled = false;

        transform.SetParent(grabPoint);
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);

        // SCALE RESET (Ýsteðin üzerine eklendi)
        transform.localScale = data.grabbedLocalScale;
    }

    public void OnDrop(Vector3 direction, float force)
    {
        Release(direction, force);
        isJustDropped = true;
    }

    public void OnThrow(Vector3 direction, float force)
    {
        Release(direction, force);
        isJustThrowed = true;
    }

    private void Release(Vector3 direction, float force)
    {
        IsGrabbed = false;
        transform.SetParent(null);

        rb.isKinematic = false; // Fizik geri gelir
        rb.useGravity = true;
        col.enabled = true;

        rb.AddForce(direction * force, ForceMode.Impulse);
        ChangeLayer(ungrabableLayer);
    }

    public void OnFocus()
    {
        // Yerleþirken focus olmasýn (Titreþimi önler)
        if (isJustDropped || isJustThrowed || isGettingPutOnTray) return;

        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : grabableOutlinedLayer);
    }

    public void OnLoseFocus()
    {
        if (isJustDropped || isJustThrowed || isGettingPutOnTray) return;

        ChangeLayer(grabableLayer);
    }

    // ... (TryCombine, CanCombine, Fill, UpdateVisuals, ChangeLayer vs. AYNEN KALSIN) ...
    public bool TryCombine(IGrabable otherItem)
    {
        if (currentIngredientType != HolderIngredient.Empty) return false;
        Fryable item = otherItem as Fryable;
        if (item == null) return false;
        if (item.CurrentCookingState != CookAmount.REGULAR) return false;
        if (item.data.type != HolderIngredient.Empty && item.data.type != HolderIngredient.CrispyChicken)
        {
            Fill(item.data.type, item);
            return true;
        }
        return false;
    }

    public bool CanCombine(IGrabable otherItem)
    {
        if (currentIngredientType != HolderIngredient.Empty) return false;
        Fryable item = otherItem as Fryable;
        if (item == null) return false;
        if (item.CurrentCookingState != CookAmount.REGULAR) return false;
        if (item.data.type != HolderIngredient.Empty && item.data.type != HolderIngredient.CrispyChicken) return true;
        return false;
    }

    private void Fill(HolderIngredient newIngedient, Fryable sourceItem)
    {
        currentIngredientType = newIngedient;
        PlayerManager.Instance.ForceUpdatePlayerSlotIcon(this, IconData);
        UpdateVisuals();
        Instantiate(data.smokePrefabLocal, transform);
        Destroy(sourceItem.gameObject);
    }

    private void UpdateVisuals()
    {
        foreach (var mapping in visualMappings)
        {
            if (mapping.visualObject != null)
            {
                mapping.visualObject.SetActive(mapping.type == currentIngredientType);
            }
        }
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
        foreach (var mapping in visualMappings)
        {
            if (mapping.visualObject != null) mapping.visualObject.layer = layer;
        }
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

    public void OnHolster() { gameObject.SetActive(false); }
    public void OnUseHold() { }
    public void OnUseRelease() { }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && gameObject.layer == ungrabableLayer) ChangeLayer(grabableLayer);
        isJustDropped = false;
        isJustThrowed = false;
    }
}