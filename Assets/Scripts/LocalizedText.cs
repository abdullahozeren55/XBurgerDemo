using UnityEngine;
using TMPro;

public class LocalizedText : MonoBehaviour
{
    public string localizationKey; // JSON'daki Key'i buraya yazacaksýn (Örn: "UI_START_GAME")

    private TMP_Text _textComp;

    private void Awake()
    {
        _textComp = GetComponent<TMP_Text>();
    }

    private void Start()
    {
        // Baþlangýçta metni ayarla
        UpdateText();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
        {
            // 1. Abone ol (Gelecekteki deðiþiklikler için)
            LocalizationManager.Instance.OnLanguageChanged += UpdateText;

            // 2. HEMEN GÜNCELLE (Kaçýrdýðým deðiþiklikler için)
            // Bu satýrý eklemezsen, pasifken yapýlan deðiþiklikleri algýlamaz.
            UpdateText();
        }
    }

    private void OnDisable()
    {
        // Abonelikten çýk (Hata vermemesi için þart)
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateText;
    }

    // Bu fonksiyon dil deðiþince otomatik çalýþacak
    public void UpdateText()
    {
        if (_textComp != null && LocalizationManager.Instance != null)
        {
            string newValue = LocalizationManager.Instance.GetText(localizationKey);

            _textComp.text = newValue;
        }
    }
}