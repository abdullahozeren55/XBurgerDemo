using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Cookable;

public class WholeIngredient : MonoBehaviour, IGrabable
{
    public IGrabable Master => this;
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public Sprite Icon { get => data.icon; set => data.icon = value; }

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }
    public bool IsThrowable { get => data.isThrowable; set => data.isThrowable = value; }

    public Transform LeftHandPoint { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public float ThrowMultiplier { get => data.throwMultiplier; set => data.throwMultiplier = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool OutlineShouldBeGreen { get => outlineShouldBeGreen; set => outlineShouldBeGreen = value; }
    private bool outlineShouldBeGreen;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    [HideInInspector] public bool CanGetSliced;

    public WholeIngredientData data;

    public string FocusTextKey { get => data.focusTextKey; set => data.focusTextKey = value; }
    [Space]

    [Header("Instantiate Settings")]
    [SerializeField] private GameObject[] instantiateObjects;
    
    private Rigidbody rb;
    private Collider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;
    private int grabbedLayer;

    private bool isJustThrowed;
    private bool isJustDropped;

    private Quaternion collisionRotation;

    private float lastSoundTime = 0f;
    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grabbedLayer = LayerMask.NameToLayer("Grabbed");

        IsGrabbed = false;

        isJustThrowed = false;
        isJustDropped = false;

        CanGetSliced = true;
    }

    public void OnHolster()
    {
    }

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(grabbedLayer);

        col.enabled = false;

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabLocalPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabLocalRotationOffset);

        SoundManager.Instance.PlaySoundFX(data.audioClips[0], transform, data.grabSoundVolume, data.grabSoundMinPitch, data.grabSoundMaxPitch);
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

    public void Slice(bool shouldExplode)
    {
        if (CanGetSliced)
        {
            ChangeLayer(ungrabableLayer);

            for (int i = 0; i < data.objectAmount; i++)
            {
                foreach (GameObject go in instantiateObjects)
                {
                    float rand = shouldExplode ? Random.Range(data.minForceExplode, data.maxForceExplode) : Random.Range(data.minForce, data.maxForce);

                    GameObject newObject = Instantiate(go, transform.position, Quaternion.identity);

                    Rigidbody rb = newObject.GetComponent<Rigidbody>();

                    if (rb != null)
                    {
                        // Generate a random force direction and magnitude
                        Vector3 randomForce = new Vector3(
                            Random.Range(-0.5f, 0.5f), // Random x direction
                            Random.Range(0.5f, 1f), // Random y direction
                            Random.Range(-0.5f, 0.5f)  // Random z direction
                        ).normalized * rand;

                        // Apply the random force to the Rigidbody
                        rb.AddForce(randomForce, ForceMode.Impulse);
                    }
                }
                
            }

            Instantiate(shouldExplode ? data.destroyParticleExplode : data.destroyParticle, transform.position, transform.rotation * data.instantiateRotationOffset);

            if (shouldExplode)
                SoundManager.Instance.PlaySoundFX(data.audioClips[4], transform, data.explodeSoundVolume, data.explodeSoundMinPitch, data.explodeSoundMaxPitch, false);
            else
                SoundManager.Instance.PlaySoundFX(data.audioClips[3], transform, data.openSoundVolume, data.openSoundMinPitch, data.openSoundMaxPitch, false);

            Destroy(gameObject);
        }  
    }

    public void HandlePackOpening()
    {
        CanGetSliced = false;
        Invoke("TurnOnSlice", 0.2f);
    }

    private void TurnOnSlice()
    {
        CanGetSliced = true;
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

            if (data.throwParticles != null)
                Instantiate(data.throwParticles, transform.position, collisionRotation);
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

            if (data.dropParticles != null)
                Instantiate(data.dropParticles, transform.position, collisionRotation);
        }

        // Ses çaldýk, zamaný kaydet
        lastSoundTime = Time.time;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed)
            {
                CalculateCollisionRotation(collision);

                ChangeLayer(grabableLayer);

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                CalculateCollisionRotation(collision);

                ChangeLayer(grabableLayer);           

                isJustDropped = false;
            }

            HandleSoundFX(collision);

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

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
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
