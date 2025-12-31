using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BurgerBox : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public Sprite Icon { get => data.icon[boxSituation]; set => data.icon[boxSituation] = value; }

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabTypes[boxSituation]; set => data.handGrabTypes[boxSituation] = value; }
    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }

    public Transform LeftHandPoint { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset[boxSituation]; set => data.grabPositionOffset[boxSituation] = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset[boxSituation]; set => data.grabRotationOffset[boxSituation] = value; }

    public Vector3 GrabLocalPositionOffset { get => data.grabLocalPositionOffset[boxSituation]; set => data.grabLocalPositionOffset[boxSituation] = value; }
    public Vector3 GrabLocalRotationOffset { get => data.grabLocalRotationOffset[boxSituation]; set => data.grabLocalRotationOffset[boxSituation] = value; }
    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    private bool isGettingPutOnTray;

    public BurgerBoxData data;

    [HideInInspector] public bool canAddToTray;

    [SerializeField] private Tray tray;
    public string FocusTextKey { get => data.focusTextKeys[burgerNo]; set => data.focusTextKeys[burgerNo] = value; }
    [HideInInspector] public int burgerNo = 0; //0 for "Burger Box" text, rest is for menu names in order
    private int boxSituation = 0; //0 for open, 1 for close
    [Space]

    public List<BurgerIngredientData.IngredientType> allBurgerIngredientTypes = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> allSauces = new List<SauceBottle.SauceType>();

    public GameObject topPart;

    [Header("Closing Animation Settings")]
    [SerializeField] private float lidCloseDuration = 0.2f; // Kapak kapanma süresi

    [Header("Closed Collider Settings")]
    [SerializeField] private Vector3 closedColliderCenter; // Kapalýykenki Center
    [SerializeField] private Vector3 closedColliderSize;   // Kapalýykenki Size

    private Rigidbody rb;
    private BoxCollider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;
    private int onTrayLayer;

    [HideInInspector] public bool isJustThrowed;
    [HideInInspector] public bool isJustDropped;
    [HideInInspector] public bool CanBeReceived;

    [HideInInspector] public GameManager.BurgerTypes burgerType;

    private float lastSoundTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<BoxCollider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");
        onTrayLayer = LayerMask.NameToLayer("OnTray");

        IsGrabbed = false;
        isGettingPutOnTray = false;

        isJustThrowed = false;
        isJustDropped = false;
        CanBeReceived = true;

        canAddToTray = false;

        if (topPart != null)
        {
            topPart.transform.localRotation = Quaternion.Euler(180f, 0f, 0f);
        }
    }

    public void PutOnTray(Vector3 trayPos)
    {
        canAddToTray = false;
        isGettingPutOnTray = true;
        ChangeLayer(onTrayLayer);

        isJustDropped = false;
        isJustThrowed = false;

        rb.velocity = Vector3.zero;
        rb.isKinematic = true;

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.DOMove(trayPos, data.timeToPutOnTray).SetEase(Ease.OutQuad));
        seq.Join(transform.DORotateQuaternion(Quaternion.Euler(data.trayRotation), data.timeToPutOnTray).SetEase(Ease.OutCubic));

        seq.OnComplete(() =>
        {
            SoundManager.Instance.PlaySoundFX(data.audioClips[3], transform, data.closeSoundVolume, data.closeSoundMinPitch, data.closeSoundMaxPitch);

            tray.PrepareForSquash();

            // --- COLLIDER BAÞLANGIÇ DEÐERLERÝNÝ KAYDET ---
            // Animasyon baþlamadan önceki son halini (Açýk halini) alýyoruz.
            Vector3 startColSize = Vector3.one;
            Vector3 startColCenter = Vector3.zero;

            if (col != null)
            {
                startColSize = col.size;
                startColCenter = col.center;
            }
            // ---------------------------------------------

            // Burger ne zaman ezilmeye baþlasýn? (0.5 = Yolun yarýsý/90 Derece)
            float squashStartT = 0.5f;

            // --- MASTER TWEEN ---
            DOVirtual.Float(0f, 1f, lidCloseDuration, (t) =>
            {
                // A) KAPAK ROTASYONU (Her zaman döner)
                // 180'den 0'a iner. t > 1 olunca negatife düþer (Juice).
                float currentAngle = Mathf.LerpUnclamped(180f, 0f, t);
                topPart.transform.localRotation = Quaternion.Euler(currentAngle, 0f, 0f);

                // B) BURGER SQUASH (Sadece temas sonrasý)
                float burgerProgress = 0f;
                if (t >= squashStartT)
                {
                    // 0.5 ile 1.0 arasýný -> 0.0 ile 1.0 arasýna çevir (Remap)
                    burgerProgress = (t - squashStartT) / (1f - squashStartT);
                }
                tray.UpdateSquash(burgerProgress);

                // C) COLLIDER GÜNCELLEME (Her zaman deðiþir)
                // Kutu kapanýrken collider da sürekli küçülmeli ki fiziksel olarak otursun.
                // OutBack sayesinde kapanýnca hafifçe daha da küçülüp (sýkýþýp) geri yerine oturur.
                if (col != null)
                {
                    col.size = Vector3.LerpUnclamped(startColSize, closedColliderSize, t);
                    col.center = Vector3.LerpUnclamped(startColCenter, closedColliderCenter, t);
                }

            }).SetEase(Ease.OutQuad)
              .OnComplete(FinishPutOnTray);
        });
    }

    public void OnHolster()
    {
    }

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(grabbedLayer);
        isGettingPutOnTray = false;

        CanBeReceived = true;

        tray.currentBox = this;

        col.enabled = false;

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);

        rb.isKinematic = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;

        transform.localPosition = GrabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(GrabLocalRotationOffset);
    }
    public void OnFocus()
    {
        if (!isJustDropped && !isJustThrowed)
            ChangeLayer(grabableOutlinedLayer);
    }
    public void OnLoseFocus()
    {
        if (!isJustDropped && !isJustThrowed)
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

    private void FinishPutOnTray() //Gets called in oncomplete seq
    {
        tray.ResetTray();

        GameManager.Instance.CheckBurgerType(allBurgerIngredientTypes, allSauces, this);

        gameObject.tag = "BurgerBoxClosed";

        boxSituation = 1;
        
        ChangeLayer(grabableLayer);
    }

    public void SetBurgerType(GameManager.BurgerTypes type)
    {
        burgerNo = (int) type + 1;
        burgerType = type;

        PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
        topPart.layer = layer;
    }

    private void OnDisable()
    {
        PlayerManager.Instance.ResetPlayerGrab(this);
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
        if (impactForce >= data.throwThreshold)
        {
            // === FIRLATMA SESÝ (Hýzlý) ===
            SoundManager.Instance.PlaySoundFX(
                data.audioClips[2],
                transform,
                data.throwSoundVolume,
                data.throwSoundMinPitch,
                data.throwSoundMaxPitch
            );
        }
        else
        {
            // === DÜÞME SESÝ (Yavaþ/Orta) ===
            SoundManager.Instance.PlaySoundFX(
                data.audioClips[1],
                transform,
                data.dropSoundVolume,
                data.dropSoundMinPitch,
                data.dropSoundMaxPitch
            );
        }

        // Ses çaldýk, zamaný kaydet
        lastSoundTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !isGettingPutOnTray && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed)
            {
                ChangeLayer(grabableLayer);
                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                ChangeLayer(grabableLayer);
                isJustDropped = false;
            }

            HandleSoundFX(collision);

        }
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
}
