using DG.Tweening;
using UnityEngine;

public class Fryable : MonoBehaviour, IGrabable
{
    [Header("Data")]
    public FryableData data;

    [Header("Cooking State")]
    [SerializeField] private float currentCookingTime = 0f;
    public Cookable.CookAmount CurrentCookingState = Cookable.CookAmount.RAW;

    [Header("Conversion Settings")]
    [SerializeField] private BurgerIngredient burgerIngredientScript; // Inspector'dan atayacaðýn kapalý script

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
    public string FocusTextKey { get => data.focusTextKeys[(int)CurrentCookingState]; set => data.focusTextKeys[(int)CurrentCookingState] = value; }

    // --- Logic Variables ---
    private bool isGettingPutOnBasket; // Yerleþme animasyonu kilidi
    private bool isAddedToBasket;      // Þu an sepetin içinde mi?

    private float targetCookTime;
    private float targetBurnTime;

    // References
    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private BoxCollider boxCollider;

    // Cache Layers
    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int grabableOutlinedGreenLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;
    private int onTrayLayer; // "OnTray" ile ayný olabilir veya ayrý bir layer açabilirsin

    // Baðlý olduðu sepet (Varsa)
    [HideInInspector] public FryerBasket currentBasket;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshFilter = GetComponent<MeshFilter>();
        boxCollider = GetComponent<BoxCollider>();

        // Layer ID'lerini al
        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        grabableOutlinedGreenLayer = LayerMask.NameToLayer("GrabableOutlinedGreen");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");
        onTrayLayer = LayerMask.NameToLayer("OnTray"); // Þimdilik OnTray kullanýyoruz, karýþýklýk olmasýn

        // --- RANDOM PÝÞME SÜRESÝ HESABI ---
        // 1. Rastgele bir çarpan belirle (Örn: 0.85 ile 1.15 arasý)
        // cookingVariance 0.15 ise -> 0.85f ile 1.15f arasý sayý üretir
        float varianceMultiplier = Random.Range(1f - data.cookingVariance, 1f + data.cookingVariance);

        // 2. Data'daki base deðeri bu çarpanla çarpýp yerel deðiþkene ata
        targetCookTime = data.timeToCook * varianceMultiplier;

        // Yanma süresine de ayný çarpaný mý uygulayalým yoksa ona da mý random atalým?
        // Bence AYNI ÇARPAN daha mantýklý. 
        // Mantýk: "Bu patates ince kesilmiþ, o yüzden hem çabuk piþer hem çabuk yanar."
        targetBurnTime = data.timeToBurn * varianceMultiplier;

