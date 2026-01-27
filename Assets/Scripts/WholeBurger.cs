using System.Collections.Generic;
using UnityEngine;

public class WholeBurger : MonoBehaviour, IGrabable
{
    [Header("Identity")]
    public WholeBurgerData data;
    public GameManager.BurgerTypes BurgerType = GameManager.BurgerTypes.Null;

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

    private bool isJustThrowed;
    private bool isJustDropped;

    private float lastSoundTime = 0f;

    private Quaternion collisionRotation;

    private float pitchMultiplier;

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

        pitchMultiplier = height >= 1 ? 0.8f : height >= 0.5f ? 1f : 1.2f;

        // Enum'ý kaydet
        BurgerType = type;

        TotalBurgerHeight = height;
        ChangeLayer(grabableLayer);

        if (data.dropParticle != null)
            Instantiate(data.dropParticle, transform.position, Quaternion.identity);

        SoundManager.Instance.PlaySoundFX(
                data.audioClips[3],
                transform,
                data.initSoundVolume,
                data.initSoundMinPitch * pitchMultiplier,
                data.initSoundMaxPitch * pitchMultiplier, false
            );
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

        SoundManager.Instance.PlaySoundFX(
                data.audioClips[0],
                transform,
                data.grabSoundVolume,
                data.grabSoundMinPitch * pitchMultiplier,
                data.grabSoundMaxPitch * pitchMultiplier, false
            );
    }

    public void OnDrop(Vector3 direction, float force) => Release(direction, force, false);
    public void OnThrow(Vector3 direction, float force) => Release(direction, force, true);

    private void Release(Vector3 direction, float force, bool isThrowed)
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

        if (isThrowed) isJustThrowed = true;
        else isJustDropped = true;

            ChangeLayer(ungrabableLayer);
    }

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
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed)
            {
                CalculateCollisionRotation(collision);

                ChangeLayer(grabableLayer);

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                CalculateCollisionRotation(collision);

                ChangeLayer(grabableLayer);

                isJustDropped = false;
            }
            
            HandleSoundFX(collision);

        }


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

    public List<GameObject> GetVisualParts()
    {
        // Eðer childVisuals listesi zaten temizse (sadece köfte, ekmek vs. varsa) direkt bunu dön:
        return new List<GameObject>(childVisuals);

        // HAFÝF NOT: Eðer childVisuals listesinde de duman varsa, 
        // burada bir temizlik yapabilirsin. Örn:
        /*
        List<GameObject> cleanList = new List<GameObject>();
        foreach(var part in childVisuals) {
            if(part.GetComponent<ParticleSystem>() == null) cleanList.Add(part);
        }
        return cleanList;
        */
    }

    private void CalculateCollisionRotation(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];

        Vector3 normal = contact.normal;
        Vector3 hitPoint = contact.point + normal * 0.02f;

        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent == Vector3.zero)
            tangent = Vector3.Cross(normal, Vector3.forward);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal, tangent);

        // Normal yönüne göre rotation hesapla
        collisionRotation = Quaternion.LookRotation(normal) * Quaternion.Euler(0, 180, 0);
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
                data.throwSoundMinPitch * pitchMultiplier,
                data.throwSoundMaxPitch * pitchMultiplier, false
            );

            if (data.throwParticle != null)
                Instantiate(data.throwParticle, transform.position, collisionRotation);
        }
        else
        {
            // === DÜÞME SESÝ (Yavaþ/Orta) ===
            SoundManager.Instance.PlaySoundFX(
                data.audioClips[1],
                transform,
                data.dropSoundVolume,
                data.dropSoundMinPitch * pitchMultiplier,
                data.dropSoundMaxPitch * pitchMultiplier, false
            );

            if (data.dropParticle != null)
                Instantiate(data.dropParticle, transform.position, collisionRotation);
        }

        // Ses çaldýk, zamaný kaydet
        lastSoundTime = Time.time;
    }
}