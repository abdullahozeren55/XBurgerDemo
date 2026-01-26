using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public enum CookAmount
{
    RAW,
    REGULAR,
    BURNT
}

// Müşterileri kodda tanımak için Enum (String yerine Enum daha güvenli)
public enum CustomerID
{
    None,       // Boş/Tanımsız durumlar için
    Player,     // Sinan (Sen)

    // --- Müşteriler ---
    FamilyFather,
    FamilyMother,
    FamilyKid,
    OldMan,
    OldWoman,
    AloneKid,
    TeenGirl0,
    TeenGirl1,

    // --- Diğer Hedefler (Gerekirse eklersin) ---
    // Radio,
    // TV,
    // TableCenter
}
public class GameManager : MonoBehaviour
{
    public enum BurgerTypes
    {
        Null,
        ClassicBurger,
        CheeseBurger,
        DoubleCheeseBurger,
        FullyLoadedBurger,
        GardenBurger,
        DoubleGardenBurger,
        SmokedBurger,
        FlamingBurger,
        PlusBurger,
        CrispyChickenBurger,
        FlamingChickenBurger,
        SmokedChickenBurger,
        ExtremeChickenBurger,
        ChickBurger,
        GrownUpBurger,
        UndefinedBurger0,
        UndefinedBurger1,
        UndefinedBurger2,
        UndefinedBurger3,
        UndefinedBurger4,
    }

    public enum DrinkTypes
    {
        Null,
        Cola,
        OrangeSoda,
        LemonLime,
        OrangeJuice,
        Lemonade,
        CherryJuice,
        Ayran,
    }

    public enum CupSize
    {
        Small,
        Medium,
        Large,
    }

    public enum CursorType
    {
        Default,
        Hand,
        Retro
    }

    [System.Serializable]
    public class CursorSettings
    {
        public CursorType type;
        public Sprite sprite; // İmleç resmi
        public Vector2 hotspot;   // Tıklama noktası
    }

    [Header("Cursor Settings")]
    public CursorSettings[] cursors;
    public Vector2 targetCursorSize = new Vector2(32f, 32f);
    public RectTransform cursorRect; // CursorImage'in RectTransform'u
    public Image cursorImage;        // CursorImage'in Image componenti
    public Canvas cursorCanvas;      // CursorCanvas (FPS modunda kapatmak için)
    private Vector2 currentHotspot;  // O anki offset
    private Vector2 _virtualMousePosition;
    private CursorType _currentCursorType = CursorType.Default; // Şu anki imleç
    private CursorType _savedCursorType = CursorType.Default;   // Hafızadaki imleç
    private bool _savedCursorLocked = true;                     // Hafızadaki kilit durumu

    [Header("Burger Lists")]
    public List<BurgerIngredientData.IngredientType> nullBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> nullBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> classicBurger = new List<BurgerIngredientData.IngredientType>();
    public List <SauceBottle.SauceType> classicBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> cheeseBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> cheeseBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> doubleCheeseBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> doubleCheeseBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> fullyLoadedBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> fullyLoadedBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> gardenBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> gardenBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> doubleGardenBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> doubleGardenBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> smokedBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> smokedBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> flamingBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> flamingBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> plusBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> plusBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> crispyChickenBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> crispyChickenBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> flamingChickenBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> flamingChickenBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> smokedChickenBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> smokedChickenBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> extremeChickenBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> extremeChickenBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> chickBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> chickBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> grownUpBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> grownUpBurgerSauces = new List<SauceBottle.SauceType>();

    private List<List<BurgerIngredientData.IngredientType>> allBurgerMenus;
    private List<List<SauceBottle.SauceType>> allBurgerSauces;

    [Header("Other Settings")]
    [Space]
    public BurgerCombineArea burgerCombineArea;
    [Space]
    public OrderThrowArea orderThrowArea;

    public static GameManager Instance;

    public Volume PostProcessVolume;

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

