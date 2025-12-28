using DG.Tweening;
using UnityEngine;
using static FryableData;

public class Fryable : MonoBehaviour, IGrabable
{
    [Header("Data")]
    public FryableData data;

    [Header("Cooking State")]
    [SerializeField] private float currentCookingTime = 0f;
    [SerializeField] private Cookable.CookAmount state = Cookable.CookAmount.RAW;

    // --- IGrabable Properties ---
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public Sprite Icon { get => data.icon; set => data.icon = value; }
    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;

    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }
    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }
    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }
    public string FocusTextKey { get => data.focusTextKeys[(int)state]; set => data.focusTextKeys[(int)state] = value; }

    // --- Logic Variables ---
    private bool isGettingPutOnBasket; // Yerleþme animasyonu kilidi
    private bool isAddedToBasket;      // Þu an sepetin içinde mi?

    // References
    private Rigidbody rb;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private MeshCollider meshCollider;

    // Cache Layers
    private int grabableLayer;
    private int grabableOutlinedLayer;
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
        meshCollider = GetComponent<MeshCollider>();

        // Layer ID'lerini al
        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");
        onTrayLayer = LayerMask.NameToLayer("OnTray"); // Þimdilik OnTray kullanýyoruz, karýþýklýk olmasýn

        UpdateVisuals();
    }

    // --- COOKING LOGIC (Mevcut Kodun) ---
    public void Cook(float heatAmount)
    {
        if (state == Cookable.CookAmount.BURNT) return;
        currentCookingTime += heatAmount;
        CheckState();
    }

    private void CheckState()
    {
        Cookable.CookAmount oldState = state;

        if (currentCookingTime >= data.timeToBurn) state = Cookable.CookAmount.BURNT;
        else if (currentCookingTime >= data.timeToCook) state = Cookable.CookAmount.REGULAR;
        else state = Cookable.CookAmount.RAW;

        if (state != oldState)
        {
            UpdateVisuals();
            // Ýleride buraya ses/efekt eklenebilir
        }
    }

    private void UpdateVisuals()
    {
        if (meshRenderer == null || data == null) return;
        switch (state)
        {
            case Cookable.CookAmount.RAW: meshRenderer.material = data.rawMat; break;
            case Cookable.CookAmount.REGULAR: meshRenderer.material = data.cookedMat; break;
            case Cookable.CookAmount.BURNT: meshRenderer.material = data.burntMat; break;
        }
    }

    // --- BASKET LOGIC (Sepete Yerleþme) ---

    public void SetVisualState(int stackLevel, Transform container, float currentStackZ)
    {
        if (data.basketMeshes == null || data.basketMeshes.Length == 0) return;

        // 1. Config'i çek
        int index = Mathf.Clamp(stackLevel, 0, data.basketMeshes.Length - 1);
        FryableData.MeshConfig config = data.basketMeshes[index];

        // 2. Mesh'i güncelle
        if (config.mesh != null)
        {
            meshFilter.mesh = config.mesh;
            if (meshCollider != null)
            {
                meshCollider.sharedMesh = config.mesh;
                meshCollider.convex = true;
            }
        }

        // 3. TARGET LOCAL POZÝSYONU HESAPLA (Dünya koordinatýna hiç girmiyoruz)
        // Sepetin tabanýna (0,0) göre ne kadar yukarýda (Z) ve offseti ne?
        Vector3 targetLocalPos = new Vector3(0, 0, currentStackZ);
        targetLocalPos += config.posOffset;

        // 4. TARGET LOCAL ROTASYONU HESAPLA
        // Container'ýn dönüþü bizi ilgilendirmiyor çünkü onun çocuðu (Child) olacaðýz.
        // Sadece kendi içimizdeki düzeltme (Offset) ve varyasyon (Random) önemli.

        // Önce Mesh düzeltmesi (fix), sonra rastgele döndürme (random)
        Quaternion fixRot = Quaternion.Euler(config.rotOffset);
        Quaternion randomRot = Quaternion.identity;

        // Çarpma sýrasý önemli: Önce yamukluðu düzelt, sonra çevir.
        Quaternion targetLocalRot = randomRot * fixRot;

        // 5. Yerleþ (Direkt local deðerleri gönderiyoruz)
        PutOnBasketLocal(targetLocalPos, targetLocalRot, container);
    }

    // YENÝ FONKSÝYON: Sadece Local çalýþýr
    private void PutOnBasketLocal(Vector3 localPos, Quaternion localRot, Transform containerParent)
    {
        isGettingPutOnBasket = true;
        PlayerManager.Instance.ResetPlayerGrab(this);
        ChangeLayer(onTrayLayer);

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        // Önce parent yap, böylece Local koordinatlar anlam kazanýr
        transform.parent = containerParent;

        Sequence seq = DOTween.Sequence();

        // TransformPoint / InverseTransformPoint YOK! Direkt adrese teslim.
        seq.Join(transform.DOLocalMove(localPos, 0.2f).SetEase(Ease.OutQuad));
        seq.Join(transform.DOLocalRotateQuaternion(localRot, 0.2f).SetEase(Ease.OutCubic));

        seq.OnComplete(() => {
            isAddedToBasket = true;
            isGettingPutOnBasket = false;
            //if (data.dropSound) SoundManager.Instance.PlaySoundFX(data.dropSound, transform, 1f, 0.9f, 1.1f);
        });
    }

    // Elde tutulurken çaðrýlacak (Eski haline dön)
    public void ResetToHandVisual()
    {
        if (data.handMesh == null) return;

        meshFilter.mesh = data.handMesh.mesh;
        if (meshCollider != null) meshCollider.sharedMesh = data.handMesh.mesh;

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

    // --- IGRABABLE IMPLEMENTATION ---

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(grabbedLayer);

        ResetToHandVisual();

        // Eðer bir sepetten alýyorsak, sepetten kaydýný sil
        if (currentBasket != null && isAddedToBasket)
        {
            //currentBasket.RemoveItem(this); // Sepet koduna bunu ekleyeceðiz
            currentBasket = null;
            isAddedToBasket = false;
        }

        meshCollider.enabled = false;
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
        meshCollider.enabled = true;
        IsGrabbed = false;
        transform.SetParent(null);
        rb.useGravity = true;
        rb.isKinematic = false;
        rb.AddForce(direction * force, ForceMode.Impulse);

        ChangeLayer(ungrabableLayer); // Havada tutulamasýn
    }

    public void OnFocus()
    {
        if (!isGettingPutOnBasket)
            ChangeLayer(grabableOutlinedLayer);
    }

    public void OnLoseFocus()
    {
        if (!isGettingPutOnBasket)
            ChangeLayer(grabableLayer);
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == grabableOutlinedLayer && OutlineShouldBeRed)
            ChangeLayer(interactableOutlinedRedLayer);
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
            ChangeLayer(grabableOutlinedLayer);
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
}