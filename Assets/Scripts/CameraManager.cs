using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [SerializeField] private CursorFollow cursorFollow;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private Monitor monitor;
    [Space]
    [SerializeField] private CinemachineVirtualCamera firstPersonCam;
    [SerializeField] private CinemachineVirtualCamera customerCam;
    [SerializeField] private CinemachineVirtualCamera monitorCam;
    [SerializeField] private CinemachineVirtualCamera cutsceneDollyCam;
    [SerializeField] private CinemachineVirtualCamera phoneCam;

    private FirstPersonController firstPersonController;
    private KeyCode interactKey;
    private bool isFocusedOnMonitor;
    private bool isFocused;

    private CinemachineVirtualCamera currentCam;
    private int basePriority = 10;

    private void Awake()
    {
        if (Instance == null)
        {
            // If not, set this instance as the singleton
            Instance = this;

            // Optionally, mark GameManager as not destroyed between scene loads
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If an instance already exists, destroy this one to enforce the singleton pattern
            Destroy(gameObject);
        }

        firstPersonController = FindFirstObjectByType<FirstPersonController>();
        interactKey = firstPersonController.throwKey;

        isFocusedOnMonitor = false;
        isFocused = false;
    }

    private void Update()
    {
        if ((isFocusedOnMonitor || isFocused) && !DialogueManager.Instance.IsInDialogue)
        {
            CheckDefocus();
        }
    }

    public void SwitchToCamera(CinemachineVirtualCamera targetCam)
    {
        if (targetCam == null || targetCam == currentCam)
            return;

        // Lower priority of current camera
        if (currentCam != null)
            currentCam.Priority = basePriority;

        // Raise priority of target camera
        targetCam.Priority = basePriority + 1;
        currentCam = targetCam;
    }

    // Optional: Use this to get current camera
    public CinemachineVirtualCamera GetCurrentCamera()
    {
        return currentCam;
    }

    public void SwitchToFirstPersonCamera()
    {
        SwitchToCamera(firstPersonCam);
    }

    public void SwitchToCustomerCamera()
    {
        SwitchToCamera(customerCam);
    }

    private void CheckDefocus()
    {
        if (Input.GetKeyDown(interactKey))
        {
            if (isFocused)
                DefocusCameraForPhone();
            else
                DefocusCameraForMonitor();
            
        }
    }

    public void FocusCameraForMonitor()
    {
        if (firstPersonController.CanMove) firstPersonController.CanMove = false;
        crosshair.SetActive(false);

        PhoneManager.Instance.CanUsePhone = false;

        SwitchToCamera(monitorCam);

        Invoke("FinishMonitorFocus", 0.5f);
    }

    private void FinishMonitorFocus()
    {
        isFocusedOnMonitor = true;

        cursorFollow.StartCursorFollow();

        monitor.defocusText.SetActive(true);
    }

    private void DefocusCameraForMonitor()
    {
        cursorFollow.EndCursorFollow();
        monitor.defocusText.SetActive(false);
        SwitchToCamera(firstPersonCam);

        if (!firstPersonController.CanMove) firstPersonController.CanMove = true;
        crosshair.SetActive(true);

        PhoneManager.Instance.CanUsePhone = true;

        isFocusedOnMonitor = false;
    }

    private void FinishMonitorDefocus()
    {
        crosshair.SetActive(true);
        monitor.ChangeLayerToInteractable();

        if (!firstPersonController.CanMove) firstPersonController.CanMove = true;
        PhoneManager.Instance.CanUsePhone = true;

        isFocusedOnMonitor = false;
    }

    public void FocusCameraForPhone(Camera camera)
    {
        if (firstPersonController.CanMove) firstPersonController.CanMove = false;

        PhoneManager.Instance.CanUsePhone = false;

        SwitchToCamera(phoneCam);

        Invoke("FinishPhoneFocus", 1f);
    }

    private void FinishPhoneFocus()
    {
        isFocused = true;
    }

    private void DefocusCameraForPhone()
    {
        SwitchToCamera(firstPersonCam);

        if (!firstPersonController.CanMove) firstPersonController.CanMove = true;
        PhoneManager.Instance.CanUsePhone = true;

        isFocused = false;
    }
}
