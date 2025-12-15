using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Collections; // Coroutine için gerekli

public class RebindUI : MonoBehaviour
{
    [Header("Hangi Aksiyon?")]
    [SerializeField] private InputActionReference inputActionReference;

    [Header("Ayarlar")]
    [SerializeField] private bool excludeGamepad = false; // True ise: Klavye + Mouse kabul eder, Gamepad'i dýþlar.
    [SerializeField] private int selectedBindingIndex = 0;

    [Header("UI Bileþenleri")]
    [SerializeField] private TMP_Text actionNameText;
    [SerializeField] private TMP_Text bindingText;
    [SerializeField] private GameObject helperText;
    [SerializeField] private GameObject keyText;
    [SerializeField] private Button myButton;

    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;
    private InputAction _targetAction;

    private static bool isAnyRebindingInProgress = false;

    private void Start()
    {
        if (myButton == null) myButton = GetComponent<Button>();

        string actionId = inputActionReference.action.id.ToString();

        if (InputManager.Instance != null)
        {
            _targetAction = InputManager.Instance.GetAction(actionId);
        }
        else
        {
            _targetAction = inputActionReference.action;
        }

        UpdateUI();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += UpdateUI;

        UpdateUI(); // Açýlýr açýlmaz güncelle
    }

    private void OnDisable()
    {
        StopRebindingLogic();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateUI;
    }

    // Butona basýnca artýk Coroutine baþlatýyoruz (Bekleme için)
    public void StartRebinding()
    {
        StartCoroutine(StartRebindingRoutine());
    }

    private IEnumerator StartRebindingRoutine()
    {
        if (isAnyRebindingInProgress) yield break;
        if (_targetAction == null) { Debug.LogError("HATA: Hedef Aksiyon Yok!"); yield break; }

        isAnyRebindingInProgress = true;
        if (myButton != null) myButton.interactable = false;

        // --- KRÝTÝK NOKTA 1: GECÝKME ---
        // Kullanýcý butona týkladý, parmaðýný çekmesi için 0.2sn veriyoruz.
        // Böylece "Sol Týk" anýnda algýlanýp menü kapanmýyor.
        yield return new WaitForSecondsRealtime(0.1f);

        _targetAction.Disable();

        keyText.SetActive(false);
        helperText.SetActive(true);

        // Operasyonu Hazýrla
        var rebindOperation = _targetAction.PerformInteractiveRebinding(selectedBindingIndex);

        // --- KRÝTÝK NOKTA 2: FÝLTRELEME MANTIÐI ---

        if (excludeGamepad)
        {
            // SENARYO: KLAVYE & MOUSE MENÜSÜ
            // Gamepad'i yasakla
            rebindOperation.WithControlsExcluding("<Gamepad>");

            // Mouse'un kendisini deðil, HAREKETÝNÝ yasakla (Delta ve Position)
            // Böylece týklamalar serbest, ama fareyi kaydýrmak tuþ atamasý yapmaz.
            rebindOperation.WithControlsExcluding("<Mouse>/position");
            rebindOperation.WithControlsExcluding("<Mouse>/delta");
        }
        else
        {
            // SENARYO: GAMEPAD MENÜSÜ
            // Sadece Gamepad'i kabul et
            rebindOperation.WithControlsHavingToMatchPath("<Gamepad>");
        }

        // Ortak Ayarlar
        rebindOperation.WithControlsExcluding("<Pointer>/position"); // Dokunmatik ekran vs için garanti olsun
        rebindOperation.WithCancelingThrough("<Keyboard>/escape");
        rebindOperation.OnComplete(operation => RebindCompleted());
        rebindOperation.OnCancel(operation => RebindCompleted());

        // Baþlat
        _rebindingOperation = rebindOperation.Start();
    }

    private void RebindCompleted()
    {
        StopRebindingLogic();

        if (InputManager.Instance != null)
        {
            InputManager.Instance.SaveBindingOverrides();
        }
    }

