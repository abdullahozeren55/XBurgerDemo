using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class JumpscareDoor : MonoBehaviour, IInteractable
{
    [Header("Audio Settings")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip jumpscareSound;
    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioSource jumpscareAudioSource;

    public Image FocusImage { get => focusImage; set => focusImage = value; }
    [SerializeField] private Image focusImage;
    [Space]

    [Header("Open Close Settings")]
    [SerializeField] private float timeToRotate = 0.3f;
    [SerializeField] private float openYRotation = 90.0f;
    private Quaternion closeRotation;
    private Quaternion openRotation;
    private Coroutine rotateCoroutine;
    [HideInInspector] public bool isOpened;

    [Header("Jumpscare Settings")]
    [SerializeField] private GameObject jumpscareGO;
    [SerializeField] private float jumpscareXMoveAmount;
    [SerializeField] private DialogueData dialogueAfterJumpscare;
    private bool isJumpscared;
    private bool isHandlingJumpscareDoor;
    private Collider col;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private GameManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;
    private void Awake()
    {
        isOpened = false;
        closeRotation = transform.parent.rotation;
        openRotation = Quaternion.Euler(closeRotation.x, openYRotation, closeRotation.z);

        col = GetComponent<Collider>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        isJumpscared = false;
        isHandlingJumpscareDoor = false;
    }

    public void OnInteract()
    {
        if (isJumpscared)
        {
            HandleRotation();
        }
        else if (!isHandlingJumpscareDoor)
        {
            HandleJumpscareRotation();
        }
        
    }

    public void OnFocus()
    {
        if (!isHandlingJumpscareDoor)
        {
            gameObject.layer = OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer;
        }
        
    }

    public void OnLoseFocus()
    {
        if (!isHandlingJumpscareDoor)
        {
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

    public void HandleRotation()
    {
        isOpened = !isOpened;

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        PlaySound(!isOpened);
        rotateCoroutine = StartCoroutine(ToggleRotate(isOpened));
    }

    private void HandleJumpscareRotation()
    {
        isHandlingJumpscareDoor = true;
        isOpened = !isOpened;
        gameObject.layer = uninteractableLayer;

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        PlaySound(!isOpened);
        rotateCoroutine = StartCoroutine(ToggleJumpscareRotate(isOpened));
    }

    private void PlaySound(bool isOpen)
    {
        doorAudioSource.Stop();

        if (isOpen)
        {
            doorAudioSource.PlayOneShot(closeSound);
        }
        else
        {
            doorAudioSource.PlayOneShot(openSound);
        }
    }

    private void PlayJumpscareSound()
    {
        jumpscareAudioSource.PlayOneShot(jumpscareSound);
    }

    private void PlayDialogue()
    {
        DialogueManager.Instance.StartSelfDialogue(dialogueAfterJumpscare);
    }

    private IEnumerator ToggleRotate(bool shouldOpen)
    {

        Quaternion targetRotation = shouldOpen ? openRotation : closeRotation;
        Quaternion startingRotation = transform.parent.rotation;

        float timeElapsed = 0f;

        while (timeElapsed < timeToRotate)
        {
            transform.parent.rotation = Quaternion.Slerp(startingRotation, targetRotation, timeElapsed / timeToRotate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.parent.rotation = targetRotation;
        rotateCoroutine = null;
    }

    private IEnumerator ToggleJumpscareRotate(bool shouldOpen)
    {
        col.enabled = false;

        Quaternion targetRotation = shouldOpen ? openRotation : closeRotation;
        Quaternion startingRotation = transform.parent.rotation;

        Vector3 jumpscareStartPos = jumpscareGO.transform.position;
        Vector3 jumpscareTargetPos = new Vector3(jumpscareStartPos.x + jumpscareXMoveAmount, jumpscareStartPos.y, jumpscareStartPos.z);

        float timeElapsed = 0f;

        Invoke("PlayJumpscareSound", timeToRotate / 3);

        while (timeElapsed < timeToRotate)
        {
            transform.parent.rotation = Quaternion.Slerp(startingRotation, targetRotation, timeElapsed / timeToRotate);
            jumpscareGO.transform.position = Vector3.Lerp(jumpscareStartPos, jumpscareTargetPos, timeElapsed / timeToRotate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        transform.parent.rotation = targetRotation;
        jumpscareGO.transform.position = jumpscareTargetPos;

        col.enabled = true;

        isHandlingJumpscareDoor = false;
        isJumpscared = true;
        gameObject.layer = interactableLayer;

        Invoke("PlayDialogue", 0.5f);

        rotateCoroutine = null;
    }
}
