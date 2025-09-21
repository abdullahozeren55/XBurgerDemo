using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tray : MonoBehaviour
{
    [SerializeField] private float startPointYHeight = 0.01f;
    [SerializeField] private float boxClosingSquashMinLimit = 0.16f;
    [SerializeField] private Transform burgerBoxTransform;
    [SerializeField] private Transform ingredientsParent;
    [SerializeField] private BoxCollider boxCollider;
    [SerializeField] private BoxCollider burgerCollider;

    [Header("Holograms")]
    [SerializeField] private GameObject onion;
    [SerializeField] private GameObject lettuce;
    [SerializeField] private GameObject tomato;
    [SerializeField] private GameObject pickle;
    [SerializeField] private GameObject patty;
    [SerializeField] private GameObject cheese;
    [SerializeField] private GameObject bottomBun;
    [SerializeField] private GameObject topBun;
    [SerializeField] private GameObject box;
    [SerializeField] private GameObject sauce;

    [Header("Sauces")]
    [SerializeField] private GameObject[] sauces; //0 ketchup, 1 mayo, 2 mustard, 3 bbq

    private Vector3 currentLocationToPutBurgerIngredient;
    private Vector3 hologramLocation;

    private List<BurgerIngredient> allBurgerIngredients = new List<BurgerIngredient>();
    private List<SauceBottle.SauceType> allSauces = new List<SauceBottle.SauceType>();
    private List<GameObject> allGO = new List<GameObject>();

    private float boxColliderStartZ;
    private float boxColliderStartCenterZ;

    private float burgerColliderStartZ;
    private float burgerColliderStartCenterZ;

    private bool burgerIsDone;

    [HideInInspector] public BurgerIngredient currentIngredient;
    [HideInInspector] public BurgerBox currentBox;

    private int onTrayLayer;

    private void Awake()
    {
        currentIngredient = null;
        currentBox = null;

        boxColliderStartZ = boxCollider.size.z;
        boxColliderStartCenterZ = boxCollider.center.z;

        burgerColliderStartZ = burgerCollider.size.z;
        burgerColliderStartCenterZ = burgerCollider.center.z;

        burgerIsDone = false;

        ResetPosition();

        onTrayLayer = LayerMask.NameToLayer("OnTray");
    }

    private void UpdateCurrentLocationToPutBurgerIngredient(float heightIncreaseAmount)
    {
        currentLocationToPutBurgerIngredient.y += heightIncreaseAmount;

        Vector3 newSize = boxCollider.size;
        newSize.z += heightIncreaseAmount/12;
        Vector3 newCenter = boxCollider.center;
        newCenter.z += heightIncreaseAmount / 24f;
        boxCollider.size = newSize;
        boxCollider.center = newCenter;

        newSize = burgerCollider.size;
        newSize.z += heightIncreaseAmount/12;
        newCenter = burgerCollider.center;
        newCenter.z += heightIncreaseAmount / 24f;

        burgerCollider.size = newSize;
        burgerCollider.center = newCenter;

    }

    public void AddSauce(SauceBottle.SauceType type)
    {
        if (!allSauces.Contains(type) && !burgerIsDone)
        {
            GameObject go = Instantiate(type == SauceBottle.SauceType.Ketchup ? sauces[0] :
                        type == SauceBottle.SauceType.Mayo ? sauces[1] :
                        type == SauceBottle.SauceType.Mustard ? sauces[2] : sauces[3], sauce.transform.position, sauce.transform.rotation, transform);

            currentLocationToPutBurgerIngredient.y += 0.003f;

            go.transform.parent = ingredientsParent;

            allSauces.Add(type);
            allGO.Add(go);

            TurnOffAllHolograms();
        }
            
    }

    public void ResetTray()
    {
        foreach (GameObject go in allGO)
        {
            Destroy(go);
        }

        foreach (BurgerIngredient burgerIngredient in allBurgerIngredients)
        {
            currentBox.allBurgerIngredientTypes.Add(burgerIngredient.data.ingredientType);
        }

        foreach(SauceBottle.SauceType sauceType in allSauces)
        {
            currentBox.allSauces.Add(sauceType);
        }

        allBurgerIngredients.Clear();
        allSauces.Clear();
        allGO.Clear();

        burgerIsDone = false;
        currentBox = null;
        currentIngredient = null;

        ResetPosition();
    }

    public void TurnOnSauceHologram(SauceBottle.SauceType type)
    {
        if (allBurgerIngredients.Count != 0 && !allSauces.Contains(type) && !burgerIsDone)
        {
            hologramLocation = currentLocationToPutBurgerIngredient;
            hologramLocation.y += 0.003f;

            sauce.transform.position = hologramLocation;
            sauce.transform.rotation = Quaternion.Euler(90, Random.Range(0, 360), 0);
            sauce.SetActive(true);
        }
            
    }

    public void TurnOnHologram(BurgerIngredientData.IngredientType type)
    {
        hologramLocation = currentLocationToPutBurgerIngredient;
        hologramLocation.y += currentIngredient.data.yHeight;

        if (allBurgerIngredients.Count == 0)
        {
            if (type == BurgerIngredientData.IngredientType.BOTTOMBUN && currentIngredient.cookAmount == Cookable.CookAmount.REGULAR)
            {
                currentIngredient.canAddToTray = true;
                bottomBun.transform.position = hologramLocation;
                bottomBun.SetActive(true);
            }
        }
        else if (!burgerIsDone)
        {
            if (type == BurgerIngredientData.IngredientType.PICKLE)
            {
                currentIngredient.canAddToTray = true;
                pickle.transform.position = hologramLocation;
                pickle.SetActive(true);
            }
            else if (type == BurgerIngredientData.IngredientType.LETTUCE)
            {
                currentIngredient.canAddToTray = true;
                lettuce.transform.position = hologramLocation;
                lettuce.SetActive(true);
            }
            else if (type == BurgerIngredientData.IngredientType.ONION)
            {
                currentIngredient.canAddToTray = true;
                onion.transform.position = hologramLocation;
                onion.SetActive(true);
            }
            else if (type == BurgerIngredientData.IngredientType.TOMATO)
            {
                currentIngredient.canAddToTray = true;
                tomato.transform.position = hologramLocation;
                tomato.SetActive(true);
            }
            else if (type == BurgerIngredientData.IngredientType.CHEESE)
            {
                currentIngredient.canAddToTray = true;
                cheese.transform.position = hologramLocation;
                cheese.SetActive(true);
            }
            else if (type == BurgerIngredientData.IngredientType.PATTY && currentIngredient.cookAmount == Cookable.CookAmount.REGULAR)
            {
                currentIngredient.canAddToTray = true;
                patty.transform.position = hologramLocation;
                patty.SetActive(true);
            }
            else if (type == BurgerIngredientData.IngredientType.BOTTOMBUN && currentIngredient.cookAmount == Cookable.CookAmount.REGULAR)
            {
                currentIngredient.canAddToTray = true;
                bottomBun.transform.position = hologramLocation;
                bottomBun.SetActive(true);
            }
            else if (type == BurgerIngredientData.IngredientType.TOPBUN && currentIngredient.cookAmount == Cookable.CookAmount.REGULAR)
            {
                currentIngredient.canAddToTray = true;
                topBun.transform.position = hologramLocation;
                topBun.SetActive(true);
            }
        }
    }

    public void TurnOnBoxHologram()
    {
        if (burgerIsDone)
        {
            currentBox.canAddToTray = true;
            box.transform.position = burgerBoxTransform.position;
            box.SetActive(true);
        }
    }

    public void TurnOffAllHolograms()
    {
        onion.SetActive(false);
        lettuce.SetActive(false);
        tomato.SetActive(false);
        pickle.SetActive(false);
        patty.SetActive(false);
        cheese.SetActive(false);
        bottomBun.SetActive(false);
        topBun.SetActive(false);
        box.SetActive(false);
        sauce.SetActive(false);
    }

    private void ResetPosition()
    {
        currentLocationToPutBurgerIngredient = transform.position;

        Vector3 newSize = boxCollider.size;
        newSize.z = boxColliderStartZ;
        Vector3 newCenter = boxCollider.center;
        newCenter.z = boxColliderStartCenterZ;
        boxCollider.size = newSize;
        boxCollider.center = newCenter;

        newSize = burgerCollider.size;
        newSize.z = burgerColliderStartZ;
        newCenter = burgerCollider.center;
        newCenter.z = burgerColliderStartCenterZ;
        burgerCollider.size = newSize;
        burgerCollider.center = newCenter;

        burgerCollider.enabled = false;

        UpdateCurrentLocationToPutBurgerIngredient(startPointYHeight);
    }

    public void TrySquashingBurger()
    {
        // Eðer yükseklik min limitten küçükse squash yok
        if (currentLocationToPutBurgerIngredient.y <= boxClosingSquashMinLimit)
            return;

        // Ne kadar squash yapýlacaðýný hesapla
        float excessHeight = (currentLocationToPutBurgerIngredient.y - boxClosingSquashMinLimit) * 3f;

        // Ýstediðin oranda Z scale küçült
        float squashFactor = Mathf.Clamp01(excessHeight); // istersen çarpan ekleyebilirsin
        float targetZ = Mathf.Max(0f, 1f - squashFactor); // Z scale küçülür ama negatif olmaz

        // Tween ile squash (sadece Z ekseni)
        ingredientsParent
            .DOScale(new Vector3(ingredientsParent.localScale.x, ingredientsParent.localScale.y, targetZ), 0.16f)
            .SetEase(Ease.Linear); // lineer, geri yaylanma yok
    }

    private void Squash()
    {
        ingredientsParent
    .DOScale(new Vector3(1.1f, 1.1f, 1f - 2f * currentIngredient.data.yHeight), 0.2f) // biraz daha uzun süre
    .SetEase(Ease.OutQuad)                        // daha yumuþak çöküþ
    .OnComplete(() =>
    {
        ingredientsParent.DOScale(Vector3.one, 0.3f) // kalkýþý daha uzun yap
            .SetEase(Ease.OutElastic, 1f + 2f * currentIngredient.data.yHeight);          // lastikli, overshootlu
    });
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer != onTrayLayer && (other.CompareTag("BurgerIngredient") || other.CompareTag("BurgerBox")))
        {
            if (currentIngredient != null && other.gameObject.GetInstanceID() == currentIngredient.gameObject.GetInstanceID() && currentIngredient.canAddToTray)
            {

                if (currentIngredient.data.ingredientType == BurgerIngredientData.IngredientType.TOPBUN)
                    burgerIsDone = true;

                if (!burgerCollider.enabled)
                    burgerCollider.enabled = true;

                allBurgerIngredients.Add(currentIngredient);
                allGO.Add(currentIngredient.gameObject);

                UpdateCurrentLocationToPutBurgerIngredient(currentIngredient.data.yHeight);

                currentIngredient.PutOnTray(currentLocationToPutBurgerIngredient, ingredientsParent);

                Invoke("Squash", currentIngredient.data.timeToPutOnTray / 1.2f);

                UpdateCurrentLocationToPutBurgerIngredient(currentIngredient.data.yHeight);
            }
            else if (currentBox != null && other.gameObject.GetInstanceID() == currentBox.gameObject.GetInstanceID() && currentBox.canAddToTray)
            {
                currentBox.PutOnTray(burgerBoxTransform.position);
            }
            
        }
    }
}
