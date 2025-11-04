using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Keys : MonoBehaviour, IInteractable
{
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;

    [Header("Audio Settings")]
    public AudioClip pickUpSound;

    public string FocusText { get => focusText; set => focusText = value; }
    [SerializeField] private string focusText;
    [Space]

    [Header("Lock Settings")]
    [SerializeField] private Door[] lockedDoors;
    [SerializeField] private Window window;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;

    [Header("Random Place Settings")]
    [SerializeField] private Transform[] keyPossiblePositions;

    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    public PlayerManager.HandRigTypes HandRigType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");

        int place = Random.Range(0, keyPossiblePositions.Length);
        transform.position = keyPossiblePositions[place].position;
    }

    public void HandleFinishDialogue()
    {

    }

    public void ChangeLayer(int layerIndex)
    {
        gameObject.layer = layerIndex;
    }

    public void OnInteract()
    {
        if (!CanInteract) return;

        meshRenderer.enabled = false;   
        meshCollider.enabled = false;

        foreach (Door door in lockedDoors)
            //door.IsLocked = false;

        window.IsLocked = false;

        Destroy(gameObject, pickUpSound.length);
    }

    public void OnFocus()
    {
        if (!CanInteract) return;

        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
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

    private void OnDestroy()
    {
        PlayerManager.Instance.ResetPlayerGrabAndInteract();
    }
}
