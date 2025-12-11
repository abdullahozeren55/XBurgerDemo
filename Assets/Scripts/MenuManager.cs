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

    // Alt Menüler (Bunlar ekranýn dýþýnda, yukarýda ve aþaðýda bekleyecek)
    public RectTransform settingsGeneralRect;
    public RectTransform settingsAudioRect;

    // Þu an hangi alt menüdeyiz? (Back tuþu için önemli)
    private enum SettingsState { Main, General, Audio }
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

        // BAÞLANGIÇ POZÝSYONLARI
        if (settingsGeneralRect != null && settingsAudioRect != null)
        {
            float width = GetCanvasWidth();
            float height = canvasRect.rect.height; // Yüksekliði al

            // Genel Ayarlar: YUKARIDA beklesin (+Height)
            settingsGeneralRect.anchoredPosition = new Vector2(0, height);

            // Ses Ayarlarý: AÞAÐIDA beklesin (-Height)
            settingsAudioRect.anchoredPosition = new Vector2(0, -height);

            // Ana Ayarlar Sayfasý: SettingsMenu'nun içinde ortada
            settingsMainRect.anchoredPosition = Vector2.zero;
        }
    }

    private void Start()
    {
        // BAÞLANGIÇ POZÝSYONLARINI AYARLA
        // Oyun açýldýðýnda Main ortada, Settings saðda (ekran dýþýnda) olsun.
        // Ýkisini de Active yapýyoruz ki kayarken görünsünler (Gizlemeyi pozisyonla yapýyoruz)

        if (mainMenuRect != null && settingsRect != null)
        {
            float width = GetCanvasWidth();

            mainMenu.SetActive(true);
            settingsMenu.SetActive(true);

            mainMenuRect.anchoredPosition = Vector2.zero; // Merkezde
            settingsRect.anchoredPosition = new Vector2(width, 0); // Saðda dýþarýda
        }
    }

    private void Update()
    {
        if (CanPause && Input.GetKeyDown(KeyCode.Escape))
        {
            // Settings açýksa önce onu kapatýp Main'e mi dönsün yoksa direkt oyuna mý?
            // Basitlik için: Pause menüsü mantýðý aynen kalsýn.
            HandlePauseMenu(!pauseMenu.activeSelf);
            HandleTimeScale(pauseMenu.activeSelf ? 0f : 1f);

            GameManager.Instance.SetCursor(GameManager.CursorType.Default);
            GameManager.Instance.SetCursorLock(!pauseMenu.activeSelf);

            SetPlayerCanPlay(!pauseMenu.activeSelf);
        }
    }

    // --- YENÝ EKLENEN ANÝMASYON FONKSÝYONLARI ---

    public void FixMenuPositions()
    {
        // 1. Her þeyi durdur
        mainMenuRect.DOKill(true);
        settingsRect.DOKill(true);
        settingsMainRect.DOKill(true);
        settingsGeneralRect.DOKill(true);
        settingsAudioRect.DOKill(true);
        isBusy = false;

        // 2. Yeni geniþliði al
        float width = GetCanvasWidth();
        float height = GetCanvasHeight();

        // 3. Pozisyonlarý IÞINLA (Animasyon yok)
        if (isSettingsOpen)
        {
            // Settings AÇIK: Settings ortada, Main solda dýþarýda
            settingsRect.anchoredPosition = Vector2.zero;
            mainMenuRect.anchoredPosition = new Vector2(-width, 0);

            // 4. Þimdi DÝKEY (Y) Konumlarý düzelt (Alt Menüler)
            switch (currentSettingsState)
            {
                case SettingsState.Main:
                    // Ana sayfa ortada, diðerleri dýþarýda
                    settingsMainRect.anchoredPosition = Vector2.zero;
                    settingsGeneralRect.anchoredPosition = new Vector2(0, height);  // Yukarýda
                    settingsAudioRect.anchoredPosition = new Vector2(0, -height);   // Aþaðýda
                    break;

                case SettingsState.General:
                    // Genel ayarlar ortada, Ana sayfa aþaðý itilmiþ (Çünkü genel yukarýdan indi)
                    settingsGeneralRect.anchoredPosition = Vector2.zero;
                    settingsMainRect.anchoredPosition = new Vector2(0, -height);    // Aþaðýda
                    settingsAudioRect.anchoredPosition = new Vector2(0, -height);   // Aþaðýda (Farketmez)
                    break;

                case SettingsState.Audio:
                    // Ses ayarlarý ortada, Ana sayfa yukarý itilmiþ (Çünkü ses aþaðýdan çýktý)
                    settingsAudioRect.anchoredPosition = Vector2.zero;
                    settingsMainRect.anchoredPosition = new Vector2(0, height);     // Yukarýda
                    settingsGeneralRect.anchoredPosition = new Vector2(0, height);  // Yukarýda (Farketmez)
                    break;
            }
        }
        else
        {
            // Main AÇIK: Main ortada, Settings saðda dýþarýda
            mainMenuRect.anchoredPosition = Vector2.zero;
            settingsRect.anchoredPosition = new Vector2(width, 0);

            // Settings kapalýyken içini de "Reset" pozisyonuna getirelim ki
            // tekrar açýldýðýnda temiz baþlasýn.
            settingsMainRect.anchoredPosition = Vector2.zero;
            settingsGeneralRect.anchoredPosition = new Vector2(0, height);
            settingsAudioRect.anchoredPosition = new Vector2(0, -height);
            currentSettingsState = SettingsState.Main;
        }

        Canvas.ForceUpdateCanvases();
    }

    public void OpenGeneralSettings()
    {
        if (isBusy) return;
        isBusy = true;
        currentSettingsState = SettingsState.General;

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);

        float height = canvasRect.rect.height;

        // 1. Ana Ayarlar Sayfasý: AÞAÐI kaysýn (-Height) ve çýksýn
        settingsMainRect.DOAnchorPosY(-height, slideDuration).SetEase(slideEase).SetUpdate(true);

        // 2. Genel Ayarlar: YUKARIDAN gelsin (0)
        settingsGeneralRect.anchoredPosition = new Vector2(0, height); // Reset
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

        // 1. Genel Ayarlar: YUKARI geri gitsin (+Height)
        settingsGeneralRect.DOAnchorPosY(height, slideDuration).SetEase(slideEase).SetUpdate(true);

        // 2. Ana Ayarlar Sayfasý: AÞAÐIDAN geri gelsin (0)
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
        settingsMainRect.DOAnchorPosY(height, slideDuration).SetEase(slideEase).SetUpdate(true);

        // 2. Ses Ayarlarý: AÞAÐIDAN gelsin (0)
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
        settingsAudioRect.DOAnchorPosY(-height, slideDuration).SetEase(slideEase).SetUpdate(true);

        // 2. Ana Ayarlar Sayfasý: YUKARIDAN geri gelsin (0)
        settingsMainRect.DOAnchorPosY(0, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    public void OpenSettings()
    {
        if (isBusy) return;
        isBusy = true;
        isSettingsOpen = true; // <-- ARTIK SETTINGS AÇIK

        foreach (var flicker in mainMenuFlickers)
        {
            if (flicker != null)
                flicker.enabled = false; // Sadece scripti kapatýyoruz, obje kalýyor
        }

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);

        float width = GetCanvasWidth();

        mainMenuRect.DOAnchorPosX(-width, slideDuration).SetEase(slideEase).SetUpdate(true);

        // Saðdan gelmesi için önce konumunu garantiye al
        settingsRect.anchoredPosition = new Vector2(width, 0);

        settingsRect.DOAnchorPosX(0, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    public void CloseSettings()
    {
        if (isBusy) return;
        isBusy = true;
        isSettingsOpen = false; // <-- ARTIK MAIN AÇIK

        foreach (var flicker in mainMenuFlickers)
        {
            if (flicker != null)
                flicker.enabled = true; // Sadece scripti kapatýyoruz, obje kalýyor
        }

        SoundManager.Instance.PlayUISoundFX(swingSound, swingVolume, swingMinPitch, swingMaxPitch);

        float width = GetCanvasWidth();

        settingsRect.DOAnchorPosX(width, slideDuration).SetEase(slideEase).SetUpdate(true);

        // Soldan gelmesi için önce konumunu garantiye al
        mainMenuRect.anchoredPosition = new Vector2(-width, 0);

        mainMenuRect.DOAnchorPosX(0, slideDuration)
            .SetEase(slideEase)
            .SetUpdate(true)
            .OnComplete(() => isBusy = false);
    }

    public void HandleBack()
    {
        if (isBusy) return;

        // Eðer Ana Menüdeysek -> Çýkýþ Sorusu veya Hiçbir þey
        if (!isSettingsOpen)
        {
            // Belki çýkýþ popup'ý açarsýn
            return;
        }

        // Eðer Settings Açýk ama Alt Menüdeyiz
        switch (currentSettingsState)
        {
            case SettingsState.General:
                CloseGeneralSettings();
                break;
            case SettingsState.Audio:
                CloseAudioSettings();
                break;
            case SettingsState.Main:
                // Alt menüde deðiliz, ana Settings sayfasýndayýz -> Ana Menüye dön
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
}