using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [Header("UI Stuff")]
    public bool CanPause = false;
    public GameObject mainMenu;
    public GameObject pauseMenu;

    private Canvas myCanvas;

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

        myCanvas = GetComponentInChildren<Canvas>();

        HandleCursorState(true);
    }

    private void Update()
    {
        if (CanPause && Input.GetKeyDown(KeyCode.Escape))
        {
            HandlePauseMenu(!pauseMenu.activeSelf);
            HandleTimeScale(pauseMenu.activeSelf ? 0f : 1f);
            HandleCursorState(pauseMenu.activeSelf);
            SetPlayerCanPlay(!pauseMenu.activeSelf);
        }
    }

    public void HandleMainMenu(bool shouldTurnOn) => mainMenu.SetActive(shouldTurnOn);
    public void HandlePauseMenu(bool shouldTurnOn) => pauseMenu.SetActive(shouldTurnOn);
    public void HandleTimeScale(float timeScale) => Time.timeScale = timeScale;
    public void SetCanPause(bool pause) => CanPause = pause;
    public void SetPlayerCanPlay(bool can) => PlayerManager.Instance.SetPlayerBasicMovements(can);

    public void HandleCursorState(bool shouldBeFree)
    {
        Cursor.lockState = shouldBeFree ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = shouldBeFree;
    }

    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    public void QuitGame()
    {
        Debug.Log("QUIT");
        Application.Quit();
    }

    private void OnEnable()
    {
        // Sahne yüklendiðinde tetiklenecek eventi dinle
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // Sahne her deðiþtiðinde çalýþýr
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndAssignCamera();
    }

    void FindAndAssignCamera()
    {
        // 1. Sahnedeki "UI_Camera" etiketli veya isimli kamerayý bul
        // (Sahne 2'deki kameranýn adý "UI_Camera" olmalý!)
        GameObject camObj = GameObject.Find("UI_Camera");

        if (camObj != null)
        {
            Camera uiCam = camObj.GetComponent<Camera>();
            Camera mainCam = Camera.main; // O anki sahnenin ana kamerasý

            // A. Canvas'a kamerayý ver
            if (myCanvas != null)
            {
                myCanvas.worldCamera = uiCam;

                // Plane Distance ayarý bozulabilir, onu da sabitle
                myCanvas.planeDistance = 5;
            }

            // B. (ÖNEMLÝ) Main Camera'nýn Stack'ine bu yeni UI kamerasýný ekle
            // Yoksa Overlay çalýþmaz, sadece siyah ekran görürsün.
            if (mainCam != null)
            {
                var cameraData = mainCam.GetUniversalAdditionalCameraData();

                // Zaten ekli mi diye bak, deðilse ekle
                bool isAlreadyInStack = false;
                foreach (var c in cameraData.cameraStack)
                {
                    if (c == uiCam) isAlreadyInStack = true;
                }

                if (!isAlreadyInStack)
                {
                    cameraData.cameraStack.Add(uiCam);
                }
            }
        }
        else
        {
            Debug.LogError("YENÝ SAHNEDE UI_CAMERA BULUNAMADI! ÝSMÝNÝ KONTROL ET.");
        }
    }
}
