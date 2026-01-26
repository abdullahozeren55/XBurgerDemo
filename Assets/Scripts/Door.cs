using DG.Tweening;
using System.Collections;
using UnityEngine;
using UnityEngine.AI; // NavMeshObstacle için gerekli

public class Door : MonoBehaviour, IInteractable
{
    // ... (Eski deðiþkenler aynen kalýyor) ...
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;
    public DoorData data;

    public enum DoorState { Normal, Locked, Jumpscare }
    public DoorState doorState = DoorState.Normal;

    [Header("NPC & NavMesh Settings")]
    [SerializeField] private NavMeshObstacle navObstacle; // Inspector'dan ata
    [SerializeField] private bool autoCloseForNPC = true;
    [SerializeField] private float autoCloseDelay = 3f;

    // ... (Jumpscare vs deðiþkenleri aynen kalsýn) ...
    [Header("Settings")]
    [SerializeField] private GameObject jumpscareGO;
    public bool shouldBeUninteractableAfterInteraction;
    public bool shouldPlayDialogueAfterInteraction;
    public DialogueData dialogueAfterInteraction;
    private bool isDialoguePlayed = false;
    private bool isLockedAnimating = false;

    public string FocusTextKey { get => data.focusTextKeys[doorStateNum]; set => data.focusTextKeys[doorStateNum] = value; }
    private int doorStateNum = 0;
    public PlayerManager.HandRigTypes HandRigType { get => data.handRigType; set => data.handRigType = value; }
    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;

    public bool isOpened;
    private Vector3 closeEuler;
    private Vector3 openEuler;

    // Layers
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    private Coroutine autoCloseCoroutine;

    private void Awake()
    {
        // Parent üzerinden dönüyor, koruyoruz
        closeEuler = transform.parent.localRotation.eulerAngles;
        openEuler = new Vector3(closeEuler.x, closeEuler.y + data.openYRotation, closeEuler.z);

        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");

        // Baþlangýçta obstacle ayarý
        if (navObstacle != null)
        {
            navObstacle.carving = true; // Kapalýyken delik açsýn
        }
    }

    // --- OYUNCU ÝÇÝN (E Tuþu) ---
    public void OnInteract()
    {
        if (!CanInteract) return;
        ToggleDoor(true); // Player açtý
    }

    // --- NPC ÝÇÝN (Müþteri) ---
    public void OpenByNPC()
    {
        if (isOpened) return; // Zaten açýksa elleme
        ToggleDoor(false); // NPC açtý, ses/efekt farký olabilir diye ayýrdým

        // Otomatik kapatma
        if (autoCloseForNPC)
        {
            if (autoCloseCoroutine != null) StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = StartCoroutine(AutoCloseRoutine());
        }
    }

    private void ToggleDoor(bool isPlayerAction)
    {
        switch (doorState)
        {
            case DoorState.Normal:
                HandleRotation(isPlayerAction);
                break;
            case DoorState.Locked:
                HandleLocked(); // Kilitliyse NPC de açamaz
                break;
            case DoorState.Jumpscare:
                HandleJumpscare();
                break;
        }
    }

    public void HandleRotation(bool isPlayerAction)
    {
        isOpened = !isOpened;

        // --- NAVMESH UPDATE ---
        if (navObstacle != null)
        {
            // Eðer açýldýysa carving kapat (geçilebilir olsun)
            // Eðer kapandýysa carving aç (duvar olsun)
            navObstacle.carving = !isOpened;
        }
        // ----------------------

        SoundManager.Instance.PlaySoundFX(isOpened ? data.openSound : data.closeSound, transform, data.openVolume, 1f, 1f, true, data.audioTag);

        doorStateNum = isOpened ? 1 : 0;
        if (PlayerManager.Instance != null) PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);

        transform.parent.DOKill();
        transform.parent.DOLocalRotate(isOpened ? openEuler : closeEuler, data.timeToRotate)
            .SetEase(Ease.InOutSine);
    }

    private IEnumerator AutoCloseRoutine()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        if (isOpened)
        {
            // Kapýyý kapat
            HandleRotation(false);
        }
    }

    // ... (HandleLocked, HandleJumpscare, OnFocus, OnLoseFocus AYNEN KALSIN) ...
    // Sadece HandleJumpscare içinde de navObstacle.carving = false; yapmayý unutma ki kapý uçunca geçilebilsin.

    private void HandleLocked()
    {
        if (isLockedAnimating) return;
        isLockedAnimating = true;

        SoundManager.Instance.PlaySoundFX(data.lockedSound, transform, data.lockedVolume, 1f, 1f);

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

        if (navObstacle != null) navObstacle.carving = false; // Geçilebilir yap

        SoundManager.Instance.PlaySoundFX(isOpened ? data.openSound : data.closeSound, transform, data.openVolume, 1f, 1f);

        Vector3 startPos = jumpscareGO.transform.position;
        Vector3 targetPos = startPos + transform.TransformDirection(data.jumpscareMoveAmount);

        Tween doorTween = transform.parent.DOLocalRotate(openEuler, data.timeToRotate).SetEase(Ease.InOutSine);
        Tween jumpscareTween = jumpscareGO.transform.DOMove(targetPos, data.timeToJumpscare).SetEase(Ease.OutBack);

        Sequence seq = DOTween.Sequence();
        seq.Append(doorTween);
        seq.Insert(data.timeToRotate * data.jumpscareDoorRotatePercentValue, jumpscareTween);
        seq.InsertCallback(data.timeToRotate * data.jumpscareSoundEffectPercentValue, () =>
        {
            SoundManager.Instance.PlaySoundFX(data.jumpscareSound, transform, data.jumpscareVolume, 1f, 1f);
        });
        seq.InsertCallback(data.timeToRotate * data.jumpscareEffectPercentValue, () =>
        {
            CameraManager.Instance.PlayJumpscareEffects(data.jumpscareType);
        });
        seq.OnComplete(() =>
        {
            doorState = DoorState.Normal;
        });
    }

    // ... (Interface implementation'lar aynen kalýyor) ...
    public void ChangeLayer(int layerIndex) { gameObject.layer = layerIndex; }
    public void HandleFinishDialogue() { }
    public void OnFocus() { if (!CanInteract) return; ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer); }
    public void OnLoseFocus() { if (!CanInteract) return; ChangeLayer(interactableLayer); }
    public void OutlineChangeCheck() { if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed) ChangeLayer(interactableOutlinedRedLayer); else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed) ChangeLayer(interactableOutlinedLayer); }
    public void SetLayerUninteractable(bool should) { ChangeLayer(should ? uninteractableLayer : interactableLayer); }
}