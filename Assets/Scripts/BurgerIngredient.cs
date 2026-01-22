using DG.Tweening;
using UnityEngine;

public class BurgerIngredient : MonoBehaviour, IGrabable
{
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

    public Transform LeftHandPoint { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    private bool isGettingPutOnTray;
    private bool isAddedToBurger;

    public BurgerIngredientData data;

    private float targetCookTime;
    private float targetBurnTime;

    [SerializeField] private BurgerCombineArea burgerCombineArea;
    public string FocusTextKey { get => data.focusTextKeys[(int)cookAmount]; set => data.focusTextKeys[(int)cookAmount] = value; }
    [Space]

    [HideInInspector] public bool canAddToTray;

    [Header("Cooking State (Auto-Managed)")]
    public float currentCookTime = 0f;
    public CookAmount cookAmount = CookAmount.RAW;

    // Hangi ýzgaradayým? (Grill.cs tarafýndan atanýr)
    [HideInInspector] public Grill currentGrill;
    [HideInInspector] public ParticleSystem attachedSmoke;

    private Rigidbody rb;
    private Collider col;
    private MeshCollider meshCol;
    private MeshRenderer meshRenderer;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;
    private int onTrayLayer;

    private bool isJustThrowed;
    private bool isJustDropped;
    private bool isStuck;
    public bool canStick;

    private float lastSoundTime = 0f;
    private bool isStuckAndCantPlayAudioUntilPickedAgain;

    private Quaternion collisionRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        meshCol = GetComponent<MeshCollider>();
        meshRenderer = GetComponent<MeshRenderer>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");
        onTrayLayer = LayerMask.NameToLayer("OnTray");

        IsGrabbed = false;
        isGettingPutOnTray = false;
        isAddedToBurger = false;

        isJustThrowed = false;
        isStuck = false;

        canAddToTray = false;

        // 1. Rastgele çarpan (Örn: 0.85 ile 1.15 arasý)
        // Ayný Fryable'daki mantýk: "Ýnce kesilen hem çabuk piþer hem çabuk yanar"
        float varianceMultiplier = Random.Range(1f - data.cookingVariance, 1f + data.cookingVariance);

        // 2. Data'daki base deðeri çarpýp yerel deðiþkene alýyoruz
        targetCookTime = data.timeToCook * varianceMultiplier;
        targetBurnTime = data.timeToBurn * varianceMultiplier;

        if (data.isSauce)
        {
            isAddedToBurger = true;
            isGettingPutOnTray = false;
            isJustDropped = false;
            isJustThrowed = false;

            meshCol.convex = false;

            SetOnGrabableLayer();
        }

        UpdateVisuals();
    }

    private void Start()
    {
        //SIRF INSPECTORDA SCRIPT DISABLE EDÝLEBÝLSÝN DÝYE KOYDUM
    }

    public bool ProcessCooking(float deltaTime)
    {
        if (!data.isCookable) return false;
        if (cookAmount == CookAmount.BURNT) return false;

        currentCookTime += deltaTime;

        CookAmount oldState = cookAmount;
        CookAmount newState = cookAmount;

        // --- DEÐÝÞÝKLÝK BURADA ---
        // Artýk data.timeToBurn yerine targetBurnTime kullanýyoruz
        if (currentCookTime >= targetBurnTime)
        {
            newState = CookAmount.BURNT;
        }
        // Artýk data.timeToCook yerine targetCookTime kullanýyoruz
        else if (currentCookTime >= targetCookTime)
        {
            newState = CookAmount.REGULAR;
        }
        else
        {
            newState = CookAmount.RAW;
        }
        // -------------------------

        if (newState != oldState)
        {
            ChangeCookAmount((int)newState);
        }

        return cookAmount != CookAmount.BURNT;
    }

