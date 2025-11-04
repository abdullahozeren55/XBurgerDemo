using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Window : MonoBehaviour, IInteractable
{
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;

    [Header("Audio Settings")]
    public AudioClip openSound;
    public AudioClip closeSound;
    private AudioSource audioSource;

    public string FocusText { get => focusTexts[windowStateNum]; set => focusTexts[windowStateNum] = value; }
    [SerializeField] private string[] focusTexts;
    private int windowStateNum = 0;
    [Space]

    [Header("Open Close Settings")]
    [SerializeField] private float timeToOpen = 0.3f;
    [SerializeField] private float openZPosition = 6.5f;
    private Vector3 closePosition;
    private Vector3 openPosition;
    private Coroutine openCoroutine;
    private bool isOpened;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    [Header("Lock Settings")]
    public bool IsLocked;
    public AudioClip lockedSound;
    [SerializeField] private float timeToLockOpen = 0.12f;
    [SerializeField] private float lockOpenZPosition = 0.1f;
    private Vector3 lockedOpenPosition;
    private bool inLockOpen;

    private GameObject[] windowParts;

    public PlayerManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    void Awake()
    {
        // Get the number of children
        int childCount = transform.childCount;

        // Initialize the array to hold the children
        windowParts = new GameObject[childCount];

        // Loop through each child and store it in the array
        for (int i = 0; i < childCount; i++)
        {
            // Get the child GameObject
            windowParts[i] = transform.GetChild(i).gameObject;
        }

        isOpened = false;

        closePosition = windowParts[0].transform.position;
        openPosition = new Vector3(closePosition.x, closePosition.y, openZPosition);
        lockedOpenPosition = new Vector3(closePosition.x, closePosition.y, lockOpenZPosition);

        audioSource = windowParts[0].GetComponent<AudioSource>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        inLockOpen = false;
    }

    public void HandleFinishDialogue()
    {

    }

    public void OnFocus()
    {
        if (!CanInteract) return;

        if (!inLockOpen)
        {
            ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
        }
        
    }

    public void OnInteract()
    {
        if (!CanInteract) return;

        if (!IsLocked)
            HandlePosition();
        else
            HandleLocked();
    }

    public void OnLoseFocus()
    {
        if (!CanInteract) return;

        if (!inLockOpen)
        {
            ChangeLayer(interactableLayer);
        }
        
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
        {
            ChangeLayer(interactableOutlinedRedLayer);
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            ChangeLayer(interactableOutlinedLayer);
        }
    }

    public void HandlePosition()
    {
        isOpened = !isOpened;

        if (openCoroutine != null)
        {
            StopCoroutine(openCoroutine);
            openCoroutine = null;
        }

        PlaySound(!isOpened);
        openCoroutine = StartCoroutine(ToggleReposition(isOpened));
    }

    private void HandleLocked()
    {

        inLockOpen = true;

        ChangeLayer(uninteractableLayer);

        audioSource.Stop();

        audioSource.PlayOneShot(lockedSound);

        if (openCoroutine != null)
        {
            StopCoroutine(openCoroutine);
            openCoroutine = null;
        }

        openCoroutine = StartCoroutine(ToggleLockReposition());
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

    public void ChangeLayer(int layerIndex)
    {
        gameObject.layer = layerIndex;
        foreach (GameObject child in windowParts)
            child.layer = layerIndex;
    }

    private IEnumerator ToggleReposition(bool shouldOpen)
    {

        Vector3 targetPosition = shouldOpen ? openPosition : closePosition;
        Vector3 startingPosition = windowParts[0].transform.position;

        float timeElapsed = 0f;

        while (timeElapsed < timeToOpen)
        {
            windowParts[0].transform.position = Vector3.Lerp(startingPosition, targetPosition, timeElapsed / timeToOpen);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        windowParts[0].transform.position = targetPosition;
        openCoroutine = null;
    }

    private IEnumerator ToggleLockReposition()
    {

        float timeElapsed = 0f;

        while (timeElapsed < timeToLockOpen)
        {
            windowParts[0].transform.position = Vector3.Lerp(closePosition, lockedOpenPosition, timeElapsed / timeToLockOpen);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        windowParts[0].transform.position = lockedOpenPosition;

        timeElapsed = 0f;

        while (timeElapsed < timeToLockOpen)
        {
            windowParts[0].transform.position = Vector3.Lerp(lockedOpenPosition, closePosition, timeElapsed / timeToLockOpen);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        windowParts[0].transform.position = closePosition;

        timeElapsed = 0f;

        while (timeElapsed < timeToLockOpen)
        {
            windowParts[0].transform.position = Vector3.Lerp(closePosition, lockedOpenPosition, timeElapsed / timeToLockOpen);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        windowParts[0].transform.position = lockedOpenPosition;

        timeElapsed = 0f;

        while (timeElapsed < timeToLockOpen)
        {
            windowParts[0].transform.position = Vector3.Lerp(lockedOpenPosition, closePosition, timeElapsed / timeToLockOpen);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        windowParts[0].transform.position = closePosition;

        inLockOpen = false;

        ChangeLayer(interactableLayer);

        openCoroutine = null;
    }


}
