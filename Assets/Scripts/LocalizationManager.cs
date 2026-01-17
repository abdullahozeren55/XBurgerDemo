using System; // Action için gerekli
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public enum FontType
{
    PixelOutlined,
    Retro,
    RetroBold,
    RetroOutlined,
    UIKeyboard
}
public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    public enum GameLanguage
    {
        English,
        Turkish,
        Chinese,    // Yeni
        Japanese,   // Yeni
        Spanish,    // Yeni
        Russian,    // Yeni
        Portuguese  // Yeni
    }

    [Header("Language Settings")]
    public GameLanguage currentLanguage = GameLanguage.English;
    public TextAsset localizationJSON;

    [Header("Font Settings")]
    public List<LanguageFontProfile> fontProfiles; // Inspector'dan buraya profilleri (TR, EN, JA) sürükleyeceksin
    public TMP_FontAsset defaultFallbackFont; // Her þey ters giderse kullanýlacak font

    private Dictionary<string, string> _localizedTexts;

    // --- YENÝ: DÝL DEÐÝÞTÝ EVENTÝ ---
    // Diðer scriptler buna abone olacak. Dil deðiþince hepsine "Güncellen!" diye baðýracaðýz.
    public event Action OnLanguageChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // Önce kayýtlý dili yükle (PlayerPrefs)
            LoadSavedLanguage();
            LoadLocalization();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- YENÝ: DÝL DEÐÝÞTÝRME FONKSÝYONU (ENUM ÝLE) ---
    public void ChangeLanguage(GameLanguage newLang)
    {
        if (currentLanguage == newLang) return;

        currentLanguage = newLang;

        // 1. Sözlüðü yeni dile göre tekrar doldur
        LoadLocalization();

        // 2. Sisteme haber ver (Event Fýrlat)
        OnLanguageChanged?.Invoke();

        // 3. Kaydet (Bir sonraki açýlýþta hatýrlasýn)
        string langCode = GetCodeFromLanguage(newLang);
        PlayerPrefs.SetString("Language", langCode);
        PlayerPrefs.Save();
    }

    // Kod tekrarýný önlemek ve temiz tutmak için bu yardýmcý metodu yazdým
    public string GetCurrentLanguageCode()
    {
        return GetCodeFromLanguage(currentLanguage);
    }

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

    // --- YENÝ: DÝL DEÐÝÞTÝRME FONKSÝYONU (STRING ÝLE - Settings.cs ÝÇÝN) ---
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
                ChangeLanguage(GameLanguage.English); // Fallback
                break;
        }
    }

    private void LoadSavedLanguage()
    {
        string savedLang = PlayerPrefs.GetString("Language", "en"); // Varsayýlan Ýngilizce
        ChangeLanguage(savedLang); // Yukarýdaki string metodunu kullanarak yükle
    }

    private void LoadLocalization()
    {
        _localizedTexts = new Dictionary<string, string>();

        if (localizationJSON == null)
        {
            Debug.LogError("Localization JSON atanmadý!");
            return;
        }

        LocalizationData data = JsonUtility.FromJson<LocalizationData>(localizationJSON.text);

        if (data == null || data.entries == null)
        {
            Debug.LogError("Localization JSON parse edilemedi!");
            return;
        }

        foreach (var entry in data.entries)
        {
            string value = "";

            // --- KRÝTÝK GÜNCELLEME: Yeni alanlarý buraya ekledik ---
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

            // Eðer çeviri boþsa veya yoksa Ýngilizce dönsün (Fallback)
            if (string.IsNullOrEmpty(value)) value = entry.en;

            if (!_localizedTexts.ContainsKey(entry.key))
            {
                _localizedTexts.Add(entry.key, value);
            }
        }
    }

    public string GetText(string key)
    {
        if (_localizedTexts == null) return key;
        if (_localizedTexts.TryGetValue(key, out string value)) return value;
        return key;
    }

    public TMP_FontAsset GetFontForCurrentLanguage(FontType type)
    {
        // 1. Þu anki dil için uygun profili bul
        LanguageFontProfile profile = fontProfiles.Find(x => x.language == currentLanguage);

        if (profile != null)
        {
            // 2. Profilin içinden istenen türdeki (Header, Dialogue vs.) fontu çek
            TMP_FontAsset font = profile.GetFont(type);
            if (font != null) return font;
        }

        // Eðer o dilde veya o türde font yoksa varsayýlaný döndür
        return defaultFallbackFont;
    }
}