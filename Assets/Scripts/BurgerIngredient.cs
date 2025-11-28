using DG.Tweening;
using UnityEngine;
using static SauceBottle;

public class BurgerIngredient : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    private bool isGettingPutOnTray;
    private bool isAddedToBurger;

    public BurgerIngredientData data;

    [SerializeField] private Tray tray;
    public string FocusTextKey { get => data.focusTextKeys[(int)cookAmount]; set => data.focusTextKeys[(int)cookAmount] = value; }
    [Space]

    [HideInInspector] public bool canAddToTray;

    private Rigidbody rb;
    private Collider col;
    private MeshCollider meshCol;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int onTrayLayer;

    private bool isJustThrowed;
    private bool isJustDropped;
    private bool isStuck;
    public bool canStick;

    private Cookable cookable;
    public Cookable.CookAmount cookAmount;

    private float lastSoundTime = 0f;
    private bool isStuckAndCantPlayAudioUntilPickedAgain;

    private Quaternion collisionRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        meshCol = GetComponent<MeshCollider>();
        cookable = GetComponent<Cookable>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        onTrayLayer = LayerMask.NameToLayer("OnTray");

        IsGrabbed = false;
        isGettingPutOnTray = false;
        isAddedToBurger = false;

        isJustThrowed = false;
        isStuck = false;

        canAddToTray = false;

        if (data.isSauce)
        {
            isAddedToBurger = true;
            isGettingPutOnTray = false;
            isJustDropped = false;
            isJustThrowed = false;

            meshCol.convex = false;

            SetOnGrabableLayer();
        }
    }

    public void SetOnTrayLayer()
    {
        isGettingPutOnTray = true;
        PlayerManager.Instance.ResetPlayerGrab(this);
        gameObject.layer = onTrayLayer;
    }

    public void SetOnGrabableLayer()
    {
        isGettingPutOnTray = false;
        gameObject.layer = grabableLayer;
    }

    public void PutOnTray(Vector3 trayPos, Quaternion trayRot, Transform parentTray)
    {
        canAddToTray = false;
        
        SetOnTrayLayer();

        gameObject.layer = onTrayLayer;

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        transform.parent = parentTray;

        var moveTween = transform.DOMove(trayPos, data.timeToPutOnTray).SetEase(Ease.OutQuad);
        var rotateTween = transform.DORotateQuaternion(trayRot, data.timeToPutOnTray).SetEase(Ease.OutCubic);

        Sequence seq = DOTween.Sequence();
        seq.Join(moveTween);
        seq.Join(rotateTween);

        seq.OnComplete(() => {

            isAddedToBurger = true;
            isGettingPutOnTray = false;
            isJustDropped = false;
            isJustThrowed = false;

            if (data.isSauce)
                meshCol.convex = false;

            SetOnGrabableLayer();

            SoundManager.Instance.PlaySoundFX(data.audioClips[3], transform, data.traySoundVolume, data.traySoundMinPitch, data.traySoundMaxPitch);

        });
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

        if (isAddedToBurger)
        {
            if (data.isSauce)
            {
                tray.RemoveSauce();
                meshCol.convex = true;
            }   
            else
                tray.RemoveIngredient();

            isAddedToBurger = false;
        }

        tray.currentIngredient = this;

        if (data.isSauce)
            tray.TurnOnSauceHologram(data.sauceType);
        else
            tray.TurnOnHologram(data.ingredientType);

        col.enabled = false;

        if (cookable != null)
            cookable.StopCooking();

        if (isStuck)
            Unstick();

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume,
                                          data.grabSoundMinPitch * (cookAmount == Cookable.CookAmount.RAW ? 1f : cookAmount == Cookable.CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier),
                                          data.grabSoundMaxPitch * (cookAmount == Cookable.CookAmount.RAW ? 1f : cookAmount == Cookable.CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier));

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
        if (!isJustDropped && !isJustThrowed)
            gameObject.layer = grabableOutlinedLayer;
    }
    public void OnLoseFocus()
    {
        if (!isJustDropped && !isJustThrowed)
            gameObject.layer = grabableLayer;
    }

    public void OnDrop(Vector3 direction, float force)
    {
        col.enabled = true;

        tray.TurnOffAllHolograms();

        IsGrabbed = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustDropped = true;
    }

    public void OnThrow(Vector3 direction, float force)
    {
        col.enabled = true;

        tray.TurnOffAllHolograms();

        IsGrabbed = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustThrowed = true;
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == grabableOutlinedLayer && OutlineShouldBeRed)
        {
            gameObject.layer = interactableOutlinedRedLayer;
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            gameObject.layer = grabableOutlinedLayer;
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

        if (value == 0)
        {
            cookAmount = Cookable.CookAmount.RAW;
            canStick = true;

            
        }
        else if (value == 1)
        {
            cookAmount = Cookable.CookAmount.REGULAR;
            canStick = false;
        }
        else
        {
            cookAmount = Cookable.CookAmount.BURNT;
            canStick = false;
        }

        PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);
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
                data.throwSoundMinPitch * (cookAmount == Cookable.CookAmount.RAW ? 1f : cookAmount == Cookable.CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier),
                data.throwSoundMaxPitch * (cookAmount == Cookable.CookAmount.RAW ? 1f : cookAmount == Cookable.CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier), false
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
                data.dropSoundMinPitch * (cookAmount == Cookable.CookAmount.RAW ? 1f : cookAmount == Cookable.CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier),
                data.dropSoundMaxPitch * (cookAmount == Cookable.CookAmount.RAW ? 1f : cookAmount == Cookable.CookAmount.REGULAR ? data.cookedSoundMultiplier : data.burntSoundMultiplier), false
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

                gameObject.layer = grabableLayer;

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                CalculateCollisionRotation(collision);

                gameObject.layer = grabableLayer;

                isJustDropped = false;
            }

            if (!isStuckAndCantPlayAudioUntilPickedAgain && !isGettingPutOnTray && !isAddedToBurger)
                HandleSoundFX(collision);

        }

        
    }

    public void OnUseHold()
    {
    }

    public void OnUseRelease()
    {
    }
}
