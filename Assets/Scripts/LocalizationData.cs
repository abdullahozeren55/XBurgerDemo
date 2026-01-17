using System;

[Serializable]
public class LocalizationEntry
{
    public string key;
    public string en; // Ýngilizce
    public string tr; // Türkçe
    public string zh; // Çince (Basitleþtirilmiþ)
    public string ja; // Japonca
    public string es; // Ýspanyolca (LatAm)
    public string ru; // Rusça
    public string pt; // Portekizce (Brezilya)
}

[Serializable]
public class LocalizationData
{
    public LocalizationEntry[] entries;
}
