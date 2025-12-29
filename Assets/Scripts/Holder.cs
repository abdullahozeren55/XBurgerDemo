using UnityEngine;
using System.Collections.Generic; // List için gerekli

public class Holder : MonoBehaviour, IGrabable
{
    public enum HolderIngredient
    {
        Empty,
        Fries,
        OnionRing,
        // Ilerde buraya Nugget vs. eklersin
    }

    // YENÝ: Hangi enum hangi objeyi açacak?
    [System.Serializable]
    public struct VisualMapping
    {
        public HolderIngredient type;
        public GameObject visualObject; // Child olarak koyduðun, scale'i ayarlanmýþ obje
    }

    [Header("Visual References")]
    // Tek bir obje yerine liste tutuyoruz
    [SerializeField] private List<VisualMapping> visualMappings;

    [Header("Data & IGrabable")]
    [SerializeField] private HolderData data;

    // --- IGrabable Properties ---
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public Sprite Icon { get => data.icon; set => data.icon = value; }
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

    // Focus Text artýk içeriðe göre deðiþiyor
    public string FocusTextKey
    {
        get => data.focusTextKeys[(int)currentIngredientType];
        set => data.focusTextKeys[(int)currentIngredientType] = value;
    }

    private HolderIngredient currentIngredientType = HolderIngredient.Empty;

    // --- References ---
    private Rigidbody rb;
    private Collider col;
    private int grabableLayer;
    private int grabableOutlinedGreenLayer;
    private int ungrabableLayer;
    private int grabbedLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // Content baþlarken kapalý olsun (veya editördeki duruma göre)
        UpdateVisuals();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        grabableOutlinedGreenLayer = LayerMask.NameToLayer("GrabableOutlinedGreen");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
    }

    // --- ASIL OLAY: BÝRLEÞTÝRME MANTIÐI ---
    public bool TryCombine(IGrabable otherItem)
    {
        // 1. Zaten doluysak kimseyi alamayýz
        if (currentIngredientType != HolderIngredient.Empty) return false;

        // 2. Gelen þey bir "Fryable" (Kýzartmalýk) mý?
        Fryable item = otherItem as Fryable;
        if (item == null) return false;

        // 3. Sadece REGULAR (Piþmiþ) kabul edelim.
        if (item.CurrentCookingState != Cookable.CookAmount.REGULAR)
        {
            return false;
        }

        // 4. Türüne göre doldur
        if (item.data.type == FryableData.FryableType.Fries)
        {
            Fill(HolderIngredient.Fries, item);
            return true;
        }
        else if (item.data.type == FryableData.FryableType.OnionRing)
        {
            Fill(HolderIngredient.OnionRing, item);
            return true;
        }

        return false;
    }

    public bool CanCombine(IGrabable otherItem)
    {
        // 1. Doluysak olmaz
        if (currentIngredientType != HolderIngredient.Empty) return false;

        // 2. Fryable mý?
        Fryable item = otherItem as Fryable;
        if (item == null) return false;

        // 3. Piþmiþ mi?
        if (item.CurrentCookingState != Cookable.CookAmount.REGULAR) return false;

        // 4. Tür uyuyor mu? (Patates veya Soðan Halkasý)
        if (item.data.type == FryableData.FryableType.Fries) return true;
        if (item.data.type == FryableData.FryableType.OnionRing) return true;

        return false;
    }

    private void Fill(HolderIngredient newIngedient, Fryable sourceItem)
    {
        currentIngredientType = newIngedient;

        // Görseli aç
        UpdateVisuals();

        // Yerdeki malzemeyi yok et!
        Destroy(sourceItem.gameObject);

        // Ses efekti eklenebilir: "Hýþýrt"
    }

    private void UpdateVisuals()
    {
        // Listeyi dön, tipi tutaný aç, tutmayaný kapat
        foreach (var mapping in visualMappings)
        {
            if (mapping.visualObject != null)
            {
                bool isActive = (mapping.type == currentIngredientType);
                mapping.visualObject.SetActive(isActive);
            }
        }
    }

    // --- IGrabable Standartlarý ---

    public void OnGrab(Transform grabPoint)
    {
        IsGrabbed = true;
        rb.isKinematic = true;
        rb.useGravity = false;
        col.enabled = false;

        transform.SetParent(grabPoint);
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);

        ChangeLayer(grabbedLayer);
    }

    public void OnDrop(Vector3 direction, float force) => Release(direction, force);
    public void OnThrow(Vector3 direction, float force) => Release(direction, force);

    private void Release(Vector3 direction, float force)
    {
        IsGrabbed = false;
        transform.SetParent(null);
        rb.isKinematic = false;
        rb.useGravity = true;
        col.enabled = true;
        rb.AddForce(direction * force, ForceMode.Impulse);
        ChangeLayer(ungrabableLayer);
    }

    public void OnFocus() { ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : grabableOutlinedLayer); }
    public void OnLoseFocus() { ChangeLayer(grabableLayer); }

    public void OutlineChangeCheck()
    {
        // OutlineChangeCheck mantýðý aynen kalýyor, 
        // ancak ChangeLayer çaðrýldýðýnda child objelerin de layer'ý deðiþmeli.

        if (gameObject.layer == grabableOutlinedLayer)
        {
            if (OutlineShouldBeRed)
                ChangeLayer(interactableOutlinedRedLayer);
            else if (OutlineShouldBeGreen)
                ChangeLayer(grabableOutlinedGreenLayer);
        }
        else if (gameObject.layer == grabableOutlinedGreenLayer)
        {
            if (OutlineShouldBeRed)
                ChangeLayer(interactableOutlinedRedLayer);
            else if (!OutlineShouldBeGreen)
                ChangeLayer(grabableOutlinedLayer);
        }
        else if (gameObject.layer == interactableOutlinedRedLayer)
        {
            if (!OutlineShouldBeRed)
            {
                if (OutlineShouldBeGreen)
                    ChangeLayer(grabableOutlinedGreenLayer);
                else
                    ChangeLayer(grabableOutlinedLayer);
            }
        }
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;

        // Sadece ana objenin deðil, içindeki child objelerin de layer'ýný deðiþtiriyoruz.
        // Böylece outline shader'ý içindeki patatesi de parlatýr.
        foreach (var mapping in visualMappings)
        {
            if (mapping.visualObject != null)
            {
                mapping.visualObject.layer = layer;
            }
        }
    }

    public void OnHolster() { gameObject.SetActive(false); }
    public void OnUseHold() { }
    public void OnUseRelease() { }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && gameObject.layer == ungrabableLayer) ChangeLayer(grabableLayer);
    }
}