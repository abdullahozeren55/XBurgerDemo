using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum FontType
{
    DialogueOutlined,
    RetroUIDefaultOutlined,
    RetroUIThinOutlined,
    RetroUINoOutline,
    KeyboardNoOutline,
    Dropdown
}

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    public enum GameLanguage
    {
        English,
        Turkish,
        Chinese,
        Japanese,
        Spanish,
        Russian,
        Portuguese
    }

    [Header("Language Settings")]
    public GameLanguage currentLanguage = GameLanguage.English;
    public TextAsset localizationJSON;

    [Header("Font Settings")]
    // 1. BU ARTIK SENÝN ÝNGÝLÝZCE/LATÝN PROFÝLÝN OLACAK
    [Tooltip("Ýngilizce, Türkçe, Ýspanyolca vb. için varsayýlan font seti.")]
    public LanguageFontProfile defaultFontProfile;

    // 2. SADECE RU, JA, ZH ÝÇÝN PROFÝL EKLEYECEKSÝN
    [Tooltip("Sadece özel karakter gerektiren diller (RU, JA, ZH) için profilleri buraya ekle.")]
    public List<LanguageFontProfile> fontProfiles;

    private Dictionary<string, string> _localizedTexts;
    public event Action OnLanguageChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSavedLanguage();
            LoadLocalization();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ChangeLanguage(GameLanguage newLang)
    {
        if (currentLanguage == newLang) return;
        currentLanguage = newLang;
        LoadLocalization();
        OnLanguageChanged?.Invoke();

        string langCode = GetCodeFromLanguage(newLang);
        PlayerPrefs.SetString("Language", langCode);
        PlayerPrefs.Save();
    }

    public string GetCurrentLanguageCode() => GetCodeFromLanguage(currentLanguage);

    private string GetCodeFromLanguage(GameLanguage lang)
    {
        switch (lang)
        {
            case GameLanguage.English: return "en";
            case GameLanguage.Turkish: return "tr";
            case GameLanguage.Chinese: return "zh";
            case GameLanguage.Japanese: return "ja";
            case GameLanguage.Spanish: return "es";
            case GameLanguage.Russian: return "ru";
            case GameLanguage.Portuguese: return "pt";
            default: return "en";
        }
    }

    public void ChangeLanguage(string langCode)
    {
        switch (langCode)
        {
            case "en": ChangeLanguage(GameLanguage.English); break;
            case "tr": ChangeLanguage(GameLanguage.Turkish); break;
            case "zh": ChangeLanguage(GameLanguage.Chinese); break;
            case "ja": ChangeLanguage(GameLanguage.Japanese); break;
            case "es": ChangeLanguage(GameLanguage.Spanish); break;
            case "ru": ChangeLanguage(GameLanguage.Russian); break;
            case "pt": ChangeLanguage(GameLanguage.Portuguese); break;
            default:
                Debug.LogWarning("Bilinmeyen dil kodu: " + langCode);
                ChangeLanguage(GameLanguage.English);
                break;
        }
    }

    private void LoadSavedLanguage()
    {
        string savedLang = PlayerPrefs.GetString("Language", "en");
        ChangeLanguage(savedLang);
    }

    private void LoadLocalization()
    {
        _localizedTexts = new Dictionary<string, string>();
        if (localizationJSON == null) return;

        LocalizationData data = JsonUtility.FromJson<LocalizationData>(localizationJSON.text);
        if (data == null || data.entries == null) return;

        foreach (var entry in data.entries)
        {
            string value = "";
            switch (currentLanguage)
            {
                case GameLanguage.English: value = entry.en; break;
                case GameLanguage.Turkish: value = entry.tr; break;
                case GameLanguage.Chinese: value = entry.zh; break;
                case GameLanguage.Japanese: value = entry.ja; break;
                case GameLanguage.Spanish: value = entry.es; break;
                case GameLanguage.Russian: value = entry.ru; break;
                case GameLanguage.Portuguese: value = entry.pt; break;
                default: value = entry.en; break;
            }

            if (string.IsNullOrEmpty(value)) value = entry.en;
            if (!_localizedTexts.ContainsKey(entry.key)) _localizedTexts.Add(entry.key, value);
        }
    }

    public string GetText(string key)
    {
        if (_localizedTexts == null) return key;
        return _localizedTexts.TryGetValue(key, out string value) ? value : key;
    }

    // --- YENÝLENMÝÞ FONT MANTIÐI ---
    // 1. Mevcut dilin ayarlarýný getir
    public LanguageFontProfile.FontData GetFontDataForCurrentLanguage(FontType type)
    {
        // Önce özel dile bak (Japonca, Rusça vs.)
        LanguageFontProfile specificProfile = fontProfiles.Find(x => x.language == currentLanguage);
        if (specificProfile != null)
        {
            // O profilde bu FontType tanýmlý mý diye kontrol et
            var data = specificProfile.GetFontData(type);
            if (data.font != null) return data;
        }

        // Yoksa Default (Latin) profilinden getir
        if (defaultFontProfile != null)
        {
            return defaultFontProfile.GetFontData(type);
        }

        return new LanguageFontProfile.FontData();
    }

    // 2. Default (Latin/Referans) ayarlarýný getir (ORAN HESABI ÝÇÝN GEREKLÝ)
    public LanguageFontProfile.FontData GetDefaultFontData(FontType type)
    {
        if (defaultFontProfile != null)
        {
            return defaultFontProfile.GetFontData(type);
        }
        // Acil durum kaçýþý
        return new LanguageFontProfile.FontData { basePixelSize = 16f };
    }
}