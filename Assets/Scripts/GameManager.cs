using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using static UnityEditor.VersionControl.Asset;

public class GameManager : MonoBehaviour
{
    [System.Serializable]
    public class CustomerDaySortSegment
    {
        [Range(1, 5)]
        public int Day;
        public ICustomer.CustomerName[] CustomerSort;
    }

    [System.Serializable]
    public class DayState
    {
        [Range(1, 5)]
        public int Day;
        [Range(1, 8)]
        public int Part;
        [Space]
        public Vector3 sunRotation;
        public float sunIntensity;
        public Color sunColor;

        public float skyboxExposure;
        public float skyboxRotate;
        public Color skyboxColor;
        public Color environmentColor;

        public Color fogColor;
        public float fogDensity;

        public bool shouldLightsUp;
    }
    public enum BurgerTypes
    {
        ClassicBurger,
        CheeseBurger,
        DoubleCheeseBurger,
        FullMixedBurger,
        StudentBurger,
        EconomicalStudentBurger,
        GoutBurger,
        XBurger,
        RandomBullShitBurger,
        ErtanFullMixedBurger
    }

    public enum DrinkTypes
    {
        BlackPop,
        YellowPop,
        WhitePop
    }

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
    public List<BurgerIngredientData.IngredientType> fullMixedBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> fullMixedBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> studentBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> studentBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> economicalStudentBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> economicalStudentBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> goutBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> goutBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> xBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> xBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> ertanFullMixedBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> ertanFullMixedBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]

    private List<List<BurgerIngredientData.IngredientType>> allBurgerMenus;

    private bool burgerMatched;

    private BurgerBox lastThrowedBurgerBox;
    private Drink lastThrowedDrink;
    private MeshRenderer[] lastThrowedBurgerBoxMeshRenderers;

    [Header("Customer Settings")]
    [SerializeField] private GameObject[] allCustomers; //One Ertan will be put inside and get changed based on the required Ertan.
    private ICustomer.CustomerName[] allCustomersName;

    [Header("Day Settings")]
    public Light sun;
    public Material skyboxMat;
    public DayState[] DayStates;
    public float transitionDuration = 10f;
    private int currentIndex = 0;
    private Coroutine transitionRoutine;
    [Space]
    [SerializeField] private Material[] lightMats;
    [SerializeField] private GameObject[] allLights;
    [Space]
    public CustomerDaySortSegment[] DayCustomerSort;
    private int customerCounter = 0;

    [Header("Day1 Settings")]
    [SerializeField] private GameObject afterFirstNoodleCutsceneTrigger;
    [SerializeField] private DialogueData afterFirstNoodleSelfTalk;

    private ICustomer currentCustomer;

    private int customerLayer;
    private int ungrabableLayer;

    [Header("Other Settings")]
    [Space]
    [SerializeField] private Tray tray;
    [Space]
    [SerializeField] private OrderThrowArea orderThrowArea;

    public int DayCount;

    public static GameManager Instance;

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
            fullMixedBurger,
            studentBurger,
            economicalStudentBurger,
            goutBurger,
            xBurger,
            ertanFullMixedBurger
        };

        allCustomersName = new ICustomer.CustomerName[allCustomers.Length]; // initialize

        for (int i = 0; i < allCustomers.Length; i++)
        {
            allCustomersName[i] = allCustomers[i].GetComponent<ICustomer>().PersonName;
        }

        customerLayer = LayerMask.NameToLayer("Customer");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");

        NextState();

        SetOrderThrowArea(false);
    }

    public void CallCustomer()
    {
        for (int i = 0; i < allCustomersName.Length; i++)
        {
            if (allCustomersName[i] == DayCustomerSort[DayCount - 1].CustomerSort[customerCounter])
            {
                allCustomers[i].gameObject.SetActive(true);
                customerCounter++;
            }
        }
        
    }

    public void NextState()
    {
        // O günün state’lerini filtrele
        var todaysStates = DayStates.Where(s => s.Day == DayCount).ToArray();
        if (todaysStates.Length == 0)
        {
            Debug.LogWarning("Bugün için tanýmlý DayState yok: " + DayCount);
            return;
        }

        // sýradaki state
        int nextIndex = (currentIndex + 1) % todaysStates.Length;

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(Transition(todaysStates[currentIndex], todaysStates[nextIndex]));
        currentIndex = nextIndex;
    }

    public void NextDay()
    {
        DayCount++;
        currentIndex = 0; // yeni güne baþlarken baþtan
    }

    public void AddSauceToTray(SauceBottle.SauceType type)
    {
        tray.AddSauce(type);
    }

    public void HandleAfterFirstNoodle()
    {
        afterFirstNoodleCutsceneTrigger.SetActive(true);
        DialogueManager.Instance.StartSelfDialogue(afterFirstNoodleSelfTalk);
    }

    public void SetCurrentCustomer(ICustomer customer) => currentCustomer = customer;

    public void CustomerReceiveDrink(Drink drink)
    {
        if (currentCustomer != null)
        {
            currentCustomer.ReceiveDrink(drink);
            lastThrowedDrink = drink;

            lastThrowedDrink.GetComponent<MeshRenderer>().enabled = false;
            lastThrowedDrink.GetComponent<MeshCollider>().enabled = false;
            lastThrowedDrink.GetComponent<Rigidbody>().velocity = Vector3.zero;
        }
            
    }

    public void CustomerReceiveBurger(BurgerBox burgerBox)
    {
        if (currentCustomer != null)
        {
            currentCustomer.ReceiveBurger(burgerBox);
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
        currentCustomer.ChangeLayer(customerLayer);
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
        currentCustomer.ChangeLayer(customerLayer);
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

    public void SetOrderThrowArea(bool shouldReceive) => orderThrowArea.ShouldReceive = shouldReceive;

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
                box.SetBurgerType(BurgerTypes.FullMixedBurger);
                box.SetBurgerType(BurgerTypes.StudentBurger);
                box.SetBurgerType(BurgerTypes.EconomicalStudentBurger);
                box.SetBurgerType(BurgerTypes.GoutBurger);
                box.SetBurgerType(BurgerTypes.XBurger);
                box.SetBurgerType(BurgerTypes.RandomBullShitBurger);
                box.SetBurgerType(BurgerTypes.ErtanFullMixedBurger);

                if (menu == classicBurger)
                    matchedType = BurgerTypes.ClassicBurger;

                else if (menu == cheeseBurger)
                    matchedType = BurgerTypes.CheeseBurger;

                else if (menu == doubleCheeseBurger)
                    matchedType = BurgerTypes.DoubleCheeseBurger;

                else if (menu == fullMixedBurger)
                    matchedType = BurgerTypes.FullMixedBurger;

                else if (menu == studentBurger)
                    matchedType = BurgerTypes.StudentBurger;

                else if (menu == economicalStudentBurger)
                    matchedType = BurgerTypes.EconomicalStudentBurger;

                else if (menu == goutBurger)
                    matchedType = BurgerTypes.GoutBurger;

                else if (menu == xBurger)
                    matchedType = BurgerTypes.XBurger;
                else if (menu == ertanFullMixedBurger)
                    matchedType = BurgerTypes.ErtanFullMixedBurger;

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
                
            else if (matchedType == BurgerTypes.FullMixedBurger)
            {
                requiredSauces = fullMixedBurgerSauces;
            }
                
            else if (matchedType == BurgerTypes.StudentBurger)
            {
                requiredSauces = studentBurgerSauces;
            }
                
            else if (matchedType == BurgerTypes.EconomicalStudentBurger)
            {
                requiredSauces = economicalStudentBurgerSauces;
            }
                
            else if (matchedType == BurgerTypes.GoutBurger)
            {
                requiredSauces = goutBurgerSauces;
            }
                
            else if (matchedType == BurgerTypes.XBurger)
            {
                requiredSauces = xBurgerSauces;
            }
            else if (matchedType == BurgerTypes.ErtanFullMixedBurger)
            {
                requiredSauces = ertanFullMixedBurgerSauces;
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

    IEnumerator Transition(DayState from, DayState to)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;

            // Sun
            sun.transform.rotation = Quaternion.Slerp(
                Quaternion.Euler(from.sunRotation),
                Quaternion.Euler(to.sunRotation),
                t
            );
            sun.intensity = Mathf.Lerp(from.sunIntensity, to.sunIntensity, t);
            sun.color = Color.Lerp(from.sunColor, to.sunColor, t);

            // Skybox
            skyboxMat.SetFloat("_Exposure", Mathf.Lerp(from.skyboxExposure, to.skyboxExposure, t));
            skyboxMat.SetFloat("_Rotation", Mathf.Lerp(from.skyboxRotate, to.skyboxRotate, t));
            skyboxMat.SetColor("_Tint", Color.Lerp(from.skyboxColor, to.skyboxColor, t));

            // Fog
            RenderSettings.fogColor = Color.Lerp(from.fogColor, to.fogColor, t);
            RenderSettings.fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, t);
            RenderSettings.ambientLight = Color.Lerp(from.environmentColor, to.environmentColor, t);

            yield return null;
        }

        sun.transform.rotation = Quaternion.Euler(to.sunRotation);
        sun.intensity = to.sunIntensity;
        sun.color = to.sunColor;

        skyboxMat.SetFloat("_Exposure", to.skyboxExposure);
        skyboxMat.SetFloat("_Rotation", to.skyboxRotate);
        skyboxMat.SetColor("_Tint", to.skyboxColor);

        RenderSettings.fogColor = to.fogColor;
        RenderSettings.fogDensity = to.fogDensity;
        RenderSettings.ambientLight = to.environmentColor;

        if (!from.shouldLightsUp && to.shouldLightsUp)
        {
            foreach (Material mat in lightMats)
            {
                mat.EnableKeyword("_EMISSION");
            }

            foreach (GameObject light in allLights)
            {
                light.SetActive(true);
            }
        }
        else if (from.shouldLightsUp && !to.shouldLightsUp)
        {
            foreach (Material mat in lightMats)
            {
                mat.DisableKeyword("_EMISSION");
            }

            foreach (GameObject light in allLights)
            {
                light.SetActive(false);
            }
        }
    }
}
