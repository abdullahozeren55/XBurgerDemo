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

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;

    public bool IsGettingPutOnTray { get => isGettingPutOnTray; set => isGettingPutOnTray = value; }
    public Vector3 GrabPositionOffset { get => grabPositionOffset; set => grabPositionOffset = value; }
    [SerializeField] private Vector3 grabPositionOffset = new Vector3(0.4f, 0.1f, 2f);
    public Vector3 GrabRotationOffset { get => grabRotationOffset; set => grabRotationOffset = value; }
    [SerializeField] private Vector3 grabRotationOffset = new Vector3(-5f, -70f, -70f);

    private bool isGettingPutOnTray;

    public BurgerBoxData data;

    [HideInInspector] public bool canAddToTray;

    [SerializeField] private Tray tray;
    public Sprite FocusImage { get => data.focusImages[burgerNo]; set => data.focusImages[burgerNo] = value; }
    [HideInInspector] public int burgerNo = 0;
    [Space]

    public List<BurgerIngredientData.IngredientType> allBurgerIngredientTypes = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> allSauces = new List<SauceBottle.SauceType>();

    private AudioSource audioSource;
    private Animator anim;
    private Rigidbody rb;
    private Collider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int onTrayLayer;

    private bool isJustThrowed;

    private Vector3 trayPos;

    private float audioLastPlayedTime;

    private GameObject[] childObjects;

    [HideInInspector] public GameManager.BurgerTypes burgerType;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        anim = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        onTrayLayer = LayerMask.NameToLayer("OnTray");

        IsGrabbed = false;
        IsGettingPutOnTray = false;

        isJustThrowed = false;

        canAddToTray = false;

        audioLastPlayedTime = 0f;

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
        IsGettingPutOnTray = true;
        ChangeLayer(onTrayLayer);

        PlayAudioWithRandomPitch(1);

        rb.isKinematic = true;

        this.trayPos = trayPos;

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
        IsGettingPutOnTray = false;

        tray.currentBox = this;
        tray.TurnOnBoxHologram();

        col.enabled = false;

        PlayAudioWithRandomPitch(0);

        rb.isKinematic = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;

        if (gameObject.CompareTag("BurgerBoxClosed"))
        {
            transform.localPosition = data.grabPositionOffsetForClose;
            transform.localRotation = Quaternion.Euler(data.grabRotationOffsetForClose);
        }
        else
        {
            transform.localPosition = data.grabPositionOffsetForOpen;
            transform.localRotation = Quaternion.Euler(data.grabRotationOffsetForOpen);
        }
    }
    public void OnFocus()
    {
        ChangeLayer(grabableOutlinedLayer);
    }
    public void OnLoseFocus()
    {
        ChangeLayer(grabableLayer);
    }

    public void OnDrop(Vector3 direction, float force)
    {
        tray.TurnOffAllHolograms();

        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.08f);

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    public void OnThrow(Vector3 direction, float force)
    {
        tray.TurnOffAllHolograms();

        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.08f);

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

    private void PlayAudioWithRandomPitch(int index)
    {
        audioLastPlayedTime = Time.time;
        audioSource.pitch = Random.Range(0.85f, 1.15f);
        audioSource.PlayOneShot(data.audioClips[index]);
    }

    private void FinishPutOnTray() //Gets called in animator
    {
        tray.ResetTray();

        GameManager.Instance.CheckBurgerType(allBurgerIngredientTypes, allSauces, this);

        gameObject.tag = "BurgerBoxClosed";
        
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

        GameManager.Instance.TryChangingFocusText(this, FocusImage);
    }

    private void ChangeLayer(int layer)
    {
        foreach (GameObject child in childObjects)
            child.layer = layer; 

        gameObject.layer = layer;
    }

    private void TurnOnCollider()
    {
        col.enabled = true;
    }

    private void OnDisable()
    {
        GameManager.Instance.ResetPlayerGrab(this);
    }

    private void OnDestroy()
    {
        GameManager.Instance.ResetPlayerGrab(this);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !IsGettingPutOnTray && (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Door")))
        {
            if (isJustThrowed)
            {
                PlayAudioWithRandomPitch(2);

                isJustThrowed = false;
            }
            else if (Time.time > audioLastPlayedTime + 0.1f)
            {
                PlayAudioWithRandomPitch(1);
            }

        }
    }

    public void OnUseHold()
    {
        throw new System.NotImplementedException();
    }

    public void OnUseRelease()
    {
        throw new System.NotImplementedException();
    }
}
