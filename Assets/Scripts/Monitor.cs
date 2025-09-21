using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monitor : MonoBehaviour, IInteractable
{

    [Header("Text Settings")]
    public GameObject focusText;
    public GameObject defocusText;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    private bool isLerped;

    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private GameManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    private void Awake()
    {
        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        isLerped = false;
    }

    public void OnFocus()
    {
        focusText.SetActive(true);
        gameObject.layer = OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer;
    }

    public void OnInteract()
    {
        gameObject.layer = uninteractableLayer;

        isLerped = !isLerped;
        CameraManager.Instance.FocusCameraForMonitor();
    }

    public void OnLoseFocus()
    {
        focusText.SetActive(false);
        gameObject.layer = interactableLayer;
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
        {
            gameObject.layer = interactableOutlinedRedLayer;
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            gameObject.layer = interactableOutlinedLayer;
        }
    }

    public void ChangeLayerToInteractable()
    {
        gameObject.layer = interactableLayer;
    }
}