    private void StopRebindingLogic()
    {
        if (_rebindingOperation != null)
        {
            _rebindingOperation.Dispose();
            _rebindingOperation = null;
        }

        if (_targetAction != null) _targetAction.Enable();

        if (helperText != null) helperText.SetActive(false);
        if (keyText != null) keyText.SetActive(true);

        // --- DEÐÝÞÝKLÝK BURADA: Butonu hemen açma! ---
        // myButton.interactable = true; // ESKÝSÝ BUNU SÝL

        // YENÝSÝ: Butonu açmak için biraz bekle
        StartCoroutine(EnableButtonAfterDelay());

        isAnyRebindingInProgress = false;

        UpdateUI();
    }

    private IEnumerator EnableButtonAfterDelay()
    {
        // 0.2 saniye bekle ki "Sol Týk" olayý sönümlensin
        yield return new WaitForSecondsRealtime(0.1f);

        if (myButton != null)
            myButton.interactable = true;
    }

    public void UpdateUI()
    {
        if (_targetAction != null)
        {
            // 1. Ham veriyi al
            string rawName = _targetAction.GetBindingDisplayString(selectedBindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);

            // 2. Çeviriye sok
            string prettyName = GetPrettyName(rawName);

            // 3. Ekrana yaz (Invariant kullanarak)
            if (bindingText != null)
            {
                bindingText.text = prettyName.ToUpperInvariant();
            }
        }
    }

    private string GetPrettyName(string originalName)
    {
        if (string.IsNullOrEmpty(originalName)) return "NONE";

        // Mevcut dil kodunu al (Senin LocalizationManager'dan)
        // Eðer Manager'da "CurrentLanguage" diye public deðiþken yoksa PlayerPrefs'ten de bakabiliriz.
        // Ama en temizi Manager'a sormaktýr. 
        // Varsayalým ki LocalizationManager.Instance.CurrentLanguageKey bize "tr" veya "en" veriyor.

        string lang = "en";
        if (LocalizationManager.Instance != null)
            lang = LocalizationManager.Instance.GetCurrentLanguageCode(); // Veya senin deðiþkenin adý neyse
        else
            lang = PlayerPrefs.GetString("Language", "en");

        bool isTR = lang == "tr";

        // Unity bazen "Left Button" bazen "LMB" döndürebilir ayarlara göre.
        // Gelen stringi temizleyelim
        string cleanName = originalName.Trim();

        // --- TÜRKÇE ÇEVÝRÝLER ---
        if (isTR)
        {
            switch (cleanName)
            {
                case "Space": return "BOÞLUK";
                case "Enter": return "GÝRÝÞ";
                case "Left Shift": return "SOL SHIFT";
                case "Right Shift": return "SAÐ SHIFT";
                case "Left Ctrl": return "SOL CTRL";
                case "Right Ctrl": return "SAÐ CTRL";
                case "Left Alt": return "SOL ALT";
                case "Right Alt": return "SAÐ ALT";
                case "Left Button": return "SOL TIK";
                case "Right Button": return "SAÐ TIK";
                case "Middle Button": return "ORTA TIK";
                case "Escape": return "ESC";
                case "Tab": return "TAB";
                case "Caps Lock": return "CAPS LOCK";
                case "Backspace": return "SÝLME";
                // Ok Tuþlarý
                case "Up Arrow": return "YUKARI OK";
                case "Down Arrow": return "AÞAÐI OK";
                case "Left Arrow": return "SOL OK";
                case "Right Arrow": return "SAÐ OK";
                // Numpad
                case "Numpad Enter": return "NUM GÝRÝÞ";
                // Eksik varsa buraya eklersin...
                case "Forward": return "ÝLERÝ";
                case "Back": return "GERÝ";
                default:
                    // Eðer listede yoksa (örn: "W", "A", "5") olduðu gibi döndür ama TR karakter sorunu olmasýn
                    return cleanName;
            }
        }
        // --- ÝNGÝLÝZCE CÝLALAMA (Opsiyonel) ---
        else
        {
            switch (cleanName)
            {
                case "Left Button": return "LEFT CLICK";
                case "Right Button": return "RIGHT CLICK";
                case "Middle Button": return "MIDDLE CLICK";
                case "Up Arrow": return "UP";
                case "Down Arrow": return "DOWN";
                case "Left Arrow": return "LEFT";
                case "Right Arrow": return "RIGHT";
                default: return cleanName;
            }
        }
    }
}