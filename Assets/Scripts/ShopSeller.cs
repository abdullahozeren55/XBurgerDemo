using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CameraManager;

public class ShopSeller : MonoBehaviour, IInteractable
{
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;

    [Header("Block Settings")]
    [SerializeField] private Door[] storeDoors;
    [SerializeField] private GameObject storeExitBlocker;

    [Header("Dialogue Settings")]
    [SerializeField] private DialogueData day1BeforeNoodleDialogue;

    private DialogueData beforeNoodleDialogue;
    private DialogueData afterNoodleDialogue;
    public string FocusText { get => focusTexts[doorStateNum]; set => focusTexts[doorStateNum] = value; }
    [SerializeField] private string[] focusTexts;

    private int doorStateNum = 0;
    public PlayerManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;

    private SkinnedMeshRenderer skinnedMeshRenderer;

    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    private void Awake()
    {
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        //FOR TEST (TODO: REMOVE)
        beforeNoodleDialogue = day1BeforeNoodleDialogue;
    }

    public void HandleFinishDialogue()
    {

        NoodleManager.Instance.SetCurrentNoodleStatus(NoodleManager.NoodleStatus.JustBought);

        storeExitBlocker.SetActive(false);

        foreach (Door door in storeDoors)
        {
            door.SetLayerUninteractable(false);
        }
    }

    public void OnInteract()
    {
        if (!CanInteract) return;
        //FOR TEST (TODO: REMOVE)
        DialogueManager.Instance.StartSellerDialogue(beforeNoodleDialogue, true);
    }

    public void OnFocus()
    {
        if (!CanInteract) return;

        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }

    public void OnLoseFocus()
    {
        if (!CanInteract) return;

        ChangeLayer(interactableLayer);
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
            ChangeLayer(interactableOutlinedRedLayer);
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
            ChangeLayer(interactableOutlinedLayer);
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
        skinnedMeshRenderer.gameObject.layer = layer;
    }
}
