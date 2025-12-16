using UnityEngine;
// YENİ INPUT SİSTEMİNİ KULLANMAK İÇİN BU GEREKLİ:
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Singleton (Her yerden erişmek için)
    public static InputManager Instance;

    [Header("Base Settings")]
    // Mouse Delta genelde çok büyük gelir (pixel pixel), o yüzden onu dizginlemek lazım.
    private const float BASE_MOUSE_MULTIPLIER = 0.05f;

    private const float STICK_DEADZONE = 0.15f; // %15 ölü bölge (Drift önler)
    private const float RESPONSE_CURVE = 1.8f;
    // Gamepad 0-1 arası gelir, onu hızlandırmak lazım.
    private const float BASE_GAMEPAD_MULTIPLIER = 50.0f;

    [Header("Control Settings")]
    public float mouseSensitivity = 1.0f;
    public float gamepadSensitivity = 1.0f; // Slider 1.0 iken normal hız olsun
    public bool invertY = false;
    public bool sprintIsToggle = false;
    public bool crouchIsToggle = false;

    [Header("Gamepad Settings")]
    public bool swapSticks = false; // Settings.cs'den bunu true/false yapacağız

    [Header("Gamepad Feel Settings")]
    [Range(0.01f, 0.5f)]
    public float lookSmoothTime = 0.1f; // Gecikme süresi (Düşük = Keskin, Yüksek = Ağır)

    // SmoothDamp fonksiyonu için referans değişkeni (Hafıza)
    private Vector2 _currentLookVelocity;
    private Vector2 _currentLookValue; // Anlık yumuşatılmış değer

    public void SetSwapSticks(bool isSwapped) => swapSticks = isSwapped;

    // Toggle mantığı için state takibi
    private bool _isSprintingToggled = false;
    private bool _isCrouchingToggled = false;

    // Unity'nin oluşturduğu o C# sınıfı
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

        // Kontrol sınıfını başlat
        _gameControls = new GameControls();

        LoadBindingOverrides();

        // Varsayılan değerler
        mouseSensitivity = PlayerPrefs.GetFloat("MouseSens", 1.0f);
        gamepadSensitivity = PlayerPrefs.GetFloat("GamepadSens", 1.0f);
        invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;
        sprintIsToggle = PlayerPrefs.GetInt("SprintMode", 0) == 1; // 0: Hold, 1: Toggle
        crouchIsToggle = PlayerPrefs.GetInt("CrouchMode", 0) == 1;

    }

    private void Update()
    {
        // Toggle durumlarını BURADA değiştiriyoruz.
        // Update her karede 1 kere çalışır. Böylece "Double Call" sorunu biter.

        // SPRINT TOGGLE MANTIĞI
        if (sprintIsToggle && _gameControls.Player.Sprint.triggered)
        {
            _isSprintingToggled = !_isSprintingToggled;
        }

        // CROUCH TOGGLE MANTIĞI
        if (crouchIsToggle && _gameControls.Player.Crouch.triggered)
        {
            _isCrouchingToggled = !_isCrouchingToggled;
        }
    }

    private void OnEnable()
    {
        // Oyuna girince kontrolleri dinlemeye başla
        _gameControls.Enable();
    }

    private void OnDisable()
    {
        // Oyundan çıkınca veya script kapanınca dinlemeyi bırak
        _gameControls.Disable();
    }

    private void LoadBindingOverrides()
    {
        // PlayerPrefs'te kayıtlı bir ayar var mı?
        if (PlayerPrefs.HasKey("Rebinds"))
        {
            string rebinds = PlayerPrefs.GetString("Rebinds");
            // Beyne (GameControls) bu ayarları enjekte et
            _gameControls.LoadBindingOverridesFromJson(rebinds);
        }
    }

    // 2. Ayarları Kaydet (Tuş değişince)
    public void SaveBindingOverrides()
    {
        // Beyindeki tüm değişiklikleri JSON (metin) formatına çevir
        string rebinds = _gameControls.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("Rebinds", rebinds);
        PlayerPrefs.Save();
    }

    // 3. Sıfırla (Reset Butonu için)
    public void ResetAllBindings()
    {
        _gameControls.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey("Rebinds");
        // İstersen burada UI'ı yenilemek için bir event tetikleyebilirsin
    }

    // --- OYUNCU İÇİN TERCÜMELER ---

    // Hareket (WASD) - Bize Vector2 (x,y) verir
    // Hareket (WASD) - Bize Vector2 (x,y) verir
    public Vector2 GetMovementInput()
    {
        // 1. Varsayılan (Sol Stick / WASD) verisini oku
        Vector2 defaultMove = _gameControls.Player.Movement.ReadValue<Vector2>();

        // 2. SWAP KONTROLÜ
        if (swapSticks)
        {
            // Swap açıksa: Hareket için SAĞ STICK (Look Action) kullanılacak.
            var lookDevice = _gameControls.Player.Look.activeControl?.device;

            // Eğer Sağ Stick oynatılıyorsa (ve bu bir Gamepad ise)
            if (lookDevice is Gamepad)
            {
                Vector2 rightStickVal = _gameControls.Player.Look.ReadValue<Vector2>();
                // Yürümede kavis (karesini alma) olmaz, doğrusal olmalı. Clamp yeterli.
                return Vector2.ClampMagnitude(rightStickVal, 1f);
            }
        }

        // Swap kapalıysa veya klavye kullanılıyorsa normal Movement verisi
        return Vector2.ClampMagnitude(defaultMove, 1f);
    }

    // --- 2. LOOK (Bakış) - Kavisli ve Hassas Olmalı ---
    public Vector2 GetLookInput()
    {
        Vector2 rawInput = Vector2.zero;

        // 1. Cihaz Tespiti
        var lookDevice = _gameControls.Player.Look.activeControl?.device;
        bool isMouse = lookDevice is Mouse;

        // 2. HAM VERİ (RAW)
        if (isMouse)
        {
            rawInput = _gameControls.Player.Look.ReadValue<Vector2>();

            // Mouse için smoothing'e gerek yok (Mouse zaten keskin olmalı)
            float userMultiplier = Mathf.Lerp(0.1f, 3.0f, mouseSensitivity / 100f);
            return rawInput * BASE_MOUSE_MULTIPLIER * userMultiplier;
        }
        else
        {
            // --- GAMEPAD İŞLEMLERİ ---

            // A. Ham veriyi al (Swap kontrolü ile)
            if (swapSticks)
                rawInput = _gameControls.Player.Movement.ReadValue<Vector2>();
            else
                rawInput = _gameControls.Player.Look.ReadValue<Vector2>();

            // B. Hedef Değeri Hesapla (Matematiksel İdeal Değer)
            Vector2 targetValue = Vector2.zero;
            float rawMagnitude = rawInput.magnitude;

            if (rawMagnitude >= STICK_DEADZONE)
            {
                // Deadzone Rescaling (Yumuşak Başlangıç)
                float effectiveInput = (rawMagnitude - STICK_DEADZONE) / (1f - STICK_DEADZONE);

                // Curve (Kavis) - 1.8f daha geniş bir hassasiyet aralığı sağlar
                float curvedMagnitude = Mathf.Pow(effectiveInput, RESPONSE_CURVE);

                Vector2 inputDirection = rawInput.normalized;

                // Kullanıcı Hassasiyeti
                float userSens = Mathf.Lerp(0.5f, 3.5f, gamepadSensitivity / 100f);

                // Hedef Vektör
                targetValue = inputDirection * curvedMagnitude * BASE_GAMEPAD_MULTIPLIER * userSens;

                // Dikey yavaşlatma (Y ekseni)
                targetValue.y *= 0.7f;
                if (invertY) targetValue.y *= -1;
            }
            else
            {
                // Deadzone içindeyse hedef sıfır
                targetValue = Vector2.zero;
            }

            // C. SMOOTHING (YUMUŞATMA) - İŞTE OLAY BURADA! 🧈
            // Anlık değerden (_currentLookValue), hedef değere (targetValue)
            // belli bir sürede (lookSmoothTime) yumuşakça geçiş yap.

            // Not: SmoothDamp Update içinde çağrılmalı. Burası her frame çağrılıyorsa sorun yok.
            // Ama InputManager'ın Update'inde değil, PlayerController burayı her frame çağırıyorsa çalışır.
            // InputManager.GetLookInput() genelde Update'te çağrılır.

            _currentLookValue = Vector2.SmoothDamp(_currentLookValue, targetValue, ref _currentLookVelocity, lookSmoothTime);

            // Sonuç olarak yumuşatılmış değeri Time.deltaTime ile çarpıp döndür
            return _currentLookValue * Time.deltaTime;
        }
    }

    // Zıplama - Basıldığı AN (ThisFrame) true döner
    public bool PlayerJump()
    {
        return _gameControls.Player.Jump.triggered;
    }

    // Koşma - Basılı tutulduğu sürece true döner
    public bool PlayerSprint()
    {
        if (sprintIsToggle)
        {
            return _isSprintingToggled; // Sadece değeri döndür
        }
        else
        {
            return _gameControls.Player.Sprint.phase == InputActionPhase.Performed;
        }
    }

    // Eğilme - Basılı tutulduğu sürece
    public bool PlayerCrouch()
    {
        if (crouchIsToggle)
        {
            return _isCrouchingToggled; // Sadece değeri döndür
        }
        else
        {
            return _gameControls.Player.Crouch.phase == InputActionPhase.Performed;
        }
    }

    // Etkileşim (Sol Tık) - Basıldığı AN
    public bool PlayerInteract()
    {
        return _gameControls.Player.Interact.triggered;
    }

    // Basılı Tutma Durumu (Interaction Charge için lazım olabilir)
    public bool PlayerInteractHold()
    {
        return _gameControls.Player.Interact.phase == InputActionPhase.Performed;
    }

    public bool PlayerInteractRelease()
    {
        return _gameControls.Player.Interact.WasReleasedThisFrame();
    }

    // Fırlatma (Sağ Tık)
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
        _isSprintingToggled = false; // Mod değişince state'i sıfırla
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
        // 1. Tüm aksiyonları gez (Jump, Move, Look vs.)
        foreach (InputAction action in _gameControls)
        {
            // 2. Aksiyonun tüm bindinglerini gez (Klavye, Gamepad varyasyonları)
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];

                // Binding'in yolu (örn: "<Keyboard>/space" veya "<Gamepad>/buttonSouth")
                // Override edilmişse overridePath, edilmemişse path gelir.
                // Biz orijinal path'e bakarak hangi gruba ait olduğunu anlarız.
                string path = binding.path;

                bool isGamepadBinding = path.Contains("<Gamepad>");
                bool isMouseKeyboardBinding = path.Contains("<Keyboard>") || path.Contains("<Mouse>");

                // 3. Eşleşme kontrolü
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