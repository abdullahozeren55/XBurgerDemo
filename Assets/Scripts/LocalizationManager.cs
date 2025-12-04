using System.Collections.Generic;
using UnityEngine;
using System; // Action için gerekli

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    public enum GameLanguage
    {
        English,
        Turkish,
    }

    [Header("Language Settings")]
    public GameLanguage currentLanguage = GameLanguage.English;
    public TextAsset localizationJSON;

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
        string langCode = (newLang == GameLanguage.Turkish) ? "tr" : "en";
        PlayerPrefs.SetString("Language", langCode);
        PlayerPrefs.Save();
    }

    // --- YENÝ: DÝL DEÐÝÞTÝRME FONKSÝYONU (STRING ÝLE - Settings.cs ÝÇÝN) ---
    public void ChangeLanguage(string langCode)
    {
        switch (langCode)
        {
            case "tr":
                ChangeLanguage(GameLanguage.Turkish);
                break;
            case "en":
                ChangeLanguage(GameLanguage.English);
                break;
            default:
                Debug.LogWarning("Bilinmeyen dil kodu: " + langCode);
                break;
        }
    }

    private void LoadSavedLanguage()
    {
        string savedLang = PlayerPrefs.GetString("Language", "tr");
        if (savedLang == "en") currentLanguage = GameLanguage.English;
        else currentLanguage = GameLanguage.Turkish;
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
            string value;

            switch (currentLanguage)
            {
                case GameLanguage.Turkish:
                    value = entry.tr;
                    break;
                case GameLanguage.English:
                    value = entry.en;
                    break;
                default:
                    value = entry.en;
                    break;
            }

            if (!_localizedTexts.ContainsKey(entry.key))
            {
                _localizedTexts.Add(entry.key, value);
            }
        }
    }

    public string GetText(string key)
    {
        if (_localizedTexts == null) return key;

        if (_localizedTexts.TryGetValue(key, out string value))
        {
            return value;
        }

        return key;
    }
}