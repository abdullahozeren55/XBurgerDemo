using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindowChild : MonoBehaviour, IInteractable
{
    private Window parentWindow;

    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private GameManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

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
