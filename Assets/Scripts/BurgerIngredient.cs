using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UI;

public class BurgerIngredient : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public bool IsGettingPutOnTray { get => isGettingPutOnTray; set => isGettingPutOnTray = value; }
    private bool isGettingPutOnTray;

    public float HandLerp { get => handLerp; set => handLerp = value; }
    [SerializeField] private float handLerp;

    private bool isGettingPutOnTrash;

    public BurgerIngredientData data;

    [SerializeField] private AudioSource audioSource;

    [SerializeField] private Tray tray;
    [SerializeField] private GameObject grabText;
    [SerializeField] private GameObject dropText;

    [HideInInspector] public bool canAddToTray;

    private Rigidbody rb;
    private Collider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int ungrabableLayer;
    private int onTrayLayer;

    private Vector3 trayPos;

    private bool isJustThrowed;
    private bool isStuck;
    public bool canStick;

    public Cookable.CookAmount cookAmount;

    private float audioLastPlayedTime;

    private Transform decalParent;

    private void Awake()
    {

        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        onTrayLayer = LayerMask.NameToLayer("OnTray");

        decalParent = transform.Find("DecalParent");

        IsGrabbed = false;
        IsGettingPutOnTray = false;

        isJustThrowed = false;
        isStuck = false;

        canAddToTray = false;

        audioLastPlayedTime = 0f;
    }

    public void PutOnTray(Vector3 trayPos, Transform parentTray)
    {
        IsGettingPutOnTray = true;
        gameObject.layer = onTrayLayer;

        // Ses çalma kýsmý ayný kalýyor
        if (data.audioClips.Length < 4)
        {
            PlayAudioWithRandomPitch(1);
        }
        else
        {
            if (cookAmount == Cookable.CookAmount.RAW)
                PlayAudioWithRandomPitch(1);
            else if (cookAmount == Cookable.CookAmount.REGULAR)
                PlayAudioWithRandomPitch(4);
            else if (cookAmount == Cookable.CookAmount.BURNT)
                PlayAudioWithRandomPitch(7);
        }

        col.enabled = false;
        rb.isKinematic = true;
        IsGrabbed = false;

        transform.parent = parentTray;

        // Pozisyon tween
        transform.DOMove(trayPos, data.timeToPutOnTray)
            .SetEase(Ease.OutBack); // biraz lastikli otursun

        // Rotasyon tween
        transform.DORotateQuaternion(Quaternion.identity, data.timeToPutOnTray)
            .SetEase(Ease.OutCubic);
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

        tray.currentIngredient = this;
        tray.TurnOnHologram(data.ingredientType);

        if (isStuck)
            Unstick();

        if (data.audioClips.Length < 4)
        {
            PlayAudioWithRandomPitch(0);
        }
        else
        {
            if (cookAmount == Cookable.CookAmount.RAW)
            {
                PlayAudioWithRandomPitch(0);
            }
            else if (cookAmount == Cookable.CookAmount.REGULAR)
            {
                PlayAudioWithRandomPitch(3);
            }
            else if (cookAmount == Cookable.CookAmount.BURNT)
            {
                PlayAudioWithRandomPitch(6);
            }
        }


        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        HandleText(true);

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabRotationOffset);
    }
    public void OnFocus()
    {
        if (!isGettingPutOnTrash)
        {
            HandleText(true);
            gameObject.layer = grabableOutlinedLayer;
        }
        
    }
    public void OnLoseFocus()
    {
        if (!isGettingPutOnTrash)
        {
            HandleText(false);
            gameObject.layer = grabableLayer;
        }
        
    }

    public void OnDrop(Vector3 direction, float force)
    {
        tray.TurnOffAllHolograms();

        IsGrabbed = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    public void OnThrow(Vector3 direction, float force)
    {
        tray.TurnOffAllHolograms();

        IsGrabbed = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustThrowed = true;
    }

    private void HandleText(bool isFocused)
    {
        if (isFocused)
        {
            grabText.SetActive(!IsGrabbed);
            dropText.SetActive(IsGrabbed);
        }
        else
        {
            if (grabText.activeSelf) grabText.SetActive(false);
            if (dropText.activeSelf) dropText.SetActive(false);
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

        // Normal yönüne göre rotation hesapla
        Quaternion finalRotation = Quaternion.LookRotation(normal) * Quaternion.Euler(0, 180, 0);

        for (int i = 0; i < countToDrop; i++)
        {
            Transform child = decalParent.GetChild(i);
            child.transform.parent = collision.transform;

            Vector3 randomOffset = tangent * Random.Range(-spreadRadius, spreadRadius) +
                               bitangent * Random.Range(-spreadRadius, spreadRadius);

            Vector3 spawnPoint = hitPoint + randomOffset;

            child.transform.position = spawnPoint;
            child.transform.rotation = finalRotation;
        }
    }

    private void Unstick()
    {
        transform.SetParent(null);

        rb.isKinematic = false;
        isStuck = false;
    }

    private void PlayAudioWithRandomPitch (int index)
    {
        audioLastPlayedTime = Time.time;
        audioSource.pitch = Random.Range(0.85f, 1.15f);
        audioSource.PlayOneShot(data.audioClips[index]);
    }

    public void ChangeGrabText(GameObject newText)
    {
        bool wasGrabTextActive = false;
        if (grabText.activeSelf)
        {
            wasGrabTextActive = true;
            grabText.SetActive(false);
        }

        grabText = newText;

        if (wasGrabTextActive)
            grabText.SetActive(true);
    }

    private void OnDestroy()
    {
        GameManager.Instance.ResetPlayerGrab(this);
    }

    private IEnumerator PutOnTray()
    {
        Vector3 startPos = transform.position;
        Quaternion startRotation = transform.rotation;
        Quaternion targetRotation = Quaternion.identity;

        float timeElapsed = 0f;
        float rate = 0f;

        while (timeElapsed < data.timeToPutOnTray)
        {
            rate = timeElapsed / data.timeToPutOnTray;

            transform.position = Vector3.Lerp(startPos, trayPos, rate);
            transform.rotation = Quaternion.Slerp(startRotation, targetRotation, rate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.position = trayPos;
        transform.rotation = targetRotation;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !IsGettingPutOnTray && (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Door") || collision.gameObject.CompareTag("Customer")))
        {

            if (decalParent != null && decalParent.childCount > 0)
                HandleSauceDrops(collision);

            if (isJustThrowed)
            {
                if (canStick)
                    StickToSurface(collision);

                if (data.audioClips.Length < 4)
                {
                    PlayAudioWithRandomPitch(2);
                }
                else
                {
                    if (cookAmount == Cookable.CookAmount.RAW)
                    {
                        PlayAudioWithRandomPitch(2);
                    }
                    else if (cookAmount == Cookable.CookAmount.REGULAR)
                    {
                        PlayAudioWithRandomPitch(5);
                    }
                    else if (cookAmount == Cookable.CookAmount.BURNT)
                    {
                        PlayAudioWithRandomPitch(8);
                    }
                }

                isJustThrowed = false;
            }
            else if (Time.time > audioLastPlayedTime + 0.1f)
            {
                if (data.audioClips.Length < 4)
                {
                    PlayAudioWithRandomPitch(1);
                }
                else
                {
                    if (cookAmount == Cookable.CookAmount.RAW)
                    {
                        PlayAudioWithRandomPitch(1);
                    }
                    else if (cookAmount == Cookable.CookAmount.REGULAR)
                    {
                        PlayAudioWithRandomPitch(4);
                    }
                    else if (cookAmount == Cookable.CookAmount.BURNT)
                    {
                        PlayAudioWithRandomPitch(7);
                    }
                }
            }

        }

        
    }
}
