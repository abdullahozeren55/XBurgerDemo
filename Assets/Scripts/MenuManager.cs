using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    // Bu deðiþken sahne yenilense bile hafýzada kalýr (static olduðu için)
    public static bool IsRestarting = false;
    public static int SavedMenuCameraIndex = -1;

    [HideInInspector]
    public int GlobalScaleOffset = 0;

    [Header("State")]
    public bool CanPause = false;
    private bool isGamePaused = false;

    [Header("UI References")]
    public GameObject mainMenu;
    public GameObject pauseMenu;
    public GameObject settingsMenu;
    public GameObject raycastBlockerForPause;
    public GameObject rebindBlocker;
    

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
    public RectTransform pauseMenuRect;

    [Header("Animation Settings")]
    public float slideDuration = 0.5f;
    public Ease slideEase = Ease.OutBack;

    [Header("Audio")]
    public AudioClip swingSound;
    public float swingVolume = 1f;
    public float swingMinPitch = 0.8f;
    public float swingMaxPitch = 1.2f;

    [Header("Canvas")]
    public Canvas menuCanvas;
    public Canvas mouseCanvas;

    private RectTransform canvasRect;
    private List<PixelPerfectCanvasScaler> activeScalers = new List<PixelPerfectCanvasScaler>();

    private bool isBusy = false;
    private bool isSettingsOpen = false;

    private void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }

        if (menuCanvas != null) canvasRect = menuCanvas.GetComponent<RectTransform>();

        float width = GetCanvasWidth();
        float height = GetCanvasHeight();

        // Panelleri baþlangýç konumlarýna ýþýnla
        settingsGeneralRect.anchoredPosition = new Vector2(0, height);
        settingsAudioRect.anchoredPosition = new Vector2(0, -height);
        settingsControlsRect.anchoredPosition = new Vector2(width, 0);
        if (settingsControlsKeyboardRect) settingsControlsKeyboardRect.anchoredPosition = new Vector2(0, height);
        if (settingsControlsGamepadRect) settingsControlsGamepadRect.anchoredPosition = new Vector2(0, -height);
        settingsMainRect.anchoredPosition = Vector2.zero;

        GlobalScaleOffset = PlayerPrefs.GetInt("UIScaleOffset", 0);
    }

    private void OnEnable()
    {
        // Sahne yüklendiðinde 'OnSceneLoaded' fonksiyonunu çalýþtýr
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Aboneliði iptal et (Hata almamak için þart)
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Sahne her yüklendiðinde kameralarý tazeleyelim.
        // CameraManager.Instance o sahnede yeni oluþtuðu için null gelmez.
        if (CameraManager.Instance != null)
        {
            Camera uiCam = CameraManager.Instance.GetUIMainMenuCamera();
            if (menuCanvas) menuCanvas.worldCamera = uiCam;
            if (mouseCanvas) mouseCanvas.worldCamera = uiCam;
        }

        // Sonra restart mantýðýna geç
        if (IsRestarting)
        {
            StartCoroutine(RestartSequence());
        }
        else
        {
            EnterMainMenuMode(true);
        }
    }

    private IEnumerator RestartSequence()
    {
        // 1. Önce kamerayý ve menüyü "KÜT" diye ana menü pozisyonuna oturt.
        // instant=true olduðu için animasyonsuz, direkt yerleþtirir.
        EnterMainMenuMode(true);

        // 2. BURASI ÇOK ÖNEMLÝ: 
        // Frame beklemek yerine, gerçek zamanlý çok kýsa bir bekleme koyuyoruz.
        // Bu, kamera sisteminin "Tamam, þu an baþlangýç noktam burasý" diye kaydetmesini saðlar.
        yield return new WaitForSeconds(0.1f);

        // 3. Þimdi hareketi baþlat.
        // Kamera artýk nerede olduðunu bildiði için, hedefe doðru süzülerek (blend) gidecektir.
        EnterGameplayMode();
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

    // --- YENÝ SÝSTEM: Mod Deðiþtirme Fonksiyonlarý ---

    /// <summary>
    /// Oyunu Ana Menü durumuna getirir. (Eski 'MainMenu' sahnesi mantýðý)
    /// </summary>
    public void EnterMainMenuMode(bool instant = false)
    {
        if (!instant) isBusy = true;

        float height = GetCanvasHeight();

        mainMenu.SetActive(true);
        settingsMenu.SetActive(true);
        settingsRect.anchoredPosition = new Vector2(GetCanvasWidth(), 0);
        ResetSettingsState();

        if (instant)
        {
            // ANLIK GEÇÝÞ (Oyun Açýlýþý / Restart Aný)
            HandleTimeScale(1);
            mainMenuRect.anchoredPosition = Vector2.zero;
            if (pauseMenuRect) pauseMenuRect.anchoredPosition = new Vector2(0, height);
            pauseMenu.SetActive(false);

            FinishMainMenuEnterLogic(true);

            // --- BURAYI DEÐÝÞTÝRÝYORUZ ---
            // Eski kod: if (instant && CameraManager.Instance != null) CameraManager.Instance.SwitchToRandomMainMenuCamera(true);

            if (CameraManager.Instance != null)
            {
                // Eðer Restart atýlmýþsa ve elimizde geçerli bir index varsa onu kullan
                if (IsRestarting && SavedMenuCameraIndex != -1)
                {
                    CameraManager.Instance.SwitchToMainMenuCameraByIndex(SavedMenuCameraIndex, true);
                }
                else
                {
                    // Restart deðilse veya ilk açýlýþsa rastgele devam
                    CameraManager.Instance.SwitchToRandomMainMenuCamera(true);
                }
            }
            // -----------------------------
        }
        else
        {
            // ANÝMASYONLU GEÇÝÞ (Pause -> Main Menu)

            // 1. KRÝTÝK HAMLE: ZAMANI HEMEN BAÞLAT!
            // Pause'daydýk (Time=0), bunu 1 yapmazsak Cinemachine hareket etmez!
            HandleTimeScale(1);

            // 2. KAMERA GEÇÝÞÝ
            if (CameraManager.Instance != null)
                CameraManager.Instance.SwitchToRandomMainMenuCamera(false);

            // 3. PAUSE MENÜSÜ YUKARI
            if (pauseMenu.activeSelf && pauseMenuRect != null)
            {
                pauseMenuRect.DOKill(true);
                pauseMenuRect.DOAnchorPosY(height, slideDuration)
                    .SetEase(slideEase)
                    .SetUpdate(true) // Time=1 yaptýk ama UI için SetUpdate(true) kalmasýnda zarar yok
                    .OnComplete(() => pauseMenu.SetActive(false));
            }

            // 4. ANA MENÜ AÞAÐIDAN YUKARI
            mainMenuRect.DOKill(true);
            mainMenuRect.anchoredPosition = new Vector2(0, -height);

            mainMenuRect.DOAnchorPosY(0, slideDuration)
                .SetEase(slideEase)
                .SetUpdate(true)
                .OnComplete(() =>
                {
                    FinishMainMenuEnterLogic(false);
                    isBusy = false;
                });
        }
    }

    // Kod tekrarýný önlemek için mantýk kýsmý ayrýldý
    private void FinishMainMenuEnterLogic(bool instant)
    {
        isGamePaused = false;
        isSettingsOpen = false;
        // isBusy = false; // Bunu animasyon bitimine saklýyoruz (instant ise zaten sorun yok)
        if (instant) isBusy = false;

        CanPause = false;

        HandleTimeScale(1);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCursor(GameManager.CursorType.Default);
            GameManager.Instance.SetCursorLock(false);
        }

        InputManager.Instance.SwitchToUIMode();

        if (SoundManager.Instance) SoundManager.Instance.SwitchSnapshot("Outside", 0f);

        // Eðer instant ise kamerayý da küt diye geçir
        if (instant && CameraManager.Instance != null)
            CameraManager.Instance.SwitchToRandomMainMenuCamera(true);
    }

    /// <summary>
    /// Oyuna Baþla Butonu
    /// </summary>
    public void EnterGameplayMode()
    {
        // --- YENÝ EKLENEN KISIM BAÞLANGIÇ ---

        // Eðer bu fonksiyon butona basýlarak çaðrýldýysa (Restarting modu aktif deðilse)
        if (!IsRestarting)
        {
            // YENÝ: Sahne ölmeden önce þu an hangi menü kamerasýndayýz kaydet!
            if (CameraManager.Instance != null)
            {
                SavedMenuCameraIndex = CameraManager.Instance.GetCurrentMenuCameraIndex();
            }

            IsRestarting = true;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        // Eðer buraya geldiysek, sahne yeni açýlmýþtýr ve Start() bizi buraya göndermiþtir.
        IsRestarting = false; // Bayraðý indir, bir sonraki sefere hazýr olsun.

        // --- YENÝ EKLENEN KISIM BÝTÝÞ ---


        // --- SENÝN ORÝJÝNAL KODLARIN ---
        if (isBusy) return;
        isBusy = true;

        raycastBlockerForPause.SetActive(false);

        HandleTimeScale(1);

        float height = GetCanvasHeight();

        // Kamera geçiþi
        if (CameraManager.Instance != null)
            CameraManager.Instance.SwitchToGameplayCamera();

        // UI Animasyonu
        mainMenuRect.DOKill(true);
        mainMenuRect.DOAnchorPosY(-height, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                HandleMainMenu(false);
                isBusy = false;
            });

        if (pauseMenu && pauseMenu.activeSelf) pauseMenu.SetActive(false);

        isGamePaused = false;
        isSettingsOpen = false;

        CanPause = true;

        HandleTimeScale(1);

        if (GameManager.Instance != null)
        {
            GameManager.Instance.SetCursor(GameManager.CursorType.Default);
            GameManager.Instance.SetCursorLock(true);
        }

        InputManager.Instance.SwitchToGameplayMode();

        if (SoundManager.Instance)
        {
            SoundManager.Instance.SwitchSnapshot("Outside", 0f);
        }
    }

    // ---------------------------------------------------

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

    // --- YARDIMCI METODLAR ---

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

    public void QuitGame() { Application.Quit(); }

    private float GetCanvasWidth() { if (canvasRect != null) return canvasRect.rect.width; return 1920f; }
    private float GetCanvasHeight() { if (canvasRect != null) return canvasRect.rect.height; return 1080f; }

    public void RegisterScaler(PixelPerfectCanvasScaler scaler) { if (!activeScalers.Contains(scaler)) { activeScalers.Add(scaler); scaler.UpdateScale(); } }
    public void UnregisterScaler(PixelPerfectCanvasScaler scaler) { if (activeScalers.Contains(scaler)) activeScalers.Remove(scaler); }
    public void RefreshAllCanvases(int offset = -1)
    {
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
    public void SetRebindBlocker(bool on)
    {
        rebindBlocker.SetActive(on);
    }
}