using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    // Singleton
    public static InputManager Instance;

    public enum AimAssistLevel { Off, Low, Medium, High }

    [Header("Cursor Settings")]
    public float virtualCursorSpeed = 1500.0f;

    // YENİ: Cursor Modunda mıyız? (Sağ Stick mi oynandı en son, Sol Stick mi?)
    public bool IsVirtualCursorActive { get; private set; } = true;

    [Header("Aim Assist Settings")]
    public AimAssistLevel aimAssistLevel = AimAssistLevel.Medium;

    private bool _lastInputWasMouse = true;

    [Header("Base Settings")]
    private const float BASE_MOUSE_MULTIPLIER = 0.05f;
    private const float STICK_DEADZONE = 0.15f;
    private const float RESPONSE_CURVE = 2f;
    private const float BASE_GAMEPAD_MULTIPLIER = 80.0f;

    [Header("Control Settings")]
    public float mouseSensitivity = 1.0f;
    public float gamepadSensitivity = 1.0f;
    public bool invertY = false;
    public bool sprintIsToggle = false;
    public bool crouchIsToggle = false;

    [Header("Gamepad Settings")]
    public bool swapSticks = false;

    [Header("Gamepad Feel Settings")]
    [Range(0.01f, 0.5f)]
    public float lookSmoothTime = 0.1f;
    [HideInInspector] public float aimAssistSlowdown = 1.0f;

    private Vector2 _currentLookVelocity;
    private Vector2 _currentLookValue;

    public void SetSwapSticks(bool isSwapped) => swapSticks = isSwapped;

    private bool _isSprintingToggled = false;
    private bool _isCrouchingToggled = false;

    private GameControls _gameControls;
    public event System.Action OnBindingsReset;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        _gameControls = new GameControls();
        LoadBindingOverrides();

        mouseSensitivity = PlayerPrefs.GetFloat("MouseSens", 1.0f);
        gamepadSensitivity = PlayerPrefs.GetFloat("GamepadSens", 1.0f);
        invertY = PlayerPrefs.GetInt("InvertY", 0) == 1;
        sprintIsToggle = PlayerPrefs.GetInt("SprintMode", 0) == 1;
        crouchIsToggle = PlayerPrefs.GetInt("CrouchMode", 0) == 1;
    }

    private void Update()
    {
        if (sprintIsToggle && _gameControls.Player.Sprint.triggered)
        {
            _isSprintingToggled = !_isSprintingToggled;
        }

        if (crouchIsToggle && _gameControls.Player.Crouch.triggered)
        {
            _isCrouchingToggled = !_isCrouchingToggled;
        }

        CheckActiveDevice();
    }

    private void OnEnable()
    {
        _gameControls.Enable();
    }

    private void OnDisable()
    {
        _gameControls.Disable();
    }

    private void LoadBindingOverrides()
    {
        if (PlayerPrefs.HasKey("Rebinds"))
        {
            string rebinds = PlayerPrefs.GetString("Rebinds");
            _gameControls.LoadBindingOverridesFromJson(rebinds);
        }
    }

    public void SaveBindingOverrides()
    {
        string rebinds = _gameControls.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString("Rebinds", rebinds);
        PlayerPrefs.Save();
    }

    public void ResetAllBindings()
    {
        _gameControls.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey("Rebinds");
    }

    public Vector2 GetVirtualCursorInput()
    {
        if (_gameControls.UI.enabled)
        {
            return _gameControls.UI.VirtualCursorMove.ReadValue<Vector2>();
        }
        return _gameControls.Player.VirtualCursorMove.ReadValue<Vector2>();
    }

    // --- MOVEMENT ---
    public Vector2 GetMovementInput()
    {
        Vector2 defaultMove = _gameControls.Player.Movement.ReadValue<Vector2>();

        if (swapSticks)
        {
            var lookDevice = _gameControls.Player.Look.activeControl?.device;
            if (lookDevice is Gamepad)
            {
                Vector2 rightStickVal = _gameControls.Player.Look.ReadValue<Vector2>();
                return Vector2.ClampMagnitude(rightStickVal, 1f);
            }
        }
        return Vector2.ClampMagnitude(defaultMove, 1f);
    }

    // --- LOOK ---
    public Vector2 GetLookInput()
    {
        Vector2 rawInput = Vector2.zero;
        var lookDevice = _gameControls.Player.Look.activeControl?.device;
        bool isMouse = lookDevice is Mouse;

        if (isMouse)
        {
            rawInput = _gameControls.Player.Look.ReadValue<Vector2>();
            float userMultiplier = Mathf.Lerp(0.1f, 3.0f, mouseSensitivity / 100f);
            Vector2 mouseResult = rawInput * BASE_MOUSE_MULTIPLIER * userMultiplier;
            if (invertY) mouseResult.y *= -1;
            return mouseResult;
        }

        if (swapSticks)
            rawInput = _gameControls.Player.Movement.ReadValue<Vector2>();
        else
            rawInput = _gameControls.Player.Look.ReadValue<Vector2>();

        rawInput = Vector2.ClampMagnitude(rawInput, 1f);

        Vector2 targetValue = Vector2.zero;
        float rawMagnitude = rawInput.magnitude;

        if (rawMagnitude >= STICK_DEADZONE)
        {
            float effectiveInput = (rawMagnitude - STICK_DEADZONE) / (1f - STICK_DEADZONE);
            float curvedMagnitude = Mathf.Pow(effectiveInput, RESPONSE_CURVE);
            Vector2 inputDirection = rawInput.normalized;
            float userSens = Mathf.Lerp(0.5f, 3.5f, gamepadSensitivity / 100f);
            targetValue = inputDirection * curvedMagnitude * BASE_GAMEPAD_MULTIPLIER * userSens;
            targetValue.y *= 0.7f;
            if (invertY) targetValue.y *= -1;
        }

        if (Mathf.Abs(targetValue.x) > 0.01f && Mathf.Sign(targetValue.x) != Mathf.Sign(_currentLookValue.x))
            _currentLookVelocity.x = 0f;

        if (Mathf.Abs(targetValue.y) > 0.01f && Mathf.Sign(targetValue.y) != Mathf.Sign(_currentLookValue.y))
            _currentLookVelocity.y = 0f;

        _currentLookValue = Vector2.SmoothDamp(_currentLookValue, targetValue, ref _currentLookVelocity, lookSmoothTime);
        return _currentLookValue * aimAssistSlowdown * Time.deltaTime;
    }

    public bool PlayerJump() => _gameControls.Player.Jump.triggered;

    public bool PlayerSprint()
    {
        if (sprintIsToggle) return _isSprintingToggled;
        else return _gameControls.Player.Sprint.phase == InputActionPhase.Performed;
    }

    public bool PlayerCrouch()
    {
        if (crouchIsToggle) return _isCrouchingToggled;
        else return _gameControls.Player.Crouch.phase == InputActionPhase.Performed;
    }

    public bool PlayerInteract() => _gameControls.Player.Interact.triggered;
    public bool PlayerInteractHold() => _gameControls.Player.Interact.phase == InputActionPhase.Performed;
    public bool PlayerInteractRelease() => _gameControls.Player.Interact.WasReleasedThisFrame();
    public bool PlayerThrow() => _gameControls.Player.Throw.triggered;
    public bool PlayerThrowHold() => _gameControls.Player.Throw.phase == InputActionPhase.Performed;
    public bool PlayerThrowRelease() => _gameControls.Player.Throw.WasReleasedThisFrame();
    public bool PlayerPhone() => _gameControls.Player.Phone.triggered;

    public bool PlayerPause()
    {
        return _gameControls.Player.Pause.triggered || _gameControls.UI.Pause.triggered;
    }

    public void SwitchToUIMode()
    {
        _gameControls.Player.Disable();
        _gameControls.UI.Enable();
    }

    public void SwitchToGameplayMode()
    {
        _gameControls.UI.Disable();
        _gameControls.Player.Enable();
    }

    public void SetMouseSensitivity(float val) => mouseSensitivity = val;
    public void SetGamepadSensitivity(float val) => gamepadSensitivity = val;
    public void SetInvertY(bool val) => invertY = val;
    public void SetSprintMode(bool isToggle)
    {
        sprintIsToggle = isToggle;
        _isSprintingToggled = false;
    }
    public void SetCrouchMode(bool isToggle)
    {
        crouchIsToggle = isToggle;
        _isCrouchingToggled = false;
    }

    public InputAction GetAction(string actionId) => _gameControls.FindAction(actionId);

    public void ResetBindingsForDevice(bool isGamepad)
    {
        foreach (InputAction action in _gameControls)
        {
            for (int i = 0; i < action.bindings.Count; i++)
            {
                InputBinding binding = action.bindings[i];
                string path = binding.path;
                bool isGamepadBinding = path.Contains("<Gamepad>");
                bool isMouseKeyboardBinding = path.Contains("<Keyboard>") || path.Contains("<Mouse>");

                if (isGamepad && isGamepadBinding) action.RemoveBindingOverride(i);
                else if (!isGamepad && isMouseKeyboardBinding) action.RemoveBindingOverride(i);
            }
        }
        SaveBindingOverrides();
        OnBindingsReset?.Invoke();
    }

    public float GetAssistRadius()
    {
        switch (aimAssistLevel)
        {
            case AimAssistLevel.Low: return 0.1f;
            case AimAssistLevel.Medium: return 0.2f;
            case AimAssistLevel.High: return 0.3f;
            default: return 0f;
        }
    }

    public float GetMagnetStrength()
    {
        switch (aimAssistLevel)
        {
            case AimAssistLevel.Low: return 0.75f;
            case AimAssistLevel.Medium: return 0.5f;
            case AimAssistLevel.High: return 0.25f;
            default: return 1.0f;
        }
    }

    public bool IsUsingMouse() => _lastInputWasMouse;
    public void SetAimAssistLevel(int index) => aimAssistLevel = (AimAssistLevel)index;

    private void CheckActiveDevice()
    {
        if (Mouse.current != null && Mouse.current.delta.ReadValue().sqrMagnitude > 1.0f)
            _lastInputWasMouse = true;
        else if (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
            _lastInputWasMouse = true;

        if (Gamepad.current != null)
        {
            Vector2 rightStick = Gamepad.current.rightStick.ReadValue();
            Vector2 leftStick = Gamepad.current.leftStick.ReadValue();

            if (rightStick.magnitude > 0.2f || leftStick.magnitude > 0.2f)
                _lastInputWasMouse = false;
            else if (Gamepad.current.buttonSouth.wasPressedThisFrame ||
                     Gamepad.current.buttonEast.wasPressedThisFrame ||
                     Gamepad.current.buttonWest.wasPressedThisFrame ||
                     Gamepad.current.buttonNorth.wasPressedThisFrame)
                _lastInputWasMouse = false;
        }
    }
}