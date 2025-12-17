using DG.Tweening; // DOTween kütüphanesi þart
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

    [Header("UI Stuff")]
    public bool CanPause = false;
    [Space]
    public Volume globalVolume;
    [Space]
    public GameObject mainMenu; // GameObject olarak kalabilir (SetActive için)
    public GameObject pauseMenu;
    public GameObject settingsMenu; // YENÝ: Settings menüsü objesi

    [Header("Settings Sub-Panels")]
    // Ayarlarýn Ana Menüsü (Main Settings Page)
    public RectTransform settingsMainRect;
    public RectTransform settingsGeneralRect;
    public RectTransform settingsAudioRect;
    public RectTransform settingsControlsRect;
    public RectTransform settingsControlsKeyboardRect; // Klavye/Mouse Sayfasý
    public RectTransform settingsControlsGamepadRect;  // Gamepad Sayfasý

    public GameObject rebindBlocker;

    // Þu an hangi alt menüdeyiz? (Back tuþu için önemli)
    private enum SettingsState
    {
        Main,
        General,
        Audio,
        Controls,
        Controls_Keyboard, // Yeni
        Controls_Gamepad   // Yeni
    }

    private SettingsState currentSettingsState = SettingsState.Main;

    [Header("Animation References")]
    // Animasyon yapacaðýmýz panellerin RectTransform'larý
    public UIFlicker[] mainMenuFlickers;
    public RectTransform mainMenuRect;
    public RectTransform settingsRect;

    [Header("Animation Settings")]
    public float slideDuration = 0.5f;
    public Ease slideEase = Ease.OutBack; // Juicy efekt için
    [Space]
    public AudioClip swingSound;
    public float swingVolume = 1f;
    public float swingMinPitch = 0.8f;
    public float swingMaxPitch = 1.2f;

    private Canvas myCanvas;
    private RectTransform canvasRect; // Canvas'ýn boyutunu almak için
    private List<PixelPerfectCanvasScaler> activeScalers = new List<PixelPerfectCanvasScaler>();
    private bool isBusy = false; // Animasyon sýrasýnda týklamayý engellemek için
    private bool isSettingsOpen = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        myCanvas = GetComponentInChildren<Canvas>();
        if (myCanvas != null) canvasRect = myCanvas.GetComponent<RectTransform>();

        float width = GetCanvasWidth();
        float height = GetCanvasHeight();

        // Genel Ayarlar: YUKARIDA beklesin (+Height)
        settingsGeneralRect.anchoredPosition = new Vector2(0, height);

        // Ses Ayarlarý: AÞAÐIDA beklesin (-Height)
        settingsAudioRect.anchoredPosition = new Vector2(0, -height);

        // Kontrol Ayarlarý: SAÐDA beklesin (+Width)
        settingsControlsRect.anchoredPosition = new Vector2(width, 0);

        // Klavye (Yukarýdan gelecek General gibi)
        if (settingsControlsKeyboardRect != null)
            settingsControlsKeyboardRect.anchoredPosition = new Vector2(0, height);

        // Gamepad (Aþaðýdan gelecek Audio gibi)
        if (settingsControlsGamepadRect != null)
            settingsControlsGamepadRect.anchoredPosition = new Vector2(0, -height);

        // Ana Ayarlar Sayfasý: SettingsMenu'nun içinde ortada
        settingsMainRect.anchoredPosition = Vector2.zero;
    }

    private void Start()
    {
        if (mainMenuRect != null && settingsRect != null)
        {
            float width = GetCanvasWidth();

            mainMenu.SetActive(true);
            settingsMenu.SetActive(true); // Settings ana parent açýk kalsýn

            // --- DEÐÝÞÝKLÝK BURADA ---
            // 1. Ana Menü Açýk, Settings Kapalý (Ekran Dýþý)
            mainMenuRect.gameObject.SetActive(true);
            mainMenuRect.anchoredPosition = Vector2.zero;

            // Settings paneli aslýnda kapalý deðil, ekran dýþýnda bekliyor.
            // Ama içindeki alt menüler kapalý olmalý.
            settingsRect.anchoredPosition = new Vector2(width, 0);

            // 2. Settings Alt Menülerini Kapat (Sadece Main açýk kalsýn)
            if (settingsMainRect) settingsMainRect.gameObject.SetActive(true); // Settings Main açýk
            settingsMainRect.anchoredPosition = Vector2.zero;

            if (settingsGeneralRect) settingsGeneralRect.gameObject.SetActive(false);
            if (settingsAudioRect) settingsAudioRect.gameObject.SetActive(false);
            if (settingsControlsRect) settingsControlsRect.gameObject.SetActive(false);
            if (settingsControlsKeyboardRect) settingsControlsKeyboardRect.gameObject.SetActive(false);
            if (settingsControlsGamepadRect) settingsControlsGamepadRect.gameObject.SetActive(false);
        }

        InputManager.Instance.SwitchToUIMode();
    }

    private void Update()
    {
        if (CanPause && InputManager.Instance.PlayerPause())
        {
            // Settings açýksa önce onu kapatýp Main'e mi dönsün yoksa direkt oyuna mý?
            // Basitlik için: Pause menüsü mantýðý aynen kalsýn.
            HandlePauseMenu(!pauseMenu.activeSelf);
            HandleTimeScale(pauseMenu.activeSelf ? 0f : 1f);

            GameManager.Instance.SetCursor(GameManager.CursorType.Default);
            GameManager.Instance.SetCursorLock(!pauseMenu.activeSelf);

            SetPlayerCanPlay(!pauseMenu.activeSelf);

            if (pauseMenu.activeSelf)
            {
                InputManager.Instance.SwitchToUIMode();
            }
            else
            {
                InputManager.Instance.SwitchToGameplayMode();
            }
        }
    }

    // --- YENÝ EKLENEN ANÝMASYON FONKSÝYONLARI ---

    public void FixMenuPositions()
    {
        // 1. Önce animasyonlarý durdur
        mainMenuRect.DOKill(true);
        settingsRect.DOKill(true);
        settingsMainRect.DOKill(true);
        if (settingsGeneralRect) settingsGeneralRect.DOKill(true);
        if (settingsAudioRect) settingsAudioRect.DOKill(true);
        if (settingsControlsRect) settingsControlsRect.DOKill(true);
        if (settingsControlsKeyboardRect) settingsControlsKeyboardRect.DOKill(true);
        if (settingsControlsGamepadRect) settingsControlsGamepadRect.DOKill(true);

        isBusy = false;
        float width = GetCanvasWidth();
        float height = GetCanvasHeight();

        // 2. TÜMÜNÜ KAPAT (Temiz sayfa)
        // Ana menü ve Settings parent hariç, altlarý kapatýyoruz.
        // Konumlarý sonra ayarlayacaðýz.

        // *Dikkat:* Ana Parentler Settings açýk/kapalý durumuna göre açýlacak.

        if (isSettingsOpen)
        {
            // --- SETTINGS AÇIK ---
            mainMenuRect.gameObject.SetActive(false); // Ana menü kapalý
            settingsRect.gameObject.SetActive(true);  // Settings Parent açýk

            settingsRect.anchoredPosition = Vector2.zero;
            mainMenuRect.anchoredPosition = new Vector2(-width, 0);

            // Alt Menüleri Kapat (Sadece aktif olaný birazdan açacaðýz)
            settingsMainRect.gameObject.SetActive(false);
            if (settingsGeneralRect) settingsGeneralRect.gameObject.SetActive(false);
            if (settingsAudioRect) settingsAudioRect.gameObject.SetActive(false);
            if (settingsControlsRect) settingsControlsRect.gameObject.SetActive(false);
            if (settingsControlsKeyboardRect) settingsControlsKeyboardRect.gameObject.SetActive(false);
            if (settingsControlsGamepadRect) settingsControlsGamepadRect.gameObject.SetActive(false);

            // Sadece Aktif Olan State'i AÇ ve Konumla
            switch (currentSettingsState)
            {
                case SettingsState.Main:
                    settingsMainRect.gameObject.SetActive(true);
                    settingsMainRect.anchoredPosition = Vector2.zero;

                    // Diðerlerinin konumunu "bekleme" yerine al (kapalý olsalar bile)
                    if (settingsGeneralRect) settingsGeneralRect.anchoredPosition = new Vector2(0, height);
                    if (settingsControlsRect) settingsControlsRect.anchoredPosition = new Vector2(width, 0);
                    break;

                case SettingsState.General:
                    if (settingsGeneralRect)
                    {
                        settingsGeneralRect.gameObject.SetActive(true);
                        settingsGeneralRect.anchoredPosition = Vector2.zero;
                    }
                    settingsMainRect.anchoredPosition = new Vector2(0, -height);
                    break;

                // ... (Diðer case'ler benzer mantýkla) ...

                case SettingsState.Controls:
                    if (settingsControlsRect)
                    {
                        settingsControlsRect.gameObject.SetActive(true);
                        settingsControlsRect.anchoredPosition = Vector2.zero;
                    }
                    settingsMainRect.anchoredPosition = new Vector2(-width, 0);
                    break;

                case SettingsState.Controls_Keyboard:
                    if (settingsControlsKeyboardRect)
                    {
                        settingsControlsKeyboardRect.gameObject.SetActive(true);
                        settingsControlsKeyboardRect.anchoredPosition = Vector2.zero;
                    }
                    if (settingsControlsRect) settingsControlsRect.anchoredPosition = new Vector2(0, -height);
                    break;
            }
        }
        else
        {
            // --- ANA MENÜ AÇIK ---
            mainMenuRect.gameObject.SetActive(true);
            settingsRect.gameObject.SetActive(false); // Settings komple kapalý

            mainMenuRect.anchoredPosition = Vector2.zero;
            settingsRect.anchoredPosition = new Vector2(width, 0);

            // Settings içini de resetle
            ResetSettingsState();
        }

        Canvas.ForceUpdateCanvases();
    }

    // --- KLAVYE / MOUSE SAYFASI (YUKARIDAN ÝNER) ---
    public void OpenControlsKeyboard()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Controls_Keyboard;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = canvasRect.rect.height;

        // GÝDEN: Controls
        settingsControlsRect.DOAnchorPosY(-height, slideDuration)
            .SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsControlsRect.gameObject.SetActive(false)); // Kapat

        // GELEN: Keyboard
        settingsControlsKeyboardRect.gameObject.SetActive(true); // Aç
        settingsControlsKeyboardRect.anchoredPosition = new Vector2(0, height);

        settingsControlsKeyboardRect.DOAnchorPosY(0, slideDuration)
            .SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    // --- CLOSE KEYBOARD ---
    public void CloseControlsKeyboard()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Controls;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = canvasRect.rect.height;

        // GÝDEN: Keyboard
        settingsControlsKeyboardRect.DOAnchorPosY(height, slideDuration)
            .SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsControlsKeyboardRect.gameObject.SetActive(false)); // Kapat

        // GELEN: Controls
        settingsControlsRect.gameObject.SetActive(true); // Aç
        settingsControlsRect.DOAnchorPosY(0, slideDuration)
            .SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    // --- GAMEPAD SAYFASI (AÞAÐIDAN ÇIKAR) ---
    public void OpenControlsGamepad()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Controls_Gamepad;

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);

        float height = canvasRect.rect.height;

        // 1. Controls Sayfasý: YUKARI kaysýn (+Height)
        settingsControlsRect.DOAnchorPosY(height, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsControlsRect.gameObject.SetActive(false)); // Kapat

        // 2. Gamepad Sayfasý: AÞAÐIDAN gelsin (0)
        settingsControlsGamepadRect.gameObject.SetActive(true); // Aç
        settingsControlsGamepadRect.anchoredPosition = new Vector2(0, -height); // Reset
        settingsControlsGamepadRect.DOAnchorPosY(0, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    public void CloseControlsGamepad()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Controls; // Geri Controls'a dönüyoruz

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);

        float height = canvasRect.rect.height;

        // 1. Gamepad Sayfasý: AÞAÐI geri gitsin (-Height)
        settingsControlsGamepadRect.DOAnchorPosY(-height, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsControlsGamepadRect.gameObject.SetActive(false)); // Kapat

        // 2. Controls Sayfasý: YUKARIDAN geri gelsin (0)
        settingsControlsRect.gameObject.SetActive(true); // Aç
        settingsControlsRect.DOAnchorPosY(0, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    public void OpenGeneralSettings()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.General;

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = canvasRect.rect.height;

        // --- GÝDEN: Main Settings Page ---
        settingsMainRect.DOAnchorPosY(-height, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => settingsMainRect.gameObject.SetActive(false)); // KAPAT

        // --- GELEN: General Settings ---
        settingsGeneralRect.gameObject.SetActive(true); // AÇ
        settingsGeneralRect.anchoredPosition = new Vector2(0, height); // Reset pos

        settingsGeneralRect.DOAnchorPosY(0, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    public void CloseGeneralSettings()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Main;

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float height = canvasRect.rect.height;

        // --- GÝDEN: General Settings ---
        settingsGeneralRect.DOAnchorPosY(height, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => settingsGeneralRect.gameObject.SetActive(false)); // KAPAT

        // --- GELEN: Main Settings Page ---
        settingsMainRect.gameObject.SetActive(true); // AÇ

        settingsMainRect.DOAnchorPosY(0, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    public void OpenAudioSettings()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Audio;

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);

        float height = canvasRect.rect.height;

        // 1. Ana Ayarlar Sayfasý: YUKARI kaysýn (+Height)
        settingsMainRect.DOAnchorPosY(height, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsMainRect.gameObject.SetActive(false)); // KAPAT

        // 2. Ses Ayarlarý: AÞAÐIDAN gelsin (0)
        settingsAudioRect.gameObject.SetActive(true); // AÇ
        settingsAudioRect.anchoredPosition = new Vector2(0, -height); // Reset
        settingsAudioRect.DOAnchorPosY(0, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    public void CloseAudioSettings()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Main;

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);

        float height = canvasRect.rect.height;

        // 1. Ses Ayarlarý: AÞAÐI geri gitsin (-Height)
        settingsAudioRect.DOAnchorPosY(-height, slideDuration).SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsAudioRect.gameObject.SetActive(false));

        // 2. Ana Ayarlar Sayfasý: YUKARIDAN geri gelsin (0)
        settingsMainRect.gameObject.SetActive(true); // AÇ

        settingsMainRect.DOAnchorPosY(0, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    public void OpenControlsSettings()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Controls;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float width = GetCanvasWidth();

        // GÝDEN: Main
        settingsMainRect.DOAnchorPosX(-width, slideDuration)
            .SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsMainRect.gameObject.SetActive(false)); // Kapat

        // GELEN: Controls
        settingsControlsRect.gameObject.SetActive(true); // Aç
        settingsControlsRect.anchoredPosition = new Vector2(width, 0);

        settingsControlsRect.DOAnchorPosX(0, slideDuration)
            .SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    // --- CLOSE CONTROLS ---
    public void CloseControlsSettings()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.Main;
        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);
        float width = GetCanvasWidth();

        // GÝDEN: Controls
        settingsControlsRect.DOAnchorPosX(width, slideDuration)
            .SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => settingsControlsRect.gameObject.SetActive(false)); // Kapat

        // GELEN: Main
        settingsMainRect.gameObject.SetActive(true); // Aç
        settingsMainRect.DOAnchorPosX(0, slideDuration)
            .SetEase(slideEase).SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    public void OpenSettings()
    {
        if (isBusy) return;
        isBusy = true;
        isSettingsOpen = true;

        foreach (var flicker in mainMenuFlickers)
            if (flicker != null) flicker.enabled = false;

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);

        float width = GetCanvasWidth();

        // --- GÝDEN: Main Menu ---
        mainMenuRect.DOAnchorPosX(-width, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // Ýþ bitti, kapat gitsin
                mainMenuRect.gameObject.SetActive(false);
            });

        // --- GELEN: Settings ---
        settingsRect.gameObject.SetActive(true); // Önce aç
        settingsRect.anchoredPosition = new Vector2(width, 0); // Yerini garantile

        settingsRect.DOAnchorPosX(0, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    public void CloseSettings()
    {
        if (isBusy) return;
        isBusy = true;
        isSettingsOpen = false;

        foreach (var flicker in mainMenuFlickers)
            if (flicker != null) flicker.enabled = true;

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);

        float width = GetCanvasWidth();

        // --- GÝDEN: Settings ---
        settingsRect.DOAnchorPosX(width, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // Sadece Settings ana panelini kapatmak yetmez,
                // performans için tüm alt panellerin state'ini koruyarak parent'ý kapatabiliriz.
                settingsRect.gameObject.SetActive(false);

                // Bir sonraki açýlýþ için "Settings Main" hariç hepsini kapatýp resetleyelim
                // Böylece Settings'i açýnca kaldýðý yerden deðil, baþtan baþlar.
                ResetSettingsState();
            });

        // --- GELEN: Main Menu ---
        mainMenuRect.gameObject.SetActive(true); // Önce aç
        mainMenuRect.anchoredPosition = new Vector2(-width, 0);

        mainMenuRect.DOAnchorPosX(0, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    // Settings kapatýlýnca alt menüleri temizleyen yardýmcý fonksiyon
    private void ResetSettingsState()
    {
        settingsMainRect.gameObject.SetActive(true);
        settingsMainRect.anchoredPosition = Vector2.zero;

        settingsGeneralRect.gameObject.SetActive(false);
        settingsAudioRect.gameObject.SetActive(false);
        settingsControlsRect.gameObject.SetActive(false);
        settingsControlsKeyboardRect.gameObject.SetActive(false);
        settingsControlsGamepadRect.gameObject.SetActive(false);

        currentSettingsState = SettingsState.Main;
    }

    public void HandleBack()
    {
        if (isBusy) return;

        if (!isSettingsOpen)
        {
            return;
        }

        switch (currentSettingsState)
        {
            case SettingsState.General:
                CloseGeneralSettings();
                break;
            case SettingsState.Audio:
                CloseAudioSettings();
                break;
            case SettingsState.Controls:
                CloseControlsSettings();
                break;

            // --- YENÝ EKLENEN CASELER ---
            case SettingsState.Controls_Keyboard:
                CloseControlsKeyboard();
                break;
            case SettingsState.Controls_Gamepad:
                CloseControlsGamepad();
                break;

            case SettingsState.Main:
                CloseSettings();
                break;
        }
    }

    // Canvas geniþliðini dinamik al (Çözünürlük deðiþse de çalýþýr)
    private float GetCanvasWidth()
    {
        if (canvasRect != null) return canvasRect.rect.width;
        return 1920f; // Fallback
    }

    // Canvas yüksekliðini dinamik al (Çözünürlük deðiþse de çalýþýr)
    private float GetCanvasHeight()
    {
        if (canvasRect != null) return canvasRect.rect.height;
        return 1080f; // Fallback
    }

    // ---------------------------------------------

    public void HandleMainMenu(bool shouldTurnOn)
    {
        mainMenu.SetActive(shouldTurnOn);
        // Ana menü açýldýðýnda pozisyonlarý resetle (Oyun içinden dönünce kaymýþ olmasýn)
        if (shouldTurnOn && mainMenuRect != null && settingsRect != null)
        {
            mainMenuRect.anchoredPosition = Vector2.zero;
            settingsRect.anchoredPosition = new Vector2(GetCanvasWidth(), 0);
            settingsMenu.SetActive(true); // Settings de arkada hazýr beklesin
        }
    }

    public void HandlePauseMenu(bool shouldTurnOn) => pauseMenu.SetActive(shouldTurnOn);
    public void HandleTimeScale(float timeScale) => Time.timeScale = timeScale;
    public void SetCanPause(bool pause) => CanPause = pause;
    public void SetPlayerCanPlay(bool can) => PlayerManager.Instance.SetPlayerBasicMovements(can);

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
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndAssignCamera();

        if (DayManager.Instance != null)
        {
            if (scene.name == "Scene0")
            {
                DayManager.Instance.ResetForGameplay();

                InputManager.Instance.SwitchToGameplayMode();

                SetPlayerCanPlay(true);
                HandleTimeScale(1);
                GameManager.Instance.SetCursor(GameManager.CursorType.Default);
                GameManager.Instance.SetCursorLock(true);
                HandleMainMenu(false);
                HandlePauseMenu(false);
                SetCanPause(true);
                UpdateDoFState(false);
                if (SoundManager.Instance) SoundManager.Instance.SwitchSnapshot("Outside", 0f);
            }
            else if (scene.name == "MainMenu")
            {
                DayManager.Instance.ResetForMainMenu();

                InputManager.Instance.SwitchToUIMode();

                HandleTimeScale(1);
                GameManager.Instance.SetCursor(GameManager.CursorType.Default);
                GameManager.Instance.SetCursorLock(false);
                HandleMainMenu(true);
                HandlePauseMenu(false);
                SetCanPause(false);
                UpdateDoFState(true);
                if (SoundManager.Instance) SoundManager.Instance.SwitchSnapshot("Outside", 0f);
            }
        }
    }

    void FindAndAssignCamera()
    {
        GameObject camObj = GameObject.Find("UI_Camera");

        if (camObj != null)
        {
            Camera uiCam = camObj.GetComponent<Camera>();
            Camera mainCam = Camera.main;

            if (myCanvas != null)
            {
                myCanvas.worldCamera = uiCam;
                myCanvas.planeDistance = 5;
            }

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

    public void RegisterScaler(PixelPerfectCanvasScaler scaler)
    {
        if (!activeScalers.Contains(scaler))
        {
            activeScalers.Add(scaler);
            scaler.UpdateScale();
        }
    }

    public void UnregisterScaler(PixelPerfectCanvasScaler scaler)
    {
        if (activeScalers.Contains(scaler))
        {
            activeScalers.Remove(scaler);
        }
    }

    public void RefreshAllCanvases(int offset = -1)
    {
        foreach (var scaler in activeScalers)
        {
            if (scaler != null)
                scaler.UpdateScale(offset);
        }
    }

    public void SetRebindBlocker(bool on)
    {
        rebindBlocker.SetActive(on);
    }
}