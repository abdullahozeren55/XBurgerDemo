using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct TraySlotOffset
{
    public string slotName; // Inspector'da karýþmasýn diye "Slot 0", "Slot 1" yazarsýn
    public Vector3 localPosition;
    public Vector3 localRotation;
}
public class Tray : MonoBehaviour, IGrabable
{
    // ... (Mevcut Deðiþkenler Aynen Kalsýn) ...
    public IGrabable Master => this;

    public Collider GetCollider => col;

    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;
    public Sprite Icon { get => data.icon; set => data.icon = value; }

    // --- YENÝ SLOT SÝSTEMÝ ---
    [Header("Universal Slots")]
    [SerializeField] private Transform[] slotPoints; // 6 adet nokta
    [SerializeField] private Transform[] slotApexes; // 6 adet apex

    // Hangi slot dolu? (true = dolu)
    private bool[] isSlotOccupied;

    // Hangi eþya hangi slotta oturuyor? (Eþya -> SlotIndex)
    private Dictionary<IGrabable, int> itemToSlotMap = new Dictionary<IGrabable, int>();

    // Sadece toplu iþlemler (ChangeLayer gibi) için liste
    private List<IGrabable> itemsOnTray = new List<IGrabable>();

    // ... (Data Variables) ...
    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }
    public PlayerManager.HandRigTypes HandRigType { get => data.handRigType; set => data.handRigType = value; }
    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }
    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }
    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }
    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }
    public Transform LeftHandPoint { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }
    public TrayData data;
    public string FocusTextKey { get => data.focusTextKey; set => data.focusTextKey = value; }

    private Rigidbody rb;
    private Collider col;
    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;
    private bool isJustThrowed;
    private bool isJustDropped;
    private float lastSoundTime = 0f;

    // --- SÝGORTA ÝÇÝN COROUTINE REFERANSI ---
    private Coroutine safetyResetCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");

        // Slot takibini baþlat
        if (slotPoints != null)
        {
            isSlotOccupied = new bool[slotPoints.Length];
        }

        itemsOnTray.Clear();
        itemToSlotMap.Clear();
    }

    // ... (TryPlaceItem, RemoveItem, ChangeLayer AYNEN KALSIN) ...
    public void TryPlaceItem(Collider other)
    {
        IGrabable item = other.GetComponent<IGrabable>()?.Master;
        if (item == null) return;

        // Zaten tepsideyse iþlem yapma
        if (itemsOnTray.Contains(item)) return;

        // --- BOÞ SLOT BULMA MANTIÐI (RANDOM) ---

        // 1. Boþ olan slotlarýn indekslerini listele
        List<int> emptySlots = new List<int>();
        for (int i = 0; i < isSlotOccupied.Length; i++)
        {
            if (!isSlotOccupied[i]) emptySlots.Add(i);
        }

        // 2. Eðer hiç boþ yer yoksa çýk
        if (emptySlots.Count == 0) return;

        // 3. Rastgele bir slot seç
        int randomIndex = emptySlots[Random.Range(0, emptySlots.Count)];
        Transform targetPoint = slotPoints[randomIndex];
        Transform targetApex = slotApexes[randomIndex];

        // --- TÝP KONTROLÜ VE YERLEÞTÝRME ---

        bool placed = false;

        // A. DRINK CUP
        if (item is DrinkCup drinkCup)
        {
            if (drinkCup.IsFull && drinkCup.HasLid)
            {
                // Index numarasýný da gönderiyoruz!
                drinkCup.PlaceOnTray(targetPoint, targetApex, this, randomIndex);
                placed = true;
            }
        }
        // B. HOLDER
        else if (item is Holder holder)
        {
            if (holder.CurrentIngredient != Holder.HolderIngredient.Empty)
            {
                // Index numarasýný da gönderiyoruz!
                holder.PlaceOnTray(targetPoint, targetApex, this, randomIndex);
                placed = true;
            }
        }
        else if (item is BurgerBox burgerBox)
        {
            // Kutu dolu mu? (Opsiyonel: Boþ kutu tepsiye konmasýn dersen)
            // if (!burgerBox.IsBoxFull) return; // Þimdilik kapalý, boþ da koyabil

            // Yerleþtir
            burgerBox.PlaceOnTray(targetPoint, targetApex, this, randomIndex);
            placed = true;
        }

        // --- KAYIT ÝÞLEMLERÝ ---
        if (placed)
        {
            // Slotu kilitle
            isSlotOccupied[randomIndex] = true;

            // Haritaya iþle
            itemToSlotMap.Add(item, randomIndex);
            itemsOnTray.Add(item);
        }
    }

    public void RemoveItem(IGrabable item)
    {
        // Önce listelerden sil
        if (itemsOnTray.Contains(item)) itemsOnTray.Remove(item);

        // Hangi slotta oturuyordu?
        if (itemToSlotMap.ContainsKey(item))
        {
            int slotIndex = itemToSlotMap[item];

            // Slotu boþa çýkar
            if (slotIndex >= 0 && slotIndex < isSlotOccupied.Length)
            {
                isSlotOccupied[slotIndex] = false;
            }

            // Haritadan sil
            itemToSlotMap.Remove(item);
        }
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
        foreach (var item in itemsOnTray) if (item != null) item.ChangeLayer(layer);
    }

    // --- DÜZELTÝLEN GRAB ---
    public void OnGrab(Transform grabPoint)
    {
        // 1. Önce Sigortayý kapat (Eðer çalýþýyorsa)
        if (safetyResetCoroutine != null)
        {
            StopCoroutine(safetyResetCoroutine);
            safetyResetCoroutine = null;
        }

        // 2. State Temizliði (HAYATÝ)
        // Eðer yerdeyken Ungrabable kaldýysa bile Grab anýnda her þeyi resetliyoruz.
        isJustDropped = false;
        isJustThrowed = false;

        ChangeLayer(grabbedLayer);
        transform.DOKill();

        // 3. Fizik Ayarlarý (KÝNEMATÝK ÞART)
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true; // <-- BÖYLECE DOTWEEN ÝLE FÝZÝK ÇATIÞMAZ

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutQuad);
        transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutQuad);
        transform.localScale = Vector3.one;

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);
    }

    // --- DÜZELTÝLEN DROP ---
    public void OnDrop(Vector3 direction, float force)
    {
        transform.DOKill();

        IsGrabbed = false;
        transform.SetParent(null);

        rb.useGravity = true;
        rb.isKinematic = false; // <-- Fizik geri gelsin

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustDropped = true;
        ChangeLayer(ungrabableLayer);

        // --- GÜVENLÝK SÝGORTASI BAÞLAT ---
        // Eðer 1 saniye içinde yere çarpýp düzelmezse, biz elle düzeltiriz.
        if (safetyResetCoroutine != null) StopCoroutine(safetyResetCoroutine);
        safetyResetCoroutine = StartCoroutine(SafetyLayerReset());
    }

    // --- DÜZELTÝLEN THROW ---
    public void OnThrow(Vector3 direction, float force)
    {
        transform.DOKill();

        IsGrabbed = false;
        transform.SetParent(null);

        rb.useGravity = true;
        rb.isKinematic = false; // <-- Fizik geri gelsin

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustThrowed = true;
        ChangeLayer(ungrabableLayer);

        // Sigorta
        if (safetyResetCoroutine != null) StopCoroutine(safetyResetCoroutine);
        safetyResetCoroutine = StartCoroutine(SafetyLayerReset());
    }

    // --- YENÝ: SÝGORTA COROUTINE ---
    // Eðer obje Ungrabable layerýnda takýlýrsa (çarpýþma algýlanmazsa) bunu devreye sokar.
    private IEnumerator SafetyLayerReset()
    {
        yield return new WaitForSeconds(1.0f); // 1 saniye bekle

        // Eðer hala Grab edilmediyse ve hala Ungrabable veya Grabbed ise
        if (!IsGrabbed && (gameObject.layer == ungrabableLayer || gameObject.layer == grabbedLayer))
        {
            // Resetle
            ChangeLayer(grabableLayer);
            isJustDropped = false;
            isJustThrowed = false;
        }
    }

    // ... (Kalanlar Ayný) ...
    public void OnHolster() { }
    public void OnFocus() { if (!isJustDropped && !isJustThrowed) ChangeLayer(grabableOutlinedLayer); }
    public void OnLoseFocus() { if (!isJustDropped && !isJustThrowed) ChangeLayer(grabableLayer); }
    public void OutlineChangeCheck()
    {
        if (gameObject.layer == grabableOutlinedLayer && OutlineShouldBeRed) ChangeLayer(interactableOutlinedRedLayer);
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed) ChangeLayer(grabableOutlinedLayer);
    }
    private void OnDestroy() { PlayerManager.Instance.ResetPlayerGrab(this); }

    private void HandleSoundFX(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < data.dropThreshold || Time.time - lastSoundTime < data.soundCooldown) return;
        if (impactForce >= data.throwThreshold) SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, data.throwSoundVolume, data.throwSoundMinPitch, data.throwSoundMaxPitch);
        else SoundManager.Instance.PlaySoundFX(data.audioClips[1], transform, data.dropSoundVolume, data.dropSoundMinPitch, data.dropSoundMaxPitch);
        lastSoundTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            // Eðer sigorta çalýþýyorsa durdur, çünkü doðal yolla çarpýþtýk
            if (safetyResetCoroutine != null)
            {
                StopCoroutine(safetyResetCoroutine);
                safetyResetCoroutine = null;
            }

            if (isJustThrowed) { ChangeLayer(grabableLayer); isJustThrowed = false; }
            else if (isJustDropped) { ChangeLayer(grabableLayer); isJustDropped = false; }
            HandleSoundFX(collision);
        }
    }

    public void OnUseHold() { }
    public void OnUseRelease() { }
    public bool TryCombine(IGrabable otherItem) { return false; }
    public bool CanCombine(IGrabable otherItem) { return false; }
}