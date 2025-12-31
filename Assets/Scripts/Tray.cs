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
    [SerializeField] private BoxCollider sauceCollider;

    [Header("Sauces")]
    [SerializeField] private GameObject[] sauces; //0 ketchup, 1 mayo, 2 mustard, 3 bbq
    public AudioClip sauceOnTraySound;
    public float sauceOnTrayVolume = 1f;
    public float sauceOnTrayMinPitch = 0.8f;
    public float sauceOnTrayMaxPitch = 1.2f;

    private Vector3 currentLocationToPutBurgerIngredient;

    [HideInInspector] public List<BurgerIngredient> allBurgerIngredients = new List<BurgerIngredient>();
    private List<SauceBottle.SauceType> allSauces = new List<SauceBottle.SauceType>();
    private List<GameObject> allGO = new List<GameObject>();

    private float boxColliderStartZ;
    private float boxColliderStartCenterZ;

    private float sauceColliderStartZ;
    private float sauceColliderStartCenterZ;

    private bool burgerIsDone;

    [HideInInspector] public BurgerBox currentBox;
    [HideInInspector] public bool isBoxingProcessStarted = false;

    private Vector3 initialSquashScale;
    private Vector3 targetSquashScale;

    private int onTrayLayer;
    private int grabableLayer;

    private void Awake()
    {
        currentBox = null;

        boxColliderStartZ = boxCollider.size.z;
        boxColliderStartCenterZ = boxCollider.center.z;

        sauceColliderStartZ = sauceCollider.size.z;
        sauceColliderStartCenterZ = sauceCollider.center.z;

        burgerIsDone = false;

        ResetPosition();

        onTrayLayer = LayerMask.NameToLayer("OnTray");
        grabableLayer = LayerMask.NameToLayer("Grabable");

        GameManager.Instance.tray = this;
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


        newSize = sauceCollider.size;
        newSize.z += heightIncreaseAmount / 12;
        newCenter = sauceCollider.center;
        newCenter.z += heightIncreaseAmount / 24f;

        sauceCollider.size = newSize;
        sauceCollider.center = newCenter;
    }

    public void AddSauce(SauceBottle.SauceType type)
    {
        if (!allSauces.Contains(type) && !burgerIsDone)
        {
            UpdateCurrentLocationToPutBurgerIngredient(0.003f); // Sos için sabit deðer
            // 1. Rotasyonu ve Pozisyonu BURADA hesaplýyoruz
            Quaternion targetRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            Vector3 targetPosition = currentLocationToPutBurgerIngredient;

            // 2. Instantiate iþlemini doðru transform deðerleriyle yapýyoruz
            // Parent olarak direkt ingredientsParent veriyoruz.
            GameObject go = Instantiate(
                type == SauceBottle.SauceType.Ketchup ? sauces[0] :
                type == SauceBottle.SauceType.Mayo ? sauces[1] :
                type == SauceBottle.SauceType.Mustard ? sauces[2] : sauces[3],
                targetPosition,
                targetRotation,
                ingredientsParent);

            // 3. Yüksekliði sosun kalýnlýðý kadar arttýr
            UpdateCurrentLocationToPutBurgerIngredient(0.003f); // Sos için sabit deðer

            go.transform.parent = ingredientsParent;

            if (allBurgerIngredients.Count > 0)
                allBurgerIngredients[allBurgerIngredients.Count - 1].SetOnTrayLayer();

            allBurgerIngredients.Add(go.GetComponent<BurgerIngredient>());
            allSauces.Add(type);
            allGO.Add(go);

            SoundManager.Instance.PlaySoundFX(sauceOnTraySound, go.transform, sauceOnTrayVolume, sauceOnTrayMinPitch, sauceOnTrayMaxPitch);
        }
    }

    public void RemoveSauce()
    {
        UpdateCurrentLocationToPutBurgerIngredient(-2 * 0.003f);
        allBurgerIngredients.RemoveAt(allBurgerIngredients.Count - 1);
        allSauces.RemoveAt(allSauces.Count - 1);
        allGO.RemoveAt(allGO.Count - 1);

        if (allBurgerIngredients.Count > 0)
            allBurgerIngredients[allBurgerIngredients.Count - 1].SetOnGrabableLayer();
    }

    public void ResetTray()
    {
        isBoxingProcessStarted = false; // YENÝ: Resetlenince false olsun

        foreach (GameObject go in allGO)
        {
            Destroy(go);
        }

        foreach (BurgerIngredient burgerIngredient in allBurgerIngredients)
        {
            if (!burgerIngredient.data.isSauce)
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

        ResetPosition();
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


        newSize = sauceCollider.size;
        newSize.z += sauceColliderStartZ;
        newCenter = sauceCollider.center;
        newCenter.z += sauceColliderStartCenterZ;

        sauceCollider.size = newSize;
        sauceCollider.center = newCenter;

        sauceCollider.enabled = false;

        UpdateCurrentLocationToPutBurgerIngredient(startPointYHeight);
    }

    public void PrepareForSquash()
    {
        // Baþlangýç scale'ini kaydet (Genelde 1,1,1)
        initialSquashScale = ingredientsParent.localScale;

        // --- HEDEF SCALE HESAPLAMA ---
        // Eðer yükseklik limitin altýndaysa ezilme olmasýn (Target = Initial)
        if (currentLocationToPutBurgerIngredient.y <= boxClosingSquashMinLimit)
        {
            targetSquashScale = initialSquashScale;
            return;
        }

        // Ne kadar ezileceðini hesapla
        float excessHeight = (currentLocationToPutBurgerIngredient.y - boxClosingSquashMinLimit) * 4.5f;
        float squashFactor = Mathf.Clamp01(excessHeight);

        // Sadece Z ekseninde hedefi belirle (Min 0.6f'ye kadar inebilir)
        float targetZ = Mathf.Max(0.6f, 1f - squashFactor);

        // X ve Y sabit kalacak (Yanlardan taþma ÝPTAL), sadece Z hedefi deðiþiyor
        targetSquashScale = new Vector3(initialSquashScale.x, initialSquashScale.y, targetZ);
    }

    // Bu fonksiyonu BurgerBox her karede (Update) çaðýracak
    // t: 0 (Açýk) ile 1 (Kapalý) arasý deðer (Ease.OutBack ile 1'i geçebilir)
    public void UpdateSquash(float t)
    {
        // Unclamped kullanýyoruz ki OutBack overshoot yaptýðýnda (t > 1 olduðunda)
        // Burger hedeften daha fazla ezilsin, sonra geri gelsin.
        ingredientsParent.localScale = Vector3.LerpUnclamped(initialSquashScale, targetSquashScale, t);
    }

    public void RemoveIngredient()
    {
        UpdateCurrentLocationToPutBurgerIngredient(-2 * allBurgerIngredients[allBurgerIngredients.Count - 1].data.yHeight);
        allBurgerIngredients.RemoveAt(allBurgerIngredients.Count - 1);
        allGO.RemoveAt(allGO.Count - 1);

        if (allBurgerIngredients.Count > 0)
            allBurgerIngredients[allBurgerIngredients.Count - 1].SetOnGrabableLayer();
        else
            sauceCollider.enabled = false;

            burgerIsDone = false;
    }

    private void Squash()
    {
        // currentIngredient baðýmlýlýðýný tamamen kaldýrdýk.
        // Artýk sabit bir "ezilme" deðeri kullanýyoruz. 
        // X ve Y hafif geniþlerken (1.1), Z hafifçe basýlýyor (0.9).

        ingredientsParent
            .DOScale(new Vector3(1.1f, 1.1f, 0.9f), 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // Geri eski haline (1,1,1) dönerken standart elastiklik kullanýyoruz.
                ingredientsParent.DOScale(Vector3.one, 0.3f)
                    .SetEase(Ease.OutElastic);
            });
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == onTrayLayer) return;

        if (other.CompareTag("BurgerIngredient"))
        {
            BurgerIngredient ingredient = other.GetComponent<BurgerIngredient>();
            if (ingredient == null) return;
            if (allBurgerIngredients.Contains(ingredient)) return;

            bool isCompatible = CheckIfIngredientIsCompatible(ingredient);

            if (isCompatible)
            {
                if (ingredient.data.ingredientType == BurgerIngredientData.IngredientType.TOPBUN)
                    burgerIsDone = true;

                if (!sauceCollider.enabled)
                    sauceCollider.enabled = true;

                if (allBurgerIngredients.Count > 0)
                    allBurgerIngredients[allBurgerIngredients.Count - 1].SetOnTrayLayer();

                ingredientsParent.DOKill();
                ingredientsParent.localScale = Vector3.one;

                if (ingredient.data.isSauce)
                    allSauces.Add(ingredient.data.sauceType);

                allBurgerIngredients.Add(ingredient);
                allGO.Add(ingredient.gameObject);

                UpdateCurrentLocationToPutBurgerIngredient(ingredient.data.yHeight);

                // --- ROTASYON HESABI BURAYA TAÞINDI ---
                Quaternion targetRotation;

                if (ingredient.data.isSauce)
                {
                    // Soslar: X=90 sabit, Y=Random
                    targetRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
                }
                else
                {
                    // Malzemeler: X=0 sabit, Y=Random
                    targetRotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
                }
                // --------------------------------------

                // Artýk storedRotation deðil, taze hesapladýðýmýz targetRotation'ý veriyoruz.
                ingredient.PutOnTray(currentLocationToPutBurgerIngredient, targetRotation, ingredientsParent);

                Invoke("Squash", ingredient.data.timeToPutOnTray / 1.2f);
                UpdateCurrentLocationToPutBurgerIngredient(ingredient.data.yHeight);

                // RefreshHologram SÝLÝNDÝ
            }
        }
        else if (other.CompareTag("BurgerBox"))
        {
            BurgerBox boxComponent = other.GetComponent<BurgerBox>();
            if (boxComponent != null && boxComponent == currentBox && boxComponent.canAddToTray)
            {
                if (!burgerIsDone) return;
                if (allBurgerIngredients.Count == 0) return;

                isBoxingProcessStarted = true;

                if (allBurgerIngredients.Count > 0)
                {
                    BurgerIngredient lastIngredient = allBurgerIngredients[allBurgerIngredients.Count - 1];
                    lastIngredient.SetOnTrayLayer();

                    // TurnOffAllHolograms ve OnLoseFocus SÝLÝNDÝ/GEREKSÝZ
                }

                currentBox.PutOnTray(burgerBoxTransform.position);
            }
        }
    }

    // Bu fonksiyon, hologramýn yaptýðý iþi manuel yapar.
    // Tepsiye çarpan malzeme, þu anki sýraya uygun mu?
    private bool CheckIfIngredientIsCompatible(BurgerIngredient ingredient)
    {
        // 1. Eðer burger hiç baþlamadýysa (Boþ tepsi)
        if (allBurgerIngredients.Count == 0)
        {
            // Sadece BOTTOM BUN (Alt Ekmek) kabul et ve piþmiþ olmalý
            return ingredient.data.ingredientType == BurgerIngredientData.IngredientType.BOTTOMBUN &&
                   ingredient.cookAmount == Cookable.CookAmount.REGULAR;
        }

        // 2. Burger bitmiþse (burgerIsDone) -> Hiçbir þey kabul etme
        if (burgerIsDone) return false;

        // 3. Sýradaki malzemeler
        // Buradaki kurallar TurnOnHologram fonksiyonundaki kurallarla AYNI olmalý.
        BurgerIngredientData.IngredientType type = ingredient.data.ingredientType;

        switch (type)
        {
            case BurgerIngredientData.IngredientType.PICKLE:
            case BurgerIngredientData.IngredientType.LETTUCE:
            case BurgerIngredientData.IngredientType.ONION:
            case BurgerIngredientData.IngredientType.TOMATO:
            case BurgerIngredientData.IngredientType.CHEESE:
                return true; // Bunlar her zaman eklenebilir

            case BurgerIngredientData.IngredientType.PATTY:
                // Köfte sadece piþmiþse
                return ingredient.cookAmount == Cookable.CookAmount.REGULAR;

            case BurgerIngredientData.IngredientType.BOTTOMBUN:
                // Ýkinci bir alt ekmek? (Big Mac tarzý). Þimdilik izin verelim ama piþmiþ olmalý.
                return ingredient.cookAmount == Cookable.CookAmount.REGULAR;

            case BurgerIngredientData.IngredientType.TOPBUN:
                // Üst ekmek sadece piþmiþse ve burgeri bitirir.
                return ingredient.cookAmount == Cookable.CookAmount.REGULAR;

            case BurgerIngredientData.IngredientType.CRISPYCHICKEN:
                return ingredient.cookAmount == Cookable.CookAmount.REGULAR;

            default:
                return false;
        }
    }
}
