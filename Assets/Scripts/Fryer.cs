using DG.Tweening;
using UnityEngine;

public class Fryer : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private Transform oilSurfaceTransform; // Hareket edecek Yað Objesi (Plane)
    [SerializeField] private MeshRenderer oilMeshRenderer;
    [SerializeField] private ParticleSystem bubbleParticles;

    [Header("Turbulence Settings (Köpürme)")]
    [SerializeField] private float baseWeight = 0.2f;
    [SerializeField] private float weightPerBasket = 0.4f;
    [SerializeField] private float surgeAmount = 0.3f;
    [SerializeField] private float surgeDuration = 0.3f;
    [SerializeField] private float settleDuration = 0.5f;
    [SerializeField] private float minEmission = 5f;
    [SerializeField] private float maxEmission = 30f;

    [Header("Splash Juice Settings")]
    [SerializeField] private GameObject splashPrefab; // Instantiate edilecek prefab
    [SerializeField] private Transform[] splashSpawnPoints; // 0: Sol, 1: Sað (Yaðýn child'ý olsunlar)

    [Space]
    [SerializeField] private float minSplashEmission = 10f; // Boþ sepet için
    [SerializeField] private float maxSplashEmission = 50f; // Full sepet için
    [Space]
    [SerializeField] private float minSplashSpeed = 2f;
    [SerializeField] private float maxSplashSpeed = 6f;

    [Header("Physics Settings (Seviye Yükselmesi)")]
    [SerializeField] private float risePerEmptyBasket = 0.0002f; // Boþ sepet ne kadar yükseltir?
    [SerializeField] private float risePerFullBasket = 0.0006f;  // Dolu sepet ne kadar yükseltir? (2 tanesi 0.0012 eder)
    [SerializeField] private float levelSurgeAmount = 0.0003f;   // Dalarken oluþan dalga yüksekliði (Taþma efekti)

    // Logic Variables
    private Material oilMat;
    private int totalFoodItemsCount = 0;
    private int emptyBasketsCount = 0; // Boþ sepet sayýsý

    private const int BASKET_CAPACITY = 3;

    private float currentWeight;       // Shader deðiþkeni
    private float initialLocalZ;       // Yaðýn baþlangýç yüksekliði

    // Tween References (Çakýþma önlemek için)
    private Tween turbulenceTween;
    private Tween levelTween;

    private void Awake()
    {
        if (oilMeshRenderer != null)
        {
            oilMat = oilMeshRenderer.material;
        }

        // Baþlangýç yüksekliðini kaydet (Referans noktasý)
        if (oilSurfaceTransform != null)
            initialLocalZ = oilSurfaceTransform.localPosition.z;
        else
            Debug.LogError("Fryer: Oil Surface Transform atanmadý! Yað yükselmeyecek.");

        currentWeight = baseWeight;
        UpdateShaderAndParticles();
    }

    private void Update()
    {
        UpdateShaderAndParticles();
    }

    // --- BASKET TARAFINDAN ÇAÐRILANLAR ---

    // Artýk "laneIndex" parametresi alýyorlar (Hangi sepet?)

    public void OnBasketDown(int itemCount, int laneIndex)
    {
        if (itemCount > 0) totalFoodItemsCount += itemCount;
        else emptyBasketsCount++;

        float surgeMultiplier = itemCount > 0 ? (float)itemCount / BASKET_CAPACITY : 0.5f;

        AnimateTurbulence(true, surgeMultiplier);
        AnimateOilLevel(true, surgeMultiplier);

        // JUICE: Partikülü Çak!
        SpawnDynamicSplash(itemCount, laneIndex);
    }

    public void OnBasketUp(int itemCount, int laneIndex)
    {
        if (itemCount > 0) totalFoodItemsCount -= itemCount;
        else emptyBasketsCount--;

        if (totalFoodItemsCount < 0) totalFoodItemsCount = 0;
        if (emptyBasketsCount < 0) emptyBasketsCount = 0;

        AnimateTurbulence(false, 0f);
        AnimateOilLevel(false, 0f);

        // JUICE: Çýkarken de damlasýn, ama belki biraz daha az þiddetli (tercihen)
        SpawnDynamicSplash(itemCount, laneIndex);
    }

    // --- YENÝ JUICE FONKSÝYONU ---
    private void SpawnDynamicSplash(int itemCount, int laneIndex)
    {
        if (splashPrefab == null) return;
        if (splashSpawnPoints == null || splashSpawnPoints.Length <= laneIndex) return;

        // 1. Doðru pozisyonda yarat
        Transform targetPoint = splashSpawnPoints[laneIndex];
        GameObject newSplash = Instantiate(splashPrefab, targetPoint.position, Quaternion.Euler(-90f, 0f, 0f)); // Rotation identity veya prefab'ýnki kalabilir

        // 2. Oraný Hesapla (0 ile 1 arasý)
        // Sepet boþsa (0 item) yine de bir aðýrlýðý var, o yüzden min 0 deðil.
        // Amaç: Boþ sepette Min deðerler, Dolu sepette Max deðerler.
        float ratio = Mathf.Clamp01((float)itemCount / BASKET_CAPACITY);

        // 3. Deðerleri Lerple
        float targetEmission = Mathf.Lerp(minSplashEmission, maxSplashEmission, ratio);
        float targetSpeed = Mathf.Lerp(minSplashSpeed, maxSplashSpeed, ratio);

        // 4. Particle System'e Müdahale Et
        ParticleSystem ps = newSplash.GetComponent<ParticleSystem>();
        if (ps != null)
        {
            // Hýz Ayarý (Start Speed)
            var main = ps.main;
            // Constant yerine RandomBetweenTwoConstants kullanýyoruz ki doðal dursun.
            // Alt limit ile üst limit arasýnda hafif fark olsun.
            main.startSpeed = new ParticleSystem.MinMaxCurve(targetSpeed * 0.8f, targetSpeed * 1.2f);

            // Sayý Ayarý (Emission Burst)
            var emission = ps.emission;
            // Eðer Burst kullanýyorsan (ki splash için burst mantýklý):
            if (emission.burstCount > 0)
            {
                ParticleSystem.Burst burst = emission.GetBurst(0);
                burst.count = new ParticleSystem.MinMaxCurve(targetEmission);
                emission.SetBurst(0, burst);
            }
            else
            {
                // Burst yoksa RateOverTime ile oynayalým (ama tavsiyem Burst)
                emission.rateOverTime = targetEmission;
            }
        }

        // NOT: Prefab'ýn üzerinde "Stop Action -> Destroy" ayarlýysa buna gerek yok.
        // Ama garanti olsun, pis kod severiz:
        Destroy(newSplash, 2f);
    }

    // --- ANÝMASYON MANTIÐI ---

    // 1. KÖPÜRME / TURBULENCE (Eski Mantýk)
    private void AnimateTurbulence(bool triggerSurge, float surgeIntensity)
    {
        // HESAP: (Toplam Malzeme / 3) * TamSepetAðýrlýðý
        // Yani 1 malzeme varsa 1/3 etki, 3 malzeme varsa tam etki.
        float addedWeight = ((float)totalFoodItemsCount / BASKET_CAPACITY) * weightPerBasket;
        float targetVal = baseWeight + addedWeight;

        targetVal = Mathf.Clamp(targetVal, 0f, 1.5f);

        if (turbulenceTween != null && turbulenceTween.IsActive()) turbulenceTween.Kill();

        if (triggerSurge)
        {
            // Surge miktarý da giren malzeme sayýsýna göre artsýn
            float dynamicSurge = surgeAmount * surgeIntensity;
            float surgeTarget = targetVal + dynamicSurge;

            Sequence seq = DOTween.Sequence();
            seq.Append(DOTween.To(() => currentWeight, x => currentWeight = x, surgeTarget, surgeDuration).SetEase(Ease.OutCirc));
            seq.Append(DOTween.To(() => currentWeight, x => currentWeight = x, targetVal, settleDuration).SetEase(Ease.OutQuad));
            turbulenceTween = seq;
        }
        else
        {
            turbulenceTween = DOTween.To(() => currentWeight, x => currentWeight = x, targetVal, settleDuration).SetEase(Ease.OutQuad);
        }
    }

    private void AnimateOilLevel(bool isEntering, float surgeIntensity)
    {
        if (oilSurfaceTransform == null) return;

        // HESAP: Her malzeme için "Tam Sepet Yükselmesi / 3" kadar yüksel
        float risePerItem = risePerFullBasket / BASKET_CAPACITY;

        float totalRise = (emptyBasketsCount * risePerEmptyBasket) + (totalFoodItemsCount * risePerItem);
        float targetZ = initialLocalZ + totalRise;

        if (levelTween != null && levelTween.IsActive()) levelTween.Kill();

        if (isEntering)
        {
            // Dalma efekti de malzeme sayýsýna göre þiddetlensin
            float dynamicSurge = levelSurgeAmount * (0.5f + (surgeIntensity * 0.5f)); // Min %50 surge olsun
            float surgeZ = targetZ + dynamicSurge;

            Sequence seq = DOTween.Sequence();
            seq.Append(oilSurfaceTransform.DOLocalMoveZ(surgeZ, surgeDuration).SetEase(Ease.OutBack));
            seq.Append(oilSurfaceTransform.DOLocalMoveZ(targetZ, settleDuration).SetEase(Ease.InOutSine));
            levelTween = seq;
        }
        else
        {
            levelTween = oilSurfaceTransform.DOLocalMoveZ(targetZ, settleDuration * 1.2f).SetEase(Ease.OutQuad);
        }
    }

    private void UpdateShaderAndParticles()
    {
        if (oilMat == null) return;

        oilMat.SetFloat("_EffectWeight", currentWeight);

        if (bubbleParticles != null)
        {
            var emission = bubbleParticles.emission;

            // Mevcut aðýrlýða göre partikül sayýsýný oranla
            // baseWeight -> minEmission
            // baseWeight + (2 * weightPerBasket) -> maxEmission (2 sepet dolusu malzeme varsayýmýyla maxladým)

            float maxExpectedWeight = baseWeight + (2 * weightPerBasket);
            float t = Mathf.InverseLerp(baseWeight, maxExpectedWeight, currentWeight);

            emission.rateOverTime = Mathf.Lerp(minEmission, maxEmission, t);
        }
    }
}