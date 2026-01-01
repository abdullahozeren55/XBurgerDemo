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

    [Header("Container Settings")] 
    [SerializeField] private Transform foodContainer; // Malzemelerin child olacaðý boþ obje (Sepet Tabaný)
    [SerializeField] private Transform basketEntryApex; //Malzemeler girerken tepeden yay çizecekler iþte o tepe
    [SerializeField] private int capacity = 3; // Maksimum kaç yýðýn alýr?
    [SerializeField] private BoxCollider catchCollider; // Trigger olan collider

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

    [Header("Audio")]
    //public AudioClip liftSound; // Metal sesi
    //public AudioClip dropSound; // Yaða girme sesi
    //public string audioTag = "SFX";

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
        // Sadece FÝZÝKSEL OLARAK YAÐDAYSA piþir
        // (Eskiden isFrying kontrol ediyorduk, o yüzden havada piþiyordu)
        if (isPhysicallyInOil && heldItems.Count > 0)
        {
            foreach (var item in heldItems)
            {
                item.Cook(Time.deltaTime);
            }
        }
    }

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

        // 4. Zaten sepette mi? (Çift trigger korumasý)
        if (heldItems.Contains(incomingItem)) return;

        // 5. Halihazýrda grab edilmiþ mi? (Elimizdekini zorla almasýn, fýrlatýlýnca alsýn)
        // Not: IGrabable interface'inden kontrol edebiliriz
        if (incomingItem.IsGrabbed) return;

        if (incomingItem.CurrentCookingState != Cookable.CookAmount.RAW) return;

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

        item.SetVisualState(targetIndex, foodContainer, currentStackHeight, localApexPos);

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

                currentSeq.InsertCallback(hitWaterTime, () => SyncFryerState(true));
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

            if (!isFrying) // ASKIDA
            {
                Vector3 targetEuler = IsHeavy ? hangingHeavyPoint.eulerAngles : hangingPoint.eulerAngles;
                float momentumAngle = IsHeavy ? 6f : 4f;
                float settleTime = IsHeavy ? 0.35f : 0.2f;

                settleSeq.Append(transform.DORotate(targetEuler + new Vector3(momentumAngle, 0, 0), 0.1f).SetEase(Ease.OutSine));
                settleSeq.Append(transform.DORotate(targetEuler, settleTime).SetEase(Ease.OutBack));
            }
            else // YAÐDA (YÜZME)
            {
                float floatAmount = IsHeavy ? 0.015f : 0.04f;
                float floatDuration = IsHeavy ? 0.4f : 0.6f;

                settleSeq.Append(transform.DOMove(cookingPoint.position + new Vector3(0, floatAmount, 0), floatDuration * 0.4f).SetEase(Ease.OutSine));
                settleSeq.Append(transform.DOMove(cookingPoint.position, floatDuration * 0.6f).SetEase(Ease.InOutSine));
            }
        });
    }

    // --- YARDIMCI METOD (KOD TEKRARINI ÖNLEMEK ÝÇÝN) ---
    private void SyncFryerState(bool enteringOil)
    {
        if (connectedFryer == null) return;

        if (enteringOil)
        {
            if (!isPhysicallyInOil)
            {
                connectedFryer.OnBasketDown(IsHeavy);
                isPhysicallyInOil = true;
                RefreshTopItemInteractability(); // YENÝ: Girdik, kilitle!
            }
        }
        else
        {
            if (isPhysicallyInOil)
            {
                connectedFryer.OnBasketUp(IsHeavy);
                isPhysicallyInOil = false;
                RefreshTopItemInteractability(); // YENÝ: Çýktýk, kilidi aç!
            }
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
}