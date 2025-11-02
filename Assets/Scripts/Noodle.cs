using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Noodle : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public NoodleManager.NoodleStatus NoodleStatus;
    public Cookable.CookAmount CookAmount; //RAW USED FOR RAW, REGULAR USED FOR COOKED, BURNT USED FOR FINISHED

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public bool IsUseable { get => isUseable; set => isUseable = value; }
    [SerializeField] private bool isUseable = true;

    public bool IsGettingPutOnHologram;
    public bool CanGetFocused;

    public NoodleData data;
    public string FocusText { get => focusText; set => focusText = value; }

    [SerializeField] private string focusText;
    [Space]

    [Header("Colliders")]
    [SerializeField] private Collider bottomCollider;
    [SerializeField] private Collider lidCollider;

    [Header("Other Settings")]
    [SerializeField] private Transform saucePackInstantiatePoint;
    [SerializeField] private GameObject saucePack;
    [SerializeField] private GameObject saucePackVisual;

    private SkinnedMeshRenderer skinnedMeshRederer;
    private Rigidbody rb;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;

    private Vector3 hologramPos;
    private Quaternion hologramRotation;

    private bool isJustThrowed;
    private bool isJustDropped;

    private bool isOpened;
    private bool lidAnimStarted;
    private bool saucePackInstantiated;

    private Coroutine openLidCoroutine;
    private Coroutine instantiateSaucePackCoroutine;


    private void Awake()
    {
        skinnedMeshRederer = GetComponent<SkinnedMeshRenderer>();
        rb = GetComponent<Rigidbody>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        CanGetFocused = true;

        IsGrabbed = false;
        IsGettingPutOnHologram = false;

        isJustThrowed = false;
        isJustDropped = false;
    }

    public void PutOnHologram(Vector3 hologramPos, Quaternion hologramRotation)
    {
        IsGettingPutOnHologram = true;
        isJustThrowed = false;
        isJustDropped = false;

        NoodleManager.Instance.SetHologramHouseNoodle(false);

        if (NoodleStatus == NoodleManager.NoodleStatus.SaucePackInstantiated)
            NoodleStatus = NoodleManager.NoodleStatus.OnHouseHologram;
        else if (NoodleStatus == NoodleManager.NoodleStatus.LidClosed)
            NoodleStatus = NoodleManager.NoodleStatus.WaitingToBeReady;

        gameObject.layer = ungrabableLayer;

        IsGrabbed = false;

        this.hologramPos = hologramPos;
        this.hologramRotation = hologramRotation;

        StartCoroutine(PutOnHologram());
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

        SetColliders(false);

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, 1f, 0.85f, 1.15f);

        rb.isKinematic = false;
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        NoodleManager.Instance.SetCurrentNoodle(gameObject);

        if (NoodleStatus == NoodleManager.NoodleStatus.SaucePackInstantiated || NoodleStatus == NoodleManager.NoodleStatus.LidClosed)
            NoodleManager.Instance.SetHologramHouseNoodle(true);

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);
    }
    public void OnFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed && CanGetFocused)
        {
            gameObject.layer = grabableOutlinedLayer;
        }

    }
    public void OnLoseFocus()
    {
        if (!IsGettingPutOnHologram && !isJustDropped && !isJustThrowed && CanGetFocused)
        {
            gameObject.layer = grabableLayer;
        }

    }

    public void OnDrop(Vector3 direction, float force)
    {
        SetColliders(true);

        IsGrabbed = false;

        transform.SetParent(null);

        NoodleManager.Instance.SetHologramHouseNoodle(false);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustDropped = true;
    }

    public void OnThrow(Vector3 direction, float force)
    {
        SetColliders(true);

        IsGrabbed = false;

        transform.SetParent(null);

        NoodleManager.Instance.SetHologramHouseNoodle(false);

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

    private void SetColliders(bool value)
    {
        bottomCollider.enabled = value;
        lidCollider.enabled = value == true ? isOpened : false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !IsGettingPutOnHologram && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed)
            {

                SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, 1f, 0.85f, 1.15f);

                gameObject.layer = grabableLayer;

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                gameObject.layer = grabableLayer;
                
                SoundManager.Instance.PlaySoundFX(data.audioClips[1], transform, 1f, 0.85f, 1.15f);

                isJustDropped = false;
            }

        }


    }

    private IEnumerator PutOnHologram()
    {
        rb.isKinematic = true;

        Vector3 startPos = transform.position;
        Quaternion startRotation = transform.rotation;

        float timeElapsed = 0f;
        float rate = 0f;

        while (timeElapsed < data.timeToPutOnHologram)
        {

            rate = timeElapsed / data.timeToPutOnHologram;

            transform.position = Vector3.Lerp(startPos, hologramPos, rate);
            transform.rotation = Quaternion.Slerp(startRotation, hologramRotation, rate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = hologramPos;
        transform.rotation = hologramRotation;

        CanGetFocused = false;
        IsGettingPutOnHologram = false;

        if (NoodleStatus == NoodleManager.NoodleStatus.WaitingToBeReady)
        {
            if (GameManager.Instance.DayCount == 0)
                GameManager.Instance.HandleAfterFirstNoodle();
        }
    }

    public void OnUseHold()
    {
        if (!isOpened)
        {
            PlayerManager.Instance.SetPlayerUseHandLerp(data.usePositionOffset, data.useRotationOffset, data.timeToUse);
            PlayerManager.Instance.SetPlayerLeftUseHandLerp(data.useLeftPositionOffset, data.useLeftRotationOffset);
            PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

            if (!lidAnimStarted)
            {
                if (openLidCoroutine != null)
                {
                    StopCoroutine(openLidCoroutine);
                    openLidCoroutine = null;
                }

                openLidCoroutine = StartCoroutine(HandleLid(true));
            }
        }
        else if (!saucePackInstantiated)
        {
            PlayerManager.Instance.SetPlayerUseHandLerp(data.use2PositionOffset, data.use2RotationOffset, data.timeToUse);
            PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

            if (instantiateSaucePackCoroutine != null)
            {
                StopCoroutine(instantiateSaucePackCoroutine);
                instantiateSaucePackCoroutine = null;
            }

            instantiateSaucePackCoroutine = StartCoroutine(InstantiateSaucePack());
        }
        else
        {
            PlayerManager.Instance.SetPlayerUseHandLerp(data.usePositionOffset, data.useRotationOffset, data.timeToUse);
            PlayerManager.Instance.SetPlayerLeftUseHandLerp(data.use2LeftPositionOffset, data.use2LeftRotationOffset);
            PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

            if (!lidAnimStarted)
            {
                if (openLidCoroutine != null)
                {
                    StopCoroutine(openLidCoroutine);
                    openLidCoroutine = null;
                }

                openLidCoroutine = StartCoroutine(HandleLid(false));
            }
        }
        
    }

    public void OnUseRelease()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(GrabPositionOffset, GrabRotationOffset, data.timeToUse / 2f);
        PlayerManager.Instance.PlayerResetLeftHandLerp();
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        if (openLidCoroutine != null && !lidAnimStarted)
        {
            StopCoroutine(openLidCoroutine);
            openLidCoroutine = null;
        }

        if (instantiateSaucePackCoroutine != null && !saucePackInstantiated)
        {
            StopCoroutine(instantiateSaucePackCoroutine);
            instantiateSaucePackCoroutine = null;
        }

        if (saucePackInstantiated && NoodleStatus != NoodleManager.NoodleStatus.SauceAdded && CookAmount == Cookable.CookAmount.RAW)
        {
            IsUseable = false;
        }

    }

    private IEnumerator InstantiateSaucePack()
    {
        yield return new WaitForSeconds(data.timeToHandleLid);

        Instantiate(saucePack, saucePackInstantiatePoint.position, Random.rotation);

        Destroy(saucePackVisual);

        saucePackInstantiated = true;

        NoodleStatus = NoodleManager.NoodleStatus.SaucePackInstantiated;

        NoodleManager.Instance.SetHologramHouseNoodle(true);

        yield return new WaitForSeconds(data.timeToUse);

        PlayerManager.Instance.PlayerOnUseReleaseGrabable(true);
    }

    private IEnumerator HandleLid(bool shouldOpen)
    {
        yield return new WaitForSeconds(data.timeToHandleLid);

        lidAnimStarted = true;

        float startVal = skinnedMeshRederer.GetBlendShapeWeight(0);
        float endVal = shouldOpen ? 0f : 100f;

        float elapsedTime = 0f;
        float value = startVal;

        if (shouldOpen)
        {
            SoundManager.Instance.PlaySoundFX(data.audioClips[3], transform, 1f, 0.85f, 1.15f);
        }
        else
        {
            SoundManager.Instance.PlaySoundFX(data.audioClips[4], transform, 1f, 0.85f, 1.15f);
        }

            while (elapsedTime < data.timeToHandleLid)
            {
                value = Mathf.Lerp(startVal, endVal, elapsedTime / data.timeToHandleLid);

                skinnedMeshRederer.SetBlendShapeWeight(0, value);
                NoodleManager.Instance.HandleHologramNoodleLid(value);

                elapsedTime += Time.deltaTime;
                yield return null;
            }

        skinnedMeshRederer.SetBlendShapeWeight(0, endVal);
        NoodleManager.Instance.HandleHologramNoodleLid(endVal);

        NoodleStatus = CookAmount == Cookable.CookAmount.REGULAR ? NoodleManager.NoodleStatus.Ready : shouldOpen ? NoodleManager.NoodleStatus.LidOpened : NoodleManager.NoodleStatus.LidClosed;

        isOpened = shouldOpen;

        if (NoodleStatus == NoodleManager.NoodleStatus.LidOpened)
            PlayerManager.Instance.PlayerOnUseReleaseGrabable(false);
        else if (NoodleStatus == NoodleManager.NoodleStatus.LidClosed)
        {
            NoodleManager.Instance.SetHologramHouseNoodle(true);
            NoodleManager.Instance.SetHologramHouseNoodleCollider(true);
            NoodleManager.Instance.SetCurrentSmokeParticleSystem(false);
            PlayerManager.Instance.PlayerOnUseReleaseGrabable(true);
        }    

        lidAnimStarted = false;

        openLidCoroutine = null;

    }
}
