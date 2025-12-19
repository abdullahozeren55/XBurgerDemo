using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [HideInInspector]
    public int GlobalScaleOffset = 0;

    [Header("State")]
    public bool CanPause = false;
    private bool isGamePaused = false;

    [Header("UI References")]
    public Volume globalVolume;
    public GameObject mainMenu;
    public GameObject pauseMenu;
    public GameObject settingsMenu;
    public GameObject raycastBlockerForPause;
    public GameObject rebindBlocker;

    [Header("Rebind Ýpuçlarý")]
    public GameObject rebindHintKeyboard; // Ýçinde "ESC - Ýptal" yazan obje
    public GameObject rebindHintGamepad;  // Ýçinde "Gamepad Ýkonu - Ýptal" yazan obje

    [Header("Settings Sub-Panels")]
    public RectTransform settingsMainRect;
    public RectTransform settingsGeneralRect;
    public RectTransform settingsAudioRect;
    public RectTransform settingsControlsRect;
    public RectTransform settingsControlsKeyboardRect;
    public RectTransform settingsControlsGamepadRect;

    private enum SettingsState
    {
        Main, General, Audio, Controls, Controls_Keyboard, Controls_Gamepad
    }
    private SettingsState currentSettingsState = SettingsState.Main;

    [Header("Animation References")]
    public RectTransform mainMenuRect;
    public RectTransform settingsRect;
    public RectTransform pauseMenuRect; // Pause Menüsü Animasyonu için

    [Header("Animation Settings")]
    public float slideDuration = 0.5f;
    public Ease slideEase = Ease.OutBack;

    [Header("Audio")]
    public AudioClip swingSound;
    public float swingVolume = 1f;
    public float swingMinPitch = 0.8f;
    public float swingMaxPitch = 1.2f;

    private Canvas myCanvas;
    private RectTransform canvasRect;
    private List<PixelPerfectCanvasScaler> activeScalers = new List<PixelPerfectCanvasScaler>();

    private bool isBusy = false;
    private bool isSettingsOpen = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }

        myCanvas = GetComponentInChildren<Canvas>();
        if (myCanvas != null) canvasRect = myCanvas.GetComponent<RectTransform>();

        float width = GetCanvasWidth();
        float height = GetCanvasHeight();

        // Panelleri ýþýnla
        settingsGeneralRect.anchoredPosition = new Vector2(0, height);
        settingsAudioRect.anchoredPosition = new Vector2(0, -height);
        settingsControlsRect.anchoredPosition = new Vector2(width, 0);
        if (settingsControlsKeyboardRect) settingsControlsKeyboardRect.anchoredPosition = new Vector2(0, height);
        if (settingsControlsGamepadRect) settingsControlsGamepadRect.anchoredPosition = new Vector2(0, -height);
        settingsMainRect.anchoredPosition = Vector2.zero;

        GlobalScaleOffset = PlayerPrefs.GetInt("UIScaleOffset", 0);
    }

    private void Start()
    {
        if (mainMenuRect != null && settingsRect != null)
        {
            float width = GetCanvasWidth();

            mainMenu.SetActive(true);
            settingsMenu.SetActive(true);
            pauseMenu.SetActive(false);

            mainMenuRect.gameObject.SetActive(true);
            mainMenuRect.anchoredPosition = Vector2.zero;

            settingsRect.anchoredPosition = new Vector2(width, 0);
            ResetSettingsState();
        }

        isGamePaused = false;
        InputManager.Instance.SwitchToUIMode();

        // --- ÝÞTE EKSÝK OLAN PARÇA ---
        // Oyun ilk açýldýðýnda kimse cursor'ý açmýyordu. Elle açýyoruz.
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCursor(GameManager.CursorType.Default);
            GameManager.Instance.SetCursorLock(false); // Kilit Açýk = Görünür Cursor
        }
    }

    private void Update()
    {
        // 1. OYUN AKARKEN PAUSE
        if (CanPause && !isGamePaused && !isSettingsOpen)
        {
            if (InputManager.Instance.PlayerPause())
            {
                TogglePause();
            }
        }
        // 2. MENÜDEYKEN GERÝ GELME
        else if (isGamePaused || isSettingsOpen)
        {
            if (InputManager.Instance.UICancelTriggered())
            {
                HandleBack();
            }
            else if (isGamePaused && !isSettingsOpen && InputManager.Instance.PlayerPause())
            {
                TogglePause();
            }
        }
    }

    public void TogglePause()
    {
        if (pauseMenuRect) pauseMenuRect.DOKill(true);

        isGamePaused = !isGamePaused;

        if (isGamePaused)
        {
            GameManager.Instance.SaveCursorState();

            GameManager.Instance.SetCursor(GameManager.CursorType.Default);
            GameManager.Instance.SetCursorLock(false);

            pauseMenu.SetActive(true);
            if (pauseMenuRect)
            {
                float height = GetCanvasHeight();
                pauseMenuRect.anchoredPosition = new Vector2(0, height);
                pauseMenuRect.DOAnchorPosY(0, slideDuration).SetEase(slideEase).SetUpdate(true);
            }
        }
        else
        {
            GameManager.Instance.RestoreCursorState();

            if (isSettingsOpen)
            {
                settingsRect.DOKill(true);
                settingsRect.gameObject.SetActive(false);
                isSettingsOpen = false;
                ResetSettingsState();
            }

            if (pauseMenuRect)
            {
                float height = GetCanvasHeight();
                pauseMenuRect.DOAnchorPosY(height, slideDuration)
                    .SetEase(slideEase).SetUpdate(true)
                    .OnComplete(() => pauseMenu.SetActive(false));
            }
            else
            {
                pauseMenu.SetActive(false);
            }
        }

        raycastBlockerForPause.SetActive(isGamePaused);
        UpdateDoFState(isGamePaused);
        HandleTimeScale(isGamePaused ? 0f : 1f);
        SetPlayerCanPlay(!isGamePaused);

        if (isGamePaused) InputManager.Instance.SwitchToUIMode();
        else InputManager.Instance.SwitchToGameplayMode();
    }

    public void OpenSettings()
    {
        if (isBusy) return;
        isBusy = true;
        isSettingsOpen = true;

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);

        float width = GetCanvasWidth();

        settingsRect.DOKill(true);
        settingsRect.gameObject.SetActive(true);
        settingsRect.anchoredPosition = new Vector2(width, 0);

        if (isGamePaused)
        {
            if (pauseMenuRect)
            {
                pauseMenuRect.DOKill(true);
                pauseMenuRect.DOAnchorPosX(-width, slideDuration)
                    .SetEase(slideEase).SetUpdate(true)
                    .OnComplete(() => pauseMenuRect.gameObject.SetActive(false));
            }

            settingsRect.DOAnchorPosX(0, slideDuration)
                .SetEase(slideEase).SetUpdate(true)
                .OnComplete(() => isBusy = false);
        }
        else
        {
            mainMenuRect.DOKill(true);
            mainMenuRect.DOAnchorPosX(-width, slideDuration)
                .SetEase(slideEase).SetUpdate(true)
                .OnComplete(() => mainMenuRect.gameObject.SetActive(false));

            settingsRect.DOAnchorPosX(0, slideDuration)
                .SetEase(slideEase).SetUpdate(true)
                .OnComplete(() => isBusy = false);
        }
    }

    public void CloseSettings()
    {
        isBusy = true;
        isSettingsOpen = false;

        settingsRect.DOKill(true);

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);

        float width = GetCanvasWidth();

        settingsRect.DOAnchorPosX(width, slideDuration)
            .SetEase(slideEase).SetUpdate(true)
            .OnComplete(() =>
            {
                settingsRect.gameObject.SetActive(false);
                ResetSettingsState();
                isBusy = false;
            });

        if (isGamePaused)
        {
            if (pauseMenuRect)
            {
                pauseMenuRect.DOKill(true);
                pauseMenuRect.gameObject.SetActive(true);
                pauseMenuRect.anchoredPosition = new Vector2(-width, 0);
                pauseMenuRect.DOAnchorPosX(0, slideDuration).SetEase(slideEase).SetUpdate(true);
            }
        }
        else
        {
            mainMenuRect.DOKill(true);
            mainMenuRect.gameObject.SetActive(true);
            mainMenuRect.anchoredPosition = new Vector2(-width, 0);
            mainMenuRect.DOAnchorPosX(0, slideDuration).SetEase(slideEase).SetUpdate(true);
        }
    }

    public void HandleBack()
    {
        // Eðer tuþ atama ekraný (Blocker) açýksa geri dönmeyi engelle
        if (rebindBlocker != null && rebindBlocker.activeSelf) return;

        if (isSettingsOpen)
        {
            switch (currentSettingsState)
            {
                case SettingsState.General: CloseGeneralSettings(); break;
                case SettingsState.Audio: CloseAudioSettings(); break;
                case SettingsState.Controls: CloseControlsSettings(); break;
                case SettingsState.Controls_Keyboard: CloseControlsKeyboard(); break;
                case SettingsState.Controls_Gamepad: CloseControlsGamepad(); break;
                case SettingsState.Main: CloseSettings(); break;
            }
        }
        else if (isGamePaused)
        {
            TogglePause();
        }
    }

    // --- ALT MENÜLER ---

    public void OpenControlsKeyboard()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Controls_Keyboard;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = GetCanvasHeight();

        settingsControlsRect.DOKill(true);
        settingsControlsRect.DOAnchorPosY(-height, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsControlsRect.gameObject.SetActive(false));

        settingsControlsKeyboardRect.DOKill(true);
        settingsControlsKeyboardRect.gameObject.SetActive(true);
        settingsControlsKeyboardRect.anchoredPosition = new Vector2(0, height);
        settingsControlsKeyboardRect.DOAnchorPosY(0, slideDuration).SetEase(slideEase).SetUpdate(true).OnComplete(() => isBusy = false);
    }

    public void CloseControlsKeyboard()
    {
        isBusy = true;
        currentSettingsState = SettingsState.Controls;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = GetCanvasHeight();

        settingsControlsKeyboardRect.DOKill(true);
        settingsControlsKeyboardRect.DOAnchorPosY(height, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsControlsKeyboardRect.gameObject.SetActive(false));

        settingsControlsRect.DOKill(true);
        settingsControlsRect.gameObject.SetActive(true);
        settingsControlsRect.DOAnchorPosY(0, slideDuration).SetEase(slideEase).SetUpdate(true).OnComplete(() => isBusy = false);
    }

    public void OpenControlsGamepad()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Controls_Gamepad;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = GetCanvasHeight();

        settingsControlsRect.DOKill(true);
        settingsControlsRect.DOAnchorPosY(height, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsControlsRect.gameObject.SetActive(false));

        settingsControlsGamepadRect.DOKill(true);
        settingsControlsGamepadRect.gameObject.SetActive(true);
        settingsControlsGamepadRect.anchoredPosition = new Vector2(0, -height);
        settingsControlsGamepadRect.DOAnchorPosY(0, slideDuration).SetEase(slideEase).SetUpdate(true).OnComplete(() => isBusy = false);
    }

    public void CloseControlsGamepad()
    {
        isBusy = true;
        currentSettingsState = SettingsState.Controls;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = GetCanvasHeight();

        settingsControlsGamepadRect.DOKill(true);
        settingsControlsGamepadRect.DOAnchorPosY(-height, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsControlsGamepadRect.gameObject.SetActive(false));

        settingsControlsRect.DOKill(true);
        settingsControlsRect.gameObject.SetActive(true);
        settingsControlsRect.DOAnchorPosY(0, slideDuration).SetEase(slideEase).SetUpdate(true).OnComplete(() => isBusy = false);
    }

    public void OpenGeneralSettings()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.General;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = GetCanvasHeight();

        settingsMainRect.DOKill(true);
        settingsMainRect.DOAnchorPosY(-height, slideDuration).SetEase(slideEase).SetUpdate(true).OnComplete(() => settingsMainRect.gameObject.SetActive(false));

        settingsGeneralRect.DOKill(true);
        settingsGeneralRect.gameObject.SetActive(true);
        settingsGeneralRect.anchoredPosition = new Vector2(0, height);
        settingsGeneralRect.DOAnchorPosY(0, slideDuration).SetEase(slideEase).SetUpdate(true).OnComplete(() => isBusy = false);
    }

    public void CloseGeneralSettings()
    {
        isBusy = true;
        currentSettingsState = SettingsState.Main;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = GetCanvasHeight();

        settingsGeneralRect.DOKill(true);
        settingsGeneralRect.DOAnchorPosY(height, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsGeneralRect.gameObject.SetActive(false));

        settingsMainRect.DOKill(true);
        settingsMainRect.gameObject.SetActive(true);
        settingsMainRect.DOAnchorPosY(0, slideDuration).SetEase(slideEase).SetUpdate(true).OnComplete(() => isBusy = false);
    }

    public void OpenAudioSettings()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Audio;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = GetCanvasHeight();

        settingsMainRect.DOKill(true);
        settingsMainRect.DOAnchorPosY(height, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsMainRect.gameObject.SetActive(false));

        settingsAudioRect.DOKill(true);
        settingsAudioRect.gameObject.SetActive(true);
        settingsAudioRect.anchoredPosition = new Vector2(0, -height);
        settingsAudioRect.DOAnchorPosY(0, slideDuration).SetEase(slideEase).SetUpdate(true).OnComplete(() => isBusy = false);
    }

    public void CloseAudioSettings()
    {
        isBusy = true;
        currentSettingsState = SettingsState.Main;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = GetCanvasHeight();

        settingsAudioRect.DOKill(true);
        settingsAudioRect.DOAnchorPosY(-height, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsAudioRect.gameObject.SetActive(false));

        settingsMainRect.DOKill(true);
        settingsMainRect.gameObject.SetActive(true);
        settingsMainRect.DOAnchorPosY(0, slideDuration).SetEase(slideEase).SetUpdate(true).OnComplete(() => isBusy = false);
    }

    public void OpenControlsSettings()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Controls;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float width = GetCanvasWidth();

        settingsMainRect.DOKill(true);
        settingsMainRect.DOAnchorPosX(-width, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsMainRect.gameObject.SetActive(false));

        settingsControlsRect.DOKill(true);
        settingsControlsRect.gameObject.SetActive(true);
        settingsControlsRect.anchoredPosition = new Vector2(width, 0);
        settingsControlsRect.DOAnchorPosX(0, slideDuration).SetEase(slideEase).SetUpdate(true).OnComplete(() => isBusy = false);
    }

    public void CloseControlsSettings()
    {
        isBusy = true;
        currentSettingsState = SettingsState.Main;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float width = GetCanvasWidth();

        settingsControlsRect.DOKill(true);
        settingsControlsRect.DOAnchorPosX(width, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsControlsRect.gameObject.SetActive(false));

        settingsMainRect.DOKill(true);
        settingsMainRect.gameObject.SetActive(true);
        settingsMainRect.DOAnchorPosX(0, slideDuration).SetEase(slideEase).SetUpdate(true).OnComplete(() => isBusy = false);
    }

    private void ResetSettingsState()
    {
        settingsMainRect.gameObject.SetActive(true);
        settingsMainRect.anchoredPosition = Vector2.zero;

        if (settingsGeneralRect) settingsGeneralRect.gameObject.SetActive(false);
        if (settingsAudioRect) settingsAudioRect.gameObject.SetActive(false);
        if (settingsControlsRect) settingsControlsRect.gameObject.SetActive(false);
        if (settingsControlsKeyboardRect) settingsControlsKeyboardRect.gameObject.SetActive(false);
        if (settingsControlsGamepadRect) settingsControlsGamepadRect.gameObject.SetActive(false);

        currentSettingsState = SettingsState.Main;
    }

    public void FixMenuPositions()
    {
        mainMenuRect.DOKill(true);
        settingsRect.DOKill(true);
        if (pauseMenuRect) pauseMenuRect.DOKill(true);
        isBusy = false;
    }

    public void HandleMainMenu(bool shouldTurnOn)
    {
        mainMenu.SetActive(shouldTurnOn);
        if (shouldTurnOn && mainMenuRect != null && settingsRect != null)
        {
            mainMenuRect.anchoredPosition = Vector2.zero;
            settingsRect.anchoredPosition = new Vector2(GetCanvasWidth(), 0);
            settingsMenu.SetActive(true);
            isGamePaused = false;
        }
    }

    public void HandlePauseMenu(bool shouldTurnOn) => pauseMenu.SetActive(shouldTurnOn);
    public void HandleTimeScale(float timeScale) => Time.timeScale = timeScale;
    public void SetCanPause(bool pause) => CanPause = pause;
    public void SetPlayerCanPlay(bool can) => PlayerManager.Instance.SetPlayerCanPlay(can);

    public void LoadScene(string sceneName) { SceneManager.LoadScene(sceneName); }
    public void QuitGame() { Application.Quit(); }

    private float GetCanvasWidth() { if (canvasRect != null) return canvasRect.rect.width; return 1920f; }
    private float GetCanvasHeight() { if (canvasRect != null) return canvasRect.rect.height; return 1080f; }

    private void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndAssignCamera();

        if (DayManager.Instance != null)
        {
            if (scene.name == "Scene0")
            {
                // --- RESTORED: DayManager Logic ---
                DayManager.Instance.ResetForGameplay();

                isGamePaused = false;
                isSettingsOpen = false;
                isBusy = false;

                HandleMainMenu(false);
                if (pauseMenu) pauseMenu.SetActive(false);

                CanPause = true;
                InputManager.Instance.SwitchToGameplayMode();

                HandleTimeScale(1);
                GameManager.Instance.SetCursor(GameManager.CursorType.Default);
                GameManager.Instance.SetCursorLock(true);
                SetPlayerCanPlay(true);

                UpdateDoFState(false);
                if (SoundManager.Instance) SoundManager.Instance.SwitchSnapshot("Outside", 0f);
            }
            else if (scene.name == "MainMenu")
            {
                // --- RESTORED: DayManager Logic ---
                DayManager.Instance.ResetForMainMenu();

                isGamePaused = false;
                isSettingsOpen = false;
                isBusy = false;

                HandleMainMenu(true);
                if (pauseMenu) pauseMenu.SetActive(false);
                CanPause = false;
                InputManager.Instance.SwitchToUIMode();

                HandleTimeScale(1);
                GameManager.Instance.SetCursor(GameManager.CursorType.Default);
                GameManager.Instance.SetCursorLock(false);

                UpdateDoFState(true);
                if (SoundManager.Instance) SoundManager.Instance.SwitchSnapshot("Outside", 0f);
            }
        }
    }

    // --- YARDIMCI METODLAR ---

    void FindAndAssignCamera()
    {
        GameObject camObj = GameObject.Find("UI_Camera");
        if (camObj != null)
        {
            Camera uiCam = camObj.GetComponent<Camera>();
            if (myCanvas != null)
            {
                myCanvas.worldCamera = uiCam;
                myCanvas.planeDistance = 5;
            }

            Camera mainCam = Camera.main;
            if (mainCam != null)
            {
                var cameraData = mainCam.GetUniversalAdditionalCameraData();
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
    }

    private void UpdateDoFState(bool enableDoF)
    {
        if (globalVolume == null) globalVolume = FindObjectOfType<Volume>();
        if (globalVolume != null)
        {
            if (globalVolume.profile.TryGet(out DepthOfField dof))
            {
                dof.active = enableDoF;
            }
        }
    }

    public void RegisterScaler(PixelPerfectCanvasScaler scaler) { if (!activeScalers.Contains(scaler)) { activeScalers.Add(scaler); scaler.UpdateScale(); } }
    public void UnregisterScaler(PixelPerfectCanvasScaler scaler) { if (activeScalers.Contains(scaler)) activeScalers.Remove(scaler); }
    public void RefreshAllCanvases(int offset = -1)
    {
        // Eðer yeni bir deðer geldiyse hafýzaya al
        if (offset != -1)
        {
            GlobalScaleOffset = offset;
        }

        foreach (var scaler in activeScalers)
        {
            if (scaler != null)
                scaler.UpdateScale(offset != -1 ? offset : GlobalScaleOffset);
        }
    }
    public void SetRebindBlocker(bool on, bool isGamepadMode = false)
    {
        rebindBlocker.SetActive(on);

        // Eðer blocker açýlýyorsa, doðru ipucunu göster
        if (on)
        {
            if (rebindHintKeyboard) rebindHintKeyboard.SetActive(!isGamepadMode);
            if (rebindHintGamepad) rebindHintGamepad.SetActive(isGamepadMode);
        }
    }
}