using DG.Tweening;
using UnityEngine;

public class Fryer : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private MeshRenderer oilMeshRenderer;
    [SerializeField] private ParticleSystem bubbleParticles; // Dün yaptýðýmýz baloncuk sistemi
    [SerializeField] private ParticleSystem splashParticles; // "Splash" için (Opsiyonel, varsa baðla)

    [Header("Settings")]
    [SerializeField] private float baseWeight = 0.2f;       // Sakin hal
    [SerializeField] private float weightPerBasket = 0.4f;  // Sepet baþý artýþ
    [SerializeField] private float surgeAmount = 0.3f;      // Ýlk daldýðýnda ne kadar fazladan fýrlasýn? (Juice)
    [SerializeField] private float surgeDuration = 0.3f;    // Fýrlama süresi
    [SerializeField] private float settleDuration = 0.5f;   // Geri durulma süresi

    [Header("Bubble Settings")]
    [SerializeField] private float minEmission = 5f;        // Sakin baloncuk sayýsý
    [SerializeField] private float maxEmission = 30f;       // Full coþmuþ baloncuk sayýsý

    // Logic
    private Material oilMat;
    private int activeFryingCount = 0; // Þu an içinde yemek olan kaç sepet var?
    private float currentWeight;       // Shader'a giden anlýk deðer
    private Tween fryerTween;          // Çakýþmalarý yönetmek için tween referansý

    private void Awake()
    {
        if (oilMeshRenderer != null)
        {
            // Material instance oluþtur ki diðer yaðlar etkilenmesin
            oilMat = oilMeshRenderer.material;
        }

        currentWeight = baseWeight;
        UpdateShaderAndParticles();
    }

    private void Update()
    {
        // Update içinde her kare set etmek en temizidir,
        // tween ile direkt material'e dalmaktan daha performanslý ve güvenlidir.
        UpdateShaderAndParticles();
    }

    // Basket scriptinden çaðrýlacak: Sepet yaða GÝRDÝ
    public void OnBasketDown(bool hasFood)
    {
        // Her türlü splash yap (Boþ sepet de fiziksel olarak çarpar)
        if (splashParticles != null) splashParticles.Play();

        // Ama kaynama efektini sadece yemek varsa deðiþtir
        if (hasFood)
        {
            activeFryingCount++;
            AnimateOil(true); // true = Surge (Coþku) yap
        }
    }

    // Basket scriptinden çaðrýlacak: Sepet yaðdan ÇIKTI
    public void OnBasketUp(bool hasFood)
    {
        if (hasFood)
        {
            activeFryingCount--;
            // Güvenlik: Eksiye düþmesin
            if (activeFryingCount < 0) activeFryingCount = 0;

            AnimateOil(false); // false = Sakince düþ
        }
    }

    private void AnimateOil(bool triggerSurge)
    {
        // 1. Yeni Hedef Nedir?
        // Örn: 1 sepet varsa: 0.2 + (1 * 0.4) = 0.6
        float targetVal = baseWeight + (activeFryingCount * weightPerBasket);

        // Limit koyalým (Shader bozulmasýn diye max 1.5 falan olsun, ne olur ne olmaz)
        targetVal = Mathf.Clamp(targetVal, 0f, 1.5f);

        // Varsa eski tween'i öldür (Overlap yönetimi)
        if (fryerTween != null && fryerTween.IsActive()) fryerTween.Kill();

        if (triggerSurge)
        {
            // --- JUICE MOMENT ---
            // Önce hedefin de üstüne çýk (Surge), sonra hedefe dön.
            float surgeTarget = targetVal + surgeAmount;

            Sequence seq = DOTween.Sequence();
            // Hýzlýca fýrla (OutCirc: Patlayýcý bir çýkýþ)
            seq.Append(DOTween.To(() => currentWeight, x => currentWeight = x, surgeTarget, surgeDuration)
                              .SetEase(Ease.OutCirc));
            // Sakince hedefe otur
            seq.Append(DOTween.To(() => currentWeight, x => currentWeight = x, targetVal, settleDuration)
                              .SetEase(Ease.OutQuad));

            fryerTween = seq;
        }
        else
        {
            // --- SAKÝN DÖNÜÞ ---
            // Sepet çýktý, yavaþça seviyeyi düþür.
            fryerTween = DOTween.To(() => currentWeight, x => currentWeight = x, targetVal, settleDuration)
                                .SetEase(Ease.OutQuad);
        }
    }

    private void UpdateShaderAndParticles()
    {
        if (oilMat == null) return;

        // 1. Shader Update
        // "_EffectWeight" senin ShaderGraph'ta verdiðin Reference Name olmalý! Kontrol et.
        oilMat.SetFloat("_EffectWeight", currentWeight);

        // 2. Particle Emission Update
        // currentWeight 0.2 ile 1.2 arasýnda geziyor. Bunu Emission deðerine mapleyelim.
        if (bubbleParticles != null)
        {
            var emission = bubbleParticles.emission;

            // Remap mantýðý: Weight arttýkça emission artar
            // Mathf.InverseLerp(0.2f, 1.2f, currentWeight) -> 0 ile 1 arasý oran verir
            float t = Mathf.InverseLerp(baseWeight, baseWeight + (2 * weightPerBasket), currentWeight);

            emission.rateOverTime = Mathf.Lerp(minEmission, maxEmission, t);
        }
    }
}