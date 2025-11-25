using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static Cookable;

public class WholeIngredient : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
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

    private bool isJustThrowed;
    private bool isJustDropped;

    private Transform decalParent;

    private Quaternion collisionRotation;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        decalParent = transform.Find("DecalParent");

        IsGrabbed = false;

        isJustThrowed = false;
        isJustDropped = false;

        CanGetSliced = true;
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

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

        IsGrabbed = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);

        isJustDropped = true;
    }

    public void OnThrow(Vector3 direction, float force)
    {
        col.enabled = true;

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

    public void Slice(bool shouldExplode)
    {
        if (CanGetSliced)
        {
            gameObject.layer = ungrabableLayer;

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
                SoundManager.Instance.PlaySoundFX(data.audioClips[4], transform, data.explodeSoundVolume, data.explodeSoundMinPitch, data.explodeSoundMaxPitch);

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

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && !collision.gameObject.CompareTag("Player"))
        {
            if (isJustThrowed)
            {
                CalculateCollisionRotation(collision);

                if (decalParent != null && decalParent.childCount > 0)
                    HandleSauceDrops(collision);

                gameObject.layer = grabableLayer;

                SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, data.throwSoundVolume, data.throwSoundMinPitch, data.throwSoundMaxPitch);

                if (data.throwParticles != null)
                    Instantiate(data.throwParticles, transform.position, collisionRotation);

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                CalculateCollisionRotation(collision);

                if (decalParent != null && decalParent.childCount > 0)
                    HandleSauceDrops(collision);

                gameObject.layer = grabableLayer;

                SoundManager.Instance.PlaySoundFX(data.audioClips[1], transform, data.dropSoundVolume, data.dropSoundMinPitch, data.dropSoundMaxPitch);

                if (data.dropParticles != null)
                    Instantiate(data.dropParticles, transform.position, collisionRotation);

                isJustDropped = false;
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
