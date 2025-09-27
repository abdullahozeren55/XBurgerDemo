using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CoolerChild : MonoBehaviour, IInteractable
{
    private Cooler parentCooler;

    public GameManager.HandRigTypes HandRigType { get => parentCooler.HandRigType; set => parentCooler.HandRigType = value; }
    public bool OutlineShouldBeRed { get => parentCooler.OutlineShouldBeRed; set => parentCooler.OutlineShouldBeRed = value; }
    public Image FocusImage { get => parentCooler.FocusImage; set => parentCooler.FocusImage = value; }

    private void Awake()
    {
        parentCooler = GetComponentInParent<Cooler>();
    }

    public void OnFocus()
    {
        parentCooler.OnFocus();
    }

    public void OnInteract()
    {
        parentCooler.OnInteract();
    }

    public void OnLoseFocus()
    {
        parentCooler.OnLoseFocus();
    }

    public void OutlineChangeCheck()
    {
        parentCooler.OutlineChangeCheck();
    }
}
