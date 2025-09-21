using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SupplyBox : MonoBehaviour, IInteractable
{
    public GameManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private GameManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    [Header("Text Settings")]
    public GameObject grabSupplyText;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;

    [Header("Supply Settings")]
    [SerializeField] private GameObject supply;
    [SerializeField] private Transform pointToSpawn;

    private void Awake()
    {
        int childCount = transform.childCount;

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
    }

    public void OnFocus()
    {
        grabSupplyText.SetActive(true);
        gameObject.layer = OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer;
    }

    public void OnLoseFocus()
    {
        grabSupplyText.SetActive(false);
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

    public void OnInteract()
    {
        GameObject instantiatedSupply = Instantiate(supply, pointToSpawn.position, Quaternion.Euler(0f, -90f, 180f), null);
        GameManager.Instance.ResetPlayerGrabAndInteract();
        GameManager.Instance.ChangePlayerCurrentGrabable(instantiatedSupply.GetComponent<IGrabable>());
    }
}
