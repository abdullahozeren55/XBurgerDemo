using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class StickIconUpdater : MonoBehaviour
{
    [Header("Bu Hangi Aksiyon?")]
    [Tooltip("Eðer bu 'Hareket' (Movement) analogu ise iþaretle. Look ise boþ býrak.")]
    [SerializeField] private bool isMovementAction = true;

    [Header("Xbox Sprites")]
    [SerializeField] private Sprite xb_Stick_L; // Üstünde L yazan (veya sol ikon)
    [SerializeField] private Sprite xb_Stick_R; // Üstünde R yazan

    [Header("PlayStation Sprites")]
    [SerializeField] private Sprite ps_Stick_L;
    [SerializeField] private Sprite ps_Stick_R;

    private Image _targetImage;

    private void Awake()
    {
        _targetImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        // Olaylara abone ol
        Settings.OnPromptsChanged += UpdateIcon;
        Settings.OnStickLayoutChangedEvent += UpdateIcon;

        // Açýlýnca hemen güncelle
        UpdateIcon();
    }

    private void OnDisable()
    {
        // Abonelikten çýk
        Settings.OnPromptsChanged -= UpdateIcon;
        Settings.OnStickLayoutChangedEvent -= UpdateIcon;
    }

    public void UpdateIcon()
    {
        if (_targetImage == null) return;

        // 1. Þu anki durumlarý öðren
        bool isXbox = Settings.IsXboxPrompts;
        bool isSwapped = false;

        if (InputManager.Instance != null)
            isSwapped = InputManager.Instance.swapSticks;
        else
            isSwapped = PlayerPrefs.GetInt("StickLayout", 0) == 1; // InputManager yoksa Prefs'ten bak

        // 2. Hangi taraftayým? (Mantýk Tablosu)
        // Eðer Hareket aksiyonuysam ve Swap yoksa -> SOL (L)
        // Eðer Hareket aksiyonuysam ve Swap varsa -> SAÐ (R)
        // Eðer Bakýþ aksiyonuysam ve Swap yoksa -> SAÐ (R)
        // Eðer Bakýþ aksiyonuysam ve Swap varsa -> SOL (L)

        bool showLeftStick = false;

        if (isMovementAction)
        {
            showLeftStick = !isSwapped; // Normalde Soldur, Swaplanýnca Sað olur
        }
        else // Look Action
        {
            showLeftStick = isSwapped; // Normalde Saðdýr, Swaplanýnca Sol olur
        }

        // 3. Sprite'ý Seç
        if (isXbox)
        {
            _targetImage.sprite = showLeftStick ? xb_Stick_L : xb_Stick_R;
        }
        else // PS
        {
            _targetImage.sprite = showLeftStick ? ps_Stick_L : ps_Stick_R;
        }
    }
}