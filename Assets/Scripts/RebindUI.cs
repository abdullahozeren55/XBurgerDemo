using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic; // List için

public class RebindUI : MonoBehaviour
{
    [Header("Hangi Aksiyon?")]
    [SerializeField] private InputActionReference inputActionReference;

    [Header("Ayarlar")]
    [SerializeField] private bool excludeGamepad = false;
    [SerializeField] private int selectedBindingIndex = 0;

    [Header("Klavye Görseli (Buton 1)")]
    [SerializeField] private GameObject keyboardButtonObj; // Klavye butonu (Parent)
    [SerializeField] private TMP_Text keyboardButtonText;  // İçindeki yazı
    [SerializeField] private Button keyboardButtonComp;    // Tıklanma özelliği için

    [Header("Mouse Görseli (Buton 2)")]
    [SerializeField] private GameObject mouseButtonObj;    // Mouse butonu (Parent)
    [SerializeField] private Image mouseButtonImage;       // İçindeki ikon resmi
    [SerializeField] private Button mouseButtonComp;       // Tıklanma özelliği için

    [Header("Mouse Sprite Tanımları")]
    [Tooltip("Unity'den gelen tuş ismi (örn: leftButton) ile Sprite eşleşmesi")]
    [SerializeField] private Sprite defaultMouseIcon;
    [SerializeField] private Sprite defaultGamepadIcon;
    [SerializeField] private List<MouseIconMap> mouseIcons;

    [Header("Helper (Yanıp Sönen Yazı)")]
    [SerializeField] private GameObject helperTextObj;     // "Bir tuşa basın..." objesi
    [SerializeField] private TMP_Text helperTextComp;      // Alpha ayarı için Text bileşeni

    [Header("Gamepad Sprite Tanımları")]
    [Tooltip("Buraya 'xb_a', 'ps_cross', 'xb_lb' gibi isimlerle sprite'ları ekle")]
    [SerializeField] private List<GamepadIconMap> gamepadIcons;

    private InputActionRebindingExtensions.RebindingOperation _rebindingOperation;
    private InputAction _targetAction;
    private static bool isAnyRebindingInProgress = false;
    private Coroutine _pulseCoroutine; // Yanıp sönme animasyonu için

    [System.Serializable]
    public struct MouseIconMap
    {
        public string controlName; // örn: "leftButton", "rightButton", "middleButton"
        public Sprite icon;
    }

    [System.Serializable]
    public struct GamepadIconMap
    {
        public string spriteName; // Dosya adı (örn: xb_a)
        public Sprite icon;
    }

    private void Start()
    {
        // 1. Hedef Aksiyonu Bul
        string actionId = inputActionReference.action.id.ToString();

        if (InputManager.Instance != null)
        {
            _targetAction = InputManager.Instance.GetAction(actionId);
        }
        else
        {
            _targetAction = inputActionReference.action;
        }

        helperTextObj.SetActive(false);

        // 2. Başlangıçta doğru butonu göster
        UpdateUI();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += UpdateUI;

        // Ayarlardan ikon tipi değişirse
        Settings.OnPromptsChanged += UpdateUI;

        // --- YENİ: Reset atılırsa ---
        if (InputManager.Instance != null)
            InputManager.Instance.OnBindingsReset += UpdateUI;

        UpdateUI();
    }

    private void OnDisable()
    {
        StopRebindingLogic();

        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateUI;

        Settings.OnPromptsChanged -= UpdateUI;

        // --- YENİ: Abonelikten çık ---
        if (InputManager.Instance != null)
            InputManager.Instance.OnBindingsReset -= UpdateUI;
    }

    // --- REBIND BAŞLATMA ---
    // Bu fonksiyonu hem Klavye Butonuna hem Mouse Butonuna onClick olarak vereceksin.
    public void StartRebinding()
    {
        MenuManager.Instance.SetRebindBlocker(true);
        StartCoroutine(StartRebindingRoutine());
    }

