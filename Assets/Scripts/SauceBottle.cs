using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SauceBottle : MonoBehaviour, IGrabable
{
    public bool IsGrabbed { get => isGrabbed; set => isGrabbed = value; }
    private bool isGrabbed;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public Vector3 GrabPositionOffset { get => grabPositionOffset; set => grabPositionOffset = value; }
    [SerializeField] private Vector3 grabPositionOffset = new Vector3(0.4f, 0.1f, 2f);
    public Vector3 GrabRotationOffset { get => grabRotationOffset; set => grabRotationOffset = value; }
    [SerializeField] private Vector3 grabRotationOffset = new Vector3(-5f, -70f, -70f);

    public bool IsUseable { get => data.isUseable; set => data.isUseable = value; }

    [Space]

    [SerializeField] private Vector3 stabPositionOffset = new Vector3(0.2f, 0.3f, 1.5f);
    [SerializeField] private Vector3 stabRotationOffset = new Vector3(2.5f, -70f, -100f);

    [Space]

    [SerializeField] private Tray tray;

    public SauceBottleData data;

    public enum SauceType
    {
        Ketchup,
        Mayo,
        Mustard,
        BBQ,
        Water
    }

    public Sprite FocusImage { get => focusImage; set => focusImage = value; }
    [SerializeField] private Sprite focusImage;
    [Space]
    [SerializeField] private ParticleSystem pourParticle;
    [Space]
    [SerializeField] private SauceType sauceType;

    private bool isPlayingParticles;

    private AudioSource audioSource;
    private Rigidbody rb;
    private Collider col;

    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int ungrabableLayer;

    private bool isJustThrowed;

    private float audioLastPlayedTime;

    private Coroutine stabCoroutine;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        IsGrabbed = false;

        isJustThrowed = false;

        audioLastPlayedTime = 0f;
    }

    public void OnGrab(Transform grabPoint)
    {
        gameObject.layer = ungrabableLayer;

        col.enabled = false;

        tray.TurnOnSauceHologram(sauceType);

        PlayAudioWithRandomPitch(0);

        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = false;

        IsGrabbed = true;

        transform.SetParent(grabPoint);
        transform.position = grabPoint.position;
        transform.localPosition = data.grabPositionOffset;
        transform.localRotation = Quaternion.Euler(data.grabRotationOffset);
    }
    public void OnFocus()
    {
        gameObject.layer = grabableOutlinedLayer;
    }
    public void OnLoseFocus()
    {
        gameObject.layer = grabableLayer;
    }

    public void OnDrop(Vector3 direction, float force)
    {
        tray.TurnOffAllHolograms();

        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.1f);

        if (stabCoroutine != null)
        {
            StopCoroutine(stabCoroutine);
            stabCoroutine = null;
        }

        pourParticle.Stop();
        isPlayingParticles = false;

        transform.SetParent(null);

        rb.useGravity = true;

        rb.AddForce(direction * force, ForceMode.Impulse);
    }

    public void OnThrow(Vector3 direction, float force)
    {
        tray.TurnOffAllHolograms();

        IsGrabbed = false;

        Invoke("TurnOnCollider", 0.1f);

        if (stabCoroutine != null)
        {
            StopCoroutine(stabCoroutine);
            stabCoroutine = null;
        }

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

    private void TurnOnCollider()
    {
        col.enabled = true;
    }

    private void OnDisable()
    {
        OnLoseFocus();
    }

    private void OnDestroy()
    {
        OnLoseFocus();
    }

    private void PlayAudioWithRandomPitch(int index)
    {
        audioLastPlayedTime = Time.time;
        audioSource.pitch = Random.Range(0.85f, 1.15f);
        audioSource.PlayOneShot(data.audioClips[index]);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsGrabbed && (collision.gameObject.CompareTag("Ground") || collision.gameObject.CompareTag("Door") || collision.gameObject.CompareTag("Customer")))
        {
            if (isJustThrowed)
            {
                pourParticle.Stop();
                isPlayingParticles = false;

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
        PlayerManager.Instance.SetPlayerUseHandLerp(stabPositionOffset, stabRotationOffset, data.timeToStab);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        if (stabCoroutine != null)
        {
            StopCoroutine(stabCoroutine);
            stabCoroutine = null;
        }

        stabCoroutine = StartCoroutine(Stab(true));
    }

    public void OnUseRelease()
    {
        PlayerManager.Instance.SetPlayerUseHandLerp(grabPositionOffset, grabRotationOffset, data.timeToStab / 2f);
        PlayerManager.Instance.SetPlayerIsUsingItemXY(false, false);

        pourParticle.Stop();
        isPlayingParticles = false;

        if (stabCoroutine != null)
        {
            StopCoroutine(stabCoroutine);
            stabCoroutine = null;
        }

        stabCoroutine = StartCoroutine(Stab(false));
    }

    private IEnumerator Stab(bool shouldStab)
    {
        Vector3 startPos = transform.localPosition;
        Quaternion startRot = transform.localRotation;

        Vector3 endPos = shouldStab ? data.stabPositionOffset : data.grabPositionOffset;
        Quaternion endRot = shouldStab ? Quaternion.Euler(data.stabRotationOffset) : Quaternion.Euler(data.grabRotationOffset);

        float elapsedTime = 0f;
        float value = 0f;

        while (elapsedTime < data.timeToStab)
        {
            value = elapsedTime / data.timeToStab;

            transform.localPosition = Vector3.Lerp(startPos, endPos, value);
            transform.localRotation = Quaternion.Lerp(startRot, endRot, value);

            if (shouldStab && !isPlayingParticles && value > 0.6f)
            {
                pourParticle.Play();
                isPlayingParticles = true;
            }

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = endPos;
        transform.localRotation = endRot;

        stabCoroutine = null;

    }
}
