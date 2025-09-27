using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IInteractable
{
    public void OnInteract();
    public void OnFocus();
    public void OnLoseFocus();
    public void OutlineChangeCheck();
    public GameManager.HandRigTypes HandRigType {  get; set; }

    public bool OutlineShouldBeRed {  get; set; }

    public Image FocusImage { get; set; }

}