    private void UpdateVisuals()
    {
        if (meshRenderer == null || data == null) return;

        if (!data.isCookable) return;

        switch (cookAmount)
        {
            case CookAmount.RAW:
                meshRenderer.material = data.rawMat;
                break;
            case CookAmount.REGULAR:
                meshRenderer.material = data.cookedMat;
                break;
            case CookAmount.BURNT:
                meshRenderer.material = data.burntMat;
                break;
        }
    }

    public void SetOnTrayLayer()
    {
        isGettingPutOnTray = true;
        PlayerManager.Instance.ResetPlayerGrab(this);
        ChangeLayer(onTrayLayer);
    }

    public void SetOnGrabableLayer()
    {
        isJustDropped = false;
        isJustThrowed = false;
        isGettingPutOnTray = false;
        ChangeLayer(grabableLayer);
    }

    public void PutOnTray(Vector3 trayPos, Quaternion trayRot, Transform parentTray, Vector3 apexWorldPos)
    {
        canAddToTray = false;    
        SetOnTrayLayer();
        ChangeLayer(onTrayLayer);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        transform.parent = parentTray;

        // --- YENÝ PATH MANTIÐI ---

        // 1. Hedef ve Apex noktalarýný parent'ýn yerel koordinatlarýna çevir
        Vector3 targetLocalPos = parentTray.InverseTransformPoint(trayPos);
        Vector3 apexLocalPos = parentTray.InverseTransformPoint(apexWorldPos);

        // 2. Yol Dizisi: [Apex, Hedef]
        // Not: DOTween path, "bulunduðum yerden baþla" mantýðýyla çalýþýr.
        Vector3[] pathPoints = new Vector3[] { apexLocalPos, targetLocalPos };

        // 3. Hareketi Baþlat (DOLocalPath)
        // PathType.CatmullRom yumuþak bir kavis saðlar.
        Sequence seq = DOTween.Sequence();

        seq.Join(transform.DOLocalPath(pathPoints, data.timeToPutOnTray, PathType.CatmullRom)
            .SetEase(Ease.OutSine)); // Yavaþça baþla, hýzla otur

        seq.Join(transform.DORotateQuaternion(trayRot, data.timeToPutOnTray)
            .SetEase(Ease.OutCubic));

        seq.OnComplete(() => {
            isAddedToBurger = true;
            isGettingPutOnTray = false;
            isJustDropped = false;
            isJustThrowed = false;

            if (data.isSauce) meshCol.convex = false;

            bool isLastItem = burgerCombineArea != null && burgerCombineArea.allBurgerIngredients.Count > 0 &&
                          burgerCombineArea.allBurgerIngredients[burgerCombineArea.allBurgerIngredients.Count - 1] == this;

            if (isLastItem && !burgerCombineArea.isBoxingProcessStarted)
                SetOnGrabableLayer();
            else
                SetOnTrayLayer();

            SoundManager.Instance.PlaySoundFX(data.audioClips[3], transform, data.traySoundVolume, data.traySoundMinPitch, data.traySoundMaxPitch);
        });
    }

    public void OnHolster()
    {
    }

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(grabbedLayer);

        if (isAddedToBurger)
        {
            if (data.isSauce)
            {
                burgerCombineArea.RemoveSauce();
                meshCol.convex = true;
            }   
            else
                burgerCombineArea.RemoveIngredient();

            isAddedToBurger = false;
        }

        if (attachedSmoke != null)
        {
            if (cookAmount == CookAmount.RAW)
            {
                attachedSmoke.Stop();
                attachedSmoke = null; // Artýk dumaným yok
            }
            else
            {
                //piþmiþ veya yanmýþsa tutuyoruz ve prewarm özelliðini açýyoruz ki artýk envantere koyup geri aldýðýmýzda dumanýný baþtan oluþturmasýn hep dumanlý olsun
                var main = attachedSmoke.main;
                main.loop = true;
                main.prewarm = true;
            }
        }

        // Eðer bir ýzgaradaysak, önce oradan çýkýþ yapalým
        if (currentGrill != null)
        {
            currentGrill.RemoveItem(this); // Grill'deki sesi ve slotu günceller
        }

