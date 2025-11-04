using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BroomHolder : MonoBehaviour, IInteractable
{
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;
    public string FocusText { get => focusText; set => focusText = value; }
    [SerializeField] private string focusText;
    [Space]

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;

    [Header("Broom Settings")]
    [SerializeField] private GameObject broom;
    [SerializeField] private Transform pointToSpawnKnife;

    public PlayerManager.HandRigTypes HandRigType { get => throw new System.NotImplementedException(); set => throw new System.NotImplementedException(); }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    private void Awake()
    {
        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
    }

    public void HandleFinishDialogue()
    {

    }

    public void ChangeLayer(int layerIndex)
    {
        gameObject.layer = layerIndex;
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

    public void OnInteract()
    {
        if (!CanInteract) return;

        GameObject instantiatedKnife = Instantiate(broom, pointToSpawnKnife.position, Quaternion.Euler(-135f, 0f, 0f), null);
        PlayerManager.Instance.ResetPlayerGrabAndInteract();
        PlayerManager.Instance.ChangePlayerCurrentGrabable(instantiatedKnife.GetComponent<IGrabable>());
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
}
