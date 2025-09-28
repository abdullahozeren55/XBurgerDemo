using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
    
    public Sprite FocusImage { get => data.focusImages[doorStateNum]; set => data.focusImages[doorStateNum] = value; }
    [HideInInspector] public int doorStateNum = 0;
    public GameManager.HandRigTypes HandRigType { get => data.handRigType; set => data.handRigType = value; }

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;

    [HideInInspector] public bool isOpened;
    private Vector3 closeEuler;
    private Vector3 openEuler;
    private Collider col;

    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    private void Awake()
    {
        isOpened = false;
        col = GetComponent<Collider>();

        closeEuler = transform.parent.localRotation.eulerAngles;
        openEuler = new Vector3(closeEuler.x, closeEuler.y + data.openYRotation, closeEuler.z);

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");
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
            Invoke("PlayDialogue", data.dialoguePlayDelay);
            isDialoguePlayed = true;
        }
    }

    private void PlayDialogue()
    {
        DialogueManager.Instance.StartSelfDialogue(data.dialogueAfterInteraction);
    }

    public void HandleRotation()
    {
        isOpened = !isOpened;
        PlaySound(isOpened);

        transform.parent.DOKill();
        transform.parent.DOLocalRotate(isOpened ? openEuler : closeEuler, data.timeToRotate)
            .SetEase(Ease.InOutSine);
    }

    private void HandleLocked()
    {
        if (isLockedAnimating) return;
        isLockedAnimating = true;

        gameObject.layer = uninteractableLayer;
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
               gameObject.layer = interactableLayer;
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

        seq.InsertCallback(data.jumpscareAudioDelay, () => jumpscareAudioSource.PlayOneShot(data.jumpscareSound));

        // Bitince state normal'e dönsün
        seq.OnComplete(() =>
        {
            doorState = DoorState.Normal;
            gameObject.layer = interactableLayer;
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
}
