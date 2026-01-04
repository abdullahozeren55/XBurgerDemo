using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DrinkCup : MonoBehaviour, IGrabable
{
    // ... (Mevcut Deðiþkenler Aynen Kalýyor) ...
    public IGrabable Master => this;

    // Baðlý olduðu tepsi (Varsa)
    [HideInInspector] public Tray currentTray;
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;
    public Sprite Icon { get => data.icon; set => data.icon = value; }

    public float GetFillDuration => data.fillDuration;

    [SerializeField] private GameObject drinkGO;
    [SerializeField] private GameObject lidGO;
    [SerializeField] private GameObject strawGO;

    // --- YENÝ EKLENENLER ---
    [HideInInspector] public SodaMachine currentMachine;
    public bool IsGettingFilled;
    public bool IsFull;
    public bool HasLid { get; private set; } = false; // Kapaðý var mý?
    // -----------------------

    // ... (Diðer deðiþkenler ayný) ...
    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }
    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }
    public PlayerManager.HandRigTypes HandRigType { get => data.handRigType; set => data.handRigType = value; }
    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }
    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }
    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public DrinkCupData data;
    public string FocusTextKey { get => data.focusTextKey; set => data.focusTextKey = value; }

    public GameManager.DrinkTypes DrinkType = GameManager.DrinkTypes.Null; //Soda Makinesi tarafýndan doldurulurken atanacak

    // --- JUICE STATE VARIABLES (Fryable Mantýðý) ---
    private bool isGettingPutOnTray; // Animasyon oynuyor mu?
    // ----------------------------------------------

    private Rigidbody rb;
    private Collider col;
    private Collider lidCol;
    private Collider strawCol;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int grabableOutlinedGreenLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;
    [HideInInspector] public bool isJustThrowed;
    [HideInInspector] public bool isJustDropped;
    private float lastSoundTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        lidCol = lidGO.GetComponent<Collider>();
        strawCol = strawGO.GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        grabableOutlinedGreenLayer = LayerMask.NameToLayer("GrabableOutlinedGreen");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");

        IsGrabbed = false;
        isJustThrowed = false;
        isJustDropped = false;

        // Baþlangýçta içecek kapalý olsun
        if (drinkGO != null) drinkGO.SetActive(false);
    }

    // ARTIK APEX TRANSFORM ALIYORUZ
    // Parametreye 'int slotIndex' eklendi
    // ... (Üst kýsýmlar ayný) ...

    public void PlaceOnTray(Transform targetSlot, Transform apexTransform, Tray trayRef, int slotIndex)
    {
        // 1. State Ayarla
        isGettingPutOnTray = true;
        isJustDropped = false;
        isJustThrowed = false;
        currentTray = trayRef;

        // Çarpýþmayý yoksay
        if (currentTray != null) ToggleCollisionWithTray(currentTray.GetCollider, true);

        if (PlayerManager.Instance != null && IsGrabbed)
        {
            PlayerManager.Instance.ResetPlayerGrab(this);
        }

        // 2. Fizik Kapat
        rb.isKinematic = true;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        HandleColliders(false);

        // 3. Parent Ata
        transform.SetParent(targetSlot, true);

        // 4. HEDEF OFFSET VE RANDOMÝZASYON HESABI (DÜZELTÝLDÝ)
        Vector3 baseLocalPos = Vector3.zero;
        Vector3 baseLocalRot = Vector3.zero;

        // Data'dan veriyi çek
        if (data.slotOffsets != null && slotIndex < data.slotOffsets.Length)
        {
            baseLocalPos = data.slotOffsets[slotIndex].localPosition;
            baseLocalRot = data.slotOffsets[slotIndex].localRotation;
        }

        // --- RANDOMÝZASYON (JUICE) ---
        // Senin X eksenine göre random istediðini biliyoruz.
        // Base rotasyonun Y ve Z'sini koru, X'e random çak.
        float randomX = Random.Range(0f, 360f);
        Vector3 finalTargetEuler = new Vector3(randomX, baseLocalRot.y, baseLocalRot.z);

        // KRÝTÝK NOKTA: Hedefi Quaternion'a çeviriyoruz
        Quaternion finalTargetRotation = Quaternion.Euler(finalTargetEuler);
        // -----------------------------

        // 5. APEX Hesabý
        Vector3 localApexPos;
        if (apexTransform != null)
        {
            localApexPos = targetSlot.InverseTransformPoint(apexTransform.position);
        }
        else
        {
            localApexPos = baseLocalPos + Vector3.up * 0.15f;
        }

        // 6. DOTween Sequence
        Vector3[] pathPoints = new Vector3[] { localApexPos, baseLocalPos };
        Sequence seq = DOTween.Sequence();

        // Hareket (CatmullRom)
        seq.Join(transform.DOLocalPath(pathPoints, 0.2f, PathType.CatmullRom).SetEase(Ease.OutSine));

        // --- ROTASYON DÜZELTMESÝ ---
        // DOLocalRotate yerine DOLocalRotateQuaternion kullanýyoruz.
        // Bu fonksiyon 10 dereceden 350 dereceye giderken "340 derece dönmek" yerine 
        // "-20 derece" dönmeyi (kýsa yolu) otomatik seçer. Sapýtmayý engeller.
        seq.Join(transform.DOLocalRotateQuaternion(finalTargetRotation, 0.2f).SetEase(Ease.OutBack));
        seq.Join(transform.DOScale(data.trayLocalScale, 0.2f));
        // ---------------------------

        seq.OnComplete(() =>
        {
            // Ýþlem bitince transformlarý netle (Floating point hatasý kalmasýn)
            transform.localPosition = baseLocalPos;
            transform.localRotation = finalTargetRotation;

            isGettingPutOnTray = false;
            HandleColliders(true);
            if (currentTray != null) ChangeLayer(currentTray.gameObject.layer);
        });
    }

    public void AttachLidAndStraw()
    {
        if (HasLid) return; // Zaten varsa iþlem yapma

        HasLid = true;

        // Bardaðýn kendi parçalarýný aç
        if (lidGO != null) lidGO.SetActive(true);
        if (strawGO != null) strawGO.SetActive(true);

        // Opsiyonel: Ses çal (Kapak 'çýt' sesi)
        // SoundManager.Instance.PlaySoundFX(...) 
    }

    // --- YENÝ FONKSÝYON: DOLUM ÝÞLEMÝ ---
    public void StartFilling(Color liquidColor, float duration)
    {
        // 1. Zaten doluysa veya kapak takýldýysa iþlem yapma (Renk deðiþtirme, tekrar doldurma vs.)
        if (IsFull || HasLid) return;

        // 2. Ýçecek objesini aç
        if (drinkGO != null)
        {
            drinkGO.SetActive(true);

            // 3. Rengi Ayarla
            SkinnedMeshRenderer meshRenderer = drinkGO.GetComponent<SkinnedMeshRenderer>();
            if (meshRenderer != null)
            {
                // Material rengini deðiþtir
                meshRenderer.material.color = liquidColor;
                // Eðer shader graph veya özel shader kullanýyorsan:
                // meshRenderer.material.SetColor("_BaseColor", liquidColor);

                // 4. BlendShape Animasyonu (0 -> 100)
                // "Key 1" genelde Index 0'dýr. Eðer Blender'da sýralamada 2. sýradaysa Index 1 yap.
                int blendShapeIndex = 0;

                // Önce sýfýrla
                meshRenderer.SetBlendShapeWeight(blendShapeIndex, 0f);

                // DOTween ile sürece yayarak 100 yap
                DOTween.To(() => meshRenderer.GetBlendShapeWeight(blendShapeIndex),
                           x => meshRenderer.SetBlendShapeWeight(blendShapeIndex, x),
                           96f, duration)
                           .SetEase(Ease.Linear);
            }
        }
    }
    // -------------------------------------

    public void OnHolster() { }

    private void ToggleCollisionWithTray(Collider trayCollider, bool ignore)
    {
        if (trayCollider == null) return;

        // Ana gövde
        if (col != null) Physics.IgnoreCollision(col, trayCollider, ignore);

        // Kapak
        if (lidCol != null) Physics.IgnoreCollision(lidCol, trayCollider, ignore);

        // Pipet
        if (strawCol != null) Physics.IgnoreCollision(strawCol, trayCollider, ignore);
    }

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(grabbedLayer);

        if (currentTray != null)
        {
            // --- DEÐÝÞÝKLÝK BURADA: Çarpýþmalarý tekrar aktif et ---
            ToggleCollisionWithTray(currentTray.GetCollider, false);
            // -------------------------------------------------------

            currentTray.RemoveItem(this);
            currentTray = null;
        }

        isGettingPutOnTray = false;

        if (currentMachine != null)
        {
            currentMachine.ReleaseCup();
            currentMachine = null;
        }

        if (drinkGO != null)
        {
            SkinnedMeshRenderer drinkSMR = drinkGO.GetComponent<SkinnedMeshRenderer>();
            drinkSMR.DOKill();
            drinkSMR.SetBlendShapeWeight(0, 96f);
        }

        HandleColliders(false);
        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);
        
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = false;
        IsGrabbed = true;
        
        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);

        transform.localScale = data.grabbedLocalScale;
    }

    // ... (Diðer metodlar ayný: OnFocus, OnDrop, OnThrow, Collision vs.) ...

    public void OnFocus()
    {
        if (!isJustDropped && !isJustThrowed && !IsGettingFilled && !isGettingPutOnTray)
            ChangeLayer(grabableOutlinedLayer);
    }
    public void OnLoseFocus()
    {
        if (!isJustDropped && !isJustThrowed && !IsGettingFilled && !isGettingPutOnTray)
            ChangeLayer(grabableLayer);
    }

    public void OnDrop(Vector3 direction, float force)
    {
        HandleColliders(true);
        IsGrabbed = false;
        transform.SetParent(null);
        rb.useGravity = true;
        rb.AddForce(direction * force, ForceMode.Impulse);
        isJustDropped = true;
        ChangeLayer(ungrabableLayer);
    }

    public void OnThrow(Vector3 direction, float force)
    {
        HandleColliders(true);
        IsGrabbed = false;
        transform.SetParent(null);
        rb.useGravity = true;
        rb.AddForce(direction * force, ForceMode.Impulse);
        isJustThrowed = true;
        ChangeLayer(ungrabableLayer);
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
        drinkGO.layer = layer;
        lidGO.layer = layer;
        strawGO.layer = layer;
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

    private void OnDisable() { OnLoseFocus(); }
    private void OnDestroy() { OnLoseFocus(); }

    private void HandleSoundFX(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < data.dropThreshold || Time.time - lastSoundTime < data.soundCooldown) return;
        if (impactForce >= data.throwThreshold)
            SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, data.throwSoundVolume, data.throwSoundMinPitch, data.throwSoundMaxPitch);
        else
            SoundManager.Instance.PlaySoundFX(data.audioClips[1], transform, data.dropSoundVolume, data.dropSoundMinPitch, data.dropSoundMaxPitch);
        lastSoundTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed) { ChangeLayer(grabableLayer); isJustThrowed = false; }
            else if (isJustDropped) { ChangeLayer(grabableLayer); isJustDropped = false; }
            HandleSoundFX(collision);
        }
    }

    private void HandleColliders(bool state)
    {
        col.enabled = state;
        lidCol.enabled = state;
        strawCol.enabled = state;
    }

    public void OnUseHold() { throw new System.NotImplementedException(); }
    public void OnUseRelease() { throw new System.NotImplementedException(); }
    public bool CanCombine(IGrabable otherItem)
    {
        // 1. Bardaðým zaten kapalýysa kimseyle birleþmem.
        if (HasLid) return false;

        // 2. Bardaðým boþsa kapak takmam (Opsiyonel, istersen boþ da kapatabilirsin ama genelde dolu kapatýlýr)
        if (!IsFull) return false;

        // 3. Karþýdaki obje bir KAPAK mý?
        if (otherItem is DrinkCupLid)
        {
            return true; // Yeþil ýþýk yak
        }

        return false;
    }

    // Combine tuþuna basýldý (Lid'i yiyeceðiz)
    public bool TryCombine(IGrabable otherItem)
    {
        if (CanCombine(otherItem))
        {
            // Karþýdaki obje Kapak olduðu kesin (CanCombine true döndü)
            DrinkCupLid targetLid = (DrinkCupLid)otherItem;

            // 1. Kendi görselimi aç
            AttachLidAndStraw();

            // 2. Hedef kapaðý yok et
            // Eðer kapak o an bir yerde takýlýysa veya fizikselse güvenli silme yapalým

            // Eðer kapak (çok düþük ihtimal ama) baþkasýnýn elindeyse?
            if (targetLid.IsGrabbed)
            {
                // Baþkasýnýn elinden alýp yok etmek gerekir ama 
                // senin oyun singleplayer olduðu için buna gerek yok.
                // Yine de PlayerManager kontrolü iyidir.
                if (PlayerManager.Instance != null) PlayerManager.Instance.ResetPlayerGrab(targetLid);
            }

            Destroy(targetLid.gameObject);

            // 3. Baþarýlý döndür
            return true;
        }

        return false;
    }

    public void FinishPuttingOnSodaMachine()
    {
        ChangeLayer(grabableLayer);
        isJustDropped = false;
        isJustThrowed = false;
    }

    public void FinishGettingFilled()
    {
        IsGettingFilled = false;
        IsFull = true;
        ChangeLayer(grabableLayer);
    }
}