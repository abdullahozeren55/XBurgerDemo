using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [Header("Resolution Settings")]
    public TMP_Dropdown resolutionDropdown;
    private List<Resolution> filteredResolutions;

    [Header("Language Settings")]
    public TMP_Dropdown languageDropdown;

    [Serializable]
    public struct LanguageDisplayOption
    {
        public string languageCode;     // "tr", "zh"
        public string nativeName;       // "Türkçe", "简体中文"
        public TMP_FontAsset fontAsset; // Resources klasöründeki font
        public float fontSize;          // Font boyutu (px)

        [Tooltip("Pozitif değer yukarı, Negatif değer aşağı kaydırır.")]
        public float vOffset;           // YENİ: Dikey kaydırma ayarı
    }

    // 2. LİSTEYİ OLUŞTURUYORUZ (Inspector'dan dolduracaksın)
    public List<LanguageDisplayOption> languageDisplayList;

    [Header("Quality Settings")]
    public TMP_Dropdown qualityDropdown;

    [Header("Pixelation Settings")]
    public TMP_Dropdown pixelationDropdown;

    [Header("UI Scale Settings")]
    public TMP_Dropdown uiScaleDropdown;

    [Header("Gameplay Settings")]
    public TMP_Dropdown hintsDropdown;
    public TMP_Dropdown interactTextDropdown;

    [Header("Video Settings")]
    public TMP_Dropdown vSyncDropdown;
    public TMP_Dropdown fpsDropdown;
    public CanvasGroup fpsDropdownGroup;

    [Header("Controls Settings")]
    public Slider mouseSensSlider;
    public TMP_Text mouseSensText;
    [Space]
    public Slider gamepadSensSlider;
    public TMP_Text gamepadSensText;
    [Space]
    public TMP_Dropdown invertYDropdown;
    public TMP_Dropdown sprintModeDropdown;
    public TMP_Dropdown crouchModeDropdown;
    public TMP_Dropdown stickLayoutDropdown;
    public TMP_Dropdown controllerPromptsDropdown;
    public TMP_Dropdown aimAssistDropdown;

    public static event Action OnPromptsChanged;
    public static event Action OnStickLayoutChangedEvent;
    public static bool IsXboxPrompts = true;

    [Header("Audio Sliders")]
    public Slider masterSlider;
    public Slider soundFXSlider;
    public Slider musicSlider;
    public Slider ambianceSlider;
    public Slider typewriterSlider;
    public Slider uiSlider;
    [Space]
    public float defaultMasterVolume = 1.0f;
    public float defaultSoundFXVolume = 1.0f;
    public float defaultMusicVolume = 1.0f;
    public float defaultAmbianceVolume = 1.0f;
    public float defaultTypewriterVolume = 1.0f;
    public float defaultUIVolume = 1.0f;
    [Space]
    public TMP_Text masterText;
    public TMP_Text soundFXText;
    public TMP_Text musicText;
    public TMP_Text ambianceText;
    public TMP_Text typewriterText;
    public TMP_Text uiText;

    // DİKKAT: Bu listenin sırası, Unity Inspector'daki Dropdown Options sırasıyla AYNI olmalı!
    // Option 0: English -> "en"
    // Option 1: Türkçe -> "tr"
    // Option 2: Chinese -> "zh"
    // Option 3: Japanese -> "ja"
    // Option 4: Spanish -> "es"
    // Option 5: Russian -> "ru"
    // Option 6: Portuguese -> "pt"

    private readonly int[] fpsValues = { 30, 60, 75, 120, 144, 165, 240, 300, 360, -1 };

    private readonly List<string> qualityKeys = new List<string>
    {
        "UI_TOAST_MACHINE",
        "UI_LOW",
        "UI_MEDIUM",
        "UI_HIGH",
        "UI_ULTRA"
    };

    private readonly List<string> pixelationKeys = new List<string>
    {
        "UI_MAIN_MENU_PIXELATION_0", // HD
        "UI_MAIN_MENU_PIXELATION_1", // Hafif
        "UI_MAIN_MENU_PIXELATION_2", // Orta
    };

    private readonly List<string> onOffKeys = new List<string>
    {
        "UI_ON",
        "UI_OFF"
    };

    private readonly List<string> offOnKeys = new List<string>
    {
        "UI_OFF",
        "UI_ON"
    };

    private readonly List<string> holdToggleKeys = new List<string>
    {
        "UI_HOLD",
        "UI_TOGGLE" 
    };

    private readonly List<string> stickLayoutKeys = new List<string>
    {
        "UI_NORMAL", // 0: Normal
        "UI_SWAPPED"  // 1: Ters (Solak)
    };

    private readonly List<string> promptsKeys = new List<string>
    {
        "UI_XBOX", // 0
        "UI_PS"    // 1
    };

    private readonly List<string> aimAssistKeys = new List<string>
    {
        "UI_OFF",    // 0
        "UI_LOW",    // 1
        "UI_MEDIUM", // 2
        "UI_HIGH"    // 3
    };

    void Start()
    {
        DecideResolutions();
        InitializeLanguage();
        InitializeQuality();
        InitializeVideoSettings();
        InitializeAudio();
        InitializePixelation();
        InitializeGameplaySettings();
        InitializeControls();
    }

    private void InitializeControls()
    {
        // 1. Mouse Sens (0-100 arası tamsayı olarak çekiyoruz)
        // Varsayılan 50 olsun (Ortalama hız)
        float mSens = PlayerPrefs.GetFloat("MouseSens", 30f);
        mouseSensSlider.value = mSens;
        UpdateSensText(mouseSensText, mSens);

        // 2. Gamepad Sens (0-100 arası)
        float gSens = PlayerPrefs.GetFloat("GamepadSens", 50f);
        gamepadSensSlider.value = gSens;
        UpdateSensText(gamepadSensText, gSens);

        // 3. Dropdown İçeriklerini Doldur (Localization)
        PopulateDropdown(invertYDropdown, offOnKeys);
        PopulateDropdown(sprintModeDropdown, holdToggleKeys);
        PopulateDropdown(crouchModeDropdown, holdToggleKeys);
        PopulateDropdown(stickLayoutDropdown, stickLayoutKeys);
        PopulateDropdown(aimAssistDropdown, aimAssistKeys);

        // 4. Kayıtlı Değerleri Ata
        invertYDropdown.value = PlayerPrefs.GetInt("InvertY", 0); // 0: Off, 1: On
        invertYDropdown.RefreshShownValue();

        sprintModeDropdown.value = PlayerPrefs.GetInt("SprintMode", 0); // 0: Hold, 1: Toggle
        sprintModeDropdown.RefreshShownValue();

        crouchModeDropdown.value = PlayerPrefs.GetInt("CrouchMode", 0);
        crouchModeDropdown.RefreshShownValue();

        // YENİ: Stick Layout Başlangıç Değeri
        int savedStickLayout = PlayerPrefs.GetInt("StickLayout", 0); // 0: Default, 1: Swapped
        stickLayoutDropdown.value = savedStickLayout;
        stickLayoutDropdown.RefreshShownValue();

        // Başlangıçta InputManager'a bildir (Eğer varsa)
        if (InputManager.Instance != null)
            InputManager.Instance.SetSwapSticks(savedStickLayout == 1);

        PopulateDropdown(controllerPromptsDropdown, promptsKeys);

        int savedPrompts = PlayerPrefs.GetInt("ControllerPrompts", 0); // 0: Xbox, 1: PS
        controllerPromptsDropdown.value = savedPrompts;
        controllerPromptsDropdown.RefreshShownValue();

        // Static değişkeni güncelle
        IsXboxPrompts = (savedPrompts == 0);

        int savedAssist = PlayerPrefs.GetInt("AimAssist", 1);
        aimAssistDropdown.value = savedAssist;
        aimAssistDropdown.RefreshShownValue();

        if (InputManager.Instance != null)
            InputManager.Instance.SetAimAssistLevel(savedAssist);
    }

    // Dropdown Doldurma Yardımcısı (Kod tekrarını önlemek için)
    private void PopulateDropdown(TMP_Dropdown dropdown, List<string> keys)
    {
        dropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (string key in keys)
        {
            string text = "MISSING";
            if (LocalizationManager.Instance != null)
                text = LocalizationManager.Instance.GetText(key);
            options.Add(text);
        }
        dropdown.AddOptions(options);
    }

    // UI Text Güncelleme
    private void UpdateSensText(TMP_Text textComp, float val)
    {
        // Direkt 0-100 değerini yazdırıyoruz
        if (textComp != null) textComp.text = val.ToString("F0");
    }

    // --- EVENT CALLBACKS (Inspector'dan Bağlanacaklar) ---

    public void OnAimAssistChanged(int index)
    {
        PlayerPrefs.SetInt("AimAssist", index);
        if (InputManager.Instance != null)
            InputManager.Instance.SetAimAssistLevel(index);
    }

    public void OnMouseSensChanged(float val)
    {
        PlayerPrefs.SetFloat("MouseSens", val);
        UpdateSensText(mouseSensText, val);
        if (InputManager.Instance != null) InputManager.Instance.SetMouseSensitivity(val);
    }

    public void OnGamepadSensChanged(float val)
    {
        PlayerPrefs.SetFloat("GamepadSens", val);
        UpdateSensText(gamepadSensText, val);
        if (InputManager.Instance != null) InputManager.Instance.SetGamepadSensitivity(val);
    }

    public void OnInvertYChanged(int index)
    {
        PlayerPrefs.SetInt("InvertY", index);
        if (InputManager.Instance != null) InputManager.Instance.SetInvertY(index == 1);
    }

    public void OnSprintModeChanged(int index)
    {
        PlayerPrefs.SetInt("SprintMode", index);
        if (InputManager.Instance != null) InputManager.Instance.SetSprintMode(index == 1);
    }

    public void OnCrouchModeChanged(int index)
    {
        PlayerPrefs.SetInt("CrouchMode", index);
        if (InputManager.Instance != null) InputManager.Instance.SetCrouchMode(index == 1);
    }

    public void OnStickLayoutChanged(int index)
    {
        PlayerPrefs.SetInt("StickLayout", index);
        // 0: Normal (False), 1: Swapped (True)
        if (InputManager.Instance != null) InputManager.Instance.SetSwapSticks(index == 1);

        OnStickLayoutChangedEvent?.Invoke();
    }

    public void OnControllerPromptsChanged(int index)
    {
        PlayerPrefs.SetInt("ControllerPrompts", index);
        IsXboxPrompts = (index == 0);

        OnPromptsChanged?.Invoke();
    }

    private void InitializeGameplaySettings()
    {
        // --- SEÇENEKLERİ DOLDUR ---
        List<string> options = new List<string>();
        foreach (string key in onOffKeys)
        {
            string localizedName = "MISSING";
            if (LocalizationManager.Instance != null)
                localizedName = LocalizationManager.Instance.GetText(key);
            options.Add(localizedName);
        }

        // --- HINTS DROPDOWN ---
        hintsDropdown.ClearOptions();
        hintsDropdown.AddOptions(options);
        // Varsayılan 0 (Visible)
        hintsDropdown.value = PlayerPrefs.GetInt("ShowHints", 0);
        hintsDropdown.RefreshShownValue();

        // --- INTERACT TEXT DROPDOWN ---
        interactTextDropdown.ClearOptions();
        interactTextDropdown.AddOptions(options);
        // Varsayılan 0 (Visible)
        interactTextDropdown.value = PlayerPrefs.GetInt("ShowInteractText", 0);
        interactTextDropdown.RefreshShownValue();
    }

    // ==================================================================================
    // VIDEO AYARLARI (VSYNC & FPS)
    // ==================================================================================
    private void InitializeVideoSettings()
    {
        // --- İLK AÇILIŞ KONTROLÜ ---
        if (!PlayerPrefs.HasKey("VSync"))
        {
            // DİKKAT: Unity'de vSyncCount = 1 demek AÇIK demektir.
            // Varsayılan olarak AÇIK olsun istiyorsan 1 yapmalısın.
            QualitySettings.vSyncCount = 1;
            PlayerPrefs.SetInt("VSync", 0); // Dropdown'da 0. index (UI_ON) olsun diye 0 kaydediyoruz.

            PlayerPrefs.Save();
        }

        if (!PlayerPrefs.HasKey("TargetFPS"))
        {
            Application.targetFrameRate = -1;
            PlayerPrefs.SetInt("TargetFPS", -1);
            PlayerPrefs.Save();
        }

        // 1. VSync Dropdown Doldur (0=On, 1=Off)
        PopulateDropdown(vSyncDropdown, onOffKeys);

        // Kayıtlı değeri çek. (0 ise ON, 1 ise OFF)
        // Eğer kayıt yoksa yukarıda kaydettiğimiz 0 (ON) gelir.
        int savedVSyncIndex = PlayerPrefs.GetInt("VSync", 0);

        // Unity ayarını da buna göre yap (Index 0 ise Count 1, Index 1 ise Count 0)
        QualitySettings.vSyncCount = (savedVSyncIndex == 0) ? 1 : 0;

        vSyncDropdown.value = savedVSyncIndex;
        vSyncDropdown.RefreshShownValue();

        // 2. FPS Dropdown Doldur
        InitializeFPSDropdown();

        int currentFPS = Application.targetFrameRate;
        int fpsIndex = GetFPSIndex(currentFPS);
        fpsDropdown.value = fpsIndex;
        fpsDropdown.RefreshShownValue();

        // 3. UI Kilidi: Eğer Dropdown 0 (AÇIK) ise FPS kutusu kilitlensin.
        UpdateFPSDropdownInteractivity(savedVSyncIndex == 0);
    }

    private void InitializeFPSDropdown()
    {
        fpsDropdown.ClearOptions();
        List<string> options = new List<string>();

        // "Unlimited" kelimesini dil sisteminden çek
        string unlimitedText = "Unlimited";
        if (LocalizationManager.Instance != null)
            unlimitedText = LocalizationManager.Instance.GetText("UI_UNLIMITED");

        foreach (int fps in fpsValues)
        {
            if (fps == -1)
                options.Add(unlimitedText); // -1 görünce "Sınırsız" yaz
            else
                options.Add(fps.ToString());
        }
        fpsDropdown.AddOptions(options);
    }

    // FPS değerine göre dropdown indexini bulan yardımcı fonksiyon
    private int GetFPSIndex(int targetFPS)
    {
        for (int i = 0; i < fpsValues.Length; i++)
        {
            if (fpsValues[i] == targetFPS) return i;
        }
        return fpsValues.Length - 1; // Listede yoksa "Sınırsız" varsay
    }

    // --- UI EVENTLERİ (Dropdownlara Bağlanacak) ---

    public void SetVSync(int index)
    {
        // Dropdown Listemiz: { "ON" (0), "OFF" (1) }
        // Unity vSyncCount:  1 = ON, 0 = OFF

        // Eğer index 0 (ON) seçildiyse, Unity'ye 1 gönder.
        // Eğer index 1 (OFF) seçildiyse, Unity'ye 0 gönder.
        QualitySettings.vSyncCount = (index == 0) ? 1 : 0;

        bool isVSyncOn = (index == 0);
        UpdateFPSDropdownInteractivity(isVSyncOn);

        if (isVSyncOn)
        {
            Application.targetFrameRate = -1; // VSync açıksa FPS serbest (monitöre bağlı)
        }
        else
        {
            // VSync kapandıysa FPS sınırını geri getir
            SetMaxFPS(fpsDropdown.value);
        }

        PlayerPrefs.SetInt("VSync", index);
        PlayerPrefs.Save();
    }

    public void SetMaxFPS(int index)
    {
        // Eğer VSync açıksa FPS değiştirmeye çalışma, Unity zaten takmaz
        if (QualitySettings.vSyncCount > 0) return;

        int targetFPS = fpsValues[index];
        Application.targetFrameRate = targetFPS;

        PlayerPrefs.SetInt("TargetFPS", targetFPS);
        PlayerPrefs.Save();
    }

    // Dropdown'ı pasif/aktif yapma ve şeffaflaştırma
    private void UpdateFPSDropdownInteractivity(bool isVSyncOn)
    {
        if (fpsDropdown != null)
        {
            fpsDropdown.interactable = !isVSyncOn; // VSync açıksa tıklanamaz
            fpsDropdownGroup.alpha = isVSyncOn ? 0.5f : 1f; // Yarım şeffaf veya tam
        }
    }

    // Dropdown Eventleri
    public void SetHints(int index)
    {
        // 0: Visible, 1: Hidden
        PlayerPrefs.SetInt("ShowHints", index);
        PlayerPrefs.Save();

        // Eğer oyun içindeysek anlık güncelle
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.UpdateGameplaySettings();

        if (MonitorManager.Instance != null)
            MonitorManager.Instance.UpadateShowHint();
    }

    public void SetInteractText(int index)
    {
        PlayerPrefs.SetInt("ShowInteractText", index);
        PlayerPrefs.Save();

        if (PlayerManager.Instance != null)
            PlayerManager.Instance.UpdateGameplaySettings();
    }

    private void InitializeUIScale()
    {
        uiScaleDropdown.ClearOptions();
        List<string> options = new List<string>();

        // Maksimum scale değerini bul
        int maxScale = Mathf.FloorToInt(Screen.height / 360f);
        if (maxScale < 1) maxScale = 1;

        // --- DEĞİŞİKLİK BURADA: "Varsayılan" Metnini Çek ---
        string defaultText = "Default"; // Fallback (Yedek)
        if (LocalizationManager.Instance != null)
        {
            defaultText = LocalizationManager.Instance.GetText("UI_DEFAULT");
        }
        // ----------------------------------------------------

        for (int i = 0; i < maxScale; i++)
        {
            string label = "";
            int scaleVal = maxScale - i;

            if (i == 0)
            {
                // Örn: "(3x) Varsayılan" veya "(3x) Default"
                label = $"{scaleVal}x ({defaultText})";
            }
            else
            {
                label = scaleVal + "x";
            }

            options.Add(label);
        }

        uiScaleDropdown.AddOptions(options);

        int savedOffset = PlayerPrefs.GetInt("UIScaleOffset", 0);
        if (savedOffset >= options.Count) savedOffset = options.Count - 1;

        uiScaleDropdown.value = savedOffset;
        uiScaleDropdown.RefreshShownValue();

        // Başlangıçta uygula (RefreshDropdowns'tan gelince tekrar uygulaması sorun yaratmaz)
        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.RefreshAllCanvases(savedOffset);
            MenuManager.Instance.FixMenuPositions();
        }
    }

    public void OnUIScaleChanged(int index)
    {
        PlayerPrefs.SetInt("UIScaleOffset", index);
        PlayerPrefs.Save();

        // Direkt çağırmak yerine Coroutine başlatıyoruz ki araya "Fix" sıkıştıralım
        StartCoroutine(ApplyUIScaleRoutine(index));
    }

    private IEnumerator ApplyUIScaleRoutine(int offset)
    {
        if (MenuManager.Instance != null)
        {
            // 1. Önce bütün Canvasların boyunu değiştir (Büyüt/Küçült)
            MenuManager.Instance.RefreshAllCanvases(offset);

            // 2. Unity'ye "Hadi bi hesapla şu yeni boyutları" de ve bekle
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null; // Garanti olsun diye 2 kare bekle

            // 3. ŞİMDİ o kayan panelleri yeni sınırlara ışınla
            MenuManager.Instance.FixMenuPositions();
        }
    }

    private void InitializePixelation()
    {
        pixelationDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (string key in pixelationKeys)
        {
            string localizedName = "MISSING";
            if (LocalizationManager.Instance != null)
                localizedName = LocalizationManager.Instance.GetText(key);
            options.Add(localizedName);
        }

        pixelationDropdown.AddOptions(options);

        int currentLimit = PlayerPrefs.GetInt("TextureMipmapLimit", 0);
        QualitySettings.globalTextureMipmapLimit = currentLimit;

        pixelationDropdown.value = currentLimit;
        pixelationDropdown.RefreshShownValue();
    }

    public void SetPixelation(int limitIndex)
    {
        // limitIndex büyüdükçe (0->3) pikselleşme artar (Kalite düşer)
        // Tam istediğin mantık bu.
        QualitySettings.globalTextureMipmapLimit = limitIndex;
        PlayerPrefs.SetInt("TextureMipmapLimit", limitIndex);
        PlayerPrefs.Save();
    }

    private void UpdateVolumeText(TMP_Text textComp, float value)
    {
        if (textComp != null)
        {
            // 0-1 arasındaki değeri 100 ile çarpıp tam sayıya yuvarlıyoruz
            int percentage = Mathf.RoundToInt(value * 100);
            textComp.text = "%" + percentage; // Veya istersen: percentage + "%"
        }
    }

    private void InitializeAudio()
    {
        // 1. Kayıtlı değerleri çek (Yoksa 0.75 varsayılan olsun)
        float masterVol = PlayerPrefs.GetFloat("MasterVol", defaultMasterVolume);
        float soundFXVol = PlayerPrefs.GetFloat("SoundFXVol", defaultSoundFXVolume);
        float musicVol = PlayerPrefs.GetFloat("MusicVol", defaultMusicVolume);
        float ambianceVol = PlayerPrefs.GetFloat("AmbianceVol", defaultAmbianceVolume);
        float typewriterVol = PlayerPrefs.GetFloat("TypewriterVol", defaultTypewriterVolume);
        float uiVol = PlayerPrefs.GetFloat("UIVol", defaultUIVolume);

        // 2. Sliderları bu değerlere getir
        if (masterSlider) masterSlider.value = masterVol;
        if (soundFXSlider) soundFXSlider.value = soundFXVol;
        if (musicSlider) musicSlider.value = musicVol;
        if (ambianceSlider) ambianceSlider.value = ambianceVol;
        if (typewriterSlider) typewriterSlider.value = typewriterVol;
        if (uiSlider) uiSlider.value = uiVol;

        UpdateVolumeText(masterText, masterVol);
        UpdateVolumeText(soundFXText, soundFXVol);
        UpdateVolumeText(musicText, musicVol);
        UpdateVolumeText(ambianceText, ambianceVol);
        UpdateVolumeText(typewriterText, typewriterVol);
        UpdateVolumeText(uiText, uiVol);

        // 3. Sesi de ayarla (SoundManager üzerinden)
        // NOT: SoundManager'ın Start'ında da bunu çağırıyor olabilirsin, 
        // çakışmaması için SoundManager'da PlayerPrefs okuma varsa orayı silip buradan yönetebilirsin.
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.SetMasterVolume(masterVol);
            SoundManager.Instance.SetSoundFXVolume(soundFXVol);
            SoundManager.Instance.SetMusicVolume(musicVol);
            SoundManager.Instance.SetAmbianceVolume(ambianceVol);
            SoundManager.Instance.SetTypewriterVolume(typewriterVol);
            SoundManager.Instance.SetUIVolume(uiVol);
        }
    }

    public void OnMasterSliderChanged(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetMasterVolume(val);
        PlayerPrefs.SetFloat("MasterVol", val);

        UpdateVolumeText(masterText, val);
    }

    public void OnSoundFXSliderChanged(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetSoundFXVolume(val);
        PlayerPrefs.SetFloat("SoundFXVol", val);

        UpdateVolumeText(soundFXText, val);
    }

    public void OnMusicSliderChanged(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetMusicVolume(val);
        PlayerPrefs.SetFloat("MusicVol", val);

        UpdateVolumeText(musicText, val);
    }

    public void OnAmbianceSliderChanged(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetAmbianceVolume(val);
        PlayerPrefs.SetFloat("AmbianceVol", val);

        UpdateVolumeText(ambianceText, val);
    }

    public void OnTypewriterSliderChanged(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetTypewriterVolume(val);
        PlayerPrefs.SetFloat("TypewriterVol", val);

        UpdateVolumeText(typewriterText, val);
    }

    public void OnUISliderChanged(float val)
    {
        if (SoundManager.Instance != null) SoundManager.Instance.SetUIVolume(val);
        PlayerPrefs.SetFloat("UIVol", val);

        UpdateVolumeText(uiText, val);
    }

    private void InitializeQuality()
    {
        qualityDropdown.ClearOptions();
        List<string> options = new List<string>();

        // Her bir key için o anki dildeki karşılığını çek
        foreach (string key in qualityKeys)
        {
            string localizedName = "MISSING_TEXT"; // Güvenlik önlemi

            if (LocalizationManager.Instance != null)
                localizedName = LocalizationManager.Instance.GetText(key);

            options.Add(localizedName);
        }

        qualityDropdown.AddOptions(options);

        int currentQuality = PlayerPrefs.GetInt("Quality", 2); // Varsayılan: Medium (Index 2)

        // Grafik ayarını uygula
        QualitySettings.SetQualityLevel(currentQuality, true);

        qualityDropdown.value = currentQuality;
        qualityDropdown.RefreshShownValue();
    }

    // Dropdown'a bağlanacak fonksiyon
    public void SetQuality(int qualityIndex)
    {
        QualitySettings.SetQualityLevel(qualityIndex, true);

        PlayerPrefs.SetInt("Quality", qualityIndex);
    }

    private void DecideResolutions()
    {
        Resolution[] allResolutions = Screen.resolutions;

        filteredResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        int currentResolutionIndex = 0;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            if (allResolutions[i].width < 1024 || allResolutions[i].height < 720)
                continue;

            string option = allResolutions[i].width + " x " + allResolutions[i].height;

            if (!options.Contains(option))
            {
                options.Add(option);
                filteredResolutions.Add(allResolutions[i]);

                if (allResolutions[i].width == Screen.width &&
                    allResolutions[i].height == Screen.height)
                {
                    currentResolutionIndex = filteredResolutions.Count - 1;
                }
            }
        }

        if (options.Count == 0)
        {
            string currentOption = Screen.width + " x " + Screen.height;
            options.Add(currentOption);
            filteredResolutions.Add(Screen.currentResolution);
            currentResolutionIndex = 0;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    public void SetResolution(int resolutionIndex)
    {
        StartCoroutine(SetResolutionRoutine(resolutionIndex));
    }

    private IEnumerator SetResolutionRoutine(int resolutionIndex)
    {
        Resolution resolution = filteredResolutions[resolutionIndex];
        Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen);

        // Unity'nin kendine gelmesi için biraz daha uzun bir nefes (3 kare)
        yield return null;
        yield return null;
        yield return null;

        if (MenuManager.Instance != null)
        {
            // 1. Önce Scalerları güncelle (UI Boyutlansın)
            MenuManager.Instance.RefreshAllCanvases();

            // 2. Canvas'ı zorla güncelle (Layoutlar otursun)
            Canvas.ForceUpdateCanvases();

            // 3. ŞİMDİ Pozisyonları düzelt
            MenuManager.Instance.FixMenuPositions();

            InitializeUIScale();
        }
    }

    // --- LANGUAGE KISMI (YENİ EKLENDİ) ---

    private void InitializeLanguage()
    {
        // 1. DROPDOWN'I ZENGİN METİNLE DOLDUR
        PopulateLanguageDropdown();

        // 2. MEVCUT DİLİ SEÇ
        string currentLang = PlayerPrefs.GetString("Language", "en");

        // Listeden kodu bulup indexi ayarlıyoruz
        int index = languageDisplayList.FindIndex(x => x.languageCode == currentLang);

        if (index != -1)
        {
            languageDropdown.value = index;
            languageDropdown.RefreshShownValue();
        }

        // 3. DİLİ UYGULA
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.ChangeLanguage(currentLang);
        }
    }

    private void PopulateLanguageDropdown()
    {
        languageDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (var langOption in languageDisplayList)
        {
            // Font adı ve Font boyutu
            string fontName = langOption.fontAsset.name;
            string pixelSize = langOption.fontSize.ToString("F0");

            // YENİ: Offset değerini string'e çevir
            string offsetVal = langOption.vOffset.ToString("F0");

            // --- BÜYÜLÜ FORMÜL ---
            // <voffset=14> ... </voffset>

            string finalString = $"<font=\"{fontName}\"><size={pixelSize}><voffset={offsetVal}>{langOption.nativeName}</voffset></size></font>";

            options.Add(finalString);
        }

        languageDropdown.AddOptions(options);
    }

    // YENİ EKLEME: Dropdown'dan seçim yapıldığında index'i koda çevirip dili değiştirmemiz lazım
    public void OnLanguageDropdownChanged(int index)
    {
        if (index >= 0 && index < languageDisplayList.Count)
        {
            SetLanguage(index); // Mevcut SetLanguage fonksiyonunu string alacak şekilde overload etmen gerekebilir veya aşağıya bak
        }
    }

    // Mevcut SetLanguage int alıyordu, onu güncellememiz gerekebilir.
    // Eğer senin Dropdown eventin direkt SetLanguage(int index) çağırıyorsa:
    public void SetLanguage(int index)
    {
        // Listeden kodu bul
        string selectedCode = languageDisplayList[index].languageCode;

        LocalizationManager.Instance.ChangeLanguage(selectedCode);

        PlayerPrefs.SetString("Language", selectedCode);
        PlayerPrefs.Save();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += RefreshDropdowns;
    }

    private void OnDisable()
    {

        PlayerPrefs.Save();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= RefreshDropdowns;
    }

    // Dil değişince bu çalışacak
    private void RefreshDropdowns()
    {
        // Kalite listesini yeni dile göre tekrar oluştur
        InitializeQuality();
        InitializeVideoSettings();
        InitializePixelation();
        InitializeUIScale();
        InitializeGameplaySettings();
        InitializeControls();
    }

    public void ResetGamepadUISettings()
    {
        // 1. Stick Layout (Varsayılan: 0 -> Normal)
        if (stickLayoutDropdown != null)
        {
            stickLayoutDropdown.value = 0;
            stickLayoutDropdown.RefreshShownValue();
            OnStickLayoutChanged(0); // PlayerPrefs kaydet ve InputManager'a bildir
        }

        // 2. Controller Prompts (Varsayılan: 0 -> Xbox)
        if (controllerPromptsDropdown != null)
        {
            controllerPromptsDropdown.value = 0;
            controllerPromptsDropdown.RefreshShownValue();
            OnControllerPromptsChanged(0); // PlayerPrefs kaydet ve Event fırlat
        }

        if (aimAssistDropdown != null)
        {
            aimAssistDropdown.value = 1;
            aimAssistDropdown.RefreshShownValue();
            OnAimAssistChanged(1);
        }

        Debug.Log("Gamepad Dropdownları ve Ayarları Sıfırlandı.");
    }

    public void ResetControlsSettings()
    {
        // 1. Mouse Hassasiyeti (Varsayılan: 30)
        if (mouseSensSlider != null)
        {
            mouseSensSlider.value = 30f;
            OnMouseSensChanged(30f);
        }

        // 2. Gamepad Hassasiyeti (Varsayılan: 50)
        if (gamepadSensSlider != null)
        {
            gamepadSensSlider.value = 50f;
            OnGamepadSensChanged(50f);
        }

        // 3. Invert Y (Varsayılan: Kapalı / 1)
        if (invertYDropdown != null)
        {
            invertYDropdown.value = 0;
            invertYDropdown.RefreshShownValue();
            OnInvertYChanged(0);
        }

        // 4. Sprint Mode (Varsayılan: Hold / 0)
        if (sprintModeDropdown != null)
        {
            sprintModeDropdown.value = 0;
            sprintModeDropdown.RefreshShownValue();
            OnSprintModeChanged(0);
        }

        // 5. Crouch Mode (Varsayılan: Hold / 0)
        if (crouchModeDropdown != null)
        {
            crouchModeDropdown.value = 0;
            crouchModeDropdown.RefreshShownValue();
            OnCrouchModeChanged(0);
        }

        // NOT: Senin daha önce yazdığın "ResetGamepadUISettings" fonksiyonunu da 
        // buraya dahil etmek istersen (Stick Layout, Aim Assist vb. de sıfırlansın diye)
        // şu satırı açabilirsin:
        // ResetGamepadUISettings(); 

        Debug.Log("Kontrol Ayarları Varsayılanlara Döndü.");
    }

    // ==================================================================================
    // 2. SES AYARLARI SIFIRLAMA
    // ==================================================================================
    public void ResetAudioSettings()
    {
        // Settings.cs başında tanımladığın "default...Volume" değişkenlerini kullanıyoruz.

        // Ana Ses
        if (masterSlider != null)
        {
            masterSlider.value = defaultMasterVolume;
            OnMasterSliderChanged(defaultMasterVolume);
        }

        // Ses Efektleri
        if (soundFXSlider != null)
        {
            soundFXSlider.value = defaultSoundFXVolume;
            OnSoundFXSliderChanged(defaultSoundFXVolume);
        }

        // Müzik
        if (musicSlider != null)
        {
            musicSlider.value = defaultMusicVolume;
            OnMusicSliderChanged(defaultMusicVolume);
        }

        // Ambiyans
        if (ambianceSlider != null)
        {
            ambianceSlider.value = defaultAmbianceVolume;
            OnAmbianceSliderChanged(defaultAmbianceVolume);
        }

        // Typewriter (Daktilo Sesi)
        if (typewriterSlider != null)
        {
            typewriterSlider.value = defaultTypewriterVolume;
            OnTypewriterSliderChanged(defaultTypewriterVolume);
        }

        // UI Sesi
        if (uiSlider != null)
        {
            uiSlider.value = defaultUIVolume;
            OnUISliderChanged(defaultUIVolume);
        }

        Debug.Log("Ses Ayarları Varsayılanlara Döndü.");
    }

    // ==================================================================================
    // 3. GENEL AYARLAR SIFIRLAMA
    // ==================================================================================
    public void ResetGeneralSettings()
    {
        // --- ÇÖZÜNÜRLÜK KISMI İPTAL EDİLDİ ---
        // Çözünürlük değişimi donanımı yoruyor ve ekranı kilitleyebiliyor.
        // O yüzden çözünürlüğe dokunmuyoruz, oyuncu ne seçtiyse o kalsın.
        // ------------------------------------

        // 1. Kalite (Varsayılan: Medium / 2)
        if (qualityDropdown != null)
        {
            qualityDropdown.value = 2;
            qualityDropdown.RefreshShownValue();
            SetQuality(2);
        }

        // YENİ: VSync (Varsayılan: ON / Dropdown Index 0)
        if (vSyncDropdown != null)
        {
            vSyncDropdown.value = 0; // 0 = "UI_ON"
            vSyncDropdown.RefreshShownValue();

            // SetVSync(0) çağırınca yukarıdaki yeni mantıkla Unity ayarını 1 yapacak.
            SetVSync(0);
        }

        // YENİ: FPS (Varsayılan: Unlimited)
        if (fpsDropdown != null)
        {
            // Sınırsız listenin son elemanı
            int lastIndex = fpsValues.Length - 1;
            fpsDropdown.value = lastIndex;
            fpsDropdown.RefreshShownValue();
            SetMaxFPS(lastIndex);
        }

        // 3. Arayüz Boyutu (Varsayılan: 0 - Offset Yok)
        if (uiScaleDropdown != null)
        {
            uiScaleDropdown.value = 0;
            uiScaleDropdown.RefreshShownValue();
            OnUIScaleChanged(0);
        }

        // 4. İpuçları (Varsayılan: Görünür / 0)
        if (hintsDropdown != null)
        {
            hintsDropdown.value = 0;
            hintsDropdown.RefreshShownValue();
            SetHints(0);
        }

        // 5. Etkileşim Metni (Varsayılan: Görünür / 0)
        if (interactTextDropdown != null)
        {
            interactTextDropdown.value = 0;
            interactTextDropdown.RefreshShownValue();
            SetInteractText(0);
        }

        // 6. Pikselleşme (Varsayılan: HD / 0)
        if (pixelationDropdown != null)
        {
            pixelationDropdown.value = 0; // 0: En net
            pixelationDropdown.RefreshShownValue();
            SetPixelation(0);
        }

        Debug.Log("Genel Ayarlar (Çözünürlük Hariç) Varsayılanlara Döndü.");
    }
}