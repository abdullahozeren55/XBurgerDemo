using DG.Tweening;
using UnityEngine;

public class Fryer : MonoBehaviour
{
    [Header("Audio Settings")]
    private AudioSource fryerAudioSource; // Inspector'dan ata
    [SerializeField] private float maxVolume = 0.8f;       // Full doluyken çýkacak ses
    [SerializeField] private float fadeDuration = 1f;    // Ses deðiþim hýzý

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

    [Header("Basket Management")]
    [SerializeField] private FryerBasket[] baskets; // 0: Sol, 1: Sað (Inspector'dan ata)

    // Logic Variables
    private Material oilMat;
    private int totalFoodItemsCount = 0;   // Fiziksel doluluk (Sepet girmesi/çýkmasý için)
    private int activeFoodItemsCount = 0;  // GÖRSEL doluluk (Baloncuk ve Yað seviyesi için)
    private int emptyBasketsCount = 0; // Boþ sepet sayýsý

    private const int BASKET_CAPACITY = 3;

    private float currentWeight;       // Shader deðiþkeni
    private float initialLocalZ;       // Yaðýn baþlangýç yüksekliði

    // Tween References (Çakýþma önlemek için)
    private Tween turbulenceTween;
    private Tween levelTween;
    private int maxTotalCapacity; // Oran hesabý için (Baskets.Length * Capacity)
    private Tween audioFadeTween; // Ses lerplemesi için

