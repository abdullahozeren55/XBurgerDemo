using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Drink : MonoBehaviour, IGrabable
{
    public IGrabable Master => this;
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public ItemIcon IconData { get => data.iconData; set => data.iconData = value; }

    public PlayerManager.HandRigTypes HandRigType { get => data.handRigType; set => data.handRigType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }
    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }

    public Transform LeftHandPoint { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public DrinkData data;

    public string FocusTextKey { get => data.focusTextKey; set => data.focusTextKey = value; }
    [Space]

    public GameObject lidGO;
    public GameObject drinkGO;

    private Rigidbody rb;
    private Collider col;
    private Collider lidCol;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;

    [HideInInspector] public bool isJustThrowed;
    [HideInInspector] public bool isJustDropped;

    // --- LOGIC VARIABLES ---
    [HideInInspector] public Tray currentTray;
    private bool isGettingPutOnTray;
    private bool isBeingDestroyed;
    // -----------------------

    private float lastSoundTime = 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        if (lidGO != null) lidCol = lidGO.GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");

        IsGrabbed = false;

        isJustThrowed = false;
        isJustDropped = false;
    }

    // --- YERLEÞME METODU ---
    public void PlaceOnTray(Transform targetSlot, Transform apexTransform, Tray trayRef, int slotIndex)
    {
        // 1. State
        isGettingPutOnTray = true;
        isJustDropped = false;
        isJustThrowed = false;
        currentTray = trayRef;

        // Çarpýþmayý Yoksay
        if (currentTray != null)
        {
            ToggleCollisionWithTray(currentTray.GetCollider, true);
        }

        if (PlayerManager.Instance != null && IsGrabbed)
        {
            PlayerManager.Instance.ResetPlayerGrab(this);
        }

        // 2. Fizik Kapat
        rb.isKinematic = true; // <-- BURADA TRUE YAPIYORUZ
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        HandleColliders(false);

        // 3. Parent
        transform.SetParent(targetSlot, true);

        // 4. Hedef & Randomizasyon
        Vector3 baseLocalPos = Vector3.zero;
        Vector3 baseLocalRot = Vector3.zero;

        if (data.slotOffsets != null && slotIndex < data.slotOffsets.Length)
        {
            baseLocalPos = data.slotOffsets[slotIndex].localPosition;
            baseLocalRot = data.slotOffsets[slotIndex].localRotation;
        }

        float randomZ = Random.Range(0f, 360f);
        Vector3 finalTargetEuler = new Vector3(baseLocalRot.x, baseLocalRot.y, randomZ);
        Quaternion finalTargetRotation = Quaternion.Euler(finalTargetEuler);

        // 5. APEX
        Vector3 localApexPos;
        if (apexTransform != null)
        {
            localApexPos = targetSlot.InverseTransformPoint(apexTransform.position);
        }
        else
        {
            localApexPos = baseLocalPos + Vector3.up * 0.15f;
        }

        // 6. DOTween
        Vector3[] pathPoints = new Vector3[] { localApexPos, baseLocalPos };
        Sequence seq = DOTween.Sequence();

        seq.Join(transform.DOLocalPath(pathPoints, 0.2f, PathType.CatmullRom).SetEase(Ease.OutSine));
        seq.Join(transform.DOLocalRotateQuaternion(finalTargetRotation, 0.2f).SetEase(Ease.OutBack));
        seq.Join(transform.DOScale(data.trayLocalScale, 0.2f));

        seq.OnComplete(() =>
        {
            SoundManager.Instance.PlaySoundFX(data.audioClips[3], transform, data.traySoundVolume, data.traySoundMinPitch, data.traySoundMaxPitch);
            transform.localPosition = baseLocalPos;
            transform.localRotation = finalTargetRotation;

            isGettingPutOnTray = false;
            HandleColliders(true);

            if (currentTray != null) ChangeLayer(currentTray.gameObject.layer);
        });
    }

    // --- GRAB (DÜZELTÝLDÝ) ---
    public void OnGrab(Transform grabPoint)
    {
        IsGrabbed = true;
        isJustDropped = false;
        isJustThrowed = false;

        if (currentTray != null)
        {
            ToggleCollisionWithTray(currentTray.GetCollider, false);
            currentTray.RemoveItem(this);
            currentTray = null;
        }
        isGettingPutOnTray = false;

        ChangeLayer(grabbedLayer);
        HandleColliders(false);

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;
        rb.isKinematic = true; // --- DÜZELTME: Elde tutarken Kinematic yap ---

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);

        transform.localScale = data.grabbedLocalScale;
    }

    // --- DROP (DÜZELTÝLDÝ) ---
    public void OnDrop(Vector3 direction, float force)
    {
        HandleColliders(true);
        IsGrabbed = false;
        transform.SetParent(null);

        rb.useGravity = true;
        rb.isKinematic = false; // --- DÜZELTME: Fiziði geri aç ---

        rb.AddForce(direction * force, ForceMode.Impulse);
        isJustDropped = true;
        ChangeLayer(ungrabableLayer);
    }

    // --- THROW (DÜZELTÝLDÝ) ---
    public void OnThrow(Vector3 direction, float force)
    {
        HandleColliders(true);
        IsGrabbed = false;
        transform.SetParent(null);

        rb.useGravity = true;
        rb.isKinematic = false; // --- DÜZELTME: Fiziði geri aç ---

        rb.AddForce(direction * force, ForceMode.Impulse);
        isJustThrowed = true;
        ChangeLayer(ungrabableLayer);
    }

    // --- Kalan Kýsýmlar Ayný ---

    public void OnHolster() { }

    private void ToggleCollisionWithTray(Collider trayCollider, bool ignore)
    {
        if (trayCollider == null) return;
        if (col != null) Physics.IgnoreCollision(col, trayCollider, ignore);
        if (lidCol != null) Physics.IgnoreCollision(lidCol, trayCollider, ignore);
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

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
        if (lidGO != null) lidGO.layer = layer;
        if (drinkGO != null) drinkGO.layer = layer;
    }

    private void HandleColliders(bool state)
    {
        if (col != null) col.enabled = state;
        if (lidCol != null) lidCol.enabled = state;
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

    private void OnDisable()
    {
        if (PlayerManager.Instance != null) PlayerManager.Instance.ResetPlayerGrab(this);

        if (currentTray != null)
        {
            currentTray.RemoveItem(this);
        }
    }
    private void OnDestroy()
    {
        if (PlayerManager.Instance != null) PlayerManager.Instance.ResetPlayerGrab(this);

        if (currentTray != null)
        {
            currentTray.RemoveItem(this);
        }
    }

    private void HandleSoundFX(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < data.dropThreshold || Time.time - lastSoundTime < data.soundCooldown) return;

        if (impactForce >= data.throwThreshold)
        {
            if (data.glassShatterPrefab != null)
            {
                BreakBottle(collision);
                return;
            } 
        }
        else
        {
            SoundManager.Instance.PlaySoundFX(data.audioClips[1], transform, data.dropSoundVolume, data.dropSoundMinPitch, data.dropSoundMaxPitch);
        }

        lastSoundTime = Time.time;
    }

    private void BreakBottle(Collision collision)
    {
        if (isBeingDestroyed) return;

        isBeingDestroyed = true;

        SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, data.throwSoundVolume, data.throwSoundMinPitch, data.throwSoundMaxPitch, false);

        // Varsayýlan deðerler
        Vector3 hitPoint = transform.position;
        Quaternion finalRotation = Quaternion.identity;

        // AYAR: Yüzeyden ne kadar uzaklaþýlacaðý (Metre cinsinden).
        // 0.05f (5cm) genellikle decal ve partiküllerin "clip" olmamasý için ideal bir mesafedir.
        float surfaceOffset = 0.05f;

        if (collision.contacts.Length > 0)
        {
            ContactPoint contact = collision.contacts[0];

            // --- KRÝTÝK DÜZELTME ---
            // Contact Point: Çarpýþmanýn olduðu nokta.
            // Contact Normal: Çarpýlan yüzeyin dýþa bakan dik vektörü.
            // Formül: Yeni Pozisyon = Çarpýþma Noktasý + (Yön * Mesafe)
            hitPoint = contact.point + (contact.normal * surfaceOffset);

            // Rotasyonu yüzeyin normaline göre ayarlýyoruz.
            // Böylece partikül efekti yüzeye dik (veya belirlenen offset ile) patlar.
            Quaternion surfaceRotation = Quaternion.LookRotation(contact.normal);
            finalRotation = surfaceRotation * Quaternion.Euler(data.effectRotationOffset);
        }
        else
        {
            // Eðer fizik motoru contact point veremezse (çok nadir, ama mümkün),
            // objenin kendi pozisyonunu kullanýrýz.
            // Ancak decal'in gömülmemesi için hýzý tersine kullanarak geriye alabiliriz 
            // veya olduðu gibi býrakabiliriz. Güvenlik için hafif yukarý alýyoruz.
            hitPoint = transform.position + Vector3.up * surfaceOffset;
            finalRotation = Quaternion.Euler(data.effectRotationOffset);
        }

        // Cam kýrýklarý (Shatter)
        if (data.glassShatterPrefab != null)
        {
            Instantiate(data.glassShatterPrefab, hitPoint, finalRotation);
        }

        // Sývý efekti (Splash)
        // Sývý efektleri genellikle decal projector içerir, bu yüzden offset hayatidir.
        if (data.liquidSplashPrefab != null)
        {
            Instantiate(data.liquidSplashPrefab, hitPoint, finalRotation);
        }

        if (PlayerManager.Instance != null) PlayerManager.Instance.ResetPlayerGrab(this);
        Destroy(gameObject);
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

    public void OnUseHold() { throw new System.NotImplementedException(); }
    public void OnUseRelease() { throw new System.NotImplementedException(); }
    public bool TryCombine(IGrabable otherItem) { return false; }
    public bool CanCombine(IGrabable otherItem) { return false; }
}