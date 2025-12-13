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

    [Header("Quality Settings")]
    public TMP_Dropdown qualityDropdown;

    [Header("Pixelation Settings")]
    public TMP_Dropdown pixelationDropdown;

    [Header("UI Scale Settings")]
    public TMP_Dropdown uiScaleDropdown;

    [Header("Gameplay Settings")]
    public TMP_Dropdown hintsDropdown;
    public TMP_Dropdown interactTextDropdown;

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

    // Dropdown sýrasýna göre dil kodlarý. 
    // Inspector'da Dropdown'a: 
    // Option 0: "Türkçe" yazdýysan buradaki 0. eleman "tr" olmalý.
    // Option 1: "English" yazdýysan buradaki 1. eleman "en" olmalý.
    private readonly List<string> languageCodes = new List<string> { "en", "tr" };

    private readonly List<string> qualityKeys = new List<string>
    {
        "UI_MAIN_MENU_QUALITY_0",
        "UI_MAIN_MENU_QUALITY_1",
        "UI_MAIN_MENU_QUALITY_2",
        "UI_MAIN_MENU_QUALITY_3"
    };

    private readonly List<string> pixelationKeys = new List<string>
    {
        "UI_MAIN_MENU_PIXELATION_0", // HD
        "UI_MAIN_MENU_PIXELATION_1", // Hafif
        "UI_MAIN_MENU_PIXELATION_2", // Orta
    };

    private readonly List<string> visibilityKeys = new List<string>
    {
        "UI_ON",
        "UI_OFF"
    };

    void Start()
    {
        DecideResolutions();
        InitializeLanguage();
        InitializeQuality();
        InitializeAudio();
        InitializePixelation();
        InitializeGameplaySettings();
    }

    private void InitializeGameplaySettings()
    {
        // --- SEÇENEKLERÝ DOLDUR ---
        List<string> options = new List<string>();
        foreach (string key in visibilityKeys)
        {
            string localizedName = "MISSING";
            if (LocalizationManager.Instance != null)
                localizedName = LocalizationManager.Instance.GetText(key);
            options.Add(localizedName);
        }

        // --- HINTS DROPDOWN ---
        hintsDropdown.ClearOptions();
        hintsDropdown.AddOptions(options);
        // Varsayýlan 0 (Visible)
        hintsDropdown.value = PlayerPrefs.GetInt("ShowHints", 0);
        hintsDropdown.RefreshShownValue();

        // --- INTERACT TEXT DROPDOWN ---
        interactTextDropdown.ClearOptions();
        interactTextDropdown.AddOptions(options);
        // Varsayýlan 0 (Visible)
        interactTextDropdown.value = PlayerPrefs.GetInt("ShowInteractText", 0);
        interactTextDropdown.RefreshShownValue();
    }

    // Dropdown Eventleri
    public void SetHints(int index)
    {
        // 0: Visible, 1: Hidden
        PlayerPrefs.SetInt("ShowHints", index);
        PlayerPrefs.Save();

        // Eðer oyun içindeysek anlýk güncelle
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

        // Maksimum scale deðerini bul
        int maxScale = Mathf.FloorToInt(Screen.height / 360f);
        if (maxScale < 1) maxScale = 1;

        // --- DEÐÝÞÝKLÝK BURADA: "Varsayýlan" Metnini Çek ---
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
                // Örn: "(3x) Varsayýlan" veya "(3x) Default"
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

        // Baþlangýçta uygula (RefreshDropdowns'tan gelince tekrar uygulamasý sorun yaratmaz)
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

        // Direkt çaðýrmak yerine Coroutine baþlatýyoruz ki araya "Fix" sýkýþtýralým
        StartCoroutine(ApplyUIScaleRoutine(index));
    }

    private IEnumerator ApplyUIScaleRoutine(int offset)
    {
        if (MenuManager.Instance != null)
        {
            // 1. Önce bütün Canvaslarýn boyunu deðiþtir (Büyüt/Küçült)
            MenuManager.Instance.RefreshAllCanvases(offset);

            // 2. Unity'ye "Hadi bi hesapla þu yeni boyutlarý" de ve bekle
            Canvas.ForceUpdateCanvases();
            yield return null;
            yield return null; // Garanti olsun diye 2 kare bekle

            // 3. ÞÝMDÝ o kayan panelleri yeni sýnýrlara ýþýnla
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
        // limitIndex büyüdükçe (0->3) pikselleþme artar (Kalite düþer)
        // Tam istediðin mantýk bu.
        QualitySettings.globalTextureMipmapLimit = limitIndex;
        PlayerPrefs.SetInt("TextureMipmapLimit", limitIndex);
        PlayerPrefs.Save();
    }

    private void UpdateVolumeText(TMP_Text textComp, float value)
    {
        if (textComp != null)
        {
            // 0-1 arasýndaki deðeri 100 ile çarpýp tam sayýya yuvarlýyoruz
            int percentage = Mathf.RoundToInt(value * 100);
            textComp.text = "%" + percentage; // Veya istersen: percentage + "%"
        }
    }

    private void InitializeAudio()
    {
        // 1. Kayýtlý deðerleri çek (Yoksa 0.75 varsayýlan olsun)
        float masterVol = PlayerPrefs.GetFloat("MasterVol", defaultMasterVolume);
        float soundFXVol = PlayerPrefs.GetFloat("SoundFXVol", defaultSoundFXVolume);
        float musicVol = PlayerPrefs.GetFloat("MusicVol", defaultMusicVolume);
        float ambianceVol = PlayerPrefs.GetFloat("AmbianceVol", defaultAmbianceVolume);
        float typewriterVol = PlayerPrefs.GetFloat("TypewriterVol", defaultTypewriterVolume);
        float uiVol = PlayerPrefs.GetFloat("UIVol", defaultUIVolume);

        // 2. Sliderlarý bu deðerlere getir
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
        // NOT: SoundManager'ýn Start'ýnda da bunu çaðýrýyor olabilirsin, 
        // çakýþmamasý için SoundManager'da PlayerPrefs okuma varsa orayý silip buradan yönetebilirsin.
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

        // Her bir key için o anki dildeki karþýlýðýný çek
        foreach (string key in qualityKeys)
        {
            string localizedName = "MISSING_TEXT"; // Güvenlik önlemi

            if (LocalizationManager.Instance != null)
                localizedName = LocalizationManager.Instance.GetText(key);

            options.Add(localizedName);
        }

        qualityDropdown.AddOptions(options);

        int currentQuality = PlayerPrefs.GetInt("Quality", 2); // Varsayýlan: Medium (Index 2)

        // Grafik ayarýný uygula
        QualitySettings.SetQualityLevel(currentQuality, true);

        qualityDropdown.value = currentQuality;
        qualityDropdown.RefreshShownValue();
    }

    // Dropdown'a baðlanacak fonksiyon
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
            // 1. Önce Scalerlarý güncelle (UI Boyutlansýn)
            MenuManager.Instance.RefreshAllCanvases();

            // 2. Canvas'ý zorla güncelle (Layoutlar otursun)
            Canvas.ForceUpdateCanvases();

            // 3. ÞÝMDÝ Pozisyonlarý düzelt
            MenuManager.Instance.FixMenuPositions();

            InitializeUIScale();
        }
    }

    // --- LANGUAGE KISMI (YENÝ EKLENDÝ) ---

    private void InitializeLanguage()
    {
        // Mevcut dili bul (LocalizationManager veya PlayerPrefs üzerinden)
        // Senin sisteminde dil "tr", "en" gibi string olarak tutuluyordu.
        // Örnek: LocalizationManager.Instance.CurrentLanguageKey gibi bir yerden çekiyoruz.
        // Eðer elinde böyle bir deðiþken yoksa PlayerPrefs'ten okuruz:

        string currentLang = PlayerPrefs.GetString("Language", "en"); // Varsayýlan en

        // Bu dil kodunun listede kaçýncý sýrada olduðunu bul
        int index = languageCodes.IndexOf(currentLang);

        // Eðer listede varsa dropdown'ý o sayýya getir
        if (index != -1)
        {
            languageDropdown.value = index;
            languageDropdown.RefreshShownValue();
        }
    }

    // Dropdown OnValueChanged olayýna baðlanacak fonksiyon
    public void SetLanguage(int index)
    {
        // Seçilen index'e karþýlýk gelen kodu al (0 -> tr, 1 -> en)
        string selectedCode = languageCodes[index];

        LocalizationManager.Instance.ChangeLanguage(selectedCode); 

        // 2. Kaydet (Eðer Manager kaydetmiyorsa sen kaydet)
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

    // Dil deðiþince bu çalýþacak
    private void RefreshDropdowns()
    {
        // Kalite listesini yeni dile göre tekrar oluþtur
        InitializeQuality();
        InitializePixelation();
        InitializeUIScale();
        InitializeGameplaySettings();
    }
}