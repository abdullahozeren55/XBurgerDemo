using UnityEngine;
// YENÝ INPUT SÝSTEMÝNÝ KULLANMAK ÝÇÝN BU GEREKLÝ:
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Singleton (Her yerden eriþmek için)
    public static InputManager Instance;

    [Header("Base Settings")]
    // Mouse Delta genelde çok büyük gelir (pixel pixel), o yüzden onu dizginlemek lazým.
    private const float BASE_MOUSE_MULTIPLIER = 0.05f;

    // Gamepad 0-1 arasý gelir, onu hýzlandýrmak lazým.
    private const float BASE_GAMEPAD_MULTIPLIER = 40.0f;

    [Header("Control Settings")]
    public float mouseSensitivity = 1.0f;
    public float gamepadSensitivity = 1.0f; // Slider 1.0 iken normal hýz olsun
    public bool invertY = false;
    public bool sprintIsToggle = false;
    public bool crouchIsToggle = false;

    [Header("Gamepad Settings")]
    public bool swapSticks = false; // Settings.cs'den bunu true/false yapacaðýz

    public void SetSwapSticks(bool isSwapped) => swapSticks = isSwapped;

    // Toggle mantýðý için state takibi
    private bool _isSprintingToggled = false;
    private bool _isCrouchingToggled = false;

    // Unity'nin oluþturduðu o C# sýnýfý
    private GameControls _gameControls;

    public event System.Action OnBindingsReset;

    private void Awake()
    {
        if (Instance == null)
        {
            // If not, set this instance as the singleton
            Instance = this;

            // Optionally, mark GameManager as not destroyed between scene loads
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If an instance already exists, destroy this one to enforce the singleton pattern
            Destroy(gameObject);
        }

        // Kontrol sýnýfýný baþlat
        _gameControls = new GameControls();

        LoadBindingOverrides();

        // Varsayýlan deðerler
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSens", 1.0f);
        gamepadSensitivity = PlayerPrefs.GetFloat("GamepadSens", 1.0f);
        invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;
        sprintIsToggle = PlayerPrefs.GetInt("SprintMode", 0) == 1; // 0: Hold, 1: Toggle
        crouchIsToggle = PlayerPrefs.GetInt("CrouchMode", 0) == 1;

    }

    private void Update()
    {
        // Toggle durumlarýný BURADA deðiþtiriyoruz.
        // Update her karede 1 kere çalýþýr. Böylece "Double Call" sorunu biter.

        // SPRINT TOGGLE MANTIÐI
        if (sprintIsToggle && _gameControls.Player.Sprint.triggered)
        {
            _isSprintingToggled = !_isSprintingToggled;
        }

        // CROUCH TOGGLE MANTIÐI
        if (crouchIsToggle && _gameControls.Player.Crouch.triggered)
        {
            _isCrouchingToggled = !_isCrouchingToggled;
        }
    }

    private void OnEnable()
    {
        // Oyuna girince kontrolleri dinlemeye baþla
        _gameControls.Enable();
    }

    private void OnDisable()
    {
        // Oyundan çýkýnca veya script kapanýnca dinlemeyi býrak
        _gameControls.Disable();
    }

    private void LoadBindingOverrides()
    {
        // PlayerPrefs'te kayýtlý bir ayar var mý?
        if (PlayerPrefs.HasKey("Rebinds"))
        {
            string rebinds = PlayerPrefs.GetString("Rebinds");
            // Beyne (GameControls) bu ayarlarý enjekte et
            _gameControls.LoadBindingOverridesFromJson(rebinds);
        }
    }

    // 2. Ayarlarý Kaydet (Tuþ deðiþince)
    public void SaveBindingOverrides()
    {
        // Beyindeki tüm deðiþiklikleri JSON (metin) formatýna çevir
        string rebinds = _gameControls.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("Rebinds", rebinds);
        PlayerPrefs.Save();
    }

    // 3. Sýfýrla (Reset Butonu için)
    public void ResetAllBindings()
    {
        _gameControls.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey("Rebinds");
        // Ýstersen burada UI'ý yenilemek için bir event tetikleyebilirsin
    }

    // --- OYUNCU ÝÇÝN TERCÜMELER ---

    // Hareket (WASD) - Bize Vector2 (x,y) verir
    // Hareket (WASD) - Bize Vector2 (x,y) verir
    public Vector2 GetMovementInput()
    {
        // 1. Ham verileri oku (WASD veya Sol Stick)
        Vector2 defaultMove = _gameControls.Player.Movement.ReadValue<Vector2>();

        // 2. SWAP KONTROLÜ
        if (swapSticks)
        {
            // Swap açýksa: Hareket için SAÐ STICK (Look Action) kullanýlacak.
            // O yüzden "Look" aksiyonunun cihazýna bakýyoruz.
            var lookDevice = _gameControls.Player.Look.activeControl?.device;

            // Eðer Sað Stick oynatýlýyorsa (ve bu bir Gamepad ise)
            if (lookDevice is Gamepad)
            {
                // Sað Stick verisini al, hareket verisi olarak döndür
                Vector2 rightStickVal = _gameControls.Player.Look.ReadValue<Vector2>();
                return Vector2.ClampMagnitude(rightStickVal, 1f);
            }
        }

        // Swap kapalýysa veya klavye kullanýlýyorsa normal Movement verisi
        return Vector2.ClampMagnitude(defaultMove, 1f);
    }

    // Bakýþ (Mouse) - Bize Vector2 (x,y) verir
    // Bakýþ (Mouse) - Bize Vector2 (x,y) verir
    public Vector2 GetLookInput()
    {
        Vector2 rawInput = Vector2.zero;

        // 1. Önce Look aksiyonunun kaynaðýna bakalým (Mouse mu?)
        var lookDevice = _gameControls.Player.Look.activeControl?.device;
        bool isMouse = lookDevice is Mouse;

        // 2. Eðer Mouse ise Swap dinlemez, direkt Mouse verisini al
        if (isMouse)
        {
            rawInput = _gameControls.Player.Look.ReadValue<Vector2>();
        }
        else
        {
            // Cihaz Mouse deðil (Gamepad veya Boþta)

            if (swapSticks)
            {
                // SWAP AÇIK: Bakýþ için SOL STICK (Movement) verisini kullanacaðýz.
                rawInput = _gameControls.Player.Movement.ReadValue<Vector2>();

                // Eðer sol stick oynatýlýyorsa bu kesin gamepad'dir (Klavye olsa yukarýda isMouse false olmazdý genelde ama garanti olsun)
                // isMouse zaten false, gamepadSensitivity kullanýlacak.
            }
            else
            {
                // SWAP KAPALI: Bakýþ için normal SAÐ STICK (Look) verisi.
                rawInput = _gameControls.Player.Look.ReadValue<Vector2>();
            }
        }

        // 3. Hassasiyet Hesaplamalarý (Senin orijinal kodun)
        float uiSensValue = isMouse ? mouseSensitivity : gamepadSensitivity;
        float userMultiplier = Mathf.Lerp(0.1f, 3.0f, uiSensValue / 100f);
        float finalSens = 0f;

        if (isMouse)
        {
            finalSens = BASE_MOUSE_MULTIPLIER * userMultiplier;
        }
        else
        {
            // Gamepad (Normal veya Swapped fark etmez, Time.deltaTime ile çarp)
            finalSens = BASE_GAMEPAD_MULTIPLIER * userMultiplier * Time.deltaTime;
        }

        rawInput *= finalSens;

        if (invertY) rawInput.y *= -1;

        return rawInput;
    }

    // Zýplama - Basýldýðý AN (ThisFrame) true döner
    public bool PlayerJump()
    {
        return _gameControls.Player.Jump.triggered;
    }

    // Koþma - Basýlý tutulduðu sürece true döner
    public bool PlayerSprint()
    {
        if (sprintIsToggle)
        {
            return _isSprintingToggled; // Sadece deðeri döndür
        }
        else
        {
            return _gameControls.Player.Sprint.phase == InputActionPhase.Performed;
        }
    }

    // Eðilme - Basýlý tutulduðu sürece
    public bool PlayerCrouch()
    {
        if (crouchIsToggle)
        {
            return _isCrouchingToggled; // Sadece deðeri döndür
        }
        else
        {
            return _gameControls.Player.Crouch.phase == InputActionPhase.Performed;
        }
    }

    // Etkileþim (Sol Týk) - Basýldýðý AN
    public bool PlayerInteract()
    {
        return _gameControls.Player.Interact.triggered;
    }

    // Basýlý Tutma Durumu (Interaction Charge için lazým olabilir)
    public bool PlayerInteractHold()
    {
        return _gameControls.Player.Interact.phase == InputActionPhase.Performed;
    }

    public bool PlayerInteractRelease()
    {
        return _gameControls.Player.Interact.WasReleasedThisFrame();
    }

    // Fýrlatma (Sað Týk)
    public bool PlayerThrow()
    {
        return _gameControls.Player.Throw.triggered;
    }

    public bool PlayerThrowHold()
    {
        return _gameControls.Player.Throw.phase == InputActionPhase.Performed;
    }

    public bool PlayerThrowRelease()
    {
        return _gameControls.Player.Throw.WasReleasedThisFrame();
    }

    // Telefon (Tab)
    public bool PlayerPhone()
    {
        return _gameControls.Player.Phone.triggered;
    }

    public bool PlayerPause()
    {
        return _gameControls.Player.Pause.triggered;
    }

    public void SetMouseSensitivity(float val) => mouseSensitivity = val;
    public void SetGamepadSensitivity(float val) => gamepadSensitivity = val;
    public void SetInvertY(bool val) => invertY = val;
    public void SetSprintMode(bool isToggle)
    {
        sprintIsToggle = isToggle;
        _isSprintingToggled = false; // Mod deðiþince state'i sýfýrla
    }
    public void SetCrouchMode(bool isToggle)
    {
        crouchIsToggle = isToggle;
        _isCrouchingToggled = false;
    }

    public InputAction GetAction(string actionId)
    {
        // Guid parse etmeye gerek yok, FindAction string olarak ID de kabul eder
        return _gameControls.FindAction(actionId);
    }

    public void ResetBindingsForDevice(bool isGamepad)
    {
        // 1. Tüm aksiyonlarý gez (Jump, Move, Look vs.)
        foreach (InputAction action in _gameControls)
        {
            // 2. Aksiyonun tüm bindinglerini gez (Klavye, Gamepad varyasyonlarý)
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];

                // Binding'in yolu (örn: "<Keyboard>/space" veya "<Gamepad>/buttonSouth")
                // Override edilmiþse overridePath, edilmemiþse path gelir.
                // Biz orijinal path'e bakarak hangi gruba ait olduðunu anlarýz.
                string path = binding.path;

                bool isGamepadBinding = path.Contains("<Gamepad>");
                bool isMouseKeyboardBinding = path.Contains("<Keyboard>") || path.Contains("<Mouse>");

                // 3. Eþleþme kontrolü
                if (isGamepad && isGamepadBinding)
                {
                    action.RemoveBindingOverride(i);
                }
                else if (!isGamepad && isMouseKeyboardBinding)
                {
                    action.RemoveBindingOverride(i);
                }
            }
        }

        // 4. Temizlenen hali kaydet
        SaveBindingOverrides();

        // 5. Herkese haber ver! (RebindUI güncellensin)
        OnBindingsReset?.Invoke();
    }
}