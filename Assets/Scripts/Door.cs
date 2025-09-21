using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Door : MonoBehaviour, IInteractable
{
    [Header("Audio Settings")]
    public AudioClip openSound;
    public AudioClip closeSound;
    private AudioSource audioSource;

    [Header("Text Settings")]
    public GameObject openText;
    public GameObject closeText;

    [Header("Open Close Settings")]
    [SerializeField] private float timeToRotate = 0.3f;
    [SerializeField] private float openYRotation = 90.0f;
    private Quaternion closeRotation;
    private Quaternion openRotation;
    private Coroutine rotateCoroutine;
    [HideInInspector] public bool isOpened;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    [Header("Lock Settings")]
    public bool IsLocked;
    public AudioClip lockedSound;
    public GameObject lockedText;
    [SerializeField] private float timeToLockRotate = 0.1f;
    [SerializeField] private float lockOpenYRotation = 10.0f;
    private Quaternion lockedOpenRotation;
    private bool inLockRotate;

    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private GameManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    private void Awake()
    {
        isOpened = false;
        closeRotation = transform.parent.localRotation;
        openRotation = Quaternion.Euler(closeRotation.x, openYRotation, closeRotation.z);
        lockedOpenRotation = Quaternion.Euler(closeRotation.x, lockOpenYRotation, closeRotation.z);

        audioSource = GetComponent<AudioSource>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        inLockRotate = false;
    }

    public void OnInteract()
    {
        if (!IsLocked)
            HandleRotation();
        else
            HandleLocked();
    }

    public void OnFocus()
    {
        if (!inLockRotate)
        {
            if (IsLocked)
                lockedText.SetActive(true);
            else
                HandleText(true);

            gameObject.layer = OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer;
        }
        
    }

    public void OnLoseFocus()
    {
        if (!inLockRotate)
        {
            if (IsLocked)
                lockedText.SetActive(false);
            else
                HandleText(false);

            gameObject.layer = interactableLayer;
        }
        
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
        {
            gameObject.layer = interactableOutlinedRedLayer;
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            gameObject.layer = interactableOutlinedLayer;
        }
    }

    private void HandleText(bool isFocused)
    {
        if (isFocused)
        {
            openText.SetActive(!isOpened);
            closeText.SetActive(isOpened);
        }
        else
        {
            if (openText.activeSelf) openText.SetActive(false);
            if (closeText.activeSelf) closeText.SetActive(false);
        }
    }

    public void HandleRotation()
    {
        isOpened = !isOpened;

        if (isOpened)
        {
            if (openText.activeSelf)
            {
                HandleText(true);
            }       
        }
        else
        {
            if (closeText.activeSelf)
            {
                HandleText(true);
            }
        }

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        PlaySound(!isOpened);
        rotateCoroutine = StartCoroutine(ToggleRotate(isOpened));
    }

    private void HandleLocked()
    {
        inLockRotate = true;

        gameObject.layer = uninteractableLayer;

        HandleText(false);
        lockedText.SetActive(false);

        audioSource.Stop();

        audioSource.PlayOneShot(lockedSound);

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        rotateCoroutine = StartCoroutine(ToggleLockRotate());
    }

    private void PlaySound(bool isOpen)
    {
        audioSource.Stop();

        if (isOpen)
        {
            audioSource.PlayOneShot(closeSound);
        }
        else
        {
            audioSource.PlayOneShot(openSound);
        }
    }

    private IEnumerator ToggleRotate(bool shouldOpen)
    {

        Quaternion targetRotation = shouldOpen ? openRotation : closeRotation;
        Quaternion startingRotation = transform.parent.localRotation;

        float timeElapsed = 0f;

        while (timeElapsed < timeToRotate)
        {
            transform.parent.localRotation = Quaternion.Slerp(startingRotation, targetRotation, timeElapsed / timeToRotate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.parent.localRotation = targetRotation;
        rotateCoroutine = null;
    }

    private IEnumerator ToggleLockRotate()
    {

        float timeElapsed = 0f;

        while (timeElapsed < timeToLockRotate)
        {
            transform.parent.localRotation = Quaternion.Slerp(closeRotation, lockedOpenRotation, timeElapsed / timeToLockRotate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.parent.localRotation = lockedOpenRotation;

        timeElapsed = 0f;

        while (timeElapsed < timeToLockRotate)
        {
            transform.parent.localRotation = Quaternion.Slerp(lockedOpenRotation, closeRotation, timeElapsed / timeToLockRotate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.parent.localRotation = closeRotation;

        timeElapsed = 0f;

        while (timeElapsed < timeToLockRotate)
        {
            transform.parent.localRotation = Quaternion.Slerp(closeRotation, lockedOpenRotation, timeElapsed / timeToLockRotate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.parent.localRotation = lockedOpenRotation;

        timeElapsed = 0f;

        while (timeElapsed < timeToLockRotate)
        {
            transform.parent.localRotation = Quaternion.Slerp(lockedOpenRotation, closeRotation, timeElapsed / timeToLockRotate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.parent.localRotation = closeRotation;

        inLockRotate = false;

        gameObject.layer = interactableLayer;

        rotateCoroutine = null;
    }
}
