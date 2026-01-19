using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewFontProfile", menuName = "Localization/Font Profile")]
public class LanguageFontProfile : ScriptableObject
{
    public LocalizationManager.GameLanguage language;

    [Serializable]
    public struct FontData
    {
        public FontType type;
        public TMP_FontAsset font;

        [Header("Pixel Perfect Settings")]
        [Tooltip("Bu fontun '1x' boyutu nedir? (Örn: Latin 16, Japonca 24)")]
        public float basePixelSize;

        // DEÐÝÞÝKLÝK BURADA: Vector2 yerine sadece float
        [Header("Vertical Adjustment")]
        [Tooltip("Metni satýr içinde ne kadar yukarý/aþaðý kaydýralým? (Pozitif: Yukarý, Negatif: Aþaðý)")]
        public float verticalOffset;

        [Header("Spacing Adjustments")]
        public float characterSpacingOffset;
        public float wordSpacingOffset;
        public float lineSpacingOffset;
    }

    public List<FontData> fontSettings;

    // Helper: Ýstenen türdeki datayý bulur
    public FontData GetFontData(FontType type)
    {
        foreach (var mapping in fontSettings)
        {
            if (mapping.type == type)
                return mapping;
        }
        // Bulamazsa boþ döndür (Default deðerlerle)
        return new FontData { basePixelSize = 16f };
    }
}