        UpdateVisuals();
    }

    // --- COOKING LOGIC (Mevcut Kodun) ---
    public void Cook(float heatAmount)
    {
        if (CurrentCookingState == Cookable.CookAmount.BURNT) return;
        currentCookingTime += heatAmount;
        CheckState();
    }

    private void CheckState()
    {
        Cookable.CookAmount oldState = CurrentCookingState;

        // ARTIK DATA YERÝNE YEREL DEÐÝÞKENLERÝ KULLANIYORUZ:
        if (currentCookingTime >= targetBurnTime) CurrentCookingState = Cookable.CookAmount.BURNT;
        else if (currentCookingTime >= targetCookTime) CurrentCookingState = Cookable.CookAmount.REGULAR;
        else CurrentCookingState = Cookable.CookAmount.RAW;

        if (CurrentCookingState != oldState)
        {
            UpdateVisuals();
            // Ses eklersek burada "Çýt" sesi çalabilir.
        }
    }

    private void UpdateVisuals()
    {
        if (meshRenderer == null || data == null) return;
        switch (CurrentCookingState)
        {
            case Cookable.CookAmount.RAW: meshRenderer.material = data.rawMat; break;
            case Cookable.CookAmount.REGULAR: meshRenderer.material = data.cookedMat; break;
            case Cookable.CookAmount.BURNT: meshRenderer.material = data.burntMat; break;
        }
    }

    // --- BASKET LOGIC (Sepete Yerleþme) ---

    // Parametre olarak "Vector3 apexLocalPos" eklendi
    public void SetVisualState(int stackLevel, Transform container, float currentStackZ, Vector3 apexLocalPos)
    {
        if (data.basketMeshes == null || data.basketMeshes.Length == 0) return;

        // 1. Config ve Mesh iþlemleri (Ayný)
        int index = Mathf.Clamp(stackLevel, 0, data.basketMeshes.Length - 1);
        FryableData.MeshConfig config = data.basketMeshes[index];

        if (config.mesh != null)
        {
            meshFilter.mesh = config.mesh;

            if (boxCollider != null)
            {
                boxCollider.center = config.colliderCenter;
                boxCollider.size = config.colliderSize;
            }
        }

        // 2. Hedef Pozisyon (Ayný)
        Vector3 targetLocalPos = new Vector3(0, 0, currentStackZ);
        targetLocalPos += config.posOffset;

        // 3. Rotasyon (Ayný)
        Quaternion fixRot = Quaternion.Euler(config.rotOffset);
        Quaternion randomRot = Quaternion.identity; // Senin tercihin :)
        Quaternion targetLocalRot = randomRot * fixRot;

        // 4. Yerleþ (Artýk Apex'i de gönderiyoruz)
        PutOnBasketLocal(targetLocalPos, targetLocalRot, container, apexLocalPos);
    }

    // YENÝ DOPath MANTIÐI
    private void PutOnBasketLocal(Vector3 targetLocalPos, Quaternion targetLocalRot, Transform containerParent, Vector3 apexLocalPos)
    {
        isGettingPutOnBasket = true;
        PlayerManager.Instance.ResetPlayerGrab(this);
        ChangeLayer(onTrayLayer);

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        // KRÝTÝK NOKTA: "true" parametresi.
        // Bu, objeyi parent yaparken "Dünya pozisyonunu koru" demek.
        // Böylece patates olduðu yerde kalýr, ýþýnlanmaz. Sonra biz onu tween ile götürürüz.
        transform.SetParent(containerParent, true);

        Sequence seq = DOTween.Sequence();

        // --- PATH OLUÞTURMA ---
        // Yolumuz: Þu anki yerim -> Apex (Tepe) -> Hedef (Yýðýn)
        // Not: DOPath zaten "þu anki yerimden baþla" mantýðýyla çalýþýr, o yüzden baþlangýcý vermiyoruz.
        // Sadece gidilecek duraklarý veriyoruz.
        Vector3[] pathPoints = new Vector3[] { apexLocalPos, targetLocalPos };

        // Hareket (CatmullRom ile yumuþak kavis)
        seq.Join(transform.DOLocalPath(pathPoints, data.putOnBasketDuration, PathType.CatmullRom)
            .SetEase(Ease.OutSine)); // OutSine ile yavaþça baþlar, hýzla yerine oturur

        // Dönüþ (Eþ zamanlý dönsün)
        seq.Join(transform.DOLocalRotateQuaternion(targetLocalRot, data.putOnBasketDuration)
            .SetEase(Ease.OutCubic));

        seq.OnComplete(() => {
            isAddedToBasket = true;
            isGettingPutOnBasket = false;
            // if (data.dropSound) SoundManager.Instance.PlaySoundFX...

            // YENÝ: Animasyon bitti, sepete haber ver layer'ýmý ayarlasýn
            if (currentBasket != null)
            {
                currentBasket.RefreshTopItemInteractability();
            }
        });
    }

    // Elde tutulurken çaðrýlacak (Eski haline dön)
    public void ResetToHandVisual()
    {
        if (data.handMesh == null) return;

        meshFilter.mesh = data.handMesh.mesh;

        if (boxCollider != null)
        {
            boxCollider.center = data.handMesh.colliderCenter;
            boxCollider.size = data.handMesh.colliderSize;
        }

        // Offsetleri sýfýrla (Elde tutma offsetleri IGrabable'dan geliyor zaten)
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    // PutOnBasket'i biraz sadeleþtirdik, artýk hesap kitap yapmýyor sadece gidiyor
    private void PutOnBasket(Vector3 targetPos, Quaternion targetRot, Transform containerParent)
    {
        isGettingPutOnBasket = true;
        PlayerManager.Instance.ResetPlayerGrab(this);
        ChangeLayer(onTrayLayer); // Layer deðiþimi

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        transform.parent = containerParent;

        Vector3 targetLocalPos = containerParent.InverseTransformPoint(targetPos);

        Sequence seq = DOTween.Sequence();
        seq.Join(transform.DOLocalMove(targetLocalPos, 0.2f).SetEase(Ease.OutQuad));
        seq.Join(transform.DORotateQuaternion(targetRot, 0.2f).SetEase(Ease.OutCubic));

        seq.OnComplete(() => {
            isAddedToBasket = true;
            isGettingPutOnBasket = false;
            //SoundManager.Instance.PlaySoundFX(data.dropSound, transform, 1f, 0.9f, 1.1f);
        });
    }

    private void TransformToBurgerIngredient()
    {
        if (burgerIngredientScript == null) return;

        // 1. Tag deðiþtir
        gameObject.tag = "BurgerIngredient";

        // 2. Yeni scripti hazýrla
        burgerIngredientScript.enabled = true;
        burgerIngredientScript.IsGrabbed = true; // State senkronizasyonu

        // Þuna dikkat: BurgerIngredient.Awake() veya OnEnable() içinde IsGrabbed = false yapýyor olabilir.
        // O yüzden enabled yaptýktan hemen sonra IsGrabbed = true dediðimizden emin oluyoruz.

        // 3. Controller'a "Artýk muhattabýn bu yeni script" de
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.ForceUpdatePlayerGrab(burgerIngredientScript);
        }

        Destroy(this);
    }

    // --- IGRABABLE IMPLEMENTATION ---

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(grabbedLayer);

        ResetToHandVisual();

        // Eðer bir sepetten alýyorsak, sepetten kaydýný sil
        if (currentBasket != null)
        {
            currentBasket.RemoveItem(this); // Sepet koduna bunu ekleyeceðiz
            currentBasket = null;
            isAddedToBasket = false;
            isGettingPutOnBasket = false;
        }

        boxCollider.enabled = false;
        rb.isKinematic = false; // Ele alýnca fizik açýlýr (ama gravity kapalý)
        rb.useGravity = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);

        //SoundManager.Instance.PlaySoundFX(data.dropSound, transform, 1f, 1f, 1.2f); // Tutma sesi (Drop sound kullandým geçici)

        transform.localScale = data.localScaleWhenGrabbed;

        // --- DÖNÜÞÜM BLOÐU (Hybrid Item) ---
        // Eðer bu bir Çýtýr Tavuksa VE Piþmiþse
        if (data.type == Holder.HolderIngredient.CrispyChicken &&
            CurrentCookingState == Cookable.CookAmount.REGULAR)
        {
            TransformToBurgerIngredient();
        }
    }

    public void OnThrow(Vector3 direction, float force)
    {
        Release(direction, force);
    }

    public void OnDrop(Vector3 direction, float force)
    {
        Release(direction, force);
    }

    private void Release(Vector3 direction, float force)
    {
        boxCollider.enabled = true;
        IsGrabbed = false;
        transform.SetParent(null);
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.AddForce(direction * force, ForceMode.Impulse);

        ChangeLayer(ungrabableLayer); // Havada tutulamasýn
    }

    public void OnFocus()
    {
        if (isGettingPutOnBasket) return;

        // --- YENÝ KONTROL ---
        // Eðer sepetteysek, sadece "En Tepedeki" isek parlayabiliriz.
        if (isAddedToBasket && currentBasket != null)
        {
            // Tepede deðilsem, focus olmayý reddet ve OnTray (Kilitli) kal.
            if (!currentBasket.IsTopItem(this)) return;
        }
        // --------------------

        ChangeLayer(grabableOutlinedLayer);
    }

    public void OnLoseFocus()
    {
        if (isGettingPutOnBasket) return;

        // --- YENÝ KONTROL ---
        // Sepetteysek, kafamýza göre "Grabable" olamayýz.
        // Durumumuza uygun layer'a dönmemiz lazým.
        if (isAddedToBasket && currentBasket != null)
        {
            if (currentBasket.IsTopItem(this))
            {
                // Tepedeysek -> Alýnabilir hale dön
                ChangeLayer(grabableLayer);
            }
            else
            {
                // Altta kaldýysak -> Kilitli hale dön
                ChangeLayer(onTrayLayer);
            }
        }
        else
        {
            // Sepette deðilsek (Masadaysa vs.) normal davran
            ChangeLayer(grabableLayer);
        }
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
    }

    public void OutlineChangeCheck()
    {
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

    public float GetHeightForStackIndex(int stackIndex)
    {
        if (data.basketMeshes == null || data.basketMeshes.Length == 0) return 0.05f;

        // Index sýnýrýný koru (Eðer 5. kat gelirse ve mesh yoksa sonuncuyu kullan)
        int index = Mathf.Clamp(stackIndex, 0, data.basketMeshes.Length - 1);

        return data.basketMeshes[index].height;
    }

    // --- COLLISION & SOUNDS ---

    private void OnCollisionEnter(Collision collision)
    {
        // Eðer grab edildiyse veya sepete yerleþiyorsa ses çalma
        if (IsGrabbed || isGettingPutOnBasket || collision.gameObject.CompareTag("Player")) return;

        // Yere düþtüðünde layer'ý düzelt
        if (gameObject.layer == ungrabableLayer)
        {
            ChangeLayer(grabableLayer);
        }

        HandleSoundFX(collision);
    }

    private void HandleSoundFX(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        // Basit bir ses kontrolü (Veri eksikse hata vermesin diye null check)
        //if (impactForce > 0.5f && Time.time - lastSoundTime > 0.2f && data.dropSound != null)
        //{
            //SoundManager.Instance.PlaySoundFX(data.dropSound, transform, 0.5f, 0.8f, 1.2f, false);
            //lastSoundTime = Time.time;
        //}
    }

    // Gereksiz Interface Metodlarý
    public void OnHolster() { }
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

    private void OnDestroy()
    {
        // 1. Eðer bir sepetteysem, ölmeden önce vasiyetimi býrakýp çýkýyorum
        if (currentBasket != null)
        {
            // Sepete "Ben gidiyorum, listeni güncelle ve alttakinin kilidini aç" diyoruz.
            // RemoveItem içinde "Contains" kontrolü olduðu için güvenli.
            currentBasket.RemoveItem(this);
        }

        // 2. Eðer oyuncunun elindeysem veya hedefindeysem resetle
        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.ResetPlayerGrab(this);
        }
    }
}