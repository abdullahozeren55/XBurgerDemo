using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class FryerBasket : MonoBehaviour, IInteractable
{
    // --- IInteractable Özellikleri ---
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract = true;

    public string FocusTextKey { get => focusTextKeys[basketStateNum]; set => focusTextKeys[basketStateNum] = value; }
    [SerializeField] private string[] focusTextKeys; // 0: "Ýndir", 1: "Kaldýr/As"
    private int basketStateNum = 0; // 0: Askýda (Havada), 1: Kýzarýyor (Yaðda)

    public PlayerManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;
    // ---------------------------------

    public bool IsHeavy => heldItems.Count > 0;

    // Sepet þu an müsait mi? (Yaðda deðilse ve yer varsa)
    public bool CanAcceptItem => !isFrying && !isPhysicallyInOil && heldItems.Count < capacity;

    // Sepet dolu mu?
    public bool IsFull => heldItems.Count >= capacity;

    // Ýçinde kaç tane var?
    public int ItemCount => heldItems.Count;

    [Header("Container Settings")] 
    [SerializeField] private Transform foodContainer; // Malzemelerin child olacaðý boþ obje (Sepet Tabaný)
    [SerializeField] private Transform basketEntryApex; //Malzemeler girerken tepeden yay çizecekler iþte o tepe
    [SerializeField] private int capacity = 3; // Maksimum kaç yýðýn alýr?

    [Header("Position References (Normal)")]
    [SerializeField] private Transform cookingPoint; // Yaðdaki hali (Sabit)
    [SerializeField] private Transform hangingPoint; // Askýdaki hali (Normal)
    [SerializeField] private Transform apexPoint;    // Havadaki tepe noktasý (Normal)

    [Header("Position References (Heavy)")]
    [SerializeField] private Transform hangingHeavyPoint; // Askýdaki hali (Aðýr/Eðik)
    [SerializeField] private Transform apexHeavyPoint;

    [Header("Movement Settings")]
    [SerializeField] private float moveDuration = 0.5f;
    [SerializeField] private Ease moveEase = Ease.InOutSine;
    [Range(0f, 1f)]
    [SerializeField] private float directReturnThreshold = 0.35f; // %35'in altýndaysa direkt dön
    [Range(0f, 1f)][SerializeField] private float immersionThreshold = 0.85f; // %85'ten sonra piþmeye baþla, yað yükselmeye baþlasýn vs.


    [Header("Fryer Integration")]
    [SerializeField] private Fryer connectedFryer; // Yað efektini tetiklemek için
    [Tooltip("0: Sol taraf, 1: Sað Taraf (Fritözdeki spawn point array sýrasý)")]
    [SerializeField] private int fryerLaneIndex = 0; // <--- YENÝ

    [Header("Smoke Effects & Colors")] // <--- YENÝ BAÞLIK
    [SerializeField] private GameObject smokePrefab;
    [SerializeField] private float minSmokeEmission = 5f;
    [SerializeField] private float maxSmokeEmission = 20f;

    // --- YENÝ RENK AYARLARI ---
    [System.Serializable]
    public struct SmokeColorSet
    {
        public Color minColor;
        public Color maxColor;
    }
    [Space]
    [SerializeField] private SmokeColorSet rawSmokeColors;     // Çið
    [SerializeField] private SmokeColorSet cookedSmokeColors;  // Piþmiþ
    [SerializeField] private SmokeColorSet burntSmokeColors;   // Yanýk

    private ParticleSystem currentSmokeParticles;
    private Tween emissionTween;       // Lerp iþlemini tutacak referans
    private int lastKnownCookingCount = -1; // Hafýza (Baþlangýçta -1 ki ilk seferi anlasýn)

    [Header("Audio")]
    //public AudioClip liftSound; // Metal sesi
    //public AudioClip dropSound; // Yaða girme sesi
    //public string audioTag = "SFX";
    [SerializeField] private AudioClip sizzleSound; // O meþhur COSS sesi
    [SerializeField] private float maxSizzleVolume = 1f; // Full doluyken ses þiddeti
    [SerializeField] private float sizzleSoundMinPitch = 0.8f;
    [SerializeField] private float sizzleSoundMaxPitch = 1.2f;

    private bool isFrying; // true: yaðda, false: askýda
    private bool isPhysicallyInOil = false;

    private List<Fryable> heldItems = new List<Fryable>(); // Sepetin midesi
    private float currentStackHeight = 0f; // Yýðýn yüksekliði

    private Sequence currentSeq; // Ana hareket (A -> B)
    private Sequence settleSeq;  // Yerleþme/Yüzme hareketi (Bitiþteki loop)

    // Layer Cache
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;
    private int grabableLayer;
    private int grabableOutlinedLayer;
    private int onTrayLayer; // Tray layer'ý ile ayný olabilir, karýþýklýk olmasýn diye ismini böyle tuttum

    private void Awake()
    {
        // Layer ID'lerini al
        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");
        grabableLayer = LayerMask.NameToLayer("Grabable");
        grabableOutlinedLayer = LayerMask.NameToLayer("GrabableOutlined");
        onTrayLayer = LayerMask.NameToLayer("OnTray"); // "OnTray" layer'ýný kullanýyoruz

        isFrying = false;
        basketStateNum = 0;

        // Baþlangýç pozisyonu
        if (hangingPoint != null)
        {
            transform.position = hangingPoint.position;
            transform.rotation = hangingPoint.rotation;
        }
    }

    private void Update()
    {
        // --- PÝÞÝRME DÖNGÜSÜ ---
        if (isPhysicallyInOil && heldItems.Count > 0)
        {
            bool anyStateChanged = false;

            foreach (var item in heldItems)
            {
                // Durumu kaydetmeden önce yanýk mýydý?
                bool wasBurnt = item.CurrentCookingState == CookAmount.BURNT;

                // Cook metodunu çalýþtýr
                if (item.Cook(Time.deltaTime))
                {
                    anyStateChanged = true;

                    // --- YENÝ: YANMA KONTROLÜ ---
                    // Eðer önceden yanýk deðildiyse VE þimdi yanýk olduysa
                    if (!wasBurnt && item.CurrentCookingState == CookAmount.BURNT)
                    {
                        if (connectedFryer != null)
                        {
                            connectedFryer.OnItemBurnt();
                        }
                    }
                }
            }

            if (anyStateChanged)
            {
                UpdateSmokeColor();
            }
        }

        HandleSmokeLogic();
    }

    // --- TRIGGER LOGIC (YAKALAMA) ---
    // --- TRIGGER LOGIC (YAKALAMA) ---
    public void HandleCatch(Collider other)
    {
        // 1. Sepet Yaðda mý? (Yaðdayken içine malzeme düþemez, yaða düþer)
        if (isFrying) return;

        // 2. Yer var mý?
        if (heldItems.Count >= capacity) return;

        // 3. Gelen þey Fryable mý?
        Fryable incomingItem = other.GetComponent<Fryable>();
        if (incomingItem == null) return;

        // --- YENÝ EKLENEN GÜVENLÝK ---
        // Eðer bu patatesin zaten bir sahibi (sepeti) varsa, dokunma!
        // Bu sayede yandaki sepetle ayný anda kapýþmazlar.
        if (incomingItem.currentBasket != null) return;
        // -----------------------------

        // 4. Zaten sepette mi? (Kendi içimizdeki çift trigger korumasý)
        if (heldItems.Contains(incomingItem)) return;

        // 5. Halihazýrda grab edilmiþ mi? (Elimizdekini zorla almasýn, fýrlatýlýnca alsýn)
        if (incomingItem.IsGrabbed) return;

        // 6. Sadece Çið olanlarý al (Veya piþenleri de almak istersen burayý silebilirsin)
        if (incomingItem.CurrentCookingState != CookAmount.RAW) return;

        // -- KABUL EDÝLDÝ --
        AddItem(incomingItem);
    }

    private void AddItem(Fryable item)
    {
        // 1. Önceki malzemeyi kilitle
        if (heldItems.Count > 0)
        {
            Fryable topItem = heldItems[heldItems.Count - 1];
            topItem.ChangeLayer(onTrayLayer);
        }

        // Bu item þu an kaçýncý sýraya yerleþecek? (Mevcut sayý = Yeni index)
        int targetIndex = heldItems.Count;

        // O index'teki mesh'in boyu ne kadar olacak?
        float thisItemHeight = item.GetHeightForStackIndex(targetIndex);

        // 2. Listeye ekle
        heldItems.Add(item);
        item.currentBasket = this;

        Vector3 localApexPos = Vector3.zero;

        if (basketEntryApex != null)
        {
            // Apex'in dünya pozisyonunu al -> Container'ýn içine yerel olarak çevir
            localApexPos = foodContainer.InverseTransformPoint(basketEntryApex.position);
        }

        // Animasyonun Callback'i (Animasyon bitince burasý çalýþýr)
        item.SetVisualState(targetIndex, foodContainer, currentStackHeight, localApexPos, () =>
        {
            UpdateHangingVisuals();

            // --- YENÝ: JUICE SES EFEKTÝ ---
            // Malzeme yerine oturduðunda çalsýn
            if (item.data.placeSound != null && SoundManager.Instance != null)
            {
                SoundManager.Instance.PlaySoundFX(
                    item.data.placeSound,
                    item.transform, // Sesin çýkacaðý yer (3D ses)
                    item.data.placeSoundVolume,
                    item.data.placeSoundMinPitch,
                    item.data.placeSoundMaxPitch
                );
            }
        });

        currentStackHeight += thisItemHeight;
    }

    // Fryable.cs tarafýndan çaðrýlýr (OnGrab olduðunda)
    public void RemoveItem(Fryable item)
    {
        if (!heldItems.Contains(item)) return;

        // Listeden çýkar
        heldItems.Remove(item); // Index bulmaya gerek yok direkt objeyi silebiliriz
        item.currentBasket = null; // Giden artýk bizden deðildir

        // Yüksekliði güncelle (Basitçe baþtan hesaplamak en temizi, aradan çekilme durumlarý için)
        RecalculateStackHeight();

        // --- KRÝTÝK DÜZELTME ---
        // Manuel layer deðiþtirmek yerine akýllý fonksiyonu çaðýrýyoruz.
        // Bu fonksiyon:
        // 1. Yeni "En Üsttekini" bulacak.
        // 2. Sepet yaðda mý diye bakacak.
        // 3. Yaðdaysa KÝLÝTLÝ, askýdaysa GRABABLE yapacak.
        RefreshTopItemInteractability();

        // --- YENÝ EKLEME: GÖRSELÝ GÜNCELLE ---
        UpdateHangingVisuals();
    }

    private void RecalculateStackHeight()
    {
        currentStackHeight = 0f;

        // Foreach yerine for döngüsü kullanýyoruz ki index'e (kat numarasýna) eriþelim
        for (int i = 0; i < heldItems.Count; i++)
        {
            // Her elemana soruyoruz: "Sen i. katta olsan boyun kaç olur?"
            float heightAtIndex = heldItems[i].GetHeightForStackIndex(i);
            currentStackHeight += heightAtIndex;
        }
    }

    public void OnInteract()
    {
        if (!CanInteract) return;
        // Eðer hali hazýrda bir hareket varsa, onu anýnda öldür.
        // Complete parametresini false yapýyoruz ki eski animasyonun
        // "OnComplete" eventleri (ses çalma, fryer tetikleme vs.) çalýþmasýn.
        transform.DOKill(false);

        TogglePosition();
    }

    private void TogglePosition()
    {
        // --- KESÝLME KONTROLÜ ---
        float currentProgress = 0f;
        bool isEarlyReturn = false;

        if (currentSeq != null && currentSeq.IsActive())
        {
            currentProgress = currentSeq.ElapsedPercentage();
            if (currentProgress < directReturnThreshold) isEarlyReturn = true;
            currentSeq.Kill();
        }

        // Settle animasyonu varsa onu da öldür
        if (settleSeq != null && settleSeq.IsActive()) settleSeq.Kill();

        isFrying = !isFrying;
        basketStateNum = isFrying ? 1 : 0;

        PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);

        RefreshTopItemInteractability();

        //SoundManager.Instance.PlaySoundFX(isFrying ? dropSound : liftSound, transform, 1f, 1f, 1f, true, audioTag);

        Transform currentHanging = IsHeavy ? hangingHeavyPoint : hangingPoint;
        Transform currentApex = IsHeavy ? apexHeavyPoint : apexPoint;
        Transform targetTransform = isFrying ? cookingPoint : currentHanging;
        Transform pathApex = currentApex;

        // Sequence Baþlat
        currentSeq = DOTween.Sequence();

        // --- HAREKET VE ZAMANLAMA ---
        float duration = isEarlyReturn ? moveDuration * 0.6f : moveDuration;

        if (isEarlyReturn)
        {
            // Erken Dönüþ (Düz Hareket)
            currentSeq.Append(transform.DOMove(targetTransform.position, duration).SetEase(Ease.OutQuad));
            currentSeq.Join(transform.DORotateQuaternion(targetTransform.rotation, duration).SetEase(Ease.OutQuad));
        }
        else
        {
            // Normal Dönüþ (Kavisli)
            Vector3[] pathPoints = new Vector3[] { pathApex.position, targetTransform.position };
            currentSeq.Append(transform.DOPath(pathPoints, duration, PathType.CatmullRom).SetEase(moveEase));

            float halfDuration = duration / 2f;
            currentSeq.Insert(0f, transform.DORotateQuaternion(pathApex.rotation, halfDuration).SetEase(Ease.OutSine));
            currentSeq.Insert(halfDuration, transform.DORotateQuaternion(targetTransform.rotation, halfDuration).SetEase(Ease.InSine));
        }

        // --- KRÝTÝK NOKTA: YAÐ TETÝKLEME ZAMANLAMASI ---
        if (connectedFryer != null)
        {
            if (isFrying)
            {
                // Erken dönüþse %70, yoksa senin belirlediðin immersionThreshold (%85)
                float threshold = isEarlyReturn ? immersionThreshold/2f : immersionThreshold;
                float hitWaterTime = duration * threshold;

                currentSeq.InsertCallback(hitWaterTime, () =>
                {
                    SyncFryerState(true);

                    PlaySizzleSound();
                });
            }
            else
            {
                // Çýkarken hemen (0. saniye)
                currentSeq.InsertCallback(0f, () => SyncFryerState(false));
            }
        }

        // --- BÝTÝÞ EFEKTLERÝ (Sadece Salýným) ---
        currentSeq.OnComplete(() =>
        {
            settleSeq = DOTween.Sequence();

            if (!isFrying) // ASKIDA (Mevcut kod ayný kalýyor)
            {
                // ... (Askýdaki sallanma kodlarý ayný) ...
                Vector3 targetEuler = IsHeavy ? hangingHeavyPoint.eulerAngles : hangingPoint.eulerAngles;
                float momentumAngle = IsHeavy ? 6f : 4f;
                float settleTime = IsHeavy ? 0.35f : 0.2f;

                settleSeq.Append(transform.DORotate(targetEuler + new Vector3(momentumAngle, 0, 0), 0.1f).SetEase(Ease.OutSine));
                settleSeq.Append(transform.DORotate(targetEuler, settleTime).SetEase(Ease.OutBack));
            }
            else // YAÐDA (YÜZME HÝSSÝ GÜNCELLENDÝ)
            {
                // Boþ sepet (0 item): Çok yüzer, hýzlý sallanýr (Mantar gibi)
                // Dolu sepet (3 item): Az yüzer, aðýr aðýr sallanýr (Taþ gibi)

                float ratio = Mathf.Clamp01((float)heldItems.Count / capacity); // 0 (Boþ) -> 1 (Dolu)

                // Float Amount: Boþken 0.04f -> Doluyken 0.01f (Aðýrlaþýnca az batýp çýkar)
                float floatAmount = Mathf.Lerp(0.04f, 0.01f, ratio);

                // Duration: Boþken 0.6f -> Doluyken 1.2f (Aðýrlaþýnca periyot uzar)
                float floatDuration = Mathf.Lerp(0.6f, 1.2f, ratio);

                // Hafifçe yukarý çýk (Suyun kaldýrma kuvveti)
                settleSeq.Append(transform.DOMove(cookingPoint.position + new Vector3(0, floatAmount, 0), floatDuration * 0.4f).SetEase(Ease.OutSine));
                // Yavaþça dengeye otur
                settleSeq.Append(transform.DOMove(cookingPoint.position, floatDuration * 0.6f).SetEase(Ease.InOutSine));

                // Loop'a sokmak istersen (Sürekli yüzsün):
                // settleSeq.SetLoops(-1, LoopType.Yoyo); // Ama þimdilik sadece giriþteki oturma hareketini yapýyoruz.
            }
        });
    }

    // --- YARDIMCI METOD (KOD TEKRARINI ÖNLEMEK ÝÇÝN) ---
    private void SyncFryerState(bool enteringOil)
    {
        if (connectedFryer == null) return;

        int totalCount = heldItems.Count;

        // --- HESAPLAMA --- 
        // Hem giriþ hem çýkýþ için "Kaçý Saðlam?" bilgisini hazýrlayalým
        int activeCount = 0;
        foreach (var item in heldItems)
        {
            if (item.CurrentCookingState != CookAmount.BURNT)
                activeCount++;
        }
        // -----------------

        if (enteringOil)
        {
            if (!isPhysicallyInOil)
            {
                // DÜZELTME: Artýk totalCount ve activeCount'u ayrý gönderiyoruz.
                // Eðer hepsi yanýksa activeCount 0 gidecek ve Fryer köpürmeyecek.
                connectedFryer.OnBasketDown(totalCount, activeCount, fryerLaneIndex);

                isPhysicallyInOil = true;
                RefreshTopItemInteractability();
            }
        }
        else
        {
            if (isPhysicallyInOil)
            {
                // Çýkýþta da aynýsý (Burasý zaten böyleydi, activeCount hesaplamasýný yukarý taþýdýk sadece)
                connectedFryer.OnBasketUp(totalCount, activeCount, fryerLaneIndex);

                isPhysicallyInOil = false;
                RefreshTopItemInteractability();
            }
        }
    }

    private void PlaySizzleSound()
    {
        if (sizzleSound == null) return;

        // 1. Yanmamýþ (Active) malzeme sayýsýný bul
        int activeCount = 0;
        foreach (var item in heldItems)
        {
            if (item.CurrentCookingState != CookAmount.BURNT)
                activeCount++;
        }

        // 2. Eðer yanmamýþ malzeme yoksa ses çalma (Sessizce girsin)
        // (Veya istersen çok kýsýk bir ses çalsýn dersen bu satýrý silip activeCount=0 ile iþleme devam edebilirsin)
        if (activeCount == 0) return;

        // 3. Oranla: (Aktif / Kapasite) * MaxVolume
        float ratio = (float)activeCount / capacity;
        float finalVolume = ratio * maxSizzleVolume;

        // 4. Çal
        if (SoundManager.Instance != null)
        {
            SoundManager.Instance.PlaySoundFX(
                sizzleSound,
                transform,
                finalVolume,
                sizzleSoundMinPitch,
                sizzleSoundMaxPitch
            );
        }
    }

    // --- IInteractable Standart Metotlar ---

    public void OnFocus()
    {
        if (!CanInteract) return;
        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }

    public void OnLoseFocus()
    {
        if (!CanInteract) return;
        ChangeLayer(interactableLayer);
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
            ChangeLayer(interactableOutlinedRedLayer);
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
            ChangeLayer(interactableOutlinedLayer);
    }

    public void ChangeLayer(int layer)
    {
        gameObject.layer = layer;
    }

    public void HandleFinishDialogue() { }

    // Bu fonksiyonu her durum deðiþikliðinde çaðýracaðýz
    public void RefreshTopItemInteractability()
    {
        // 1. Sepet boþsa yapacak bir þey yok
        if (heldItems.Count == 0) return;

        // 2. En tepedeki elemaný bul
        Fryable topItem = heldItems[heldItems.Count - 1];

        // 3. Durum Analizi:
        // Eðer yaðdaysak (isFrying veya isPhysicallyInOil) -> KÝLÝTLE (OnTray)
        // Eðer askýdaysak -> AÇ (Grabable)

        // Not: isFrying kontrolünü de ekledim ki daha inerken bile almayý engelleyelim.
        // Ýstersen sadece isPhysicallyInOil býrakabilirsin.
        bool shouldBeLocked = isFrying || isPhysicallyInOil;

        if (shouldBeLocked)
        {
            topItem.ChangeLayer(onTrayLayer);
        }
        else
        {
            // KÝLÝDÝ AÇ (AMA NASIL?)
            // PlayerManager'a sor: "Þu an bu objeye mi bakýyorum?"
            // Not: Metodun parametre olarak (MonoBehaviour/IGrabable) ne istediðine göre topItem veriyoruz.
            if (PlayerManager.Instance.ShouldIGoWithOutlineWhenTurningBackToGrabable(topItem))
            {
                // Zaten buna bakýyormuþuz, Outline ile aç
                topItem.ChangeLayer(grabableOutlinedLayer);
            }
            else
            {
                // Bakmýyoruz, düz aç
                topItem.ChangeLayer(grabableLayer);
            }
        }
    }

    public bool IsTopItem(Fryable item)
    {
        return heldItems.Count > 0 && heldItems[heldItems.Count - 1] == item;
    }

    // --- YENÝ FONKSÝYON: CANLI AÐIRLIK TEPKÝSÝ ---
    // --- GÜNCELLENEN FONKSÝYON: KADEMELÝ AÐIRLIK VE YAYLANMA ---
    private void UpdateHangingVisuals()
    {
        // 1. Eðer yaðdaysak karýþma
        if (isFrying) return;

        // 2. Oraný Hesapla (0 ile 1 arasý)
        // Count / Capacity (Örn: 1/3 = 0.33, 2/3 = 0.66, 3/3 = 1.0)
        float ratio = Mathf.Clamp01((float)heldItems.Count / capacity);

        // 3. Hedef Pozisyonu ve Rotasyonu Ara Deðerlerden (Lerp) Bul
        // "HangingPoint" (Baþlangýç) ile "HangingHeavyPoint" (Bitiþ) arasýndaki o noktayý buluyoruz.
        Vector3 targetPos = Vector3.Lerp(hangingPoint.position, hangingHeavyPoint.position, ratio);
        Quaternion targetRot = Quaternion.Lerp(hangingPoint.rotation, hangingHeavyPoint.rotation, ratio);

        // 4. Önceki hareketleri temizle
        if (settleSeq != null && settleSeq.IsActive()) settleSeq.Kill();
        transform.DOKill();

        // 5. YAYLANMA EFEKTÝ (Ease.OutBack)
        // OutBack: Hedefe giderken biraz geçer (aþaðý çöker), sonra hedefe geri döner.
        // Süreyi biraz arttýrdým (0.25 -> 0.4) ki yaylanma hissedilsin.
        transform.DOMove(targetPos, 0.4f).SetEase(Ease.OutBack, 3f);
        transform.DORotateQuaternion(targetRot, 0.4f).SetEase(Ease.OutBack, 3f);
    }

    // --- YENÝ JUICE FONKSÝYONU ---
    private void HandleSmokeLogic()
    {
        // 1. Duman Üretmeli miyiz?
        // Sadece "Yaðda mýyýz?" ve "Sepet dolu mu?" diye bakalým.
        // ÝÇÝNDEKÝLERÝN YANIK OLUP OLMADIÐINA BAKMAYALIM.
        bool shouldEmitSmoke = isPhysicallyInOil && heldItems.Count > 0;

        int cookingItemCount = 0;

        // Yine de emission hesabý için saðlamlarý sayalým
        if (shouldEmitSmoke)
        {
            foreach (var item in heldItems)
            {
                if (item.CurrentCookingState != CookAmount.BURNT) cookingItemCount++;
            }

            // --- SÝLÝNEN SATIR ---
            // if (cookingItemCount == 0) shouldEmitSmoke = false; // <--- BU SATIR KATÝLDÝ, SÝLDÝK.
            // ---------------------
        }

        // 2. Duruma Göre Aksiyon Al
        if (shouldEmitSmoke)
        {
            if (currentSmokeParticles == null)
            {
                CreateSmoke();
                UpdateSmokeColor(); // Ýlk oluþtuðunda renk ayarla
            }

            if (currentSmokeParticles != null)
            {
                // Burasý zaten cookingItemCount 0 olsa bile çalýþacak
                // ve emission'ý Lerp ile yavaþça min deðere çekecek.
                UpdateSmokeEmission(cookingItemCount);
            }
        }
        else
        {
            // Sadece sepet boþsa veya yaðdan çýktýysa burasý çalýþýr
            if (currentSmokeParticles != null)
            {
                StopAndDetachSmoke();
            }
        }
    }

    // --- YENÝ: MATEMATÝKSEL RENK KARIÞTIRMA ---
    private void UpdateSmokeColor()
    {
        if (currentSmokeParticles == null || heldItems.Count == 0) return;

        int rawCount = 0;
        int cookedCount = 0;
        int burntCount = 0;

        // 1. Sayým Yap
        foreach (var item in heldItems)
        {
            switch (item.CurrentCookingState)
            {
                case CookAmount.RAW: rawCount++; break;
                case CookAmount.REGULAR: cookedCount++; break;
                case CookAmount.BURNT: burntCount++; break;
            }
        }

        int total = heldItems.Count;

        // 2. Aðýrlýklý Ortalama Hesapla
        // Formül: (RawSayýsý * RawRengi + CookedSayýsý * CookedRengi + ...) / ToplamSayý

        // Min Color Hesabý
        Color mixedMin = (rawSmokeColors.minColor * rawCount +
                          cookedSmokeColors.minColor * cookedCount +
                          burntSmokeColors.minColor * burntCount) / total;

        // Max Color Hesabý
        Color mixedMax = (rawSmokeColors.maxColor * rawCount +
                          cookedSmokeColors.maxColor * cookedCount +
                          burntSmokeColors.maxColor * burntCount) / total;

        // 3. Partiküle Uygula
        var main = currentSmokeParticles.main;
        main.startColor = new ParticleSystem.MinMaxGradient(mixedMin, mixedMax);
    }

    private void CreateSmoke()
    {
        if (smokePrefab == null) return;

        // Instantiate edip Sepet'e parent yapýyoruz ki sepet sallanýnca duman kaynaðý da sallansýn
        GameObject smokeObj = Instantiate(smokePrefab, transform.position, Quaternion.Euler(-90, 0, 0), transform);
        currentSmokeParticles = smokeObj.GetComponent<ParticleSystem>();
    }

    private void UpdateSmokeEmission(int currentCount)
    {
        // 1. Optimizasyon: Eðer sayý deðiþmediyse boþuna iþlem yapma
        if (currentCount == lastKnownCookingCount) return;

        var emission = currentSmokeParticles.emission;

        // Hedef oraný hesapla
        float ratio = Mathf.Clamp01((float)currentCount / capacity);
        float targetRate = Mathf.Lerp(minSmokeEmission, maxSmokeEmission, ratio);

        // 2. Önceki bir tween varsa öldür (Çakýþma kontrolü)
        if (emissionTween != null && emissionTween.IsActive()) emissionTween.Kill();

        // 3. Karar Aný: Anýnda mý, Lerp mi?
        // Eðer lastKnownCookingCount -1 ise, bu dumanýn yeni oluþtuðu (ilk dalýþ) andýr.
        if (lastKnownCookingCount == -1)
        {
            // ÝLK GÝRÝÞ: Anýnda ayarla
            emission.rateOverTime = targetRate;
        }
        else
        {
            // DEÐÝÞÝM ANI (Biri yandý): 1 saniyede Lerple
            // Þu anki deðeri al (Yarýda kalan bir tween varsa onun býraktýðý yerden devam eder)
            float startRate = emission.rateOverTime.constant;

            emissionTween = DOVirtual.Float(startRate, targetRate, 2f, (value) =>
            {
                var e = currentSmokeParticles.emission;
                e.rateOverTime = value;
            }).SetEase(Ease.Linear); // Linear veya OutQuad kullanýlabilir
        }

        // 4. Hafýzayý güncelle
        lastKnownCookingCount = currentCount;
    }

    private void StopAndDetachSmoke()
    {
        // Tween varsa temizle
        if (emissionTween != null) emissionTween.Kill();

        // Hafýzayý sýfýrla ki bir sonraki sefer yaða girdiðinde yine "Anýnda" baþlasýn
        lastKnownCookingCount = -1;

        // Mevcut durdurma iþlemleri
        currentSmokeParticles.Stop();
        currentSmokeParticles = null;
    }
}