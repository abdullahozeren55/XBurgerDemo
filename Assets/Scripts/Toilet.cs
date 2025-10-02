using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Toilet : MonoBehaviour, IInteractable
{
    [Header("Audio Settings")]
    public AudioClip openSound;
    public AudioClip closeSound;
    private AudioSource audioSource;

    public Sprite FocusImage { get => focusImages[toiletStateNum]; set => focusImages[toiletStateNum] = value; }
    [SerializeField] private Sprite[] focusImages;
    private int toiletStateNum = 0;
    [Space]

    [Header("Open Close Settings")]
    [SerializeField] private float timeToRotate = 0.3f;
    [SerializeField] private float openYRotation = 97.0f;
    private Quaternion closeRotation;
    private Quaternion openRotation;
    private Coroutine rotateCoroutine;
    private bool isOpened;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;

    private GameObject toiletPart;

    public PlayerManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    void Awake()
    {
        toiletPart = transform.GetChild(0).gameObject;

        isOpened = false;
        closeRotation = toiletPart.transform.localRotation;
        openRotation = Quaternion.Euler(closeRotation.x, openYRotation, closeRotation.z);

        audioSource = toiletPart.GetComponent<AudioSource>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
    }

    public void OnFocus()
    {
        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }

    public void OnInteract()
    {
        HandleRotation();
    }

    public void OnLoseFocus()
    {
        ChangeLayer(interactableLayer);
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

    public void HandleRotation()
    {
        isOpened = !isOpened;

        toiletStateNum = isOpened ? 1 : 0;

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        PlaySound(!isOpened);
        rotateCoroutine = StartCoroutine(ToggleRotate(isOpened));
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

    private void ChangeLayer(int layerIndex)
    {
        gameObject.layer = layerIndex;
        toiletPart.layer = layerIndex;
    }

    private IEnumerator ToggleRotate(bool shouldOpen)
    {

        Quaternion targetRotation = shouldOpen ? openRotation : closeRotation;
        Quaternion startingRotation = toiletPart.transform.localRotation;

        float timeElapsed = 0f;

        while (timeElapsed < timeToRotate)
        {
            toiletPart.transform.localRotation = Quaternion.Slerp(startingRotation, targetRotation, timeElapsed / timeToRotate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        toiletPart.transform.localRotation = targetRotation;
        rotateCoroutine = null;
    }
}