    private IEnumerator StartRebindingRoutine()
    {
        if (isAnyRebindingInProgress) yield break;
        if (_targetAction == null) { Debug.LogError("HATA: Hedef Aksiyon Yok!"); yield break; }

        isAnyRebindingInProgress = true;

        // Blocker'ı aç (Tıklamaları kessin)
        MenuManager.Instance.SetRebindBlocker(true);

        // 1. UI Butonlarının etkileşimini kapat (Görsel olarak disable olmadan önce)
        if (keyboardButtonComp) keyboardButtonComp.interactable = false;
        if (mouseButtonComp) mouseButtonComp.interactable = false;

        // --- DEĞİŞİKLİK BURADA: 0.1sn YERİNE 1 KARE BEKLE ---
        // Bu, "Tıklama anı" ile "Dinleme anı"nı birbirinden ayırır ama kullanıcı hissetmez.
        yield return null;

        // 2. Aksiyonu durdur
        _targetAction.Disable();

        // 3. GÖRSEL DÜZENLEME
        if (keyboardButtonObj != null) keyboardButtonObj.SetActive(false);
        mouseButtonObj.SetActive(false);
        helperTextObj.SetActive(true);

        // 4. Animasyonu Başlat
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);
        _pulseCoroutine = StartCoroutine(PulseHelperText());

        // 5. Operasyonu Başlat
        var rebindOperation = _targetAction.PerformInteractiveRebinding(selectedBindingIndex);

        if (excludeGamepad)
        {
            // Klavye + Mouse Modu
            rebindOperation.WithControlsExcluding("<Gamepad>");
            rebindOperation.WithControlsExcluding("<Mouse>/position");
            rebindOperation.WithControlsExcluding("<Mouse>/delta");
            rebindOperation.WithControlsExcluding("<Mouse>/scroll"); // Tekerleği de engellemiştik
        }
        else
        {
            // Gamepad Modu
            rebindOperation.WithControlsHavingToMatchPath("<Gamepad>");
            rebindOperation.WithControlsExcluding("<Gamepad>/leftStick");
            rebindOperation.WithControlsExcluding("<Gamepad>/rightStick");
        }

        rebindOperation.WithControlsExcluding("<Pointer>/position");
        rebindOperation.WithCancelingThrough("<Keyboard>/escape");
        rebindOperation.OnComplete(operation => RebindCompleted());
        rebindOperation.OnCancel(operation => RebindCompleted());

