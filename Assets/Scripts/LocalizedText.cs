using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    public string localizationKey;
    public FontType fontType = FontType.DialogueOutlined;

    private TMP_Text _textComp;

    // ARTIK RECT TRANSFORM'A ÝHTÝYACIMIZ YOK
    // private RectTransform _rectTransform; <-- Sildik

    // Hafýza Deðerleri
    private float _initialFontSize;
    private float _initialCharSpacing;
    private float _initialWordSpacing;
    private float _initialLineSpacing;

    private void Awake()
    {
        _textComp = GetComponent<TMP_Text>();
        // RectTransform atamasýný sildik.

        if (_textComp != null)
        {
            _initialFontSize = _textComp.fontSize;
            _initialCharSpacing = _textComp.characterSpacing;
            _initialWordSpacing = _textComp.wordSpacing;
            _initialLineSpacing = _textComp.lineSpacing;
        }
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

    public void UpdateContent()
    {
        if (_textComp == null || LocalizationManager.Instance == null) return;

        // 1. Verileri Çek
        var targetData = LocalizationManager.Instance.GetFontDataForCurrentLanguage(fontType);
        var defaultData = LocalizationManager.Instance.GetDefaultFontData(fontType);

        // 2. Font Ata
        if (targetData.font != null && _textComp.font != targetData.font)
        {
            _textComp.font = targetData.font;
        }

        // 3. BOYUT HESABI (Ayný Kalýyor)
        float defaultBaseSize = Mathf.Max(defaultData.basePixelSize, 0.1f);
        float scaleRatio = _initialFontSize / defaultBaseSize;

        _textComp.fontSize = targetData.basePixelSize * scaleRatio;

        // 4. OFFSET HESABI (<voffset> için)
        // Hedef dilin offseti ile Default dilin offseti arasýndaki farký bul ve scale ile çarp.
        float rawOffsetDiff = targetData.verticalOffset - defaultData.verticalOffset;
        float finalVOffset = rawOffsetDiff * scaleRatio;

        // 5. METNÝ OLUÞTUR VE YAZ (BÜYÜ BURADA)
        string rawText = LocalizationManager.Instance.GetText(localizationKey);

        // Eðer offset 0.1'den küçükse tag ekleyip string'i kirletmeyelim, gerek yok.
        if (Mathf.Abs(finalVOffset) > 0.1f)
        {
            // Metni <voffset=XX>...</voffset> içine alýyoruz.
            _textComp.text = $"<voffset={finalVOffset:F2}>{rawText}</voffset>";
        }
        else
        {
            _textComp.text = rawText;
        }

        // 6. SPACING HESABI (Ayný Kalýyor)
        _textComp.characterSpacing = _initialCharSpacing + (targetData.characterSpacingOffset - defaultData.characterSpacingOffset);
        _textComp.wordSpacing = _initialWordSpacing + (targetData.wordSpacingOffset - defaultData.wordSpacingOffset);
        _textComp.lineSpacing = _initialLineSpacing + (targetData.lineSpacingOffset - defaultData.lineSpacingOffset);
    }
}