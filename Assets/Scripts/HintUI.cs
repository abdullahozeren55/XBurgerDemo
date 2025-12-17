using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class HintUI : MonoBehaviour
{
    [System.Serializable]
    public struct InputIconData
    {
        public string controlName;
        public Sprite icon;
        public Sprite emissionMap;
        [ColorUsage(true, true)]
        public Color glowColor;
    }

    [Header("Hangi Tuşu Gösterecek?")]
    [SerializeField] private InputActionReference actionReference;

    [Header("Bileşenler")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text keyText;
    [SerializeField] private UIGlowController glowController;

    [Header("Görsel Veritabanı")]
    [SerializeField] private List<InputIconData> gamepadIcons;
    [SerializeField] private List<InputIconData> mouseIcons;

    [Header("Varsayılanlar")]
    [SerializeField] private InputIconData defaultGamepadData;
    [SerializeField] private InputIconData defaultMouseData;
    [ColorUsage(true, true)][SerializeField] private Color keyboardGlowColor = Color.white * 2f;

    private InputAction _targetAction;

    private void Start()
    {
        if (InputManager.Instance != null)
            _targetAction = InputManager.Instance.GetAction(actionReference.action.name);
        else
            _targetAction = actionReference.action;

        UpdateVisuals();
    }

    private void OnEnable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnBindingsReset += UpdateVisuals;
            InputManager.Instance.OnInputDeviceChanged += OnDeviceChanged;
        }

        Settings.OnPromptsChanged += UpdateVisuals;
        UpdateVisuals();
    }

    private void OnDisable()
    {
        if (InputManager.Instance != null)
        {
            InputManager.Instance.OnBindingsReset -= UpdateVisuals;
            InputManager.Instance.OnInputDeviceChanged -= OnDeviceChanged;
        }

        Settings.OnPromptsChanged -= UpdateVisuals;
    }

    private void OnDeviceChanged(bool isMouse)
    {
        UpdateVisuals();
    }

    // --- BURASI DEĞİŞTİ ---
    public void UpdateVisuals()
    {
        if (_targetAction == null) return;

        bool useGamepad = false;
        if (InputManager.Instance != null)
            useGamepad = !InputManager.Instance.IsUsingMouse();

        int foundBindingIndex = -1;

        // Binding Bulma Döngüsü
        for (int i = 0; i < _targetAction.bindings.Count; i++)
        {
            InputBinding b = _targetAction.bindings[i];
            string path = !string.IsNullOrEmpty(b.overridePath) ? b.overridePath : b.path;

            if (b.isComposite) continue;

            if (useGamepad)
            {
                if (path.Contains("<Gamepad>") || path.Contains("<Joystick>"))
                {
                    foundBindingIndex = i;
                    break;
                }
            }
            else
            {
                if (path.Contains("<Keyboard>") || path.Contains("<Mouse>"))
                {
                    foundBindingIndex = i;
                    break;
                }
            }
        }

        if (foundBindingIndex == -1) foundBindingIndex = 0;

        InputBinding finalBinding = _targetAction.bindings[foundBindingIndex];
        string finalPath = !string.IsNullOrEmpty(finalBinding.overridePath) ? finalBinding.overridePath : finalBinding.path;

        if (finalPath.Contains("<Gamepad>"))
        {
            // --- GAMEPAD ---
            string controlName = finalPath.Replace("<Gamepad>/", "").Trim();
            InputIconData data = GetGamepadData(controlName);

            iconImage.sprite = data.icon;

            if (glowController)
                glowController.SetVisualData(true, data.emissionMap != null ? data.emissionMap.texture : null, data.glowColor);
        }
        else if (finalPath.Contains("<Mouse>"))
        {
            // --- MOUSE ---
            string controlName = finalPath.Replace("<Mouse>/", "").Trim();
            InputIconData data = GetMouseData(controlName);

            iconImage.sprite = data.icon;

            if (glowController)
                glowController.SetVisualData(true, data.emissionMap != null ? data.emissionMap.texture : null, data.glowColor);
        }
        else
        {
            // --- KLAVYE ---
            // Unity'den ham stringi alıyoruz
            string displayString = _targetAction.GetBindingDisplayString(foundBindingIndex, InputBinding.DisplayStringOptions.DontUseShortDisplayNames);

            // Bizim özel çeviri ve format fonksiyonumuza sokuyoruz
            keyText.text = GetKeyboardKeyText(displayString);

            if (glowController)
                glowController.SetVisualData(false, null, keyboardGlowColor);
        }
    }

    // --- KLAVYE TEXT FORMATLAYICI (RebindUI'dan Çekildi) ---
    private string GetKeyboardKeyText(string originalName)
    {
        if (string.IsNullOrEmpty(originalName)) return "NONE";

        string cleanName = originalName.Replace("Keyboard/", "").Trim();

        string lowerRaw = cleanName.ToLowerInvariant();
        if (lowerRaw == "i") return "İ";
        if (lowerRaw == "ı") return "I";

        string upperKey = cleanName.ToUpperInvariant();

        string lang = "en";
        if (LocalizationManager.Instance != null)
            lang = LocalizationManager.Instance.GetCurrentLanguageCode();
        else
            lang = PlayerPrefs.GetString("Language", "en");

        bool isTR = lang == "tr";

        switch (upperKey)
        {
            case "SPACE": return isTR ? "BOŞLUK" : "SPACE";
            case "ENTER": case "RETURN": return isTR ? "GİRİŞ" : "ENTER";
            case "TAB": return "TAB";
            case "ESCAPE": case "ESC": return "ESC";
            case "CAPS LOCK": case "CAPSLOCK": return "CAPS";
            case "BACKSPACE": return isTR ? "SİL" : "BACK";
            case "LEFT SHIFT": case "LSHIFT": case "SHIFT": return isTR ? "SOL SHIFT" : "L.SHIFT";
            case "RIGHT SHIFT": case "RSHIFT": return isTR ? "SAĞ SHIFT" : "R.SHIFT";
            case "LEFT CTRL": case "LCTRL": case "CTRL": case "CONTROL": case "LEFT CONTROL": return isTR ? "SOL CTRL" : "L.CTRL";
            case "RIGHT CTRL": case "RCTRL": case "RIGHT CONTROL": return isTR ? "SAĞ CTRL" : "R.CTRL";
            case "LEFT ALT": case "LALT": case "ALT": return isTR ? "SOL ALT" : "L.ALT";
            case "RIGHT ALT": case "RALT": case "ALT GR": return isTR ? "SAĞ ALT" : "R.ALT";
            case "UP ARROW": case "UP": return "↑";
            case "DOWN ARROW": case "DOWN": return "↓";
            case "LEFT ARROW": case "LEFT": return "←";
            case "RIGHT ARROW": case "RIGHT": return "→";
            case "LEFT BUTTON": case "LMB": return isTR ? "SOL TIK" : "L.CLICK";
            case "RIGHT BUTTON": case "RMB": return isTR ? "SAĞ TIK" : "R.CLICK";
            case "MIDDLE BUTTON": case "MMB": return isTR ? "ORTA TIK" : "M.CLICK";
            case "FORWARD": return isTR ? "İLERİ" : "FWD";
            case "BACK": return isTR ? "GERİ" : "BACK";
        }

        if (upperKey.StartsWith("NUM"))
        {
            string suffix = upperKey.Replace("NUMPAD", "").Replace("NUM", "").Trim();
            if (suffix == "ENTER") return isTR ? "NUM GİRİŞ" : "NUM ENTER";
            if (int.TryParse(suffix, out int number)) return "NUM " + number;
            return "?";
        }

        if (upperKey.Length == 1)
        {
            string allAllowed = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789ÇĞİÖŞÜI";
            if (allAllowed.Contains(upperKey)) return upperKey;
        }

        return "?";
    }

    // --- ARAMA FONKSİYONLARI ---
    private InputIconData GetGamepadData(string unityControlName)
    {
        bool isXbox = Settings.IsXboxPrompts;
        string prefix = isXbox ? "xb_" : "ps_";
        string suffix = "";

        switch (unityControlName)
        {
            case "buttonSouth": suffix = isXbox ? "a" : "cross"; break;
            case "buttonEast": suffix = isXbox ? "b" : "circle"; break;
            case "buttonWest": suffix = isXbox ? "x" : "square"; break;
            case "buttonNorth": suffix = isXbox ? "y" : "triangle"; break;
            case "leftShoulder": suffix = isXbox ? "lb" : "l1"; break;
            case "rightShoulder": suffix = isXbox ? "rb" : "r1"; break;
            case "leftTrigger": suffix = isXbox ? "lt" : "l2"; break;
            case "rightTrigger": suffix = isXbox ? "rt" : "r2"; break;
            case "leftStickPress": suffix = isXbox ? "ls" : "l3"; break;
            case "rightStickPress": suffix = isXbox ? "rs" : "r3"; break;
            case "start": suffix = "start"; break;
            case "select": suffix = "select"; break;
            case "dpad/up": suffix = "dpad_up"; break;
            case "dpad/down": suffix = "dpad_down"; break;
            case "dpad/left": suffix = "dpad_left"; break;
            case "dpad/right": suffix = "dpad_right"; break;
            case "dpad": suffix = "dpad_up"; break;
            case "leftStick": suffix = "stick_l"; break;
            case "rightStick": suffix = "stick_r"; break;
            default: return defaultGamepadData;
        }

        string finalName = prefix + suffix;
        foreach (var data in gamepadIcons) { if (data.controlName == finalName) return data; }
        return defaultGamepadData;
    }

    private InputIconData GetMouseData(string controlName)
    {
        foreach (var data in mouseIcons) { if (data.controlName == controlName) return data; }
        return defaultMouseData;
    }
}