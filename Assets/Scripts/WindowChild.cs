using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WindowChild : MonoBehaviour, IInteractable
{
    private Window parentWindow;

    public GameManager.HandRigTypes HandRigType { get => parentWindow.HandRigType; set => parentWindow.HandRigType = value; }

    public bool OutlineShouldBeRed { get => parentWindow.OutlineShouldBeRed; set => parentWindow.OutlineShouldBeRed = value; }
    public Sprite FocusImage { get => parentWindow.FocusImage; set => parentWindow.FocusImage = value; }

    private void Awake()
    {
        parentWindow = GetComponentInParent<Window>();
    }

    public void OnFocus()
    {
        parentWindow.OnFocus();
    }

    public void OnInteract()
    {
        parentWindow.OnInteract();
    }

    public void OnLoseFocus()
    {
        parentWindow.OnLoseFocus();
    }

    public void OutlineChangeCheck()
    {
        parentWindow.OutlineChangeCheck();
    }
}
