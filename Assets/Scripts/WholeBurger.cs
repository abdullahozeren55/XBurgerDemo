using System.Collections.Generic;
using UnityEngine;

public class WholeBurger : MonoBehaviour, IGrabable
{
    [Header("Identity")]
    public WholeBurgerData data;
    [HideInInspector] public GameManager.BurgerTypes BurgerType = GameManager.BurgerTypes.Null;

    public float TotalBurgerHeight { get; private set; }

    private List<GameObject> childVisuals = new List<GameObject>();

    // --- IGrabable Props ---

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

    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }
    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }
    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }
    public string FocusTextKey { get => data.focusTextKeys[(int)BurgerType]; set => data.focusTextKeys[(int)BurgerType] = value; }

    // References
    private Rigidbody rb;

    // Layers
    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;
    private int grabableOutlinedGreenLayer;

    private void Awake()
    {
        // BURADA GETCOMPONENT YAPMIYORUZ!
        // Initialize içinde alacaðýz.

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");
        grabableOutlinedGreenLayer = LayerMask.NameToLayer("GrabableOutlinedGreen");
    }

    // Tray tarafýndan kurulum yapýlýrken çaðrýlacak
    // Rigidbody'yi parametre olarak alýyoruz!

    // --- INITIALIZE (GÜNCELLENDÝ) ---
    public void Initialize(List<GameObject> children, Rigidbody rigidBody, WholeBurgerData burgerData, GameManager.BurgerTypes type, float height)
    {
        childVisuals = children;
        rb = rigidBody;
        data = burgerData;

        // Enum'ý kaydet
        BurgerType = type;

        TotalBurgerHeight = height;
        ChangeLayer(grabableLayer);
    }

    // --- IGrabable Implementation ---

    public void OnGrab(Transform grabPoint)
    {
        IsGrabbed = true;

        ToggleChildColliders(false);

        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        // Parent collider olmadýðý için onu kapatmaya gerek yok.
        // Ama çocuklarýn colliderlarý açýk kalýrsa karakterin içine girince FPC'yi uçurabilir.
        // Layer "Grabbed" olunca karakterle çarpýþmýyorsa sorun yok.
        // Güvenlik için çocuk colliderlarýný kapatmak istersen buraya ekleriz ama þimdilik layer deðiþimi yeterli.

        transform.SetParent(grabPoint);
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);

        ChangeLayer(grabbedLayer);
    }

    public void OnDrop(Vector3 direction, float force) => Release(direction, force);
    public void OnThrow(Vector3 direction, float force) => Release(direction, force);

    private void Release(Vector3 direction, float force)
    {
        ToggleChildColliders(true);

        IsGrabbed = false;
        transform.SetParent(null);

        if (rb != null)
        {
            rb.isKinematic = false;
            rb.useGravity = true;
            rb.AddForce(direction * force, ForceMode.Impulse);
        }

        ChangeLayer(ungrabableLayer);
    }

    public void OnFocus()
    {
        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : grabableOutlinedLayer);
    }

    public void OnLoseFocus()
    {
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
        // Parent layer deðiþir
        gameObject.layer = layer;

        // Tüm çocuklarýn layerý deðiþir (Görsel bütünlük için)
        foreach (var child in childVisuals)
        {
            if (child != null) child.layer = layer;
        }
    }

    public void OnHolster() { gameObject.SetActive(false); }
    public void OnUseHold() { }
    public void OnUseRelease() { }

    public bool TryCombine(IGrabable otherItem)
    {
        return false;
    }

    public bool CanCombine(IGrabable otherItem)
    {
        return false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && gameObject.layer == ungrabableLayer) ChangeLayer(grabableLayer);
    }

    private void ToggleChildColliders(bool state)
    {
        foreach (var child in childVisuals)
        {
            if (child == null) continue;

            // Sadece ana colliderlarý kapatýyoruz (varsa triggerlarý deðil)
            Collider c = child.GetComponent<Collider>();
            if (c != null) c.enabled = state;
        }
    }

    // --- GÜNCELLENEN FONKSÝYON ---
    public void PackIntoBox(BurgerBox box, Transform containerPoint)
    {
        // 1. Pozisyonlama
        transform.SetParent(containerPoint);

        if (data != null)
        {
            transform.localPosition = data.boxPlacementPositionOffset;
            transform.localRotation = Quaternion.Euler(data.boxPlacementRotationOffset);
        }
        else
        {
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        box.ContainedBurgerType = BurgerType;

        // 2. Temizlik ve Devir Teslim Operasyonu

        // A) Rigidbody'yi yok et
        if (rb != null) Destroy(rb);

        // B) Çocuklardaki ChildBurger'i sil, yerine BoxChild ekle!
        foreach (var child in childVisuals)
        {
            if (child != null)
            {
                // Eski kimliði sil
                ChildBurger cb = child.GetComponent<ChildBurger>();
                if (cb != null) Destroy(cb);

                // --- YENÝ: BoxChild Ekle ve Baðla ---
                // Böylece Raycast bu köfteye çarparsa kutuyu tutmuþ sayýlacak.
                BoxChild boxChild = child.AddComponent<BoxChild>();
                boxChild.parentBox = box;
                // ------------------------------------

                // Colliderlarý AÇIK býrakýyoruz.
                // Tag'i temizle
                child.tag = "Untagged";
            }
        }

        // C) Kendi scriptimi yok et
        Destroy(this);
    }
}