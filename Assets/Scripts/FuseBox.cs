using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FuseBox : MonoBehaviour, IInteractable
{
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;

    [Header("Audio Settings")]
    public AudioClip openSound;
    public AudioClip closeSound;
    public string audioTag;

    public string FocusTextKey { get => focusTextKeys[coolerStateNum]; set => focusTextKeys[coolerStateNum] = value; }
    [SerializeField] private string[] focusTextKeys;
    private int coolerStateNum = 0;
    [Space]

    [Header("Open Close Settings")]
    [SerializeField] private float timeToOpen = 0.3f;
    [SerializeField] private float openZRotation = -130f;
    private Quaternion closeRotation;
    private Quaternion openRotation;
    private Coroutine rotateCoroutine;
    private bool isOpened;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;

    private GameObject[] fuseBoxParts;

    public PlayerManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    void Awake()
    {
        // Get the number of children
        int childCount = transform.childCount;

        // Initialize the array to hold the children
        fuseBoxParts = new GameObject[childCount];

        // Loop through each child and store it in the array
        for (int i = 0; i < childCount; i++)
        {
            // Get the child GameObject
            fuseBoxParts[i] = transform.GetChild(i).gameObject;
        }

        isOpened = false;
        closeRotation = fuseBoxParts[0].transform.localRotation;
        openRotation = Quaternion.Euler(closeRotation.x, closeRotation.y, openZRotation);

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
    }

    public void HandleFinishDialogue()
    {

    }

    public void OnFocus()
    {
        if (!CanInteract) return;

        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }

    public void OnInteract()
    {
        if (!CanInteract) return;

        HandleRotation();
    }

    public void OnLoseFocus()
    {
        if (!CanInteract) return;

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

        SoundManager.Instance.PlaySoundFX(isOpened ? openSound : closeSound, transform, 1f, 0.99f, 1.01f, true, audioTag);

        coolerStateNum = isOpened ? 1 : 0;

        PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

        rotateCoroutine = StartCoroutine(ToogleRotate(isOpened));
    }

    public void ChangeLayer(int layerIndex)
    {
        gameObject.layer = layerIndex;
        foreach (GameObject child in fuseBoxParts)
            child.layer = layerIndex;
    }

    private IEnumerator ToogleRotate(bool shouldOpen)
    {

        Quaternion targetRotation = shouldOpen ? openRotation : closeRotation;
        Quaternion startingRotation = fuseBoxParts[0].transform.localRotation;

        float timeElapsed = 0f;

        while (timeElapsed < timeToOpen)
        {
            fuseBoxParts[0].transform.localRotation = Quaternion.Slerp(startingRotation, targetRotation, timeElapsed / timeToOpen);
            timeElapsed += Time.deltaTime;
            yield return null;
        }

        fuseBoxParts[0].transform.localRotation = targetRotation;
        rotateCoroutine = null;
    }
}
