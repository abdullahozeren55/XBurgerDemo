using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public interface IInteractable
{
    public void OnInteract();
    public void OnFocus();
    public void OnLoseFocus();
    public void OutlineChangeCheck();
    public void HandleFinishDialogue();
    public void ChangeLayer(int layer);
    public PlayerManager.HandRigTypes HandRigType {  get; set; }

    public bool OutlineShouldBeRed {  get; set; }
    public bool CanInteract {  get; set; }

    public string FocusText { get; set; }

}
