using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CameraManager;

public class ShopSeller : MonoBehaviour, IInteractable
{
    public enum ShopSellerStatus
    {
        None,
        Annoyed,
        NoodleObtained
    }

    public ShopSellerStatus CurrentStatus;
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;

    [Header("Block Settings")]
    [SerializeField] private Door[] storeDoors;
    [SerializeField] private GameObject storeExitBlocker;

    [Header("Dialogue Settings")]
    [SerializeField] private DialogueData day1BeforeNoodleDialogue;
    [SerializeField] private DialogueData day1AfterAnnoyedDialogue;
    [SerializeField] private DialogueData day1AfterNoodleDialogue;
    public string FocusTextKey { get => focusTextKeys[stateNum]; set => focusTextKeys[stateNum] = value; }
    [SerializeField] private string[] focusTextKeys;

    private int stateNum = 0;
    public PlayerManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;

    private SkinnedMeshRenderer skinnedMeshRenderer;
    private Animator anim;

    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    private void Awake()
    {
        skinnedMeshRenderer = GetComponentInChildren<SkinnedMeshRenderer>();
        anim = GetComponent<Animator>();

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        CurrentStatus = ShopSellerStatus.None;
    }

    public void HandleFinishDialogue()
    {
        if (CurrentStatus == ShopSellerStatus.None)
        {
            CurrentStatus = ShopSellerStatus.Annoyed;
        }
        else if (CurrentStatus == ShopSellerStatus.NoodleObtained)
        {
            NoodleManager.Instance.SetCurrentNoodleStatus(NoodleManager.NoodleStatus.JustBought);

            storeExitBlocker.SetActive(false);

            foreach (Door door in storeDoors)
            {
                door.SetLayerUninteractable(false);
            }
        }
        
    }

    public void OnInteract()
    {
        if (!CanInteract) return;

        if (CurrentStatus == ShopSellerStatus.None)
            DialogueManager.Instance.StartSellerDialogue(day1BeforeNoodleDialogue, false);
        else if (CurrentStatus == ShopSellerStatus.Annoyed)
            DialogueManager.Instance.StartSelfDialogue(day1AfterAnnoyedDialogue);
        else if (CurrentStatus == ShopSellerStatus.NoodleObtained)
            DialogueManager.Instance.StartSellerDialogue(day1AfterNoodleDialogue, true);
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

    public void HandleDialogueAnim(ICustomer.DialogueAnim dialogueAnim)
    {
        if (dialogueAnim == ICustomer.DialogueAnim.TURNHEADTOSIDE)
            anim.SetBool("sitTurnHead", true);
        else if (dialogueAnim == ICustomer.DialogueAnim.TURNHEADBACKTONORMAL)
            anim.SetBool("sitTurnHead", false);

    }
}
