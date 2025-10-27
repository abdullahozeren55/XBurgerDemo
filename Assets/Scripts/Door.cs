using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static CameraManager;

public class Door : MonoBehaviour, IInteractable
{
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
    public bool shouldPlayDialogueAfterInteraction;
    private bool isDialoguePlayed = false;
    private bool isLockedAnimating = false;


    [Header("Audio")]
    [SerializeField] private AudioSource doorAudioSource;
    [SerializeField] private AudioSource jumpscareAudioSource;
    
    public string FocusText { get => data.focusTexts[doorStateNum]; set => data.focusTexts[doorStateNum] = value; }
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

    public void HandleFinishDialogue()
    {
        gameObject.layer = interactableLayer;
    }

    public void OnInteract()
    {
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
            StartCoroutine(PlayDialogueWithDelay(data.dialoguePlayDelay));
            isDialoguePlayed = true;
        }
    }

    private IEnumerator PlayDialogueWithDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DialogueManager.Instance.StartAfterInteractionSelfDialogue(this, data.dialogueAfterInteraction);
    }

    public void HandleRotation()
    {
        isOpened = !isOpened;
        PlaySound(isOpened);

        doorStateNum = isOpened ? 1 : 0;

        PlayerManager.Instance.TryChangingFocusText(this, FocusText);

        transform.parent.DOKill();
        transform.parent.DOLocalRotate(isOpened ? openEuler : closeEuler, data.timeToRotate)
            .SetEase(Ease.InOutSine);
    }

    private void HandleLocked()
    {
        if (isLockedAnimating) return;
        isLockedAnimating = true;

        doorAudioSource.PlayOneShot(data.lockedSound);

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

               PlayerManager.Instance.TryChangingFocusText(this, FocusText);

               isLockedAnimating = false;
           });
    }

    private void HandleJumpscare()
    {

        gameObject.layer = uninteractableLayer;
        isOpened = true;

        PlaySound(isOpened);

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

    private void PlaySound(bool opened)
    {
        doorAudioSource.Stop();
        doorAudioSource.PlayOneShot(opened ? data.openSound : data.closeSound);
    }

    public void OnFocus()
    {
        gameObject.layer = OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer;
    }

    public void OnLoseFocus()
    {
        gameObject.layer = interactableLayer;
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
            gameObject.layer = interactableOutlinedRedLayer;
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
            gameObject.layer = interactableOutlinedLayer;
    }

    public void SetLayerUninteractable(bool should)
    {
        gameObject.layer = should ? uninteractableLayer : interactableLayer;
    }
}
