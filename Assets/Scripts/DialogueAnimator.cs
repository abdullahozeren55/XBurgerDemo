using Febucci.UI;
using TMPro;
using UnityEngine;

public class DialogueAnimator : MonoBehaviour
{
    [SerializeField] private TypewriterByCharacter typewriter;
    [SerializeField] private TMP_Text textComponent;

    public bool IsBusy { get; private set; } = false;

    // Orijinal hýz deðerlerini saklamak için
    private float baseNormalWait = -1f; // -1 veriyoruz ki baþlatýlmadýðýný anlayalým
    private float baseMiddleWait = -1f;
    private float baseLongWait = -1f;

    private void Awake()
    {
        // Eðer inspector'dan atanmadýysa otomatik bul
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();

        if (typewriter != null)
        {
            // --- YENÝ: EVENT BAÐLANTISI ---
            // Her karakter göründüðünde bu fonksiyon çalýþacak
            typewriter.onCharacterVisible.AddListener(OnCharacterVisible);

            typewriter.onTextShowed.AddListener(OnTypingFinished);
            typewriter.onTextDisappeared.AddListener(OnDisappearFinished);
        }
    }

    // --- BU FONKSÝYON DEÐERLERÝ GARANTÝ ALTINA ALIR ---
    private void EnsureInitialized()
    {
        // Eðer zaten almýþsak ve deðerler saçma (0) deðilse çýk
        if (baseNormalWait > 0f) return;

        if (typewriter != null)
        {
            // Deðerleri çek
            baseNormalWait = typewriter.waitForNormalChars;
            baseMiddleWait = typewriter.waitMiddle;
            baseLongWait = typewriter.waitLong;

            // KORUMA: Eðer hala 0 geldiyse (Inspector'da 0.04 ayarlý olsa bile bazen Scripting order yüzünden 0 gelebilir)
            // Febucci'nin varsayýlanlarýna veya manuel bir güvenli deðere çek.
            if (baseNormalWait <= 0.0001f)
            {
                // HATA VAR DEMEKTÝR. Log basalým.
                Debug.LogWarning($"[DialogueAnimator] Base wait time 0 olarak algýlandý! Inspector deðerlerini kontrol et. Obje: {gameObject.name}");

                // Fallback (Acil durum) deðerleri
                baseNormalWait = 0.04f;
                baseMiddleWait = 0.08f;
                baseLongWait = 0.16f;
            }
        }
    }

    // --- BU FONKSÝYON HER HARFTE ÇALIÞIR ---
    private void OnCharacterVisible(char c)
    {
        // Boþluk karakterlerinde ses çalmasýn (Opsiyonel ama önerilir)
        if (char.IsWhiteSpace(c)) return;

        // Manager'a "Sýradaki sesi çal" de
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.PlayNextTypewriterSound();
        }
    }

    // --- YENÝ: HIZ AYARLAMA ---
    public void SetSpeed(float multiplier)
    {
        if (typewriter == null) return;

        // Önce base deðerlerin doðru olduðundan emin ol
        EnsureInitialized();

        // Güvenlik: 0'a bölme veya negatif hatasý olmasýn
        if (multiplier <= 0.01f) multiplier = 0.01f;

        // Speed artarsa, bekleme süresi AZALIR. (Ters Orantý)
        // Hýz 2x ise -> Süre 0.5x olmalý.
        // Math: Base / Multiplier

        typewriter.waitForNormalChars = baseNormalWait / multiplier;
        typewriter.waitMiddle = baseMiddleWait / multiplier;
        typewriter.waitLong = baseLongWait / multiplier;
    }

    public void SetColor(Color color)
    {
        if (textComponent != null)
        {
            textComponent.color = color;
        }
    }

    public void Show(string text)
    {
        IsBusy = true;
        gameObject.SetActive(true);

        // Eðer önceki text hala disappear oluyorsa, onu anýnda kesip yenisini yazabiliriz
        // veya Febucci'nin ShowText'i zaten resetler.
        typewriter.ShowText(text);
        typewriter.StartShowingText();
    }

    public void Hide()
    {
        // Direkt kapanmasýn, disappear efektini baþlatsýn
        if (gameObject.activeInHierarchy)
            typewriter.StartDisappearingText();
        else
            IsBusy = false;
    }

    public void ForceHide()
    {
        // Acil durum kapatmasý (Reset için)
        typewriter.StopShowingText();
        gameObject.SetActive(false);
        IsBusy = false;
    }

    public void SkipTypewriter()
    {
        typewriter.SkipTypewriter();
    }

    // --- CALLBACKS ---
    private void OnTypingFinished()
    {
        // Yazma bitti ama hala ekranda duruyor, o yüzden hala Busy sayýlýr.
        // Busy false yapmak için Disappear olmasýný bekleyeceðiz.
    }

    private void OnDisappearFinished()
    {
        // Artýk tamamen yok oldu, yeni görev alabilir.
        IsBusy = false;
        gameObject.SetActive(false);
    }

    public bool IsTyping()
    {
        return typewriter.isShowingText; // Febucci'nin kendi bool'u
    }
}