    private void Awake()
    {
        fryerAudioSource = GetComponent<AudioSource>();

        if (baskets != null && baskets.Length > 0)
        {
            // Ýlk sepetin kapasitesini baz alýyoruz
            // (FryerBasket scriptine public eriþim açmak gerekebilir veya hardcode 3 kullanabiliriz)
            maxTotalCapacity = baskets.Length * BASKET_CAPACITY;
        }
        else
        {
            maxTotalCapacity = 6; // Fallback
        }

        if (fryerAudioSource != null)
        {
            fryerAudioSource.loop = true;
            fryerAudioSource.volume = 0f;
            if (!fryerAudioSource.isPlaying) fryerAudioSource.Play();
        }

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

    // Artýk hem fiziksel (total) hem görsel (active) sayýyý ayrý istiyoruz
    public void OnBasketDown(int totalItemCount, int activeItemCount, int laneIndex)
    {
        if (totalItemCount > 0)
        {
            totalFoodItemsCount += totalItemCount;
            activeFoodItemsCount += activeItemCount;
        }
        else { emptyBasketsCount++; }

        // --- DEÐÝÞÝKLÝK BURADA: SURGE (ANLIK DALGA) HESABI ---
        // Eskiden: activeItemCount / BASKET_CAPACITY (Lineer)
        // Þimdi: Sepet kapasitesinin yarýsý dolunca full coþsun.

        float halfCapacity = BASKET_CAPACITY / 2f;
        // Eðer 3 kapasiteyse 1.5 eder. Yani 2 patates atýnca fullenir.

        float surgeMultiplier = 0f;
        if (activeItemCount > 0)
        {
            // 0 ile Yarým Kapasite arasýný 0-1 arasýna oranla
            surgeMultiplier = Mathf.Clamp01((float)activeItemCount / halfCapacity);
        }
        else if (totalItemCount == 0)
        {
            surgeMultiplier = 0.5f; // Boþ sepet için sabit
        }

        AnimateTurbulence(true, surgeMultiplier);
        AnimateOilLevel(true, surgeMultiplier);
        SpawnDynamicSplash(totalItemCount, laneIndex);

        UpdateFryerAudio();
    }

    public void OnBasketUp(int totalItemCount, int activeLeavingCount, int laneIndex)
    {
        if (totalItemCount > 0)
        {
            totalFoodItemsCount -= totalItemCount;

            // Eðer yananlar varsa zaten OnItemBurnt ile düþmüþtük. 
            // Sadece kalan saðlamlarý düþüyoruz.
            activeFoodItemsCount -= activeLeavingCount;
        }
        else
        {
            emptyBasketsCount--;
        }

        // Güvenlik (Negatif olmasýn)
        if (totalFoodItemsCount < 0) totalFoodItemsCount = 0;
        if (activeFoodItemsCount < 0) activeFoodItemsCount = 0;
        if (emptyBasketsCount < 0) emptyBasketsCount = 0;

        // Çýkarken surge yok (false)
        AnimateTurbulence(false, 0f);
        AnimateOilLevel(false, 0f);

        SpawnDynamicSplash(totalItemCount, laneIndex);

        UpdateFryerAudio();
    }

    // --- YENÝ: YANMA TETÝKLEYÝCÝSÝ ---
    public void OnItemBurnt()
    {
        // Aktif sayýyý düþür (Köpürme azalsýn)
        activeFoodItemsCount--;
        if (activeFoodItemsCount < 0) activeFoodItemsCount = 0;

        // SADECE Turbulence'ý güncelle. 
        // triggerSurge = false (Dalgalanma yapma, yavaþça sön)
        AnimateTurbulence(false, 0f);

        // YENÝ: Sesi Güncelle (Biri yandý, cýzýrtý azalmalý)
        UpdateFryerAudio();
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
        // --- DEÐÝÞÝKLÝK BURADA: HEDEF AÐIRLIK (TARGET VAL) HESABI ---

        // Mantýk: Toplam kapasitenin %50'si dolunca MAX Weight'e ulaþsýn.
        float halfTotalCapacity = maxTotalCapacity / 2f;

        // Aktif sayýsýný yarým kapasiteye oranla, 1'i geçemesin (Clamp01)
        float saturationRatio = Mathf.Clamp01((float)activeFoodItemsCount / halfTotalCapacity);

        // Artýk "Basket baþýna aðýrlýk" deðil, "Max Aðýrlýk" üzerinden gidiyoruz.
        // baseWeight: Durgun yað
        // maxTurbulenceWeight: Full coþmuþ yað (Bunu hesaplayacaðýz)

        // Eskiden weightPerBasket ile çarpýyorduk, þimdi direkt hedef aralýðý belirleyelim.
        // weightPerBasket * baskets.Length bize "Eski sistemdeki Max artýþý" verir.
        float maxAddedWeight = weightPerBasket * baskets.Length;

        // Yeni hedef deðer: Base + (Oran * MaxArtýþ)
        float addedWeight = saturationRatio * maxAddedWeight;
        float targetVal = baseWeight + addedWeight;

        targetVal = Mathf.Clamp(targetVal, 0f, 1.5f);

        if (turbulenceTween != null && turbulenceTween.IsActive()) turbulenceTween.Kill();

        if (triggerSurge)
        {
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

    // 2. Yað Seviyesi Animasyonu
    private void AnimateOilLevel(bool isEntering, float surgeIntensity)
    {
        if (oilSurfaceTransform == null) return;

        float risePerItem = risePerFullBasket / BASKET_CAPACITY;
        float totalRise = (emptyBasketsCount * risePerEmptyBasket) + (totalFoodItemsCount * risePerItem);

        float targetZ = initialLocalZ + totalRise;

        if (levelTween != null && levelTween.IsActive()) levelTween.Kill();

        if (isEntering)
        {
            float dynamicSurge = levelSurgeAmount * (0.5f + (surgeIntensity * 0.5f));
            float surgeZ = targetZ + dynamicSurge;

            Sequence seq = DOTween.Sequence();
            seq.Append(oilSurfaceTransform.DOLocalMoveZ(surgeZ, surgeDuration).SetEase(Ease.OutBack));
            seq.Append(oilSurfaceTransform.DOLocalMoveZ(targetZ, settleDuration).SetEase(Ease.InOutSine));
            levelTween = seq;
        }
        else
        {
            // Çýkarken
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

            // --- DEÐÝÞÝKLÝK BURADA: BALONCUK SAYISI ---
            // Eskiden: maxExpectedWeight = baseWeight + (2 * weightPerBasket)
            // Þimdi: baseWeight + maxAddedWeight (Yani turbulence ile ayný max deðere ulaþsýn)

            float maxAddedWeight = weightPerBasket * baskets.Length;
            float maxExpectedWeight = baseWeight + maxAddedWeight;

            // currentWeight zaten %50 dolulukta bu maxExpectedWeight'e ulaþacak þekilde ayarlandý (AnimateTurbulence içinde).
            // O yüzden buradaki InverseLerp otomatik olarak erken fullenecek.

            float t = Mathf.InverseLerp(baseWeight, maxExpectedWeight, currentWeight);

            emission.rateOverTime = Mathf.Lerp(minEmission, maxEmission, t);
        }
    }

    public void HandleGlobalCatch(Collider other)
    {
        // 1. Gelen þey geçerli bir Fryable mý?
        Fryable incomingItem = other.GetComponent<Fryable>();
        if (incomingItem == null) return;

        // Zaten bir sepeti varsa veya oyuncunun elindeyse iþlem yapma
        if (incomingItem.currentBasket != null || incomingItem.IsGrabbed) return;
        // Sadece Çið olanlarý al (Basket kodundaki kontrolü buraya da taþýdýk)
        if (incomingItem.CurrentCookingState != CookAmount.RAW) return;

        FryerBasket bestBasket = null;
        float bestScore = -1f;

        // 2. Sepetleri gez ve puanla
        for (int i = 0; i < baskets.Length; i++)
        {
            FryerBasket basket = baskets[i];

            // Eðer sepet uygun deðilse (yaðdaysa veya tamsa) direkt ele
            if (!basket.CanAcceptItem) continue;

            float score = 0f;

            // KRÝTER 1: Ýçinde item var mý? (Dolu olmaya meyilli olana öncelik)
            if (basket.ItemCount > 0)
            {
                score += 1000f; // Ýçinde item olmasý en büyük puan
                score += basket.ItemCount * 10f; // Ne kadar çok item varsa o kadar iyi (Daha çabuk dolsun)
            }

            // KRÝTER 2: Ýkisi de boþsa Sað Tarafa (Index 1) öncelik ver
            // Genelde Index 0: Sol, Index 1: Sað kabul edilir.
            if (i == 1)
            {
                score += 1f; // Eþitlik bozucu küçük puan (Tie-breaker)
            }

            // En yüksek puanlýyý seç
            if (score > bestScore)
            {
                bestScore = score;
                bestBasket = basket;
            }
        }

        // 3. Kazanan varsa ona gönder
        if (bestBasket != null)
        {
            // Sepetin kendi HandleCatch'i collider istiyor, ama biz zaten checkleri yaptýk.
            // Yine de mevcut yapýný bozmamak için onun methodunu çaðýrýyoruz.
            // Not: FryerBasket.HandleCatch içindeki kontrolleri (IsGrabbed vb) zaten yukarýda yaptýk, 
            // güvenle geçebilir.
            bestBasket.HandleCatch(other);
        }
    }

    // --- SES GÜNCELLEME MANTIÐI (YENÝ) ---
    private void UpdateFryerAudio()
    {
        if (fryerAudioSource == null) return;

        // 1. Hedef Sesi Hesapla
        // Formül: (Aktif Sayý / Toplam Kapasite) * MaxVolume
        // Örn: 3 patates var, kapasite 6 -> %50 * MaxVolume
        float ratio = Mathf.Clamp01((float)activeFoodItemsCount / maxTotalCapacity);
        float targetVolume = ratio * maxVolume;

        // Eðer hiç aktif yoksa (0) ses tamamen kýsýlsýn.

        // 2. Lerpleme (Tween Kill Mantýðýyla)
        if (audioFadeTween != null && audioFadeTween.IsActive()) audioFadeTween.Kill();

        float currentVol = fryerAudioSource.volume;

        audioFadeTween = DOVirtual.Float(currentVol, targetVolume, fadeDuration, (v) =>
        {
            fryerAudioSource.volume = v;
        }).SetEase(Ease.OutQuad);
    }
}