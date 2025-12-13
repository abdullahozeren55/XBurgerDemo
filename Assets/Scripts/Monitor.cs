using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Monitor : MonoBehaviour, IInteractable
{
    public PlayerManager.HandRigTypes HandRigType { get => hangRigType; set => hangRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes hangRigType;
    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    private bool outlineShouldBeRed;
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;
    public string FocusTextKey { get => focusTextKey; set => focusTextKey = value; }
    [SerializeField] private string focusTextKey;

    [Header("UI Settings")]
    public GameObject monitorUI;
    public RectTransform monitorScaler;
    public float monitorUIStartAppearDelay = 0.2f;
    public float monitorUIFinishDelay = 0.2f;
    public float monitorUILerpTime = 0.2f;
    public float monitorUIReverseLerpTime = 0.2f;
    public Vector3 monitorUImin = new Vector3(0.6f, 0.6f, 0.6f);

    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int uninteractableLayer;

    private Tween monitorUITween;

    private void Awake()
    {
        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        uninteractableLayer = LayerMask.NameToLayer("Uninteractable");
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
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

        PlayerManager.Instance.SetPlayerBasicMovements(false);
        CameraManager.Instance.SwitchToCamera(CameraManager.CameraName.Monitor);

        Invoke("HandleMonitorUI", monitorUIStartAppearDelay);
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

    private void HandleMonitorUI()
    {
        monitorUI.SetActive(true);

        MonitorManager.Instance.IsFocused = true;

        monitorUITween?.Kill();

        monitorUITween = monitorScaler.DOScale(Vector3.one, monitorUILerpTime)
        .SetEase(Ease.OutBack, 3.5f)
        .SetUpdate(true)
        .OnComplete(() =>
        {
            GameManager.Instance.SetCursor(GameManager.CursorType.Retro);
            GameManager.Instance.SetCursorLock(false);
        });
    }

    public void FinishMonitorUI()
    {
        PlayerManager.Instance.SetPlayerBasicMovements(true);
        CameraManager.Instance.SwitchToCamera(CameraManager.CameraName.FirstPerson);

        GameManager.Instance.SetCursor(GameManager.CursorType.Default);
        GameManager.Instance.SetCursorLock(true);

        monitorUITween?.Kill();

        monitorUITween = monitorScaler.DOScale(monitorUImin, monitorUIReverseLerpTime)
        .SetEase(Ease.InQuad)
        .SetUpdate(true);

        Invoke("FinishMonitorUIP2", monitorUIFinishDelay);
    }

    private void FinishMonitorUIP2()
    {
        monitorUI.SetActive(false);
        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }
}