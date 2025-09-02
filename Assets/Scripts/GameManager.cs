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

        public Color fogColor;
        public float fogDensity;
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
        BigSifadBurger,
        RandomBullShitBurger,
        ErtanFullMixedBurger
    }

    public enum DrinkTypes
    {
        BlackPop,
        YellowPop,
        WhitePop
    }

    public enum HandRigTypes
    {
        Interaction,
        SingleHandGrab,
        Talk,
        Nothing,
    }

    public enum GrabTypes
    {
        SingleHand
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
    public List<BurgerIngredientData.IngredientType> bigSifadBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> bigSifadBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]
    public List<BurgerIngredientData.IngredientType> ertanFullMixedBurger = new List<BurgerIngredientData.IngredientType>();
    public List<SauceBottle.SauceType> ertanFullMixedBurgerSauces = new List<SauceBottle.SauceType>();
    [Space]

    [Header("Burger Texts")]
    public GameObject classicBurgerText;
    public GameObject cheeseBurgerText;
    public GameObject doubleCheeseBurgerText;
    public GameObject fullMixedBurgerText;
    public GameObject studentBurgerText;
    public GameObject economicalStudentBurgerText;
    public GameObject goutBurgerText;
    public GameObject bigSifadBurgerText;
    public GameObject randomBullshitBurgerText;

    private List<List<BurgerIngredientData.IngredientType>> allBurgerMenus;
    private List<List<SauceBottle.SauceType>> allBurgerSauces;

    private bool burgerMatched;

    private BurgerBox lastThrowedBurgerBox;
    private Drink lastThrowedDrink;
    private MeshRenderer[] lastThrowedBurgerBoxMeshRenderers;

    [Header("Ertan Settings")]
    [SerializeField] private GameObject[] allErtans; //0 for regular, turning into an abomination as the number goes up
    [SerializeField] private Ertan[] allErtansSC; //0 for regular, turning into an abomination as the number goes up
    [Space]
    [SerializeField] private ICustomer.CustomerDayChangesSegment[] ErtanDayChanges;
    [Space]
    [HideInInspector] public bool ertanDidEatCheeseYesterday;
    [HideInInspector] public int levelOfMadness = 1; //1 for regular, turning into an abomination as the number goes up

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
    public CustomerDaySortSegment[] DayCustomerSort;
    private int customerCounter = 0;

    private ICustomer currentCustomer;

    [SerializeField] private GameObject skyColliderControllerGO;
    [Space]
    [SerializeField] private Tray tray;
    [Space]
    [SerializeField] private OrderThrowArea orderThrowArea;

    public int DayCount;

    public static GameManager Instance;

    private FirstPersonController firstPersonController;
    private CharacterController characterController;

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
            bigSifadBurger,
            ertanFullMixedBurger
        };

        allBurgerSauces = new List<List<SauceBottle.SauceType>>()
        {
            cheeseBurgerSauces,
            cheeseBurgerSauces,
            doubleCheeseBurgerSauces,
            fullMixedBurgerSauces,
            studentBurgerSauces,
            economicalStudentBurgerSauces,
            goutBurgerSauces,
            bigSifadBurgerSauces,
            ertanFullMixedBurgerSauces
        };

        allCustomersName = new ICustomer.CustomerName[allCustomers.Length]; // initialize

        for (int i = 0; i < allCustomers.Length; i++)
        {
            allCustomersName[i] = allCustomers[i].GetComponent<ICustomer>().PersonName;
        }

        NextState();

        firstPersonController = FindFirstObjectByType<FirstPersonController>();
        characterController = FindFirstObjectByType<CharacterController>();

        SetSkyCollider(false);
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


    public void ResetPlayerGrabAndInteract()
    {
        firstPersonController.ResetGrabAndInteract();
    }

    public void ResetPlayerGrab(IGrabable grabable)
    {
        firstPersonController.ResetGrab(grabable);
    }

    public void ChangePlayerCanMove(bool canMove)
    {
        firstPersonController.CanMove = canMove;
    }

    public void ChangePlayerCurrentGrabable(IGrabable objectToGrab)
    {
        firstPersonController.ChangeCurrentGrabable(objectToGrab);
    }

    public void SetPlayerPushedByCustomer(bool value)
    {
        firstPersonController.isBeingPushedByACustomer = value;
    }

    public void MovePlayer(Vector3 moveForce)
    {
        characterController.Move(moveForce);
    }

    public void AddSauceToTray(SauceBottle.SauceType type)
    {
        tray.AddSauce(type);
    }

    public void SetSkyCollider(bool value)
    {
        if (value == true)
            skyColliderControllerGO.GetComponent<SkyColliderController>().currentCustomer = currentCustomer;
        skyColliderControllerGO.SetActive(value);
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
        lastThrowedDrink.transform.position = throwPoint.position;
        lastThrowedDrink.GetComponent<MeshRenderer>().enabled = true;
        lastThrowedDrink.GetComponent<MeshCollider>().enabled = true;
        lastThrowedDrink.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse); 
    }

    public void CustomerGiveBackBurger(Transform throwPoint, Vector3 force)
    {
        lastThrowedBurgerBox.transform.position = throwPoint.position;

        foreach (MeshRenderer mr in lastThrowedBurgerBoxMeshRenderers)
        {
            mr.enabled = true;
        }
        lastThrowedBurgerBox.GetComponent<BoxCollider>().enabled = true;
        lastThrowedBurgerBox.GetComponent<Rigidbody>().AddForce(force, ForceMode.Impulse);
    }

    public void SetOrderThrowArea(bool shouldReceive) => orderThrowArea.ShouldReceive = shouldReceive;

    public void CheckBurgerType(List<BurgerIngredientData.IngredientType> type, List<SauceBottle.SauceType> sauces, BurgerBox box)
    {
        BurgerTypes matchedType = BurgerTypes.RandomBullShitBurger;
        burgerMatched = false;
        GameObject textToChange = randomBullshitBurgerText;

        foreach (var menu in allBurgerMenus)
        {
            if (AreListsEqual(type, menu))
            {
                burgerMatched = true;

                box.ChangeText(classicBurgerText, BurgerTypes.ClassicBurger);
                box.ChangeText(cheeseBurgerText, BurgerTypes.CheeseBurger);
                box.ChangeText(doubleCheeseBurgerText, BurgerTypes.DoubleCheeseBurger);
                box.ChangeText(fullMixedBurgerText, BurgerTypes.FullMixedBurger);
                box.ChangeText(studentBurgerText, BurgerTypes.StudentBurger);
                box.ChangeText(economicalStudentBurgerText, BurgerTypes.EconomicalStudentBurger);
                box.ChangeText(goutBurgerText, BurgerTypes.GoutBurger);
                box.ChangeText(bigSifadBurgerText, BurgerTypes.BigSifadBurger);
                box.ChangeText(randomBullshitBurgerText, BurgerTypes.RandomBullShitBurger);
                box.ChangeText(randomBullshitBurgerText, BurgerTypes.ErtanFullMixedBurger);

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

                else if (menu == bigSifadBurger)
                    matchedType = BurgerTypes.BigSifadBurger;
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
                textToChange = classicBurgerText;
            }
                
            else if (matchedType == BurgerTypes.CheeseBurger)
            {
                requiredSauces = cheeseBurgerSauces;
                textToChange = cheeseBurgerText;
            }
                
            else if (matchedType == BurgerTypes.DoubleCheeseBurger)
            {
                requiredSauces = doubleCheeseBurgerSauces;
                textToChange = doubleCheeseBurgerText;
            }
                
            else if (matchedType == BurgerTypes.FullMixedBurger)
            {
                requiredSauces = fullMixedBurgerSauces;
                textToChange = fullMixedBurgerText;
            }
                
            else if (matchedType == BurgerTypes.StudentBurger)
            {
                requiredSauces = studentBurgerSauces;
                textToChange = studentBurgerText;
            }
                
            else if (matchedType == BurgerTypes.EconomicalStudentBurger)
            {
                requiredSauces = economicalStudentBurgerSauces;
                textToChange = economicalStudentBurgerText;
            }
                
            else if (matchedType == BurgerTypes.GoutBurger)
            {
                requiredSauces = goutBurgerSauces;
                textToChange = goutBurgerText;
            }
                
            else if (matchedType == BurgerTypes.BigSifadBurger)
            {
                requiredSauces = bigSifadBurgerSauces;
                textToChange = bigSifadBurgerText;
            }
            else if (matchedType == BurgerTypes.ErtanFullMixedBurger)
            {
                requiredSauces = ertanFullMixedBurgerSauces;
                textToChange = randomBullshitBurgerText;
            }


            if (AreSaucesEqual(sauces, requiredSauces))
                box.ChangeText(textToChange, matchedType);
            else
                box.ChangeText(randomBullshitBurgerText, BurgerTypes.RandomBullShitBurger);
        }
        else
            box.ChangeText(randomBullshitBurgerText, BurgerTypes.RandomBullShitBurger);
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

            yield return null;
        }
    }
}
