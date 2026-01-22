using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class Grill : MonoBehaviour
{
    [Header("Slot System")]
    [SerializeField] private List<Transform> cookingSlots;
    private BurgerIngredient[] occupiedSlots;
    [SerializeField] private float positionRandomness = 0.0004f;

    [Header("Audio Settings (Advanced)")]
    private AudioSource grillAudioSource;
    [SerializeField] private float volumePerPatty = 0.3f;
    [SerializeField] private float volumePerBun = 0.15f; // Ekmek daha az ses çýkarsýn
    [SerializeField] private float maxVolume = 1.0f;
    [SerializeField] private float fadeTime = 2.0f; // Ses geçiþ süresi

    // Audio Logic Variables
    private int cookingPattyCount = 0;
    private int cookingBunCount = 0;
    private float targetVolume = 0f;
    private Coroutine audioFadeCoroutine;

    [Header("Animation Settings")]
    [SerializeField] private float placementApexHeight = 0.2f; // Tepeden inme yüksekliði (User's Z offset request)
    [SerializeField] private float placementDuration = 0.5f;   // Yerleþme süresi (Biraz daha yavaþ ve tok)
    [SerializeField] private Ease placementEase = Ease.OutSine; // Yumuþak bitiþ

    [Header("Cooking Particles (Per Slot)")]
    [SerializeField] private ParticleSystem pattyCookParticlesPrefab;
    [SerializeField] private ParticleSystem topBunCookParticlesPrefab;
    [SerializeField] private ParticleSystem bottomBunCookParticlesPrefab;

    [Header("Smoke Particles")]
    [SerializeField] private ParticleSystem smokePrefabWorld;
    [SerializeField] private ParticleSystem smokePrefabLocal;

    // Slotlardaki aktif dumanlarý takip etmek için:
    private ParticleSystem[] activeSmokeParticles;

    // Hangi slotta hangi partikül çalýþýyor takip etmek için:
    private ParticleSystem[] activeSlotParticles;

    private void Awake()
    {
        grillAudioSource = GetComponent<AudioSource>();

        if (cookingSlots != null)
        {
            occupiedSlots = new BurgerIngredient[cookingSlots.Count];
            // Partikül takip dizisini slot sayýsý kadar oluþtur
            activeSlotParticles = new ParticleSystem[cookingSlots.Count];
            activeSmokeParticles = new ParticleSystem[cookingSlots.Count];
        }

        if (grillAudioSource != null)
        {
            grillAudioSource.loop = true;
            grillAudioSource.volume = 0f; // Baþlangýçta ses yok
            grillAudioSource.Play();
        }
    }

    private void Update()
    {
        int currentlyCookingCount = 0;

        for (int i = 0; i < occupiedSlots.Length; i++)
        {
            BurgerIngredient item = occupiedSlots[i];

            // Item var mý?
            if (item != null)
            {
                bool isStillCooking = item.ProcessCooking(Time.deltaTime);

                if (isStillCooking)
                {
                    currentlyCookingCount++;
                }
                else
                {
                    // --- YENÝ MANTIK ---
                    // Eðer piþme bittiyse (yandýysa) ve partikül hala "Playing" durumundaysa
                    // (Yani henüz stop emri almadýysa) yavaþça söndür.
                    if (activeSlotParticles[i] != null && activeSlotParticles[i].isPlaying)
                    {
                        // false -> Yavaþça (0.2sn) söndür
                        StopCookParticles(i, false);
                    }

                    if (activeSmokeParticles[i] != null && activeSmokeParticles[i].isPlaying)
                        StopSmokeParticles(i, false);
                }
            }
            // Item yok ama partikül kalmýþsa (Hata durumu veya animasyon bitiþi)
            else if (activeSlotParticles[i] != null || activeSmokeParticles[i] != null) // Hata korumasý
            {
                if (activeSlotParticles[i] != null) StopCookParticles(i, true);
                if (activeSmokeParticles[i] != null) StopSmokeParticles(i, true);
            }
        }
    }

    // --- TRIGGER TARAFINDAN ÇAÐRILACAK ---
    public bool AttemptAddItem(Collider other)
    {
        BurgerIngredient item = other.GetComponent<BurgerIngredient>();

        if (item == null) return false;

        // 1. KONTROL: Veri uygunluðu
        if (!item.data.isCookable) return false;

        // [YENÝ] Sadece ÇÝÐ olanlar piþebilir. Piþmiþ veya yanmýþsa alma.
        if (item.cookAmount != CookAmount.RAW) return false;

        if (item.IsGrabbed) return false;

        // 2. KONTROL: Sahiplik Durumu (Zaten bir yerde mi?)
        if (item.currentGrill != null) return false;
        if (item.gameObject.layer == LayerMask.NameToLayer("OnTray")) return false;

        // 3. KONTROL: Boþ slot var mý?
        int emptyIndex = GetFirstEmptySlotIndex();
        if (emptyIndex == -1) return false;

        // Slotu rezerve et
        occupiedSlots[emptyIndex] = item;
        item.currentGrill = this;

        // Görsel yerleþim ve SES
        MoveItemToSlot(item, emptyIndex);

        // Izgara genel sesi (Loop) güncelle - Eklendiði için true
        UpdateAudioCounts(item.data.ingredientType, true);

        return true;
    }

    // --- BURGER INGREDIENT TARAFINDAN ÇAÐRILACAK (OnGrab) ---
    public void RemoveItem(BurgerIngredient item)
    {
        int index = GetSlotIndexForItem(item);
        if (index != -1)
        {
            occupiedSlots[index] = null;
            item.currentGrill = null;

            // Partikülleri anýnda durdur
            StopCookParticles(index, true);
            StopSmokeParticles(index, true);

            // [YENÝ MANTIK] Ses Kontrolü:
            // Eðer item ZATEN YANMIÞSA, sesi 'OnItemStateChanged' içinde kýsmýþtýk.
            // Burada tekrar kýsarsak sayaç bozulur.
            // Sadece yanmamýþ (hala ses çýkaran) itemlarý toplarken sesi düþ.
            if (item.cookAmount != CookAmount.BURNT)
            {
                UpdateAudioCounts(item.data.ingredientType, false);
            }
        }
    }

    // --- SES YÖNETÝMÝ (SoundManager Mantýðý) ---
    private void UpdateAudioCounts(BurgerIngredientData.IngredientType type, bool isAdding)
    {
        int change = isAdding ? 1 : -1;

        if (type == BurgerIngredientData.IngredientType.PATTY)
            cookingPattyCount += change;
        else if (type == BurgerIngredientData.IngredientType.BOTTOMBUN || type == BurgerIngredientData.IngredientType.TOPBUN)
            cookingBunCount += change;

        // Güvenlik (Negatife düþmesin)
        if (cookingPattyCount < 0) cookingPattyCount = 0;
        if (cookingBunCount < 0) cookingBunCount = 0;

        RecalculateTargetVolume();
    }

    private void RecalculateTargetVolume()
    {
        // Hedef sesi hesapla
        float rawVolume = (cookingPattyCount * volumePerPatty) + (cookingBunCount * volumePerBun);
        targetVolume = Mathf.Clamp(rawVolume, 0f, maxVolume);

        // Coroutine yönetimi (Çakýþmayý önle)
        if (audioFadeCoroutine != null) StopCoroutine(audioFadeCoroutine);
        audioFadeCoroutine = StartCoroutine(LerpAudioVolume());
    }

    private IEnumerator LerpAudioVolume()
    {
        if (grillAudioSource == null) yield break;

        float startVol = grillAudioSource.volume;
        float time = 0f;

        while (time < fadeTime)
        {
            time += Time.deltaTime;
            grillAudioSource.volume = Mathf.Lerp(startVol, targetVolume, time / fadeTime);
            yield return null;
        }

        grillAudioSource.volume = targetVolume;
    }

    private void MoveItemToSlot(BurgerIngredient item, int slotIndex)
    {
        Transform targetSlot = cookingSlots[slotIndex];

        // 1. FÝZÝK KAPATMA
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
        }

        // 2. PARENTING
        item.transform.SetParent(targetSlot, true);
        item.SetOnTrayLayer();

        // 3. RANDOMIZE POZÝSYON (Doðallýk için sapma)
        // Belirlediðimiz 0.0004 aralýðýnda rastgele X ve Y üret
        float randomX = Random.Range(-positionRandomness, positionRandomness);
        float randomY = Random.Range(-positionRandomness, positionRandomness);
        Vector3 randomOffset = new Vector3(randomX, randomY, 0f);

        // 4. HEDEF VE APEX HESABI
        // Slotun merkezi (0,0,0) + Data Offset + RANDOM OFFSET
        Vector3 targetLocalPos = Vector3.zero + item.data.grillPositionOffset + randomOffset;
        Quaternion targetLocalRot = Quaternion.Euler(item.data.grillRotationOffset);

        // Z ekseni yukarý baktýðý için Apex'i forward yönünde kaldýrýyoruz
        Vector3 apexLocalPos = targetLocalPos + (Vector3.forward * placementApexHeight);

        // 5. SES (First Sizzle)
        if (item.data.cookingSound != null && SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySoundFX(
                item.data.cookingSound,
                item.transform,
                item.data.cookingSoundVolume,
                item.data.cookingSoundMinPitch,
                item.data.cookingSoundMaxPitch
            );
        }

        // 6. ANÝMASYON (Hareket)
        Sequence seq = DOTween.Sequence();

        // Yay çizerek in (Apex -> Random Target)
        seq.Join(item.transform.DOLocalPath(new Vector3[] { apexLocalPos, targetLocalPos }, placementDuration, PathType.CatmullRom).SetEase(placementEase));

        // Dönerken in
        seq.Join(item.transform.DOLocalRotateQuaternion(targetLocalRot, placementDuration).SetEase(Ease.OutCubic));

        // 7. BÝTÝÞ VE SQUASH JUICE
        seq.OnComplete(() =>
        {
            // Kilidi aç
            if (item != null && item.currentGrill == this)
            {
                item.PlayPutOnSoundEffect();
                item.SetOnGrabableLayer();

                SpawnCookParticles(item, slotIndex);
                SpawnSmokeParticles(item, slotIndex);
            }

            // --- SQUASH EFFECT (SLOT ÜZERÝNDEN) ---
            targetSlot.DOKill(true);
            targetSlot.localScale = Vector3.one;

            // X ve Y geniþler, Z (Yükseklik) basýlýr
            targetSlot.DOScale(new Vector3(1.1f, 1.1f, 0.9f), 0.15f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // Eski haline lastik gibi dön
                    targetSlot.DOScale(Vector3.one, 0.3f)
                        .SetEase(Ease.OutElastic);
                });
        });
    }

    private int GetFirstEmptySlotIndex()
    {
        for (int i = 0; i < occupiedSlots.Length; i++)
            if (occupiedSlots[i] == null) return i;
        return -1;
    }

    private int GetSlotIndexForItem(BurgerIngredient item)
    {
        for (int i = 0; i < occupiedSlots.Length; i++)
            if (occupiedSlots[i] == item) return i;
        return -1;
    }

    private void SpawnCookParticles(BurgerIngredient item, int slotIndex)
    {
        // 1. Tip kontrolü yapýp doðru prefabý seçelim
        ParticleSystem prefabToSpawn = null;

        if (item.data.ingredientType == BurgerIngredientData.IngredientType.PATTY)
        {
            prefabToSpawn = pattyCookParticlesPrefab;
        }
        else if (item.data.ingredientType == BurgerIngredientData.IngredientType.BOTTOMBUN)
        {
            // Alt ekmek için özel partikül
            prefabToSpawn = bottomBunCookParticlesPrefab;
        }
        else if (item.data.ingredientType == BurgerIngredientData.IngredientType.TOPBUN)
        {
            // Üst ekmek için özel partikül
            prefabToSpawn = topBunCookParticlesPrefab;
        }

        // Eðer prefab yoksa veya atanmamýþsa çýk
        if (prefabToSpawn == null) return;

        // 2. Instantiate Ýþlemi
        // Pozisyon: Item'ýn o anki (random offsetli) konumu
        // Parent: Slot
        ParticleSystem newParticle = Instantiate(prefabToSpawn, item.transform.position, Quaternion.identity, cookingSlots[slotIndex]);

        // 3. Referansý kaydet
        activeSlotParticles[slotIndex] = newParticle;

        // Item o an hangi durumdaysa (RAW, REGULAR vs.) o renkle baþla
        UpdateParticleColor(newParticle, item.data, item.cookAmount);
    }

    private void StopCookParticles(int slotIndex, bool isImmediate = false)
    {
        ParticleSystem ps = activeSlotParticles[slotIndex];

        if (ps != null)
        {
            // Eðer "Hemen Durdur" dendiyse veya zaten sönme aþamasýndaysa direkt kes
            if (isImmediate)
            {
                ps.Stop();
                activeSlotParticles[slotIndex] = null;
            }
            else
            {
                // Yavaþça söndür (0.5sn)
                var emission = ps.emission;
                float startRate = emission.rateOverTime.constant; // O anki hýzý al

                // DOTween ile sanal bir float deðeri 0'a indiriyoruz
                DOVirtual.Float(startRate, 0f, 1f, (value) =>
                {
                    // Her adýmda emission deðerini güncelle
                    var e = ps.emission;
                    e.rateOverTime = value;
                })
                .OnComplete(() =>
                {
                    // Tween bitince tamamen durdur ve referansý sil
                    ps.Stop();
                    // Garanti olsun diye tekrar eski hýzýna çek (Object Pooling vs kullanýrsan ilerde lazým olur)
                    var e = ps.emission;
                    e.rateOverTime = startRate;

                    // Eðer bu süreçte slot dolmadýysa referansý temizle
                    if (activeSlotParticles[slotIndex] == ps)
                        activeSlotParticles[slotIndex] = null;
                });
            }
        }
    }

    // --- BURGER INGREDIENT TARAFINDAN ÇAÐRILACAK (Durum Deðiþince) ---
    public void OnItemStateChanged(BurgerIngredient item, CookAmount newState)
    {
        int index = GetSlotIndexForItem(item);
        if (index == -1) return;

        // 1. Piþme Partikülü (Cýzýrtý)
        if (activeSlotParticles[index] != null)
            UpdateParticleColor(activeSlotParticles[index], item.data, newState);

        // 2. World Duman
        if (activeSmokeParticles[index] != null)
            UpdateSmokeColor(activeSmokeParticles[index], item.data, newState);

        // 3. Local Duman
        if (item.attachedSmoke != null)
            UpdateSmokeColor(item.attachedSmoke, item.data, newState);

        // [YENÝ] 4. Ses Durumu (Yanma Kontrolü)
        // Eðer yeni durum YANIK ise, artýk cýzýrdamayý kesmeli.
        // Sanki ýzgaradan alýnmýþ gibi ses count'unu düþürüyoruz.
        if (newState == CookAmount.BURNT)
        {
            UpdateAudioCounts(item.data.ingredientType, false);
        }
    }

    private void UpdateParticleColor(ParticleSystem ps, BurgerIngredientData data, CookAmount state)
    {
        var main = ps.main;
        BurgerIngredientData.ParticleColorSet colorSet;

        // Duruma göre doðru renk setini seç
        switch (state)
        {
            case CookAmount.RAW:
                colorSet = data.rawParticleColors;
                break;
            case CookAmount.REGULAR:
                colorSet = data.cookedParticleColors;
                break;
            case CookAmount.BURNT:
                colorSet = data.burntParticleColors;
                break;
            default:
                colorSet = data.rawParticleColors;
                break;
        }

        // Partikül sistemine "Random Between Two Colors" olarak ata
        main.startColor = new ParticleSystem.MinMaxGradient(colorSet.minColor, colorSet.maxColor);
    }

    private void SpawnSmokeParticles(BurgerIngredient item, int slotIndex)
    {
        // 1. WORLD SMOKE (Mevcut Sistem)
        if (smokePrefabWorld != null)
        {
            ParticleSystem newWorldSmoke = Instantiate(smokePrefabWorld, item.transform.position, Quaternion.identity, cookingSlots[slotIndex]);
            activeSmokeParticles[slotIndex] = newWorldSmoke;
            UpdateSmokeColor(newWorldSmoke, item.data, item.cookAmount);
        }

        // 2. LOCAL SMOKE (Yeni Sistem)
        if (smokePrefabLocal != null)
        {
            // Item'ýn çocuðu yapýyoruz (transform)
            ParticleSystem newLocalSmoke = Instantiate(smokePrefabLocal, item.transform.position, Quaternion.identity, item.transform);

            // Item'a "Al bu senin dumanýn" diyoruz
            item.attachedSmoke = newLocalSmoke;

            // Rengini ayarla (Ayný mantýk)
            UpdateSmokeColor(newLocalSmoke, item.data, item.cookAmount);
        }
    }

    private void StopSmokeParticles(int slotIndex, bool isImmediate = false)
    {
        ParticleSystem ps = activeSmokeParticles[slotIndex];

        if (ps != null)
        {
            if (isImmediate)
            {
                ps.Stop();
                activeSmokeParticles[slotIndex] = null;
            }
            else
            {
                // 0.5 saniyede sönerek dur (Senin yeni tercihin)
                var emission = ps.emission;
                float startRate = emission.rateOverTime.constant;

                DOVirtual.Float(startRate, 0f, 1f, (value) =>
                {
                    var e = ps.emission;
                    e.rateOverTime = value;
                })
                .OnComplete(() =>
                {
                    ps.Stop();
                    var e = ps.emission;
                    e.rateOverTime = startRate; // Reset for pooling logic if needed

                    if (activeSmokeParticles[slotIndex] == ps)
                        activeSmokeParticles[slotIndex] = null;
                });
            }
        }
    }

    private void UpdateSmokeColor(ParticleSystem ps, BurgerIngredientData data, CookAmount state)
    {
        var main = ps.main;
        BurgerIngredientData.ParticleColorSet colorSet;

        switch (state)
        {
            case CookAmount.RAW:
                colorSet = data.rawSmokeColors;
                break;
            case CookAmount.REGULAR:
                colorSet = data.cookedSmokeColors;
                break;
            case CookAmount.BURNT:
                colorSet = data.burntSmokeColors;
                break;
            default:
                colorSet = data.rawSmokeColors;
                break;
        }

        main.startColor = new ParticleSystem.MinMaxGradient(colorSet.minColor, colorSet.maxColor);
    }
}