        col.enabled = false;

        if (isStuck)
            Unstick();

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume,
                                          data.grabSoundMinPitch * (cookAmount == CookAmount.RAW ? 1f : cookAmount == CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier),
                                          data.grabSoundMaxPitch * (cookAmount == CookAmount.RAW ? 1f : cookAmount == CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier));

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = false;

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);

        transform.localScale = data.localScaleWhenGrabbed;
    }
    public void OnFocus()
    {
        if (!isJustDropped && !isJustThrowed && !isGettingPutOnTray)
            ChangeLayer(grabableOutlinedLayer);
    }
    public void OnLoseFocus()
    {
        if (!isJustDropped && !isJustThrowed && !isGettingPutOnTray)
            ChangeLayer(grabableLayer);
    }

    public void OnDrop(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustDropped = true;

        ChangeLayer(ungrabableLayer);
    }

    public void OnThrow(Vector3 direction, float force)
    {
        col.enabled = true;

        IsGrabbed = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustThrowed = true;

        ChangeLayer(ungrabableLayer);
    }

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

    private void StickToSurface(Collision collision)
    {
        Vector3 surfaceNormal = collision.contacts[0].normal;
        Vector3 bigSideDirection = transform.up;

        // Rotasyonu ayarla
        Quaternion targetRotation = Quaternion.FromToRotation(bigSideDirection, surfaceNormal) * transform.rotation;
        transform.rotation = targetRotation;

        // Contact noktasýný al
        Vector3 contactPoint = collision.contacts[0].point;

        // Collider yarýçaplarýný al
        Vector3 extents = GetComponent<Collider>().bounds.extents;

        // Normal yönüne en yakýn ekseni bul
        Vector3 localNormal = transform.InverseTransformDirection(surfaceNormal);
        Vector3 absLocalNormal = new Vector3(Mathf.Abs(localNormal.x), Mathf.Abs(localNormal.y), Mathf.Abs(localNormal.z));

        float offset = 0f;
        if (absLocalNormal.x > absLocalNormal.y && absLocalNormal.x > absLocalNormal.z)
            offset = extents.x;
        else if (absLocalNormal.y > absLocalNormal.x && absLocalNormal.y > absLocalNormal.z)
            offset = extents.y;
        else
            offset = extents.z;

        // Biraz daha az ekle (ör. %30'u)
        offset *= 0.15f;

        // Pozisyonu ayarla
        transform.position = contactPoint + surfaceNormal * offset;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        transform.SetParent(collision.transform);

        isStuck = true;
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

    private void HandleSauceDrops(Collision collision)
    {
        ContactPoint contact = collision.contacts[0];

        Vector3 normal = contact.normal;
        Vector3 hitPoint = contact.point + normal * 0.02f;

        Vector3 tangent = Vector3.Cross(normal, Vector3.up);
        if (tangent == Vector3.zero)
            tangent = Vector3.Cross(normal, Vector3.forward);
        tangent.Normalize();
        Vector3 bitangent = Vector3.Cross(normal, tangent);

        // Rastgele offset (yüzeye paralel düzlemde)
        float spreadRadius = 0.05f;

        Transform targetTransform = collision.transform.Find("DecalParent");

        if (targetTransform == null)
            targetTransform = collision.transform;

        for (int i = 0; i < data.sauceDropAmount; i++)
        {
            Vector3 randomOffset = tangent * Random.Range(-spreadRadius, spreadRadius) +
                               bitangent * Random.Range(-spreadRadius, spreadRadius);

            Vector3 spawnPoint = hitPoint + randomOffset;

            float randomValue = Random.Range(data.targetDropScale / data.randomMultiplier, data.targetDropScale * data.randomMultiplier);

            Vector3 finalScale = new Vector3(randomValue, randomValue, randomValue);

            SauceManager.Instance.SpawnDrop(data.sauceType, spawnPoint, collisionRotation, finalScale, collision.transform);
        }

        float impactForce = collision.relativeVelocity.magnitude;

        Destroy(gameObject);
    }

    private void Unstick()
    {
        transform.SetParent(null);

        isStuckAndCantPlayAudioUntilPickedAgain = false;

        rb.isKinematic = false;
        isStuck = false;
    }

    public void ChangeCookAmount(int value)
    {
        // Enum cast
        cookAmount = (CookAmount)value;

        // Focus text güncelleme (Mevcut kodun)
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);

        // Görsel güncelle
        UpdateVisuals();

        // Sticker (Yapýþma) mantýðý (Mevcut kodun)
        if (value == 0) canStick = true;
        else canStick = false;

        // --- YENÝ: IZGARAYA HABER VER (PARTÝKÜL RENGÝ ÝÇÝN) ---
        if (currentGrill != null)
        {
            currentGrill.OnItemStateChanged(this, cookAmount);
        }
    }

    private void OnDestroy()
    {
        PlayerManager.Instance.ResetPlayerGrab(this);
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
        if (impactForce >= data.throwThreshold || isStuck)
        {
            // === FIRLATMA SESÝ (Hýzlý) ===
            SoundManager.Instance.PlaySoundFX(
                data.audioClips[2],
                transform,
                data.throwSoundVolume,
                data.throwSoundMinPitch * (cookAmount == CookAmount.RAW ? 1f : cookAmount == CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier),
                data.throwSoundMaxPitch * (cookAmount == CookAmount.RAW ? 1f : cookAmount == CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier), false
            );

            if (data.throwParticles[(int)cookAmount] != null)
                Instantiate(data.throwParticles[(int)cookAmount], transform.position, collisionRotation);

            if (isStuck)
                isStuckAndCantPlayAudioUntilPickedAgain = true;
        }
        else
        {
            // === DÜÞME SESÝ (Yavaþ/Orta) ===
            SoundManager.Instance.PlaySoundFX(
                data.audioClips[1],
                transform,
                data.dropSoundVolume,
                data.dropSoundMinPitch * (cookAmount == CookAmount.RAW ? 1f : cookAmount == CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier),
                data.dropSoundMaxPitch * (cookAmount == CookAmount.RAW ? 1f : cookAmount == CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier), false
            );

            if (data.dropParticles[(int)cookAmount] != null)
                Instantiate(data.dropParticles[(int)cookAmount], transform.position, collisionRotation);
        }

        // Ses çaldýk, zamaný kaydet
        lastSoundTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !isGettingPutOnTray && !collision.gameObject.CompareTag("Player"))
        {
            if (data.isSauce)
            {
                if (!isAddedToBurger && !isGettingPutOnTray)
                {
                    CalculateCollisionRotation(collision);
                    HandleSauceDrops(collision);
                }
                
            }
            else if (isJustThrowed)
            {
                CalculateCollisionRotation(collision);

                if (canStick && ((1 << collision.gameObject.layer) & data.stickableLayers) != 0)
                    StickToSurface(collision);

                ChangeLayer(grabableLayer);

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                CalculateCollisionRotation(collision);

                ChangeLayer(grabableLayer);

                isJustDropped = false;
            }

            if (!isStuckAndCantPlayAudioUntilPickedAgain && !isGettingPutOnTray && !isAddedToBurger)
                HandleSoundFX(collision);

        }

        
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
    }

    public void OnUseHold()
    {
    }

    public void OnUseRelease()
    {
    }
    public bool TryCombine(IGrabable otherItem)
    {
        return false;
    }

    public bool CanCombine(IGrabable otherItem)
    {
        return false;
    }

    public void PlayPutOnSoundEffect()
    {
        SoundManager.Instance.PlaySoundFX(data.audioClips[3], transform, data.traySoundVolume, data.traySoundMinPitch, data.traySoundMaxPitch);
    }
}
