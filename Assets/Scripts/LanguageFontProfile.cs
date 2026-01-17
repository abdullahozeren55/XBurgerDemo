using UnityEngine;
using TMPro;
using System;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewFontProfile", menuName = "Localization/Font Profile")]
public class LanguageFontProfile : ScriptableObject
{
    public LocalizationManager.GameLanguage language; // Bu profil hangi dilin?

    [Serializable]
    public struct FontMapping
    {
        public FontType type;       // Hangi tür? (Örn: Header)
        public TMP_FontAsset font;  // Hangi font? (Örn: NotoSansJP-Bold)
    }

    public List<FontMapping> fonts;

    // Yardýmcý fonksiyon: Ýstenen türdeki fontu bulur
    public TMP_FontAsset GetFont(FontType type)
    {
        foreach (var mapping in fonts)
        {
            if (mapping.type == type)
                return mapping.font;
        }
        return null; // Bulamazsa null döner
    }
}