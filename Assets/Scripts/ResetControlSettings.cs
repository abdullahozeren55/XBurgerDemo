using UnityEngine;
using UnityEngine.UI;

public class ResetControlSettings : MonoBehaviour
{
    [Header("Hangi Ayarlar Sýfýrlanacak?")]
    [Tooltip("True ise Gamepad, False ise Klavye/Mouse ayarlarýný sýfýrlar.")]
    [SerializeField] private bool resetGamepad = false;

    // YENÝ: Settings scriptine referans
    [Header("Referanslar")]
    [SerializeField] private Settings settingsScript;

    private Button _myButton;

    private void Awake()
    {
        _myButton = GetComponent<Button>();

        // Eðer Inspector'dan atamayý unutursan diye otomatik bulma
        if (settingsScript == null)
            settingsScript = FindObjectOfType<Settings>();

        if (_myButton != null)
        {
            _myButton.onClick.AddListener(OnResetClicked);
        }
    }

    private void OnResetClicked()
    {
        // 1. Tuþ Atamalarýný (Binding) Sýfýrla
        if (InputManager.Instance != null)
        {
            InputManager.Instance.ResetBindingsForDevice(resetGamepad);
        }

        // 2. YENÝ: Gamepad ise Dropdownlarý da Sýfýrla
        if (resetGamepad && settingsScript != null)
        {
            settingsScript.ResetGamepadUISettings();
        }

        Debug.Log(resetGamepad ? "Gamepad ayarlarý (Tuþlar + UI) sýfýrlandý." : "Klavye ayarlarý sýfýrlandý.");
    }
}