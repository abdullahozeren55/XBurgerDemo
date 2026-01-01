using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BurgerCombineArea : MonoBehaviour
{
    [SerializeField] private float startPointYHeight = 0.01f;
    [SerializeField] private Transform ingredientsParent;
    [SerializeField] private BoxCollider boxCollider;
    [SerializeField] private BoxCollider sauceCollider;

    [Header("Whole Burger Settings")]
    [SerializeField] private WholeBurgerData wholeBurgerData;

    // --- YENÝ: Apex Noktasý ---
    [Header("Movement Settings")]
    [SerializeField] private Transform entryApex; // Malzemelerin ineceði tepe nokta
    [SerializeField] private float topBunSnapDistance = 0.15f; // Üst ekmek bu kadar yaklaþmazsa yapýþmaz
    private float startApexLocalZ; // Apex'in baþlangýç yüksekliðini saklamak için

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

    [HideInInspector] public bool isBoxingProcessStarted = false;

    private int onTrayLayer;

    private void Awake()
    {

        boxColliderStartZ = boxCollider.size.z;
        boxColliderStartCenterZ = boxCollider.center.z;

        sauceColliderStartZ = sauceCollider.size.z;
        sauceColliderStartCenterZ = sauceCollider.center.z;

        // Apex'in baþlangýç Y deðerini kaydet
        if (entryApex != null) startApexLocalZ = entryApex.localPosition.z;

        burgerIsDone = false;

        ResetPosition();

        onTrayLayer = LayerMask.NameToLayer("OnTray");

        GameManager.Instance.burgerCombineArea = this;
    }

    private void UpdateCurrentLocationToPutBurgerIngredient(float heightIncreaseAmount)
    {
        currentLocationToPutBurgerIngredient.y += heightIncreaseAmount;

        // --- YENÝ: Apex'i de yukarý taþý ---
        // Böylece burger büyüdükçe "giriþ kapýsý" da yükselir.
        if (entryApex != null)
        {
            Vector3 newApexPos = entryApex.localPosition;
            newApexPos.z += heightIncreaseAmount/12f;
            entryApex.localPosition = newApexPos;
        }
        // ----------------------------------

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
            UpdateCurrentLocationToPutBurgerIngredient(0.0016f); // Sos için sabit deðer
            // 1. Rotasyonu ve Pozisyonu BURADA hesaplýyoruz
            Quaternion targetRotation = Quaternion.Euler(90f, Random.Range(0f, 360f), 0f);
            Vector3 targetPosition = currentLocationToPutBurgerIngredient;

            // 2. Instantiate iþlemini doðru transform deðerleriyle yapýyoruz
            // Parent olarak direkt ingredientsParent veriyoruz.
            GameObject go = Instantiate(
                type == SauceBottle.SauceType.Ketchup ? sauces[0] :
                type == SauceBottle.SauceType.Mayo ? sauces[1] :
                type == SauceBottle.SauceType.Mustard ? sauces[2] :
                type == SauceBottle.SauceType.BBQ ? sauces[3] : sauces[4],
                targetPosition,
                targetRotation,
                ingredientsParent);

            // 3. Yüksekliði sosun kalýnlýðý kadar arttýr
            UpdateCurrentLocationToPutBurgerIngredient(0.0016f); // Sos için sabit deðer

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
        UpdateCurrentLocationToPutBurgerIngredient(-2 * 0.0016f);
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

        allBurgerIngredients.Clear();
        allSauces.Clear();
        allGO.Clear();

        burgerIsDone = false;

        ResetPosition();
    }

    private void ResetPosition()
    {
        currentLocationToPutBurgerIngredient = transform.position;

        // --- YENÝ: Apex'i baþlangýca döndür ---
        if (entryApex != null)
        {
            Vector3 resetApexPos = entryApex.localPosition;
            resetApexPos.z = startApexLocalZ;
            entryApex.localPosition = resetApexPos;
        }
        // --------------------------------------

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
                if (burgerIsDone)
                {
                    ingredientsParent.DOScale(new Vector3 (11f, 11f, 11f), 0.3f)
                    .SetEase(Ease.OutElastic);

                    FinalizeBurger();
                }
                else
                {
                    ingredientsParent.DOScale(Vector3.one, 0.3f)
                        .SetEase(Ease.OutElastic);
                }
                    
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

                // --- YENÝ: Apex Noktasýný Gönderiyoruz ---
                // Eðer Apex atanmamýþsa kendi üstünü kullanýr (güvenlik)
                Vector3 apexPos = (entryApex != null) ? entryApex.position : currentLocationToPutBurgerIngredient + Vector3.up * 0.2f;

                ingredient.PutOnTray(currentLocationToPutBurgerIngredient, targetRotation, ingredientsParent, apexPos);
                // -----------------------------------------

                Invoke("Squash", ingredient.data.timeToPutOnTray / 1.2f);
                UpdateCurrentLocationToPutBurgerIngredient(ingredient.data.yHeight);

                // RefreshHologram SÝLÝNDÝ
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


        BurgerIngredientData.IngredientType type = ingredient.data.ingredientType;

        // --- YENÝ: Top Bun için Apex Mesafe Kontrolü (HASSAS BIRAKMA) ---
        if (type == BurgerIngredientData.IngredientType.TOPBUN && entryApex != null)
        {
            float dist = Vector3.Distance(ingredient.transform.position, entryApex.position);
            if (dist > topBunSnapDistance)
            {
                return false; // Uzaktan fýrlatýldý, kabul etme!
            }
        }
        // ----------------------------------------------------------------

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

    private void FinalizeBurger()
    {
        // 1. Mevcut Parent'ý Özgür Býrak (Detach)
        Transform finishedBurger = ingredientsParent;
        ingredientsParent = null; // Baðlantýyý kopar
        finishedBurger.SetParent(null); // Dünyaya sal
        finishedBurger.localScale = new Vector3(11f, 11f, 11f);
        // -------------------------

        // 2. Yeni Tepsi Parent'ýný Oluþtur (Reset için)
        GameObject newParent = new GameObject("IngredientsParent");
        newParent.transform.SetParent(this.transform);
        newParent.transform.localPosition = Vector3.zero;
        newParent.transform.localRotation = Quaternion.identity;
        newParent.transform.localScale = Vector3.one;
        ingredientsParent = newParent.transform;

        // 3. ÖNCE FÝZÝK EKLE (RB)
        Rigidbody rb = finishedBurger.gameObject.AddComponent<Rigidbody>();
        rb.mass = 0.2f;
        rb.interpolation = RigidbodyInterpolation.None;
        rb.collisionDetectionMode = CollisionDetectionMode.Continuous;

        // 4. SONRA WHOLEBURGER EKLE
        WholeBurger wb = finishedBurger.gameObject.AddComponent<WholeBurger>();

        // 5. Çocuklarý Dönüþtür (ChildBurger)
        List<GameObject> childrenList = new List<GameObject>();

        // --- TARÝF KONTROLÜ ÝÇÝN LÝSTELER ---
        List<BurgerIngredientData.IngredientType> currentIngredients = new List<BurgerIngredientData.IngredientType>();
        List<SauceBottle.SauceType> currentSauces = new List<SauceBottle.SauceType>();

        foreach (GameObject go in allGO)
        {
            if (go == null) continue;

            // Eski scriptleri temizle
            BurgerIngredient oldScript = go.GetComponent<BurgerIngredient>();

            // --- DÜZELTME BURADA ---
            if (oldScript != null)
            {
                // ÖNCE VERÝYÝ ÇEKÝP LÝSTEYE ATIYORUZ
                if (oldScript.data.isSauce)
                {
                    currentSauces.Add(oldScript.data.sauceType);
                }
                else
                {
                    currentIngredients.Add(oldScript.data.ingredientType);
                }

                // SONRA YOK EDÝYORUZ
                Destroy(oldScript);
            }
            // -----------------------

            Cookable cook = go.GetComponent<Cookable>();
            if (cook != null) Destroy(cook);

            Rigidbody childRb = go.GetComponent<Rigidbody>();
            if (childRb != null) Destroy(childRb);

            // Collider'larý ellemiyoruz, onlar raycast için lazým.
            go.tag = "Untagged";

            // Yeni scripti ekle
            ChildBurger cb = go.AddComponent<ChildBurger>();
            cb.parentBurger = wb; // Patronu ata

            childrenList.Add(go);
        }

        // --- TARÝF HESAPLAMA ---
        // GameManager'a sor: Bu malzemelerle hangi burger olur?
        int burgerIndex = GameManager.Instance.GetBurgerTypeIndex(currentIngredients, currentSauces);

        float totalHeight = currentLocationToPutBurgerIngredient.y - startPointYHeight;

        // 6. INITIALIZE (Data ve ID ile)
        wb.Initialize(childrenList, rb, wholeBurgerData, burgerIndex, totalHeight);

        // 8. Juice (Zýplatma)
        rb.AddForce(Vector3.up * 0.5f, ForceMode.Impulse);
        rb.AddTorque(Random.insideUnitSphere * 0.5f, ForceMode.Impulse);

        // 9. Tepsiyi Temizle (Objeleri silmeden)
        allBurgerIngredients.Clear();
        allSauces.Clear();
        allGO.Clear();

        burgerIsDone = false;

        ResetPosition();
    }
}
