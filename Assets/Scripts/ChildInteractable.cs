using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChildInteractable : MonoBehaviour, IInteractable
{
    public bool CanInteract { get => parent.CanInteract; set => parent.CanInteract = value; }

    private IInteractable parent;

    public PlayerManager.HandRigTypes HandRigType { get => parent.HandRigType; set => parent.HandRigType = value; }

    public bool OutlineShouldBeRed { get => parent.OutlineShouldBeRed; set => parent.OutlineShouldBeRed = value; }
    public string FocusText { get => parent.FocusText; set => parent.FocusText = value; }

    private void Awake()
    {
        parent = transform.parent.GetComponent<IInteractable>();
    }

    public void ChangeLayer(int layerIndex)
    {
        parent.ChangeLayer(layerIndex);
    }

    public void OnFocus()
    {
        if (!CanInteract) return;

        parent.OnFocus();
    }

    public void OnInteract()
    {
        if (!CanInteract) return;

        parent.OnInteract();

        PlayerManager.Instance.TryChangingFocusText(this, FocusText);
    }

    public void OnLoseFocus()
    {
        if (!CanInteract) return;

        parent.OnLoseFocus();
    }

    public void OutlineChangeCheck()
    {
        parent.OutlineChangeCheck();
    }

    public void HandleFinishDialogue()
    {
        parent.HandleFinishDialogue();
    }
}
