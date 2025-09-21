using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ToiletChild : MonoBehaviour, IInteractable
{
    private Toilet parentToilet;

    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private GameManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    private void Awake()
    {
        parentToilet = GetComponentInParent<Toilet>();
    }

    public void OnFocus()
    {
        parentToilet.OnFocus();
    }

    public void OnInteract()
    {
        parentToilet.OnInteract();
    }

    public void OnLoseFocus()
    {
        parentToilet.OnLoseFocus();
    }

    public void OutlineChangeCheck()
    {
        parentToilet.OutlineChangeCheck();
    }
}
