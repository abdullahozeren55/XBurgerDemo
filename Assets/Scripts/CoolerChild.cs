using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoolerChild : MonoBehaviour, IInteractable
{
    private Cooler parentCooler;

    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    [SerializeField] private GameManager.HandRigTypes handRigType;

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
