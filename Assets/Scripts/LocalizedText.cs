using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    public string localizationKey;

    // YENÝ: Bu metin hangi türde? (Inspector'dan seç: Header mý? Dialogue mi?)
    public FontType fontType = FontType.PixelOutlined;

    private TMP_Text _textComp;

    private void Awake()
    {
        _textComp = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        UpdateContent();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            LocalizationManager.Instance.OnLanguageChanged += UpdateContent;
            UpdateContent();
        }
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateContent;
    }

    // Ýsmini UpdateText'ten UpdateContent'e çevirdim çünkü artýk hem Text hem Font deðiþiyor
    public void UpdateContent()
    {
        if (_textComp != null && LocalizationManager.Instance != null)
        {
            // 1. Metni Güncelle
            _textComp.text = LocalizationManager.Instance.GetText(localizationKey);

            // 2. Fontu Güncelle
            TMP_FontAsset newFont = LocalizationManager.Instance.GetFontForCurrentLanguage(fontType);

            // Gereksiz atamadan kaçýn (Performans)
            if (newFont != null && _textComp.font != newFont)
            {
                _textComp.font = newFont;
            }
        }
    }
}