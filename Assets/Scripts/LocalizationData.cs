using System;

[Serializable]
public class LocalizationEntry
{
    public string key;
    public string tr;
    public string en;
}

[Serializable]
public class LocalizationData
{
    public LocalizationEntry[] entries;
}
