using System.Collections.Generic;
using UnityEngine;

public class LocalizationManager : MonoBehaviour
{
    public static LocalizationManager Instance;

    public enum GameLanguage
    {
        Turkish,
        English
    }

    [Header("Language Settings")]
    public GameLanguage currentLanguage = GameLanguage.Turkish;
    public TextAsset localizationJSON; // JSON dosyasýný buraya sürükleyeceðiz

    private Dictionary<string, string> _localizedTexts;

    private void Awake()
    {
        // singleton
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadLocalization();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadLocalization()
    {
        _localizedTexts = new Dictionary<string, string>();

        if (localizationJSON == null)
        {
            Debug.LogError("Localization JSON atanmadý!");
            return;
        }

        // JSON'u C# sýnýfýna çevir
        LocalizationData data = JsonUtility.FromJson<LocalizationData>(localizationJSON.text);

        if (data == null || data.entries == null)
        {
            Debug.LogError("Localization JSON parse edilemedi!");
            return;
        }

        // Her entry için diline göre deðeri sözlüðe koy
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
            else
            {
                Debug.LogWarning("Ayný key iki kez tanýmlanmýþ: " + entry.key);
            }
        }

        Debug.Log("Localization yüklendi. Toplam key: " + _localizedTexts.Count);
    }

    public string GetText(string key)
    {
        if (_localizedTexts == null)
        {
            Debug.LogWarning("Localization henüz yüklenmemiþ, key: " + key);
            return key;
        }

        if (_localizedTexts.TryGetValue(key, out string value))
        {
            return value;
        }

        Debug.LogWarning("Localization key bulunamadý: " + key);
        return key; // fallback olarak key'i döndürüyoruz
    }
}
