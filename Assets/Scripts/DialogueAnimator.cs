using DG.Tweening;
using Febucci.UI;
using TMPro;
using UnityEngine;

public class DialogueAnimator : MonoBehaviour
{
    [SerializeField] private TypewriterByCharacter typewriter;
    [SerializeField] private TMP_Text textComponent;

    public bool IsBusy { get; private set; } = false;

    // --- FONT HAFIZA DEÐERLERÝ (LocalizedText'ten transfer) ---
    private float _initialFontSize;
    private float _initialCharSpacing;
    private float _initialWordSpacing;
    private float _initialLineSpacing;
    private bool _hasInitializedValues = false;

    // Orijinal hýz deðerlerini saklamak için
    private float baseNormalWait = -1f;
    private float baseMiddleWait = -1f;
    private float baseLongWait = -1f;

    // Shader Property ID
    private static readonly int GlitchStrengthID = Shader.PropertyToID("_GlitchStrength");
    private Material textMaterialInstance;

    private void Awake()
    {
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();

        // --- HAFIZA AL ---
        // Editörde ayarladýðýn (Ýngilizce/Referans) deðerleri sakla
        if (textComponent != null)
        {
            _initialFontSize = textComponent.fontSize;
            _initialCharSpacing = textComponent.characterSpacing;
            _initialWordSpacing = textComponent.wordSpacing;
            _initialLineSpacing = textComponent.lineSpacing;
            _hasInitializedValues = true;

            textMaterialInstance = textComponent.fontMaterial;
            SetGlitchStrength(0f);
        }

        if (typewriter != null)
        {
            typewriter.onCharacterVisible.AddListener(OnCharacterVisible);
            typewriter.onTextShowed.AddListener(OnTypingFinished);
            typewriter.onTextDisappeared.AddListener(OnDisappearFinished);
        }
    }

    // --- YENÝ: FONT GÜNCELLEME (LocalizedText Mantýðý) ---
    public void UpdateFontSettings(FontType fontType)
    {
        if (textComponent == null || LocalizationManager.Instance == null || !_hasInitializedValues) return;

        // 1. Verileri Çek
        var targetData = LocalizationManager.Instance.GetFontDataForCurrentLanguage(fontType);
        var defaultData = LocalizationManager.Instance.GetDefaultFontData(fontType);

        // 2. Font Ata
        if (targetData.font != null && textComponent.font != targetData.font)
        {
            textComponent.font = targetData.font;
            // Font deðiþince materyal instance referansý bozulabilir, yenilemekte fayda var
            textMaterialInstance = textComponent.fontMaterial;
        }

        // 3. BOYUT HESABI (Scale Ratio)
        // Matematik: (Tasarým Boyutu / Default Baz) = Oran. 
        // Yeni Boyut = Hedef Baz * Oran
        float defaultBaseSize = Mathf.Max(defaultData.basePixelSize, 0.1f);
        float scaleRatio = _initialFontSize / defaultBaseSize;

        textComponent.fontSize = targetData.basePixelSize * scaleRatio;

        // 4. SPACING HESABI
        textComponent.characterSpacing = _initialCharSpacing + (targetData.characterSpacingOffset - defaultData.characterSpacingOffset);
        textComponent.wordSpacing = _initialWordSpacing + (targetData.wordSpacingOffset - defaultData.wordSpacingOffset);
        textComponent.lineSpacing = _initialLineSpacing + (targetData.lineSpacingOffset - defaultData.lineSpacingOffset);
    }

    public void SetGlitchStrength(float value)
    {
        if (textMaterialInstance != null)
        {
            textMaterialInstance.SetFloat(GlitchStrengthID, value);
        }
    }

    public void TweenGlitch(float targetValue, float duration, Ease ease = Ease.Linear)
    {
        if (textMaterialInstance != null)
        {
            textMaterialInstance.DOFloat(targetValue, GlitchStrengthID, duration).SetEase(ease);
        }
    }

    public string ApplyRichText(string content, RichTextTag tags)
    {
        string final = content;
        if (tags.HasFlag(RichTextTag.Shake)) final = $"<shake>{final}</shake>";
        if (tags.HasFlag(RichTextTag.Wave)) final = $"<wave>{final}</wave>";
        if (tags.HasFlag(RichTextTag.Wiggle)) final = $"<wiggle>{final}</wiggle>";
        return final;
    }

    private void EnsureInitialized()
    {
        if (baseNormalWait > 0f) return;

        if (typewriter != null)
        {
            baseNormalWait = typewriter.waitForNormalChars;
            baseMiddleWait = typewriter.waitMiddle;
            baseLongWait = typewriter.waitLong;

            if (baseNormalWait <= 0.0001f)
            {
                baseNormalWait = 0.04f;
                baseMiddleWait = 0.08f;
                baseLongWait = 0.16f;
            }
        }
    }

    private void OnCharacterVisible(char c)
    {
        if (char.IsWhiteSpace(c)) return;
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.PlayNextTypewriterSound();
        }
    }

    public void SetSpeed(float multiplier)
    {
        if (typewriter == null) return;
        EnsureInitialized();
        if (multiplier <= 0.01f) multiplier = 0.01f;

        typewriter.waitForNormalChars = baseNormalWait / multiplier;
        typewriter.waitMiddle = baseMiddleWait / multiplier;
        typewriter.waitLong = baseLongWait / multiplier;
    }

    public void SetColor(Color color)
    {
        if (textComponent != null) textComponent.color = color;
    }

    public void Show(string text)
    {
        IsBusy = true;
        typewriter.ShowText(text);
        typewriter.StartShowingText();
    }

    public void Hide()
    {
        if (gameObject.activeInHierarchy)
            typewriter.StartDisappearingText();
        else
            IsBusy = false;
    }

    public void ForceHide()
    {
        if (typewriter != null) typewriter.StopShowingText();
        if (textComponent != null) textComponent.text = string.Empty;
        IsBusy = false;
    }

    public void SkipTypewriter()
    {
        typewriter.SkipTypewriter();
    }

    private void OnTypingFinished() { }

    private void OnDisappearFinished()
    {
        IsBusy = false;
    }

    public bool IsTyping()
    {
        return typewriter.isShowingText;
    }
}