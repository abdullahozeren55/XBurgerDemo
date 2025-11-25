using DG.Tweening;
using UnityEngine;

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

    public BurgerIngredientData data;

    [SerializeField] private Tray tray;
    public string FocusTextKey { get => data.focusTextKeys[(int)cookAmount]; set => data.focusTextKeys[(int)cookAmount] = value; }
    [Space]

    [HideInInspector] public bool canAddToTray;

    private Rigidbody rb;
    private Collider col;

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

    private Transform decalParent;

    private Quaternion collisionRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        cookable = GetComponent<Cookable>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        onTrayLayer = LayerMask.NameToLayer("OnTray");

        decalParent = transform.Find("DecalParent");

        IsGrabbed = false;
        isGettingPutOnTray = false;

        isJustThrowed = false;
        isStuck = false;

        canAddToTray = false;
    }

    public void PutOnTray(Vector3 trayPos, Quaternion trayRot, Transform parentTray)
    {
        canAddToTray = false;
        isGettingPutOnTray = true;
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
            
            col.enabled = false;

        });
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

        tray.currentIngredient = this;
        tray.TurnOnHologram(data.ingredientType);

        col.enabled = false;

        if (cookable != null)
            cookable.StopCooking();

        if (isStuck)
            Unstick();

        if (!data.IsCookable)
        {
            SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);
        }
        else
        {
            if (cookAmount == Cookable.CookAmount.RAW)
            {
                SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);
            }
            else if (cookAmount == Cookable.CookAmount.REGULAR)
            {
                SoundManager.Instance.PlaySoundFX(data.audioClips[3], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);
            }
            else if (cookAmount == Cookable.CookAmount.BURNT)
            {
                SoundManager.Instance.PlaySoundFX(data.audioClips[6], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);
            }
        }


        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);
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
        int countToDrop = Mathf.CeilToInt(decalParent.childCount / 2f);

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

        for (int i = 0; i < countToDrop; i++)
        {
            Transform child = decalParent.GetChild(i);
            child.transform.parent = targetTransform;

            Vector3 randomOffset = tangent * Random.Range(-spreadRadius, spreadRadius) +
                               bitangent * Random.Range(-spreadRadius, spreadRadius);

            Vector3 spawnPoint = hitPoint + randomOffset;

            child.transform.position = spawnPoint;
            child.transform.rotation = collisionRotation;
        }

        if (decalParent.childCount > 0)
        {
            for (int i = 0; i < decalParent.childCount; i++)
            {
                Destroy(decalParent.GetChild(i).gameObject);
            }
        }
    }

    private void Unstick()
    {
        transform.SetParent(null);

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

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !isGettingPutOnTray && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed)
            {
                CalculateCollisionRotation(collision);

                if (decalParent != null && decalParent.childCount > 0)
                    HandleSauceDrops(collision);

                if (canStick)
                    StickToSurface(collision);

                if (!data.IsCookable)
                {
                    SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, data.throwSoundVolume, data.throwSoundMinPitch, data.throwSoundMaxPitch);
                }
                else
                {
                    if (cookAmount == Cookable.CookAmount.RAW)
                    {
                        SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, data.throwSoundVolume, data.throwSoundMinPitch, data.throwSoundMaxPitch);
                    }
                    else if (cookAmount == Cookable.CookAmount.REGULAR)
                    {
                        SoundManager.Instance.PlaySoundFX(data.audioClips[5], transform, data.throwSoundVolume, data.throwSoundMinPitch, data.throwSoundMaxPitch);
                    }
                    else if (cookAmount == Cookable.CookAmount.BURNT)
                    {
                        SoundManager.Instance.PlaySoundFX(data.audioClips[8], transform, data.throwSoundVolume, data.throwSoundMinPitch, data.throwSoundMaxPitch);
                    }
                }

                gameObject.layer = grabableLayer;

                if (data.throwParticles[(int)cookAmount] != null)
                    Instantiate(data.throwParticles[(int)cookAmount], transform.position, collisionRotation);

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                CalculateCollisionRotation(collision);

                if (decalParent != null && decalParent.childCount > 0)
                    HandleSauceDrops(collision);

                gameObject.layer = grabableLayer;
                
                SoundManager.Instance.PlaySoundFX(data.audioClips[1], transform, data.dropSoundVolume, data.dropSoundMinPitch, data.dropSoundMaxPitch);

                if (data.dropParticles[(int)cookAmount] != null)
                    Instantiate(data.dropParticles[(int)cookAmount], transform.position, collisionRotation);

                isJustDropped = false;
            }

        }

        
    }

    public void OnUseHold()
    {
    }

    public void OnUseRelease()
    {
    }
}
