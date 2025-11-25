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

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabTypes[boxSituation]; set => data.handGrabTypes[boxSituation] = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
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

    private Animator anim;
    private Rigidbody rb;
    private Collider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int onTrayLayer;

    [HideInInspector] public bool isJustThrowed;
    [HideInInspector] public bool isJustDropped;
    [HideInInspector] public bool CanBeReceived;

    private GameObject[] childObjects;

    [HideInInspector] public GameManager.BurgerTypes burgerType;

    private float lastSoundTime = 0f;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        onTrayLayer = LayerMask.NameToLayer("OnTray");

        IsGrabbed = false;
        isGettingPutOnTray = false;

        isJustThrowed = false;
        isJustDropped = false;
        CanBeReceived = true;

        canAddToTray = false;

        // Get all MeshRenderer components in children (including inactive)
        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>(true);

        // Filter to only get GameObjects that are not this one
        childObjects = renderers
            .Select(r => r.gameObject)
            .Where(go => go != this.gameObject)
            .ToArray();
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
        seq.Join(transform.DORotateQuaternion(Quaternion.identity, data.timeToPutOnTray).SetEase(Ease.OutCubic));
        seq.OnComplete(() =>
        {
            anim.Play("TopHolderClose");
        });

    }

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(ungrabableLayer);
        isGettingPutOnTray = false;

        CanBeReceived = true;

        tray.currentBox = this;
        tray.TurnOnBoxHologram();

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
            ChangeLayer(interactableOutlinedRedLayer);
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            ChangeLayer(grabableOutlinedLayer);
        }
    }

    private void FinishPutOnTray() //Gets called in animator
    {
        tray.ResetTray();

        GameManager.Instance.CheckBurgerType(allBurgerIngredientTypes, allSauces, this);

        gameObject.tag = "BurgerBoxClosed";

        boxSituation = 1;
        
        ChangeLayer(grabableLayer);
    }

    private void TrySquashingBurger() //Gets called in animator
    {
        tray.TrySquashingBurger();
    }

    public void SetBurgerType(GameManager.BurgerTypes type)
    {
        burgerNo = (int) type + 1;
        burgerType = type;

        PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);
    }

    public void ChangeLayer(int layer)
    {
        foreach (GameObject child in childObjects)
            child.layer = layer; 

        gameObject.layer = layer;
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
}
