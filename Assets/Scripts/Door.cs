using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CameraManager;

public class Door : MonoBehaviour, IInteractable
{
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;

    public DoorData data;

    public enum DoorState
    {
        Normal,
        Locked,
        Jumpscare
    }

    public DoorState doorState = DoorState.Normal;

    [Header("Settings")] 
    [SerializeField] private GameObject jumpscareGO;
    public bool shouldBeUninteractableAfterInteraction;
    public bool shouldPlayDialogueAfterInteraction;
    [Space]
    public DialogueData dialogueAfterInteraction;
    private bool isDialoguePlayed = false;
    private bool isLockedAnimating = false;


    [Header("Audio")]
    [SerializeField] private AudioSource jumpscareAudioSource;
    
    public string FocusTextKey { get => data.focusTextKeys[doorStateNum]; set => data.focusTextKeys[doorStateNum] = value; }
    private int doorStateNum = 0;
    public PlayerManager.HandRigTypes HandRigType { get => data.handRigType; set => data.handRigType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;

    public bool isOpened;
    private Vector3 closeEuler;
    private Vector3 openEuler;

    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    private void Awake()
    {

        closeEuler = transform.parent.localRotation.eulerAngles;
        openEuler = new Vector3(closeEuler.x, closeEuler.y + data.openYRotation, closeEuler.z);

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");
    }

    public void ChangeLayer(int layerIndex)
    {
        gameObject.layer = layerIndex;
    }

    public void HandleFinishDialogue()
    {
    }

    public void OnInteract()
    {
        if (!CanInteract) return;

        switch (doorState)
        {
            case DoorState.Normal:
                HandleRotation();
                break;

            case DoorState.Locked:
                HandleLocked();
                break;

            case DoorState.Jumpscare:
                HandleJumpscare();
                break;
        }

        if (shouldPlayDialogueAfterInteraction && !isDialoguePlayed)
        {
            DialogueManager.Instance.StartAfterInteractionSelfDialogue(this, shouldBeUninteractableAfterInteraction, dialogueAfterInteraction);
            isDialoguePlayed = true;
        }
    }

    public void HandleRotation()
    {
        isOpened = !isOpened;
        SoundManager.Instance.PlaySoundFX(isOpened ? data.openSound : data.closeSound, transform);

        doorStateNum = isOpened ? 1 : 0;

        PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);

        transform.parent.DOKill();
        transform.parent.DOLocalRotate(isOpened ? openEuler : closeEuler, data.timeToRotate)
            .SetEase(Ease.InOutSine);
    }

    private void HandleLocked()
    {
        if (isLockedAnimating) return;
        isLockedAnimating = true;

        SoundManager.Instance.PlaySoundFX(data.lockedSound, transform);

        transform.parent.DOKill();
        transform.parent.localRotation = Quaternion.Euler(closeEuler);

        Sequence seq = DOTween.Sequence();
        seq.Append(transform.parent.DOLocalRotate(new Vector3(closeEuler.x, closeEuler.y + data.lockShakeStrength, closeEuler.z), 0.1f))
           .Append(transform.parent.DOLocalRotate(closeEuler, 0.1f))
           .Append(transform.parent.DOLocalRotate(new Vector3(closeEuler.x, closeEuler.y + data.lockShakeStrength * 0.5f, closeEuler.z), 0.1f))
           .Append(transform.parent.DOLocalRotate(closeEuler, 0.1f))
           .OnComplete(() =>
           {
               doorStateNum = 2;

               PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);

               isLockedAnimating = false;
           });
    }

    private void HandleJumpscare()
    {

        ChangeLayer(uninteractableLayer);
        isOpened = true;

        SoundManager.Instance.PlaySoundFX(isOpened ? data.openSound : data.closeSound, transform);

        // Baþlangýç pozisyonu
        Vector3 startPos = jumpscareGO.transform.position;
        Vector3 targetPos = startPos + transform.TransformDirection(data.jumpscareMoveAmount);

        // Kapý açýlma tweener'ý
        Tween doorTween = transform.parent.DOLocalRotate(openEuler, data.timeToRotate)
            .SetEase(Ease.InOutSine);

        // Jumpscare tweener'ý
        Tween jumpscareTween = jumpscareGO.transform.DOMove(targetPos, data.timeToJumpscare)
            .SetEase(Ease.OutBack);

        // Sequence ile birleþtirme
        Sequence seq = DOTween.Sequence();
        seq.Append(doorTween);

        seq.Insert(data.timeToRotate * data.jumpscareDoorRotatePercentValue, jumpscareTween); // Kapý animasyonunun %x'inde baþlasýn

        seq.InsertCallback(data.timeToRotate * data.jumpscareSoundEffectPercentValue, () =>
        {
            jumpscareAudioSource.PlayOneShot(data.jumpscareSound);
        });

        seq.InsertCallback(data.timeToRotate * data.jumpscareEffectPercentValue, () =>
        {
            CameraManager.Instance.PlayJumpscareEffects(data.jumpscareType);
        });

        // Bitince state normal'e dönsün
        seq.OnComplete(() =>
        {
            doorState = DoorState.Normal;
        });
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

    public void SetLayerUninteractable(bool should)
    {
        ChangeLayer(should ? uninteractableLayer : interactableLayer);
    }
}