        allBurgerMenus = new List<List<BurgerIngredientData.IngredientType>>()
        {
            nullBurger,
            classicBurger,
            cheeseBurger,
            doubleCheeseBurger,
            fullyLoadedBurger,
            gardenBurger,
            doubleGardenBurger,
            smokedBurger,
            flamingBurger,
            plusBurger,
            crispyChickenBurger,
            flamingChickenBurger,
            smokedChickenBurger,
            extremeChickenBurger,
            chickBurger,
            grownUpBurger,
        };

        allBurgerSauces = new List<List<SauceBottle.SauceType>>()
        {
            nullBurgerSauces,
            classicBurgerSauces,
            cheeseBurgerSauces,
            doubleCheeseBurgerSauces,
            fullyLoadedBurgerSauces,
            gardenBurgerSauces,
            doubleGardenBurgerSauces,
            smokedBurgerSauces,
            flamingBurgerSauces,
            plusBurgerSauces,
            crispyChickenBurgerSauces,
            flamingChickenBurgerSauces,
            smokedChickenBurgerSauces,
            extremeChickenBurgerSauces,
            chickBurgerSauces,
            grownUpBurgerSauces,
        };

        SetOrderThrowArea(false);

    }

    private void LateUpdate()
    {
        if (Cursor.lockState == CursorLockMode.None)
        {
            // Resmi fare pozisyonuna taşı
            UpdateCursorPosition();
        }
    }

    public void AddSauceToTray(SauceBottle.SauceType type)
    {
        burgerCombineArea.AddSauce(type);
    }

    public void SetOrderThrowArea(bool shouldReceive) { if (orderThrowArea != null) orderThrowArea.ShouldReceive = shouldReceive; }

    public void SetCursor(CursorType type)
    {
        _currentCursorType = type;

        CursorSettings setting = System.Array.Find(cursors, x => x.type == type);

        if (setting != null)
        {
            cursorImage.sprite = setting.sprite;

            // YENİSİ (GARANTİ ÇÖZÜM):
            // RectTransform'un boyutunu (Width/Height) zorla 32x32 yapıyoruz.
            cursorRect.sizeDelta = targetCursorSize;

            // Scale'i 1'de tutuyoruz (Canvas Scaler zaten büyütecek)
            cursorRect.localScale = Vector3.one;

            currentHotspot = setting.hotspot;
        }
        else
        {
            Debug.LogWarning($"CursorManager: '{type}' ayarı bulunamadı!");
        }
    }

    // Mouse'u Kilitle/Aç ve Gizle/Göster
    public void SetCursorLock(bool isLocked)
    {
        // 1. İşletim sistemi faresini kilitle/aç
        Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;

        // 2. Bizim sahte imleci Göster/Gizle
        // Kilitliyken (FPS modu) bizim resmimiz görünmemeli.
        if (cursorCanvas != null)
            cursorCanvas.enabled = !isLocked;

        // 3. Garanti olsun diye sistem faresini hep gizli tutuyoruz
        // çünkü görünürlüğü bizim Canvas sağlıyor.
        Cursor.visible = false;
    }

    // --- YENİ: OPTİMİZE EDİLMİŞ BURGER KONTROLÜ ---
    // Artık BurgerBox referansı almıyor, direkt sonucu (int ID) döndürüyor.
    public BurgerTypes GetBurgerType(List<BurgerIngredientData.IngredientType> currentIngredients, List<SauceBottle.SauceType> currentSauces)
    {
        // Menüdeki tüm burgerleri tara
        for (int i = 0; i < allBurgerMenus.Count; i++)
        {
            // 1. Malzemeler Tutuyor mu?
            bool ingredientsMatch = AreListsEqual(currentIngredients, allBurgerMenus[i]);

            // 2. Soslar Tutuyor mu?
            bool saucesMatch = AreSaucesEqual(currentSauces, allBurgerSauces[i]);

            // İkisi de tutuyorsa bingo!
            if (ingredientsMatch && saucesMatch)
            {
                // Index'i Enum'a çevirip döndür
                return (BurgerTypes)i;
            }
        }

        // Hiçbiri tutmadıysa -> Random Bullshit Burger (Enum'ın son elemanı)
        return FindCurrentUndefinedBurger();
    }

    private BurgerTypes FindCurrentUndefinedBurger()
    {
        if (LoopManager.Instance == null) return BurgerTypes.UndefinedBurger0; //FALLBACK

        switch (LoopManager.Instance.LoopCount)
        {
            case 0:
                return BurgerTypes.UndefinedBurger0;

            case 1:
                return BurgerTypes.UndefinedBurger1;

            case 2:
                return BurgerTypes.UndefinedBurger2;

            case 3:
                return BurgerTypes.UndefinedBurger3;

            case 4:
                return BurgerTypes.UndefinedBurger4;

            default:
                return BurgerTypes.UndefinedBurger0;
        }

    }

    private bool AreListsEqual(List<BurgerIngredientData.IngredientType> list1, List<BurgerIngredientData.IngredientType> list2)
    {
        // Check if both lists have the same count
        if (list1.Count != list2.Count)
        {
            return false;
        }

        // Sort both lists and compare them
        var sortedList1 = list1.OrderBy(x => x).ToList();
        var sortedList2 = list2.OrderBy(x => x).ToList();

        return sortedList1.SequenceEqual(sortedList2);
    }

    private bool AreSaucesEqual(List<SauceBottle.SauceType> list1, List<SauceBottle.SauceType> list2)
    {
        // Check if both lists have the same count
        if (list1.Count != list2.Count)
        {
            return false;
        }

        // Sort both lists and compare them
        var sortedList1 = list1.OrderBy(x => x).ToList();
        var sortedList2 = list2.OrderBy(x => x).ToList();

        return sortedList1.SequenceEqual(sortedList2);
    }

    public void SaveCursorState()
    {
        _savedCursorType = _currentCursorType;
        // Cursor.lockState Locked ise true, değilse false
        _savedCursorLocked = (Cursor.lockState == CursorLockMode.Locked);
    }

    public void RestoreCursorState()
    {
        SetCursor(_savedCursorType);
        SetCursorLock(_savedCursorLocked);
    }

    private void UpdateCursorPosition()
    {
        // 1. Cihaz Tespiti
        bool isMouse = true;
        if (InputManager.Instance != null) isMouse = InputManager.Instance.IsUsingMouse();

        // 2. POZİSYON HESAPLAMA
        if (isMouse)
        {
            _virtualMousePosition = Input.mousePosition;
        }
        else
        {
            // Gamepad kullanıyorsa
            if (InputManager.Instance != null)
            {
                Vector2 input = InputManager.Instance.GetVirtualCursorInput();

                if (input.magnitude > 0.1f)
                {
                    float uiScaleFactor = cursorCanvas.scaleFactor;
                    if (uiScaleFactor <= 0) uiScaleFactor = 1f;

                    _virtualMousePosition += input * InputManager.Instance.virtualCursorSpeed * uiScaleFactor * Time.unscaledDeltaTime;
                }
            }

            // 3. CLAMP (Ekran dışına çıkmasın)
            _virtualMousePosition.x = Mathf.Clamp(_virtualMousePosition.x, 0f, Screen.width);
            _virtualMousePosition.y = Mathf.Clamp(_virtualMousePosition.y, 0f, Screen.height);

            // 4. SİSTEM FARESİNİ IŞINLA (HER ZAMAN)
            // Mouse'u hep takip ettiriyoruz, gizleme saklama yok.
            if (UnityEngine.InputSystem.Mouse.current != null)
            {
                UnityEngine.InputSystem.Mouse.current.WarpCursorPosition(_virtualMousePosition);
            }
        }

        // 5. KANVAS KOORDİNATINA ÇEVİRME
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            cursorCanvas.transform as RectTransform,
            _virtualMousePosition,
            cursorCanvas.worldCamera,
            out localPoint
        );

        // 6. HOTSPOT AYARI
        localPoint.x -= currentHotspot.x;
        localPoint.y += currentHotspot.y;

        // 7. UYGULA
        cursorRect.localPosition = localPoint;
    }
}
