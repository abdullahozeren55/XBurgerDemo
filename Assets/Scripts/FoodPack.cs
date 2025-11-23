using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FoodPack : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public PlayerManager.HandGrabTypes HandGrabType { get => data.handGrabType; set => data.handGrabType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public Vector3 GrabPositionOffset { get => data.grabPositionOffset; set => data.grabPositionOffset = value; }
    public Vector3 GrabRotationOffset { get => data.grabRotationOffset; set => data.grabRotationOffset = value; }

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    public FoodPackData data;

    public string FocusTextKey { get => data.focusTextKey; set => data.focusTextKey = value; }
    [Space]

    [SerializeField] private Rigidbody[] allRB;
    [SerializeField] private Collider[] allCollider;
    [SerializeField] private Transform[] allTransform;

    private Rigidbody rb;
    private Collider col;
    private MeshRenderer meshRenderer;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;

    private bool isJustThrowed;
    private bool isJustDropped;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();
        meshRenderer = GetComponent<MeshRenderer>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        IsGrabbed = false;

        isJustThrowed = false;
        isJustDropped = false;
    }

    public void OnGrab(Transform grabPoint)
    {
        ChangeLayer(ungrabableLayer);

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
            ChangeLayer(interactableOutlinedRedLayer);
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            ChangeLayer(grabableOutlinedLayer);
        }
    }

    public void Open(bool shouldExplode)
    {
        gameObject.layer = ungrabableLayer;
        meshRenderer.enabled = false;
        col.enabled = false;

        for (int i = 0; i < allTransform.Length; i++)
        {
            float rand = shouldExplode ? Random.Range(data.minForceExplode, data.maxForceExplode) : Random.Range(data.minForce, data.maxForce);

            if (data.haveWholeIngredient)
                allTransform[i].GetComponent<WholeIngredient>().HandlePackOpening();

            SoundManager.Instance.PlaySoundFXWithRandomDelay(data.audioClips[5], allTransform[i], data.instantiatedSoundVolume, data.instantiatedSoundMinPitch, data.instantiatedSoundMaxPitch, data.instantiatedSoundMinDelay, data.instantiatedSoundMaxDelay);


            // Generate a random force direction and magnitude
            Vector3 randomForce = new Vector3(
                    Random.Range(-0.5f, 0.5f), // Random x direction
                    Random.Range(0.5f, 1f), // Random y direction
                    Random.Range(-0.5f, 0.5f)  // Random z direction
                ).normalized * rand;

            allRB[i].isKinematic = false;
            allRB[i].AddForce(randomForce, ForceMode.Impulse);

            allCollider[i].enabled = true;
            allTransform[i].SetParent(null);
        }

        Instantiate(shouldExplode? data.destroyParticleExplode : data.destroyParticle, transform.position, Quaternion.Euler(transform.rotation.x - 90f, transform.rotation.y + 90f, transform.rotation.z + 90f));

        if (shouldExplode)
            SoundManager.Instance.PlaySoundFX(data.audioClips[4], transform, data.explodeSoundVolume, data.explodeSoundMinPitch, data.explodeSoundMaxPitch);
        else
            SoundManager.Instance.PlaySoundFX(data.audioClips[3], transform, data.openSoundVolume, data.openSoundMinPitch, data.openSoundMaxPitch);

        Destroy(gameObject);
    }

    private void ChangeLayer(int layer)
    {
        gameObject.layer = layer;

        foreach (Transform tr in allTransform)
        {
            tr.gameObject.layer = layer;
        }
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

                SoundManager.Instance.PlaySoundFX(data.audioClips[2], transform, data.throwSoundVolume, data.throwSoundMinPitch, data.throwSoundMaxPitch);

                ChangeLayer(grabableLayer);

                isJustThrowed = false;
            }
            else if (isJustDropped)
            {
                ChangeLayer(grabableLayer);

                SoundManager.Instance.PlaySoundFX(data.audioClips[1], transform, data.dropSoundVolume, data.dropSoundMinPitch, data.dropSoundMaxPitch);

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