        _rebindingOperation = rebindOperation.Start();
    }

    // --- YANIP SÖNME ANİMASYONU ---
    private IEnumerator PulseHelperText()
    {
        if (helperTextComp == null) yield break;

        float alpha = 1f;
        while (true)
        {
            // Realtime kullandık ki Pause menüsünde de yanıp sönsün
            float time = Time.unscaledTime * 3f; // Hız çarpanı
            alpha = Mathf.PingPong(time, 1f); // 0 ile 1 arası gider gelir

            // Text'in rengini güncelle (sadece Alpha değişir)
            Color c = helperTextComp.color;
            c.a = 0.2f + (alpha * 0.8f); // Tam kaybolmasın, min 0.2 olsun
            helperTextComp.color = c;

            yield return null;
        }
    }

    private void RebindCompleted()
    {
        StopRebindingLogic();
        if (InputManager.Instance != null) InputManager.Instance.SaveBindingOverrides();
    }

    private void StopRebindingLogic()
    {
        // Operasyonu temizle
        if (_rebindingOperation != null)
        {
            _rebindingOperation.Dispose();
            _rebindingOperation = null;
        }

        if (_targetAction != null) _targetAction.Enable();

        // Animasyonu durdur
        if (_pulseCoroutine != null) StopCoroutine(_pulseCoroutine);

        // Helper'ı kapat
        if (helperTextObj != null) helperTextObj.SetActive(false);

        isAnyRebindingInProgress = false;

        // --- SERİ ÇÖZÜM ---
        // Blocker'ı kaldır
        MenuManager.Instance.SetRebindBlocker(false);

        // UI'ı güncelle (Görseller yerine gelsin)
        UpdateUI();

        // Butonları ANINDA tıklanabilir yap (Delay yok, lag yok)
        if (keyboardButtonComp) keyboardButtonComp.interactable = true;
        if (mouseButtonComp) mouseButtonComp.interactable = true;

        // EnableButtonsAfterDelay coroutine'ini sildik, gerek kalmadı.
    }

    // --- KRİTİK BÖLÜM: UI GÜNCELLEME ---
    public void UpdateUI()
    {
        if (_targetAction == null) return;

        // 1. Path kontrolü
        string bindingPath = _targetAction.bindings[selectedBindingIndex].effectivePath;
        if (!string.IsNullOrEmpty(_targetAction.bindings[selectedBindingIndex].overridePath))
        {
            bindingPath = _targetAction.bindings[selectedBindingIndex].overridePath;
        }

        // 2. Cihaz Türünü Belirle
        bool isMouse = bindingPath.Contains("<Mouse>");
        bool isGamepad = bindingPath.Contains("<Gamepad>");

        if (isMouse)
        {
            // --- MOUSE MODU ---
            keyboardButtonObj.SetActive(false);
            mouseButtonObj.SetActive(true);

            string controlName = bindingPath.Replace("<Mouse>/", "").Trim();
            Sprite icon = GetMouseSprite(controlName);
            if (mouseButtonImage != null) mouseButtonImage.sprite = icon;
        }
        else if (isGamepad)
        {
            mouseButtonObj.SetActive(true); // Aynı resim objesini kullanıyoruz

            // Path örn: "<Gamepad>/buttonSouth"
            string controlName = bindingPath.Replace("<Gamepad>/", "").Trim();

            Sprite icon = GetGamepadSprite(controlName);
            if (mouseButtonImage != null) mouseButtonImage.sprite = icon;
        }
        else
        {
            // --- KLAVYE MODU ---
            mouseButtonObj.SetActive(false);
            keyboardButtonObj.SetActive(true);

            string displayString = _targetAction.GetBindingDisplayString(selectedBindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);
            string prettyName = GetKeyboardKeyText(displayString);

            if (keyboardButtonText != null) keyboardButtonText.text = prettyName;
        }
    }

    private Sprite GetGamepadSprite(string unityControlName)
    {
        // 1. Ayarlardan tercihi öğren (Xbox mı PS mi?)
        bool isXbox = Settings.IsXboxPrompts;
        string prefix = isXbox ? "xb_" : "ps_";

        // 2. Unity ismini bizim dosya ismimize (suffix) çevir
        string suffix = "";

        switch (unityControlName)
        {
            // Yüz Tuşları
            case "buttonSouth": suffix = isXbox ? "a" : "cross"; break;
            case "buttonEast": suffix = isXbox ? "b" : "circle"; break;
            case "buttonWest": suffix = isXbox ? "x" : "square"; break;
            case "buttonNorth": suffix = isXbox ? "y" : "triangle"; break;

            // Omuzlar ve Tetikler
            case "leftShoulder": suffix = isXbox ? "lb" : "l1"; break;
            case "rightShoulder": suffix = isXbox ? "rb" : "r1"; break;
            case "leftTrigger": suffix = isXbox ? "lt" : "l2"; break;
            case "rightTrigger": suffix = isXbox ? "rt" : "r2"; break;

            // Stick Basma
            case "leftStickPress": suffix = isXbox ? "ls" : "l3"; break; // xb_ls veya ps_l3
            case "rightStickPress": suffix = isXbox ? "rs" : "r3"; break;

            // Menü Tuşları
            case "start": suffix = "start"; break;  // xb_start / ps_start
            case "select": suffix = "select"; break; // xb_select / ps_select

            // D-Pad
            case "dpad/up": suffix = "dpad_up"; break;
            case "dpad/down": suffix = "dpad_down"; break;
            case "dpad/left": suffix = "dpad_left"; break;
            case "dpad/right": suffix = "dpad_right"; break;

            // Analog Hareket (Move/Look bindinglerinde genelde tüm stick gelir)
            case "leftStick": suffix = "stick_l"; break; // xb_stick_l
            case "rightStick": suffix = "stick_r"; break; // xb_stick_r

            default:
                // Bilinmeyen bir tuşsa (örn: dpad tek başına) logla ve çık
                Debug.LogWarning($"Gamepad tuşu tanımlı değil: {unityControlName}");
                return defaultGamepadIcon; // Fallback olarak M ikonu veya boş dönebiliriz
        }

        // 3. Tam ismi oluştur (örn: xb_a)
        string finalName = prefix + suffix;

        // 4. Listede ara
        foreach (var item in gamepadIcons)
        {
            if (item.spriteName == finalName)
                return item.icon;
        }

        Debug.LogWarning($"Sprite bulunamadı: {finalName}");
        return defaultGamepadIcon;
    }

    // Mouse ismine göre Sprite getiren fonksiyon
    private Sprite GetMouseSprite(string controlName)
    {
        // Inspector'daki listeden ara
        foreach (var item in mouseIcons)
        {
            if (item.controlName == controlName)
                return item.icon;
        }

        // Bulamazsa ilkini veya null döndür
        return defaultMouseIcon;
    }
    private string GetKeyboardKeyText(string originalName)
    {
        if (string.IsNullOrEmpty(originalName)) return "NONE";

        // Unity'den gelen ismi temizle
        string cleanName = originalName.Replace("Keyboard/", "").Trim();

        // --- 1. "İ" ve "I" AYRIMI (KESİN ÇÖZÜM) ---
        string lowerRaw = cleanName.ToLowerInvariant();

        if (lowerRaw == "i") return "İ"; // Küçük i (noktalı) -> Büyük İ
        if (lowerRaw == "ı") return "I"; // Küçük ı (noktasız) -> Büyük I

        // Artık diğerleri için büyütebiliriz
        string upperKey = cleanName.ToUpperInvariant();

        // --- Dil Kontrolü ---
        string lang = "en";
        if (LocalizationManager.Instance != null)
            lang = LocalizationManager.Instance.GetCurrentLanguageCode();
        else
            lang = PlayerPrefs.GetString("Language", "en");

        bool isTR = lang == "tr";

        // --- 2. ADIM: ÖZEL TUŞLAR ---

        switch (upperKey)
        {
            // Temel Tuşlar
            case "SPACE": return isTR ? "BOŞLUK" : "SPACE";
            case "ENTER": case "RETURN": return isTR ? "GİRİŞ" : "ENTER";

            case "TAB": return "TAB";
            case "ESCAPE": case "ESC": return "ESC";
            case "CAPS LOCK": case "CAPSLOCK": return "CAPS";
            case "BACKSPACE": return isTR ? "SİL" : "BACK";

            // --- MODIFIER TUŞLAR ---
            case "LEFT SHIFT":
            case "LSHIFT":
            case "SHIFT":
                return isTR ? "SOL SHIFT" : "L.SHIFT";
            case "RIGHT SHIFT":
            case "RSHIFT":
                return isTR ? "SAĞ SHIFT" : "R.SHIFT";

            case "LEFT CTRL":
            case "LCTRL":
            case "CTRL":
            case "CONTROL":
            case "LEFT CONTROL":
                return isTR ? "SOL CTRL" : "L.CTRL";
            case "RIGHT CTRL":
            case "RCTRL":
            case "RIGHT CONTROL":
                return isTR ? "SAĞ CTRL" : "R.CTRL";

            case "LEFT ALT":
            case "LALT":
            case "ALT":
                return isTR ? "SOL ALT" : "L.ALT";
            case "RIGHT ALT":
            case "RALT":
            case "ALT GR":
                return isTR ? "SAĞ ALT" : "R.ALT";

            // Yön Tuşları 
            case "UP ARROW": case "UP": return "↑";
            case "DOWN ARROW": case "DOWN": return "↓";
            case "LEFT ARROW": case "LEFT": return "←";
            case "RIGHT ARROW": case "RIGHT": return "→";

            // Mouse (Fallback olarak yazı kalsın ama normalde Sprite görünecek)
            case "LEFT BUTTON": case "LMB": return isTR ? "SOL TIK" : "L.CLICK";
            case "RIGHT BUTTON": case "RMB": return isTR ? "SAĞ TIK" : "R.CLICK";
            case "MIDDLE BUTTON": case "MMB": return isTR ? "ORTA TIK" : "M.CLICK";
            case "FORWARD": return isTR ? "İLERİ" : "FWD";
            case "BACK": return isTR ? "GERİ" : "BACK";
        }

        // --- 3. ADIM: NUMPAD FİLTRESİ ---
        if (upperKey.StartsWith("NUM"))
        {
            string suffix = upperKey.Replace("NUMPAD", "").Replace("NUM", "").Trim();
            if (suffix == "ENTER") return isTR ? "NUM GİRİŞ" : "NUM ENTER";

            if (int.TryParse(suffix, out int number))
            {
                return "NUM " + number;
            }
            return "?";
        }

        // --- 4. ADIM: TEK KARAKTER KONTROLÜ ---
        if (upperKey.Length == 1)
        {
            string allAllowed = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ÇĞİÖŞÜI";
            if (allAllowed.Contains(upperKey)) return upperKey;
        }

        return "?";
    }
}