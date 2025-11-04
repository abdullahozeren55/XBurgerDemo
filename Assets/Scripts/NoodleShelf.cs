using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static NoodleManager;

public class NoodleShelf : MonoBehaviour, IInteractable
{
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;

    private GameObject[] childs;

    public string FocusText { get => focusText; set => focusText = value; }
    [SerializeField] private string focusText;
    [Space]

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;

    [Header("Noodle Settings")]
    [SerializeField] private GameObject noodle;
    [SerializeField] private Transform pointToSpawnNoodle;

    [Header("Block Settings")]
    [SerializeField] private Door[] storeDoors;
    [SerializeField] private GameObject storeExitBlocker;

    private GameObject instantiatedNoodle;

    public PlayerManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    private void Awake()
    {

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");

        childs = new GameObject[transform.childCount];

        for (int i = 0; i < childs.Length; i++)
        {
            childs[i] = transform.GetChild(i).gameObject;
        }

        instantiatedNoodle = null;
    }

    public void HandleFinishDialogue()
    {

    }
    public void OnFocus()
    {
        if (!CanInteract) return;

        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }

    public void OnInteract()
    {
        if (!CanInteract) return;

        NoodleManager.NoodleStatus status = new NoodleManager.NoodleStatus();

        if (instantiatedNoodle != null)
        {
            status = NoodleManager.Instance.GetCurrentNoodleStatus();

            Destroy(instantiatedNoodle);
            instantiatedNoodle = null;
        }

        instantiatedNoodle = Instantiate(noodle, pointToSpawnNoodle.position, Quaternion.Euler(0f, 0f, 0f), null);
        NoodleManager.Instance.SetCurrentNoodle(instantiatedNoodle);

        if (status == NoodleManager.NoodleStatus.JustBought)
            NoodleManager.Instance.SetCurrentNoodleStatus(status);
        else
            NoodleManager.Instance.SetCurrentNoodleStatus(NoodleStatus.JustGrabbed);

        PlayerManager.Instance.ResetPlayerGrabAndInteract();
        PlayerManager.Instance.ChangePlayerCurrentGrabable(instantiatedNoodle.GetComponent<IGrabable>());

        storeExitBlocker.SetActive(true);

        foreach (Door door in storeDoors)
        {
            door.SetLayerUninteractable(true);
        }
    }

    public void OnLoseFocus()
    {
        if (!CanInteract) return;

        ChangeLayer(interactableLayer);
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
        {
            ChangeLayer(interactableOutlinedRedLayer);
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            ChangeLayer(interactableOutlinedLayer);
        }
    }

    public void ChangeLayer(int layerIndex)
    {
        gameObject.layer = layerIndex;
        foreach (GameObject child in childs)
            child.layer = layerIndex;
    }
}
