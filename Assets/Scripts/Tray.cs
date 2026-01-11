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

    public bool IsInHolder = false;

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
        if (IsInHolder) return;

        IGrabable item = other.GetComponent<IGrabable>()?.Master;
        if (item == null) return;
        if (itemsOnTray.Contains(item)) return;

        // --- ÖNEMLÝ EKLEME: IS GRABBED KONTROLÜ ---
        // Eðer oyuncu hala elinde tutuyorsa tepsi onu çekip almasýn.
        // (Normalde layerlar bunu engeller ama fizik bazen kaçýrabilir, sigorta olsun)
        if (item.IsGrabbed) return;

        // --- 1. GENEL KONTROL: Eþya zaten baþka bir tepside mi? ---
        // Her bir tipi ayrý ayrý cast edip kontrol etmek yerine, 
        // eþyayý tanýyýp "zaten yerleþmiþ mi" diye bakýyoruz.
        if (IsItemAlreadyOnAnotherTray(item)) return;


        int targetSlotIndex = -1;
        int stackIndex = 0;

        // A. SOS KAPSÜLÜ ÝSE
        if (item is SauceCapsule sauce)
        {
            // (Buradaki currentTray kontrolünü yukarýdaki fonksiyona taþýdýk)

            for (int i = 0; i < slotPoints.Length; i++)
            {
                int count = GetSauceCountInSlot(i);
                if (count > 0 && count < data.maxSaucePerSlot)
                {
                    targetSlotIndex = i;
                    stackIndex = count;

                    List<SauceCapsule> existingSauces = GetAllSaucesInSlot(i);
                    foreach (var s in existingSauces) s.SetStacked(true);

                    break;
                }
            }

            if (targetSlotIndex == -1)
            {
                targetSlotIndex = FindEmptySlotIndex();
                stackIndex = 0;
            }

            if (targetSlotIndex != -1)
            {
                sauce.PlaceOnTray(slotPoints[targetSlotIndex], slotApexes[targetSlotIndex], this, targetSlotIndex, stackIndex);
                RegisterItem(item, targetSlotIndex);
            }
            return;
        }

        // B. DÝÐER EÞYALAR
        targetSlotIndex = FindEmptySlotIndex();
        if (targetSlotIndex == -1) return;

        // Aþaðýdaki bloklarda artýk "currentTray != null" kontrolüne gerek yok,
        // en baþta "IsItemAlreadyOnAnotherTray" ile hallettik.

        if (item is DrinkCup drinkCup)
        {
            if (drinkCup.IsFull && drinkCup.HasLid)
            {
                drinkCup.PlaceOnTray(slotPoints[targetSlotIndex], slotApexes[targetSlotIndex], this, targetSlotIndex);
                RegisterItem(item, targetSlotIndex);
            }
        }
        else if (item is Holder holder)
        {
            if (holder.CurrentIngredient != Holder.HolderIngredient.Empty)
            {
                holder.PlaceOnTray(slotPoints[targetSlotIndex], slotApexes[targetSlotIndex], this, targetSlotIndex);
                RegisterItem(item, targetSlotIndex);
            }
        }
        else if (item is Drink drink)
        {
            drink.PlaceOnTray(slotPoints[targetSlotIndex], slotApexes[targetSlotIndex], this, targetSlotIndex);
            RegisterItem(item, targetSlotIndex);
        }
        else if (item is BurgerBox burgerBox)
        {
            if (burgerBox.ContainedBurgerType != GameManager.BurgerTypes.Null)
            {
                burgerBox.PlaceOnTray(slotPoints[targetSlotIndex], slotApexes[targetSlotIndex], this, targetSlotIndex);
                RegisterItem(item, targetSlotIndex);
            }
        }
        else if (item is Toy toy)
        {
            toy.PlaceOnTray(slotPoints[targetSlotIndex], slotApexes[targetSlotIndex], this, targetSlotIndex);
            RegisterItem(item, targetSlotIndex);
        }
    }

    // --- YARDIMCI METOT (Kod Tekrarýný Önler) ---
    private bool IsItemAlreadyOnAnotherTray(IGrabable item)
    {
        // C# 7.0 Pattern Matching kullanarak temiz kontrol
        switch (item)
        {
            case SauceCapsule s: return s.currentTray != null;
            case DrinkCup dc: return dc.currentTray != null;
            case Holder h: return h.currentTray != null;
            case Drink d: return d.currentTray != null;
            case BurgerBox b: return b.currentTray != null;
            case Toy t: return t.currentTray != null;
            default: return false;
        }
    }

    // --- YENÝ YARDIMCI FONKSÝYONLAR ---

    // Bir slotta kaç tane sos olduðunu sayar
    private int GetSauceCountInSlot(int slotIndex)
    {
        int count = 0;
        foreach (var storedItem in itemsOnTray)
        {
            // Eðer bu eþya o slottaysa VE o eþya bir Sos ise
            if (itemToSlotMap.ContainsKey(storedItem) && itemToSlotMap[storedItem] == slotIndex)
            {
                if (storedItem is SauceCapsule)
                {
                    count++;
                }
                else
                {
                    // Slotta sos dýþýnda biþey varsa (örn: Burger), bu slot sos için "kirli"dir.
                    // -1 döndürerek burayý pas geçilmesini saðlayabiliriz veya logic kurarýz.
                    // Þimdilik sadece sos sayýsýný dönelim.
                    return -1; // -1: Sos stacking'e uygun deðil
                }
            }
        }
        return count;
    }

    // Boþ slot bulucu
    private int FindEmptySlotIndex()
    {
        List<int> emptySlots = new List<int>();
        for (int i = 0; i < isSlotOccupied.Length; i++)
        {
            // isSlotOccupied[i] true ise orasý doludur (ya burger vardýr ya da sos limiti dolmuþtur)
            if (!isSlotOccupied[i]) emptySlots.Add(i);
        }

        if (emptySlots.Count == 0) return -1;
        return emptySlots[Random.Range(0, emptySlots.Count)];
    }

    // Eþyayý kaydetme ve slotu kilitleme
    private void RegisterItem(IGrabable item, int slotIndex)
    {
        itemToSlotMap.Add(item, slotIndex);
        itemsOnTray.Add(item);

        // Slot Doluluk Kontrolü:
        // Eðer bu bir sos ise, ve limit dolmadýysa slotu HALA BOÞ GÖSTER (ki baþkasý gelebilsin)
        if (item is SauceCapsule)
        {
            int count = GetSauceCountInSlot(slotIndex);
            if (count >= data.maxSaucePerSlot)
            {
                isSlotOccupied[slotIndex] = true; // Limit doldu, artýk dolu iþaretle
            }
            else
            {
                // Limit dolmadý, isSlotOccupied false kalmaya devam etsin
                // (Ama FindEmptySlotIndex orayý bulursa ne olacak? Oraya burger koymaya çalýþabilir!)
                // FIX: FindEmptySlotIndex sadece "Hiçbir þey olmayan" yerleri mi bulmalý?
                // HAYIR. isSlotOccupied dizisini "Baþka TÜR eþya giremez" olarak kullanalým.

                // Þöyle yapalým:
                // Sos koyduðumuz an orayý "Dolu" iþaretleyelim.
                // Ama sos yerleþtirme mantýðý (TryPlaceItem baþýndaki döngü) isSlotOccupied'a bakmýyor,
                // direkt içeriðe bakýyor. O yüzden burayý true yapmamýzda sakýnca yok.
                // Tek risk: FindEmptySlotIndex dolu slotu vermemeli.

                isSlotOccupied[slotIndex] = true;

                // FAKAT: Eðer isSlotOccupied true olursa, 2. sos oraya nasýl girecek?
                // TryPlaceItem'ýn baþýndaki A mantýðý (Sos Stackleme) isSlotOccupied'a bakmadan çalýþýyor.
                // O yüzden sorun yok.
            }
        }
        else
        {
            // Sos deðilse direkt kapat
            isSlotOccupied[slotIndex] = true;
        }
    }

    public void RemoveItem(IGrabable item)
    {
        if (itemsOnTray.Contains(item)) itemsOnTray.Remove(item);

        if (itemToSlotMap.ContainsKey(item))
        {
            int slotIndex = itemToSlotMap[item];
            itemToSlotMap.Remove(item);

            // --- SOS ÇIKINCA NE OLACAK? (FIXED) ---
            if (item is SauceCapsule)
            {
                // O slotta kalan tüm soslarý bul
                List<SauceCapsule> remainingSauces = GetAllSaucesInSlot(slotIndex);

                if (remainingSauces.Count > 0)
                {
                    // En tepedekini bul (Yüksekliðine göre karar veriyoruz)
                    // Z ekseni yüksekliði belirlediði için localPosition.z'si en büyük olan tepededir.
                    // Veya Y ekseniyse Y. (Senin sistemde Z offset ekliyorduk)

                    SauceCapsule topSauce = null;
                    float maxH = -999f;

                    foreach (var s in remainingSauces)
                    {
                        // Stacked olsun olmasýn hepsini tara
                        // Hangi ekseni yükseklik yaptýysak ona bak (SauceCapsuleData.stackHeightOffset mantýðý)
                        // Genelde Z offset vermiþtik.
                        float h = s.transform.localPosition.z;
                        if (h > maxH)
                        {
                            maxH = h;
                            topSauce = s;
                        }
                    }

                    // En tepedekini özgür býrak, diðerlerini kilitle
                    if (topSauce != null)
                    {
                        topSauce.SetStacked(false);
                    }

                    // (Opsiyonel) Garanti olsun diye diðerleri hala kilitli mi emin olabilirsin
                    // ama zaten kilitliydiler, dokunmaya gerek yok.
                }
            }
            // --------------------------------------

            // Slotu ne zaman "Boþ" (false) yapacaðýz?
            // O slotta hiç eþya kalmadýðýnda.
            if (slotIndex >= 0 && slotIndex < isSlotOccupied.Length)
            {
                // Slotun içinde baþka eþya kaldý mý kontrol et
                bool isStillOccupied = false;
                foreach (var kvp in itemToSlotMap)
                {
                    if (kvp.Value == slotIndex)
                    {
                        isStillOccupied = true;
                        break;
                    }
                }

                isSlotOccupied[slotIndex] = isStillOccupied;
            }
        }
    }

    // --- YENÝ YARDIMCI: Hepsini Getir ---
    private List<SauceCapsule> GetAllSaucesInSlot(int slotIndex)
    {
        List<SauceCapsule> sauces = new List<SauceCapsule>();
        foreach (var storedItem in itemsOnTray)
        {
            if (itemToSlotMap.ContainsKey(storedItem) && itemToSlotMap[storedItem] == slotIndex)
            {
                if (storedItem is SauceCapsule s)
                {
                    sauces.Add(s);
                }
            }
        }
        return sauces;
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