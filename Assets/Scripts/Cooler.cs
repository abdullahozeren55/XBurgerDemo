using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Cooler : MonoBehaviour, IInteractable
{
    [Header("Audio Settings")]
    public AudioClip openSound;
    public AudioClip closeSound;
    private AudioSource audioSource;

    public Image FocusImage { get => focusImage; set => focusImage = value; }
    [SerializeField] private Image focusImage;
    [Space]

    [Header("Open Close Settings")]
    [SerializeField] private float timeToOpen = 0.3f;
    [SerializeField] private float openYRotation = 135f;
    private Quaternion closeRotation;
    private Quaternion openRotation;
    private Coroutine rotateCoroutine;
    private bool isOpened;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;

    private GameObject[] coolerParts;

    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private GameManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    

    [SerializeField] private bool outlineShouldBeRed;

    void Awake()
    {
        // Get the number of children
        int childCount = transform.childCount;

        // Initialize the array to hold the children
        coolerParts = new GameObject[childCount];

        // Loop through each child and store it in the array
        for (int i = 0; i < childCount; i++)
        {
            // Get the child GameObject
            coolerParts[i] = transform.GetChild(i).gameObject;
        }

        isOpened = false;
        closeRotation = coolerParts[0].transform.localRotation;
        openRotation = Quaternion.Euler(closeRotation.x, openYRotation, closeRotation.z);

        audioSource = coolerParts[0].GetComponent<AudioSource>();

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

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        PlaySound(!isOpened);
        rotateCoroutine = StartCoroutine(ToogleRotate(isOpened));
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
        foreach (GameObject child in coolerParts)
            child.layer = layerIndex;
    }

    private IEnumerator ToogleRotate(bool shouldOpen)
    {

        Quaternion targetRotation = shouldOpen ? openRotation : closeRotation;
        Quaternion startingRotation = coolerParts[0].transform.localRotation;

        float timeElapsed = 0f;

        while (timeElapsed < timeToOpen)
        {
            coolerParts[0].transform.localRotation = Quaternion.Slerp(startingRotation, targetRotation, timeElapsed / timeToOpen);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        coolerParts[0].transform.localRotation = targetRotation;
        rotateCoroutine = null;
    }
}
