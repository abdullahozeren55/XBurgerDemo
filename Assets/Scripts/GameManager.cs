using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public enum BurgerTypes
    {
        ClassicBurger,
        CheeseBurger,
        DoubleCheeseBurger,
        FullyLoadedBurger,
        BudgetBurger,
        BrokeAhhBurger,
        GoutBurger,
        XBurger,
        LongAhhBurger,
        TomatoLoverBurger,
        BBQBurger,
        BasicBurger,
        RandomBullShitBurger,
    }

    public enum DrinkTypes
    {
        BlackPop,
        YellowPop,
        WhitePop
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
        public Vector2 hotspot;   // Tıklama noktası (Aşağıda açıklayacağım)
    }

    [Header("Cursor Settings")]
    public CursorSettings[] cursors;
    public Vector2 targetCursorSize = new Vector2(32f, 32f);
    public RectTransform cursorRect; // CursorImage'in RectTransform'u
    public Image cursorImage;        // CursorImage'in Image componenti
    public Canvas cursorCanvas;      // CursorCanvas (FPS modunda kapatmak için)
    private Vector2 currentHotspot;  // O anki offset
    private Vector2 _virtualMousePosition;

    [Header("Burger Lists")]
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
    public List<BurgerIngredientData.IngredientType> budgetBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> budgetBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> brokeAhhBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> brokeAhhBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> goutBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> goutBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> xBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> xBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> longAhhBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> longAhhBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> tomatoLoverBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> tomatoLoverBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> bbqBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> bbqBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> basicBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> basicBurgerSauces = new List<SauceBottle.SauceType>();

    private List<List<BurgerIngredientData.IngredientType>> allBurgerMenus;

    private bool burgerMatched;

    private BurgerBox lastThrowedBurgerBox;
    private Drink lastThrowedDrink;
    private MeshRenderer[] lastThrowedBurgerBoxMeshRenderers;

    public ICustomer CurrentCustomer;

    private int customerLayer;
    private int ungrabableLayer;

    [Header("Other Settings")]
    [Space]
    public Tray tray;
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
            classicBurger,
            cheeseBurger,
            doubleCheeseBurger,
            fullyLoadedBurger,
            budgetBurger,
            brokeAhhBurger,
            goutBurger,
            xBurger,
            longAhhBurger,
            tomatoLoverBurger,
            bbqBurger,
            basicBurger,
        };

        customerLayer = LayerMask.NameToLayer("Customer");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

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
        tray.AddSauce(type);
    }

    public void SetCurrentCustomer(ICustomer customer)
    {
        CurrentCustomer = customer;
        CameraManager.Instance.SetCustomerCamLookAt(CurrentCustomer.CameraLookAt);
    }
    public void CustomerReceiveDrink(Drink drink)
    {
        if (CurrentCustomer != null && !CurrentCustomer.TrueDrinkReceived)
        {
            CurrentCustomer.ReceiveDrink(drink);
            lastThrowedDrink = drink;

            lastThrowedDrink.GetComponent<MeshRenderer>().enabled = false;
            lastThrowedDrink.GetComponent<MeshCollider>().enabled = false;
            lastThrowedDrink.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
            
    }

    public void CustomerReceiveBurger(BurgerBox burgerBox)
    {
        if (CurrentCustomer != null && !CurrentCustomer.TrueBurgerReceived)
        {
            CurrentCustomer.ReceiveBurger(burgerBox);
            lastThrowedBurgerBox = burgerBox;

            lastThrowedBurgerBoxMeshRenderers = lastThrowedBurgerBox.GetComponentsInChildren<MeshRenderer>();

            foreach (MeshRenderer mr in lastThrowedBurgerBoxMeshRenderers)
            {
                mr.enabled = false;
            }

            lastThrowedBurgerBox.GetComponent<BoxCollider>().enabled = false;
            lastThrowedBurgerBox.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
            
    }

    public void CustomerGiveBackDrink(Transform throwPoint, Vector3 force)
    {
        CurrentCustomer.ChangeLayer(customerLayer);
        lastThrowedDrink.transform.position = throwPoint.position;
        lastThrowedDrink.transform.rotation = throwPoint.rotation;
        lastThrowedDrink.isJustThrowed = false;
        lastThrowedDrink.isJustDropped = true;
        lastThrowedDrink.CanBeReceived = false;
        lastThrowedDrink.ChangeLayer(ungrabableLayer);

        lastThrowedDrink.GetComponent<MeshRenderer>().enabled = true;
        lastThrowedDrink.GetComponent<MeshCollider>().enabled = true;
        lastThrowedDrink.GetComponent<Rigidbody>().velocity = force; 
    }

    public void CustomerGiveBackBurger(Transform throwPoint, Vector3 force)
    {
        CurrentCustomer.ChangeLayer(customerLayer);
        lastThrowedBurgerBox.transform.position = throwPoint.position;
        lastThrowedBurgerBox.transform.rotation = throwPoint.rotation;
        lastThrowedBurgerBox.isJustThrowed = false;
        lastThrowedBurgerBox.isJustDropped = true;
        lastThrowedBurgerBox.CanBeReceived = false;
        lastThrowedBurgerBox.ChangeLayer(ungrabableLayer);

        foreach (MeshRenderer mr in lastThrowedBurgerBoxMeshRenderers)
        {
            mr.enabled = true;
        }
        lastThrowedBurgerBox.GetComponent<BoxCollider>().enabled = true;
        lastThrowedBurgerBox.GetComponent<Rigidbody>().velocity = force;
    }

    public void SetOrderThrowArea(bool shouldReceive) { if (orderThrowArea != null) orderThrowArea.ShouldReceive = shouldReceive; }

    public void SetCursor(CursorType type)
    {
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

    public void CheckBurgerType(List<BurgerIngredientData.IngredientType> type, List<SauceBottle.SauceType> sauces, BurgerBox box)
    {
        BurgerTypes matchedType = BurgerTypes.RandomBullShitBurger;
        burgerMatched = false;

        foreach (var menu in allBurgerMenus)
        {
            if (AreListsEqual(type, menu))
            {
                burgerMatched = true;

                box.SetBurgerType(BurgerTypes.ClassicBurger);
                box.SetBurgerType(BurgerTypes.CheeseBurger);
                box.SetBurgerType(BurgerTypes.DoubleCheeseBurger);
                box.SetBurgerType(BurgerTypes.FullyLoadedBurger);
                box.SetBurgerType(BurgerTypes.BudgetBurger);
                box.SetBurgerType(BurgerTypes.BrokeAhhBurger);
                box.SetBurgerType(BurgerTypes.GoutBurger);
                box.SetBurgerType(BurgerTypes.XBurger);
                box.SetBurgerType(BurgerTypes.LongAhhBurger);
                box.SetBurgerType(BurgerTypes.TomatoLoverBurger);
                box.SetBurgerType(BurgerTypes.BBQBurger);
                box.SetBurgerType(BurgerTypes.BasicBurger);
                box.SetBurgerType(BurgerTypes.RandomBullShitBurger);

                if (menu == classicBurger)
                    matchedType = BurgerTypes.ClassicBurger;

                else if (menu == cheeseBurger)
                    matchedType = BurgerTypes.CheeseBurger;

                else if (menu == doubleCheeseBurger)
                    matchedType = BurgerTypes.DoubleCheeseBurger;

                else if (menu == fullyLoadedBurger)
                    matchedType = BurgerTypes.FullyLoadedBurger;

                else if (menu == budgetBurger)
                    matchedType = BurgerTypes.BudgetBurger;

                else if (menu == brokeAhhBurger)
                    matchedType = BurgerTypes.BrokeAhhBurger;

                else if (menu == goutBurger)
                    matchedType = BurgerTypes.GoutBurger;

                else if (menu == xBurger)
                    matchedType = BurgerTypes.XBurger;
                else if (menu == longAhhBurger)
                    matchedType = BurgerTypes.LongAhhBurger;
                else if (menu == tomatoLoverBurger)
                    matchedType = BurgerTypes.TomatoLoverBurger;
                else if (menu == bbqBurger)
                    matchedType = BurgerTypes.BBQBurger;
                else if (menu == basicBurger)
                    matchedType = BurgerTypes.BasicBurger;

                break;

            }
                
        }

        if (burgerMatched)
        {
            List<SauceBottle.SauceType> requiredSauces = new List<SauceBottle.SauceType>();
            

            if (matchedType == BurgerTypes.ClassicBurger)
            {
                requiredSauces = classicBurgerSauces;
            }
                
            else if (matchedType == BurgerTypes.CheeseBurger)
            {
                requiredSauces = cheeseBurgerSauces;
            }
                
            else if (matchedType == BurgerTypes.DoubleCheeseBurger)
            {
                requiredSauces = doubleCheeseBurgerSauces;
            }
                
            else if (matchedType == BurgerTypes.FullyLoadedBurger)
            {
                requiredSauces = fullyLoadedBurgerSauces;
            }
                
            else if (matchedType == BurgerTypes.BudgetBurger)
            {
                requiredSauces = budgetBurgerSauces;
            }
                
            else if (matchedType == BurgerTypes.BrokeAhhBurger)
            {
                requiredSauces = brokeAhhBurgerSauces;
            }
                
            else if (matchedType == BurgerTypes.GoutBurger)
            {
                requiredSauces = goutBurgerSauces;
            }
                
            else if (matchedType == BurgerTypes.XBurger)
            {
                requiredSauces = xBurgerSauces;
            }

            else if (matchedType == BurgerTypes.LongAhhBurger)
            {
                requiredSauces = longAhhBurgerSauces;
            }

            else if (matchedType == BurgerTypes.TomatoLoverBurger)
            {
                requiredSauces = tomatoLoverBurgerSauces;
            }
            else if (matchedType == BurgerTypes.BBQBurger)
            {
                requiredSauces = bbqBurgerSauces;
            }
            else if (matchedType == BurgerTypes.BasicBurger)
            {
                requiredSauces = basicBurgerSauces;
            }


            if (AreSaucesEqual(sauces, requiredSauces))
                box.SetBurgerType(matchedType);
            else
                box.SetBurgerType(BurgerTypes.RandomBullShitBurger);
        }
        else
            box.SetBurgerType(BurgerTypes.RandomBullShitBurger);
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
