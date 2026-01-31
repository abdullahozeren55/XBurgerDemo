using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    [Header("Global UI Settings")]
    // YENİ: Tüm dropdownlarda kullanılacak font tipi
    public FontType dropdownFontType = FontType.RetroUINoOutline;

    [Header("Resolution Settings")]
    public TMP_Dropdown resolutionDropdown;
    private List<Resolution> filteredResolutions;

    [Header("Language Settings")]
    public TMP_Dropdown languageDropdown;

    [Serializable]
    public struct LanguageDisplayOption
    {
        public string languageCode;
        public string nativeName;
        public TMP_FontAsset fontAsset;
        public float fontSize;
        // vOffset SİLİNDİ
    }

    public List<LanguageDisplayOption> languageDisplayList;

    [Header("Quality Settings")]
    public TMP_Dropdown qualityDropdown;

    [Header("Distortion Settings")]
    public TMP_Dropdown distortionDropdown;
    // Static olarak her yerden erişilebilir çarpan (Limitör)
    public static float GlobalDistortionMultiplier { get; private set; } = 1f;

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
    public static event Action<float> OnDistortionChanged;
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

    private readonly int[] fpsValues = { 30, 60, 75, 120, 144, 165, 240, 300, 360, -1 };

    private readonly List<string> qualityKeys = new List<string> { "UI_LOW", "UI_MEDIUM", "UI_HIGH" };
    private readonly List<string> onOffKeys = new List<string> { "UI_ON", "UI_OFF" };
    private readonly List<string> offOnKeys = new List<string> { "UI_OFF", "UI_ON" }; // Invert Y için
    private readonly List<string> holdToggleKeys = new List<string> { "UI_HOLD", "UI_TOGGLE" };
    private readonly List<string> stickLayoutKeys = new List<string> { "UI_NORMAL", "UI_SWAPPED" };
    private readonly List<string> promptsKeys = new List<string> { "UI_XBOX", "UI_PS" };
    private readonly List<string> aimAssistKeys = new List<string> { "UI_OFF", "UI_LOW", "UI_MEDIUM", "UI_HIGH" };

    void Start()
    {
        DecideResolutions();
        InitializeLanguage(); // Bunu en başa aldım ki dil değişince diğerleri doğru formatla gelsin
        InitializeQuality();
        InitializeVideoSettings();
        InitializeAudio();
        InitializeDistortion();
        InitializeGameplaySettings();
        InitializeControls();
    }

    // ==================================================================================
    // YENİ: DROPDOWN METİN FORMATLAYICI
    // ==================================================================================
    // Bu metot, LocalizedText'in yaptığı işi string tagleri ile yapar.
    private string FormatDropdownText(string rawText)
    {
        if (LocalizationManager.Instance == null) return rawText;

        var targetData = LocalizationManager.Instance.GetFontDataForCurrentLanguage(dropdownFontType);
        var defaultData = LocalizationManager.Instance.GetDefaultFontData(dropdownFontType);

        if (targetData.font == null) return rawText;

        // A. Font Boyutu Oranı (Yüzde)
        float defaultBase = Mathf.Max(defaultData.basePixelSize, 1f);
        float ratio = targetData.basePixelSize / defaultBase;
        float sizePercentage = ratio * 100f;

        // B. vOffset SİLİNDİ

        // C. Karakter Boşluğu (Bu kalsın, japonca için iyidir)
        float charSpacingDiff = targetData.characterSpacingOffset - defaultData.characterSpacingOffset;

        // --- ETİKETLER ---
        string fontName = targetData.font.name;
        string prefix = $"<font=\"{fontName}\"><size={sizePercentage:F0}%>";
        string suffix = "</size></font>";

        // Sadece cspace ekliyoruz artık
        if (Mathf.Abs(charSpacingDiff) > 0.1f)
        {
            prefix += $"<cspace={charSpacingDiff:F2}>";
            suffix = "</cspace>" + suffix;
        }

        return $"{prefix}{rawText}{suffix}";
    }

    // ==================================================================================

    private void InitializeControls()
    {
        float mSens = PlayerPrefs.GetFloat("MouseSens", 30f);
        mouseSensSlider.value = mSens;
        UpdateSensText(mouseSensText, mSens);

        float gSens = PlayerPrefs.GetFloat("GamepadSens", 50f);
        gamepadSensSlider.value = gSens;
        UpdateSensText(gamepadSensText, gSens);

        // YENİ: PopulateDropdown artık otomatik formatlıyor
        PopulateDropdown(invertYDropdown, offOnKeys);
        PopulateDropdown(sprintModeDropdown, holdToggleKeys);
        PopulateDropdown(crouchModeDropdown, holdToggleKeys);
        PopulateDropdown(stickLayoutDropdown, stickLayoutKeys);
        PopulateDropdown(aimAssistDropdown, aimAssistKeys);
        PopulateDropdown(controllerPromptsDropdown, promptsKeys);

        // Değer atamaları aynı kalıyor
        invertYDropdown.value = PlayerPrefs.GetInt("InvertY", 0);
        invertYDropdown.RefreshShownValue();

        sprintModeDropdown.value = PlayerPrefs.GetInt("SprintMode", 0);
        sprintModeDropdown.RefreshShownValue();

        crouchModeDropdown.value = PlayerPrefs.GetInt("CrouchMode", 0);
        crouchModeDropdown.RefreshShownValue();

        int savedStickLayout = PlayerPrefs.GetInt("StickLayout", 0);
        stickLayoutDropdown.value = savedStickLayout;
        stickLayoutDropdown.RefreshShownValue();
        if (InputManager.Instance != null) InputManager.Instance.SetSwapSticks(savedStickLayout == 1);

        int savedPrompts = PlayerPrefs.GetInt("ControllerPrompts", 0);
        controllerPromptsDropdown.value = savedPrompts;
        controllerPromptsDropdown.RefreshShownValue();
        IsXboxPrompts = (savedPrompts == 0);

        int savedAssist = PlayerPrefs.GetInt("AimAssist", 1);
        aimAssistDropdown.value = savedAssist;
        aimAssistDropdown.RefreshShownValue();
        if (InputManager.Instance != null) InputManager.Instance.SetAimAssistLevel(savedAssist);
    }

    // YENİLENMİŞ POPULATE METODU
    private void PopulateDropdown(TMP_Dropdown dropdown, List<string> keys)
    {
        dropdown.ClearOptions();
        List<string> options = new List<string>();
        foreach (string key in keys)
        {
            string text = "MISSING";
            if (LocalizationManager.Instance != null)
                text = LocalizationManager.Instance.GetText(key);

            // BURADA FORMATLIYORUZ
            options.Add(FormatDropdownText(text));
        }
        dropdown.AddOptions(options);
    }

    // ... (UpdateSensText ve Event Callback'ler AYNI - Kod kalabalığı olmasın diye atladım) ...
    // Sadece UpdateSensText vb. aşağıdakiler aynı kalacak.
    private void UpdateSensText(TMP_Text textComp, float val) => textComp.text = val.ToString("F0");
    public void OnAimAssistChanged(int index) { PlayerPrefs.SetInt("AimAssist", index); if (InputManager.Instance != null) InputManager.Instance.SetAimAssistLevel(index); }
    public void OnMouseSensChanged(float val) { PlayerPrefs.SetFloat("MouseSens", val); UpdateSensText(mouseSensText, val); if (InputManager.Instance != null) InputManager.Instance.SetMouseSensitivity(val); }
    public void OnGamepadSensChanged(float val) { PlayerPrefs.SetFloat("GamepadSens", val); UpdateSensText(gamepadSensText, val); if (InputManager.Instance != null) InputManager.Instance.SetGamepadSensitivity(val); }
    public void OnInvertYChanged(int index) { PlayerPrefs.SetInt("InvertY", index); if (InputManager.Instance != null) InputManager.Instance.SetInvertY(index == 1); }
    public void OnSprintModeChanged(int index) { PlayerPrefs.SetInt("SprintMode", index); if (InputManager.Instance != null) InputManager.Instance.SetSprintMode(index == 1); }
    public void OnCrouchModeChanged(int index) { PlayerPrefs.SetInt("CrouchMode", index); if (InputManager.Instance != null) InputManager.Instance.SetCrouchMode(index == 1); }
    public void OnStickLayoutChanged(int index) { PlayerPrefs.SetInt("StickLayout", index); if (InputManager.Instance != null) InputManager.Instance.SetSwapSticks(index == 1); OnStickLayoutChangedEvent?.Invoke(); }
    public void OnControllerPromptsChanged(int index) { PlayerPrefs.SetInt("ControllerPrompts", index); IsXboxPrompts = (index == 0); OnPromptsChanged?.Invoke(); }

    private void InitializeGameplaySettings()
    {
        // YENİLENMİŞ MANUEL LİSTE OLUŞTURMA
        List<string> options = new List<string>();
        foreach (string key in onOffKeys)
        {
            string localizedName = "MISSING";
            if (LocalizationManager.Instance != null)
                localizedName = LocalizationManager.Instance.GetText(key);

            // Formatla
            options.Add(FormatDropdownText(localizedName));
        }

        hintsDropdown.ClearOptions();
        hintsDropdown.AddOptions(options);
        hintsDropdown.value = PlayerPrefs.GetInt("ShowHints", 0);
        hintsDropdown.RefreshShownValue();

        interactTextDropdown.ClearOptions();
        interactTextDropdown.AddOptions(options);
        interactTextDropdown.value = PlayerPrefs.GetInt("ShowInteractText", 0);
        interactTextDropdown.RefreshShownValue();
    }

    private void InitializeVideoSettings()
    {
        // 1. Varsayılan Anahtarları Oluştur (Yoksa)
        if (!PlayerPrefs.HasKey("VSync"))
        {
            QualitySettings.vSyncCount = 1;
            PlayerPrefs.SetInt("VSync", 0); // 0 = UI_ON (Listenin ilk elemanı)
            PlayerPrefs.Save();
        }
        if (!PlayerPrefs.HasKey("TargetFPS"))
        {
            Application.targetFrameRate = -1;
            PlayerPrefs.SetInt("TargetFPS", -1);
            PlayerPrefs.Save();
        }

        // 2. VSync Dropdown Ayarları
        PopulateDropdown(vSyncDropdown, onOffKeys); // onOffKeys = {"UI_ON", "UI_OFF"}

        int savedVSyncIndex = PlayerPrefs.GetInt("VSync", 0);
        // Index 0 ise (ON) -> vSyncCount 1 olsun. Index 1 ise (OFF) -> vSyncCount 0 olsun.
        QualitySettings.vSyncCount = (savedVSyncIndex == 0) ? 1 : 0;

        vSyncDropdown.value = savedVSyncIndex;
        vSyncDropdown.RefreshShownValue();

        // 3. FPS Dropdown Ayarları
        InitializeFPSDropdown();

        // --- DÜZELTME BURADA ---
        // Eskiden 'Application.targetFrameRate' okuyorduk, bu yanlıştı.
        // Doğrudan kaydedilmiş tercihi okuyoruz:
        int savedFPS = PlayerPrefs.GetInt("TargetFPS", -1);

        // Bu FPS değeri listede kaçıncı sırada? Bulamazsa -1 (Unlimited) yap.
        int fpsIndex = GetFPSIndex(savedFPS);
        fpsDropdown.value = fpsIndex;
        fpsDropdown.RefreshShownValue();

        // 4. Değerleri Motora Uygula (Kritik Adım)
        // Eğer VSync KAPALI ise (Index 1), FPS limitini hemen uygula.
        // VSync AÇIK ise (Index 0), FPS limiti -1 olsun (Unity kuralı).
        if (savedVSyncIndex == 1)
        {
            Application.targetFrameRate = savedFPS;
        }
        else
        {
            Application.targetFrameRate = -1;
        }

        // 5. UI Etkileşimini Güncelle
        UpdateFPSDropdownInteractivity(savedVSyncIndex == 0);
    }

    private void InitializeFPSDropdown()
    {
        fpsDropdown.ClearOptions();
        List<string> options = new List<string>();

        string unlimitedText = "Unlimited";
        if (LocalizationManager.Instance != null)
            unlimitedText = LocalizationManager.Instance.GetText("UI_UNLIMITED");

        // "Unlimited" metnini kesinlikle formatlamalıyız
        string formattedUnlimited = FormatDropdownText(unlimitedText);

        foreach (int fps in fpsValues)
        {
            if (fps == -1)
                options.Add(formattedUnlimited);
            else
            {
                // Rakamları da formatlıyoruz ki font bütünlüğü bozulmasın (Japonca fontun rakamları farklı olabilir)
                options.Add(FormatDropdownText(fps.ToString()));
            }
        }
        fpsDropdown.AddOptions(options);
    }

    private int GetFPSIndex(int targetFPS)
    {
        for (int i = 0; i < fpsValues.Length; i++) { if (fpsValues[i] == targetFPS) return i; }
        return fpsValues.Length - 1;
    }

    // ... (SetVSync, SetMaxFPS, UpdateFPSDropdownInteractivity, SetHints, SetInteractText AYNI) ...
    public void SetVSync(int index)
    {
        // Index 0: ON, Index 1: OFF
        bool isVSyncOn = (index == 0);
        QualitySettings.vSyncCount = isVSyncOn ? 1 : 0;

        UpdateFPSDropdownInteractivity(isVSyncOn);

        if (isVSyncOn)
        {
            Application.targetFrameRate = -1;
        }
        else
        {
            // VSync kapattığımız an, FPS dropdown'daki değeri değil,
            // GERÇEK kayıtlı hedefi veya dropdown'ın temsil ettiği değeri almalıyız.
            // fpsDropdown.value bazen güncellenmemiş olabilir, o yüzden listeden çekmek daha güvenli:

            int currentDropdownIndex = fpsDropdown.value;
            // Diziden güvenli şekilde FPS değerini al
            if (currentDropdownIndex >= 0 && currentDropdownIndex < fpsValues.Length)
            {
                int targetFPS = fpsValues[currentDropdownIndex];
                Application.targetFrameRate = targetFPS;
                PlayerPrefs.SetInt("TargetFPS", targetFPS); // Emin olmak için tekrar kaydet
            }
        }

        PlayerPrefs.SetInt("VSync", index);
        PlayerPrefs.Save();
    }
    public void SetMaxFPS(int index) { if (QualitySettings.vSyncCount > 0) return; int targetFPS = fpsValues[index]; Application.targetFrameRate = targetFPS; PlayerPrefs.SetInt("TargetFPS", targetFPS); PlayerPrefs.Save(); }
    private void UpdateFPSDropdownInteractivity(bool isVSyncOn) { if (fpsDropdown != null) { fpsDropdown.interactable = !isVSyncOn; fpsDropdownGroup.alpha = isVSyncOn ? 0.5f : 1f; } }
    public void SetHints(int index) { PlayerPrefs.SetInt("ShowHints", index); PlayerPrefs.Save(); if (PlayerManager.Instance != null) PlayerManager.Instance.UpdateGameplaySettings(); if (MonitorManager.Instance != null) MonitorManager.Instance.UpadateShowHint(); }
    public void SetInteractText(int index) { PlayerPrefs.SetInt("ShowInteractText", index); PlayerPrefs.Save(); if (PlayerManager.Instance != null) PlayerManager.Instance.UpdateGameplaySettings(); }

    private void InitializeUIScale()
    {
        uiScaleDropdown.ClearOptions();
        List<string> options = new List<string>();

        int maxScale = Mathf.FloorToInt(Screen.height / 360f);
        if (maxScale < 1) maxScale = 1;

        for (int i = 0; i < maxScale; i++)
        {
            string label = "";
            int scaleVal = maxScale - i;
            
            label = scaleVal + "x";

            // YENİ: UI Scale metinlerini de formatlıyoruz
            options.Add(FormatDropdownText(label));
        }

        uiScaleDropdown.AddOptions(options);

        int savedOffset = PlayerPrefs.GetInt("UIScaleOffset", 1);
        if (savedOffset >= options.Count) savedOffset = options.Count - 1;

        uiScaleDropdown.value = savedOffset;
        uiScaleDropdown.RefreshShownValue();

        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.RefreshAllCanvases(savedOffset);
            MenuManager.Instance.FixMenuPositions();
        }
    }

    public void OnUIScaleChanged(int index) { PlayerPrefs.SetInt("UIScaleOffset", index); PlayerPrefs.Save(); StartCoroutine(ApplyUIScaleRoutine(index)); }
    private IEnumerator ApplyUIScaleRoutine(int offset) { if (MenuManager.Instance != null) { MenuManager.Instance.RefreshAllCanvases(offset); Canvas.ForceUpdateCanvases(); yield return null; yield return null; MenuManager.Instance.FixMenuPositions(); } }

    private void InitializeDistortion()
    {
        distortionDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (string key in aimAssistKeys) //Aim Assist ile aynı seçeneklere sahip diye kullandım
        {
            string localizedName = "MISSING";
            if (LocalizationManager.Instance != null)
                localizedName = LocalizationManager.Instance.GetText(key);

            // Senin Dropdown Formatlayıcın
            options.Add(FormatDropdownText(localizedName));
        }

        distortionDropdown.AddOptions(options);

        // Varsayılan: HIGH (3) -> Yani %100 Bozulma
        int savedIndex = PlayerPrefs.GetInt("DistortionLevel", 3);
        distortionDropdown.value = savedIndex;
        distortionDropdown.RefreshShownValue();

        // Çarpanı Başlangıçta Ayarla
        UpdateDistortionMultiplier(savedIndex);

        // YENİ: Başlangıçta event'i tetikle ki herkes güncel değeri bilsin
        OnDistortionChanged?.Invoke(GlobalDistortionMultiplier);
    }

    // --- YENİ SETTER METODU (Unity Event'e Bağlayacaksın) ---
    public void SetDistortion(int index)
    {
        PlayerPrefs.SetInt("DistortionLevel", index);
        PlayerPrefs.Save();
        UpdateDistortionMultiplier(index);

        // YENİ: Değişikliği herkese duyur
        OnDistortionChanged?.Invoke(GlobalDistortionMultiplier);
    }

    // --- YENİ HELPER: ÇARPAN MANTIĞI ---
    private void UpdateDistortionMultiplier(int index)
    {
        switch (index)
        {
            case 0: GlobalDistortionMultiplier = 0.0f; break; // OFF: Hiç bozulma yok (Midesi bulananlar için)
            case 1: GlobalDistortionMultiplier = 0.3f; break; // LOW: Az
            case 2: GlobalDistortionMultiplier = 0.7f; break; // MEDIUM: Orta
            case 3: GlobalDistortionMultiplier = 1.0f; break; // HIGH: Tam gaz
            default: GlobalDistortionMultiplier = 1.0f; break;
        }
    }

    // ... (UpdateVolumeText, InitializeAudio, Slider Callbacks AYNI) ...
    private void UpdateVolumeText(TMP_Text textComp, float value) { if (textComp != null) { int percentage = Mathf.RoundToInt(value * 100); textComp.text = "%" + percentage; } }
    private void InitializeAudio() { float masterVol = PlayerPrefs.GetFloat("MasterVol", defaultMasterVolume); float soundFXVol = PlayerPrefs.GetFloat("SoundFXVol", defaultSoundFXVolume); float musicVol = PlayerPrefs.GetFloat("MusicVol", defaultMusicVolume); float ambianceVol = PlayerPrefs.GetFloat("AmbianceVol", defaultAmbianceVolume); float typewriterVol = PlayerPrefs.GetFloat("TypewriterVol", defaultTypewriterVolume); float uiVol = PlayerPrefs.GetFloat("UIVol", defaultUIVolume); if (masterSlider) masterSlider.value = masterVol; if (soundFXSlider) soundFXSlider.value = soundFXVol; if (musicSlider) musicSlider.value = musicVol; if (ambianceSlider) ambianceSlider.value = ambianceVol; if (typewriterSlider) typewriterSlider.value = typewriterVol; if (uiSlider) uiSlider.value = uiVol; UpdateVolumeText(masterText, masterVol); UpdateVolumeText(soundFXText, soundFXVol); UpdateVolumeText(musicText, musicVol); UpdateVolumeText(ambianceText, ambianceVol); UpdateVolumeText(typewriterText, typewriterVol); UpdateVolumeText(uiText, uiVol); if (SoundManager.Instance != null) { SoundManager.Instance.SetMasterVolume(masterVol); SoundManager.Instance.SetSoundFXVolume(soundFXVol); SoundManager.Instance.SetMusicVolume(musicVol); SoundManager.Instance.SetAmbianceVolume(ambianceVol); SoundManager.Instance.SetTypewriterVolume(typewriterVol); SoundManager.Instance.SetUIVolume(uiVol); } }
    public void OnMasterSliderChanged(float val) { if (SoundManager.Instance != null) SoundManager.Instance.SetMasterVolume(val); PlayerPrefs.SetFloat("MasterVol", val); UpdateVolumeText(masterText, val); }
    public void OnSoundFXSliderChanged(float val) { if (SoundManager.Instance != null) SoundManager.Instance.SetSoundFXVolume(val); PlayerPrefs.SetFloat("SoundFXVol", val); UpdateVolumeText(soundFXText, val); }
    public void OnMusicSliderChanged(float val) { if (SoundManager.Instance != null) SoundManager.Instance.SetMusicVolume(val); PlayerPrefs.SetFloat("MusicVol", val); UpdateVolumeText(musicText, val); }
    public void OnAmbianceSliderChanged(float val) { if (SoundManager.Instance != null) SoundManager.Instance.SetAmbianceVolume(val); PlayerPrefs.SetFloat("AmbianceVol", val); UpdateVolumeText(ambianceText, val); }
    public void OnTypewriterSliderChanged(float val) { if (SoundManager.Instance != null) SoundManager.Instance.SetTypewriterVolume(val); PlayerPrefs.SetFloat("TypewriterVol", val); UpdateVolumeText(typewriterText, val); }
    public void OnUISliderChanged(float val) { if (SoundManager.Instance != null) SoundManager.Instance.SetUIVolume(val); PlayerPrefs.SetFloat("UIVol", val); UpdateVolumeText(uiText, val); }

    private void InitializeQuality()
    {
        qualityDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (string key in qualityKeys)
        {
            string localizedName = "MISSING_TEXT";
            if (LocalizationManager.Instance != null)
                localizedName = LocalizationManager.Instance.GetText(key);

            // Formatla
            options.Add(FormatDropdownText(localizedName));
        }

        qualityDropdown.AddOptions(options);
        int currentQuality = PlayerPrefs.GetInt("Quality", 2);
        QualitySettings.SetQualityLevel(currentQuality, true);
        qualityDropdown.value = currentQuality;
        qualityDropdown.RefreshShownValue();
    }

    public void SetQuality(int qualityIndex) { QualitySettings.SetQualityLevel(qualityIndex, true); PlayerPrefs.SetInt("Quality", qualityIndex); }

    private void DecideResolutions()
    {
        Resolution[] allResolutions = Screen.resolutions;
        filteredResolutions = new List<Resolution>();
        resolutionDropdown.ClearOptions();

        List<string> options = new List<string>();
        HashSet<string> addedResolutions = new HashSet<string>();

        // Başlangıçta -1 yapıyoruz ki bulamazsak tespit edebilelim
        int currentResolutionIndex = -1;

        for (int i = 0; i < allResolutions.Length; i++)
        {
            // Min çözünürlük filtresi
            if (allResolutions[i].width < 1024 || allResolutions[i].height < 720) continue;

            // Max çözünürlük filtresi (Full HD Sınırı)
            if (allResolutions[i].width > 1920 || allResolutions[i].height > 1080) continue;

            string rawOption = allResolutions[i].width + " x " + allResolutions[i].height;

            if (addedResolutions.Contains(rawOption)) continue;

            addedResolutions.Add(rawOption);
            options.Add(FormatDropdownText(rawOption));
            filteredResolutions.Add(allResolutions[i]);

            // Şu anki ekran çözünürlüğü, listeye eklediğimiz bu çözünürlükle eşleşiyor mu?
            if (allResolutions[i].width == Screen.width && allResolutions[i].height == Screen.height)
            {
                currentResolutionIndex = filteredResolutions.Count - 1;
            }
        }

        // --- KRİTİK DÜZELTME BURADA ---

        // Eğer currentResolutionIndex hala -1 ise, bu demektir ki;
        // Mevcut ekran çözünürlüğümüz (örn: 4K), filtrelenmiş listemizde YOK.
        // Bu durumda oyunu zorla listemizdeki EN YÜKSEK (son) çözünürlüğe çekmeliyiz.
        if (currentResolutionIndex == -1 && filteredResolutions.Count > 0)
        {
            // Listemiz küçükten büyüğe sıralı olduğu için son eleman en yüksek (1080p) olandır.
            currentResolutionIndex = filteredResolutions.Count - 1;

            // Hemen şimdi uygula (Oyuncu fark etmeden düzelt)
            Resolution targetRes = filteredResolutions[currentResolutionIndex];
            Screen.SetResolution(targetRes.width, targetRes.height, Screen.fullScreen);

            Debug.Log($"Çözünürlük çok yüksekti, otomatik olarak {targetRes.width}x{targetRes.height} değerine düşürüldü.");
        }
        else if (options.Count == 0)
        {
            // Hiç uygun çözünürlük bulamazsak mecburen mevcut olanı ekle (Fallback)
            string currentOption = Screen.width + " x " + Screen.height;
            options.Add(FormatDropdownText(currentOption));
            filteredResolutions.Add(Screen.currentResolution);
            currentResolutionIndex = 0;
        }

        resolutionDropdown.AddOptions(options);
        resolutionDropdown.value = currentResolutionIndex;
        resolutionDropdown.RefreshShownValue();
    }

    // ... (SetResolutionRoutine, SetResolution AYNI) ...
    public void SetResolution(int resolutionIndex) { StartCoroutine(SetResolutionRoutine(resolutionIndex)); }
    private IEnumerator SetResolutionRoutine(int resolutionIndex) { Resolution resolution = filteredResolutions[resolutionIndex]; Screen.SetResolution(resolution.width, resolution.height, Screen.fullScreen); yield return null; yield return null; yield return null; if (MenuManager.Instance != null) { MenuManager.Instance.RefreshAllCanvases(); Canvas.ForceUpdateCanvases(); MenuManager.Instance.FixMenuPositions(); InitializeUIScale(); } }

    private void InitializeLanguage()
    {
        // NOT: LanguageDropdown kendi özel yapısını kullandığı için FormatDropdownText kullanmıyoruz.
        // Orası zaten <font> tagleriyle manuel ayarlanıyor.
        PopulateLanguageDropdown();

        string currentLang = PlayerPrefs.GetString("Language", "en");
        int index = languageDisplayList.FindIndex(x => x.languageCode == currentLang);
        if (index != -1) { languageDropdown.value = index; languageDropdown.RefreshShownValue(); }

        if (LocalizationManager.Instance != null) LocalizationManager.Instance.ChangeLanguage(currentLang);
    }

    private void PopulateLanguageDropdown()
    {
        languageDropdown.ClearOptions();
        List<string> options = new List<string>();

        foreach (var langOption in languageDisplayList)
        {
            string fontName = langOption.fontAsset.name;
            string pixelSize = langOption.fontSize.ToString("F0");

            // vOffset yok, direkt temiz hali:
            string finalString = $"<font=\"{fontName}\"><size={pixelSize}>{langOption.nativeName}</size></font>";
            options.Add(finalString);
        }
        languageDropdown.AddOptions(options);
    }

    public void OnLanguageDropdownChanged(int index) { if (index >= 0 && index < languageDisplayList.Count) SetLanguage(index); }
    public void SetLanguage(int index) { string selectedCode = languageDisplayList[index].languageCode; LocalizationManager.Instance.ChangeLanguage(selectedCode); PlayerPrefs.SetString("Language", selectedCode); PlayerPrefs.Save(); }

    private void OnEnable() { if (LocalizationManager.Instance != null) LocalizationManager.Instance.OnLanguageChanged += RefreshDropdowns; }
    private void OnDisable() { PlayerPrefs.Save(); if (LocalizationManager.Instance != null) LocalizationManager.Instance.OnLanguageChanged -= RefreshDropdowns; }

    private void RefreshDropdowns()
    {
        // Language değişince her şeyi yeniden oluşturuyoruz, 
        // böylece yeni dilin fontuyla tekrar formatlanıyorlar.
        InitializeQuality();
        InitializeVideoSettings();
        InitializeDistortion();
        InitializeUIScale();
        InitializeGameplaySettings();
        InitializeControls();
        DecideResolutions(); // Çözünürlük fontu da değişsin
    }

    // Reset fonksiyonları AYNI...
    public void ResetGamepadUISettings() { if (stickLayoutDropdown != null) { stickLayoutDropdown.value = 0; stickLayoutDropdown.RefreshShownValue(); OnStickLayoutChanged(0); } if (controllerPromptsDropdown != null) { controllerPromptsDropdown.value = 0; controllerPromptsDropdown.RefreshShownValue(); OnControllerPromptsChanged(0); } if (aimAssistDropdown != null) { aimAssistDropdown.value = 1; aimAssistDropdown.RefreshShownValue(); OnAimAssistChanged(1); } Debug.Log("Gamepad Dropdownları ve Ayarları Sıfırlandı."); }
    public void ResetControlsSettings() { if (mouseSensSlider != null) { mouseSensSlider.value = 30f; OnMouseSensChanged(30f); } if (gamepadSensSlider != null) { gamepadSensSlider.value = 50f; OnGamepadSensChanged(50f); } if (invertYDropdown != null) { invertYDropdown.value = 0; invertYDropdown.RefreshShownValue(); OnInvertYChanged(0); } if (sprintModeDropdown != null) { sprintModeDropdown.value = 0; sprintModeDropdown.RefreshShownValue(); OnSprintModeChanged(0); } if (crouchModeDropdown != null) { crouchModeDropdown.value = 0; crouchModeDropdown.RefreshShownValue(); OnCrouchModeChanged(0); } Debug.Log("Kontrol Ayarları Varsayılanlara Döndü."); }
    public void ResetAudioSettings() { if (masterSlider != null) { masterSlider.value = defaultMasterVolume; OnMasterSliderChanged(defaultMasterVolume); } if (soundFXSlider != null) { soundFXSlider.value = defaultSoundFXVolume; OnSoundFXSliderChanged(defaultSoundFXVolume); } if (musicSlider != null) { musicSlider.value = defaultMusicVolume; OnMusicSliderChanged(defaultMusicVolume); } if (ambianceSlider != null) { ambianceSlider.value = defaultAmbianceVolume; OnAmbianceSliderChanged(defaultAmbianceVolume); } if (typewriterSlider != null) { typewriterSlider.value = defaultTypewriterVolume; OnTypewriterSliderChanged(defaultTypewriterVolume); } if (uiSlider != null) { uiSlider.value = defaultUIVolume; OnUISliderChanged(defaultUIVolume); } Debug.Log("Ses Ayarları Varsayılanlara Döndü."); }
    public void ResetGeneralSettings() { if (qualityDropdown != null) { qualityDropdown.value = 1; qualityDropdown.RefreshShownValue(); SetQuality(1); } if (vSyncDropdown != null) { vSyncDropdown.value = 0; vSyncDropdown.RefreshShownValue(); SetVSync(0); } if (fpsDropdown != null) { int lastIndex = fpsValues.Length - 1; fpsDropdown.value = lastIndex; fpsDropdown.RefreshShownValue(); SetMaxFPS(lastIndex); } if (uiScaleDropdown != null) { uiScaleDropdown.value = 1; uiScaleDropdown.RefreshShownValue(); OnUIScaleChanged(1); } if (hintsDropdown != null) { hintsDropdown.value = 0; hintsDropdown.RefreshShownValue(); SetHints(0); } if (interactTextDropdown != null) { interactTextDropdown.value = 0; interactTextDropdown.RefreshShownValue(); SetInteractText(0); }
        if (distortionDropdown != null)
        {
            distortionDropdown.value = 3; // Default High
            distortionDropdown.RefreshShownValue();
            SetDistortion(3);
        }
        Debug.Log("Genel Ayarlar (Çözünürlük Hariç) Varsayılanlara Döndü."); }
}