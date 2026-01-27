using DG.Tweening;
using System.Collections;
using UnityEngine;

public class SodaMachine : MonoBehaviour
{
    // ... (Deðiþkenler ayný) ...
    [Header("Components")]
    [SerializeField] private SodaButton[] buttons;
    [SerializeField] private LineRenderer streamLine;

    [Header("Cup Stacks")]
    // Inspector'da doldururken DÝKKAT: Element 0 = En Üstteki Bardak
    [SerializeField] private System.Collections.Generic.List<DrinkCup> smallCups;
    [SerializeField] private System.Collections.Generic.List<DrinkCup> mediumCups;
    [SerializeField] private System.Collections.Generic.List<DrinkCup> largeCups;

    [Header("Configuration")]
    [SerializeField] private Color[] flavorColors;
    [SerializeField] private float targetLength = 0.2f;
    [SerializeField] private float pourSpeed = 10f;

    [Header("Process Settings")]
    [SerializeField] private float pourDuration = 1.0f;
    [SerializeField] private float stopDuration = 0.3f;
    [SerializeField] private float buttonPressDepth = 0.02f;

    [Header("Cup Placement")]
    [SerializeField] private Transform cupSnapPoint;
    [SerializeField] private Transform cupPlacementApex;
    private DrinkCup currentCup;

    [Header("Audio")]
    [SerializeField] private AudioClip[] cupFillSounds; //0 small, 1 medium, 2 large
    [SerializeField] private float fillVolume = 1f;
    [SerializeField] private float fillMinPitch = 0.9f;
    [SerializeField] private float fillMaxPitch = 1.1f;
    [Tooltip("Sývýnýn musluktan çýkýp bardaða deðmesi için geçecek süre (Delay)")]
    [SerializeField] private float soundStartDelay = 0.15f; // <-- YENÝ: Bunu isteðine göre 0.1 ile 0.3 arasý ayarlarsýn

    [Header("Machine Working Sound (Loop)")]
    [SerializeField] private AudioClip machineWorkClip;
    [SerializeField] private float machineTargetVolume = 0.5f; // Ulaþýlacak ses seviyesi
    [SerializeField] private float machineFadeInTime = 0.5f;
    [SerializeField] private float machineFadeOutTime = 0.5f;

    [Header("Empty Pour FX (No Cup)")]
    [SerializeField] private GameObject splashFXPrefab; // Sýçrama Partikülü
    [SerializeField] private Transform splashPoint;     // Izgaranýn olduðu nokta (Target)
    [SerializeField] private float splashDelay = 0.25f; // Musluktan yere düþme süresi
    [SerializeField] private AudioClip splashSound;     // "Pýssss" veya þapýrdama sesi
    [SerializeField] private float splashVolume = 0.8f;
    // Sýçrama sesinin pitch aralýðý
    [SerializeField] private float splashMinPitch = 0.9f;
    [SerializeField] private float splashMaxPitch = 1.1f;

    [Header("Cup Placement Sound")]
    [SerializeField] private AudioClip cupPlaceSound;
    [SerializeField] private float cupPlaceVolume = 1f;
    [SerializeField] private float cupPlaceMinPitch = 0.9f;
    [SerializeField] private float cupPlaceMaxPitch = 1.1f;

    

    private AudioSource machineAudioSource; // Private referans

    private enum StreamState { Idle, Pouring, Stopping }
    private StreamState currentState = StreamState.Idle;
    private float currentStreamLength = 0f;
    private float currentStreamStart = 0f;
    private Material streamMat;

    private int interactableLayer;
    private int ungrabableLayer;
    private int grababaleLayer;

    // Yardýmcý Metot: Buton tipi + Bardak boyutu = Gerçek Ýçecek Tipi
    private GameManager.DrinkTypes GetSpecificDrinkType(GameManager.DrinkTypes baseType, GameManager.CupSize size)
    {
        switch (baseType)
        {
            case GameManager.DrinkTypes.Cola:
                if (size == GameManager.CupSize.Small) return GameManager.DrinkTypes.ColaSmall;
                if (size == GameManager.CupSize.Medium) return GameManager.DrinkTypes.ColaMedium;
                return GameManager.DrinkTypes.ColaLarge;

            case GameManager.DrinkTypes.OrangeSoda:
                if (size == GameManager.CupSize.Small) return GameManager.DrinkTypes.OrangeSodaSmall;
                if (size == GameManager.CupSize.Medium) return GameManager.DrinkTypes.OrangeSodaMedium;
                return GameManager.DrinkTypes.OrangeSodaLarge;

            case GameManager.DrinkTypes.LemonLime:
                if (size == GameManager.CupSize.Small) return GameManager.DrinkTypes.LemonLimeSmall;
                if (size == GameManager.CupSize.Medium) return GameManager.DrinkTypes.LemonLimeMedium;
                return GameManager.DrinkTypes.LemonLimeLarge;

            default:
                // Boyutu olmayan bir þeyse (Ayran vs.) olduðu gibi döndür
                return baseType;
        }
    }

    private void Awake()
    {
        if (streamLine != null)
        {
            streamLine.positionCount = 2;
            streamLine.gameObject.SetActive(false);
            streamLine.useWorldSpace = false;
            streamMat = streamLine.material;
        }

        interactableLayer = LayerMask.NameToLayer("Interactable");
        ungrabableLayer = LayerMask.NameToLayer("Ungrabable");
        grababaleLayer = LayerMask.NameToLayer("Grabable");

        machineAudioSource = GetComponent<AudioSource>();

        // Klibi ata
        if (machineWorkClip != null)
        {
            machineAudioSource.clip = machineWorkClip;
        }

        // Ýsteðin üzerine: Loop açýk, Ses 0, Oyun baþlar baþlamaz dönmeye baþlasýn.
        machineAudioSource.loop = true;
        machineAudioSource.volume = 0f;
        machineAudioSource.playOnAwake = true;

        if (!machineAudioSource.isPlaying)
        {
            machineAudioSource.Play();
        }
    }

    private void Start()
    {
        // Oyun baþlayýnca yýðýnlarý ayarla
        InitializeStack(smallCups);
        InitializeStack(mediumCups);
        InitializeStack(largeCups);
    }

    private void Update()
    {
        if (currentState != StreamState.Idle) HandleStreamVisuals();
    }

    public void OnButtonTriggered(SodaButton clickedBtn)
    {
        if (currentState != StreamState.Idle) return;
        StartCoroutine(PourRoutine(clickedBtn));
    }

    private IEnumerator PourRoutine(SodaButton btn)
    {
        // 1. BUTONLARI KÝLÝTLE
        SetButtonsState(false);

        // --- MOTOR SESÝ FADE IN ---
        if (machineAudioSource != null)
        {
            machineAudioSource.DOKill();
            machineAudioSource.DOFade(machineTargetVolume, machineFadeInTime);
        }

        // --- SÜRE VE DURUM AYARLAMASI ---
        // Varsayýlan olarak 1 saniye (Boþa akýtma süresi)
        float effectivePourDuration = pourDuration;

        // Eðer bardak VARSA süreyi ondan al
        if (currentCup != null)
        {
            effectivePourDuration = currentCup.GetFillDuration;

            currentCup.IsGettingFilled = true;
            currentCup.ChangeLayer(ungrabableLayer);
            if (PlayerManager.Instance != null) PlayerManager.Instance.ResetPlayerGrab(currentCup);
        }

        // 2. BUTONU ÝÇERÝ GÖM
        btn.PlayPressSound();
        btn.transform.DOKill();
        btn.transform.DOLocalMove(btn.initialPos + (Vector3.up * buttonPressDepth), 0.2f).SetEase(Ease.OutQuad);

        // 3. AKIÞI BAÞLAT
        StartPouring(btn.flavorIndex);

        // Rengi belirle (Hem bardak hem splash için lazým)
        Color selectedColor = Color.white;
        if (btn.flavorIndex >= 0 && btn.flavorIndex < flavorColors.Length)
            selectedColor = flavorColors[btn.flavorIndex];

        // --- SADECE BARDAK VARSA ÇALIÞACAK KISIMLAR ---
        if (currentCup != null)
        {
            // YENÝ: Boyuta göre spesifik tipi belirle
            GameManager.DrinkTypes finalDrinkType = GetSpecificDrinkType(btn.drinkType, currentCup.data.cupSize);

            // Görsel dolumu baþlat (Final tipi gönderiyoruz)
            float totalFillTime = effectivePourDuration + stopDuration - 0.1f;
            currentCup.StartFilling(selectedColor, totalFillTime, finalDrinkType);

            // --- DOLUM SESÝ (Sadece bardak varsa çalar) ---
            if (SoundManager.Instance != null && cupFillSounds != null && cupFillSounds.Length > 0)
            {
                int soundIndex = 0;
                // Bardaðýn süresine göre ses seçimi
                if (effectivePourDuration > 1.5f && effectivePourDuration <= 2.5f) soundIndex = 1;
                else if (effectivePourDuration > 2.5f) soundIndex = 2;

                if (soundIndex < cupFillSounds.Length && cupFillSounds[soundIndex] != null)
                {
                    SoundManager.Instance.PlaySoundFXWithRandomDelay(
                        cupFillSounds[soundIndex],
                        currentCup.transform, // Burada currentCup null olmadýðý garanti
                        fillVolume,
                        fillMinPitch,
                        fillMaxPitch,
                        soundStartDelay,
                        soundStartDelay
                    );
                }
            }
        }
        else
        {
            // Paralel bir Coroutine baþlatýyoruz ki ana akýþý bekletmesin
            StartCoroutine(HandleEmptyPourFX(effectivePourDuration, selectedColor));
        }

        // 4. BEKLE (1 saniye veya bardak süresi kadar)
        yield return new WaitForSeconds(effectivePourDuration);

        // --- MOTOR SESÝ FADE OUT ---
        if (machineAudioSource != null)
        {
            machineAudioSource.DOKill();
            machineAudioSource.DOFade(0f, machineFadeOutTime);
        }

        if (currentCup != null)
            currentCup.FinishGettingFilled();

        // 5. DURDURMA EVRESÝ
        BeginStopping();

        // 6. BUTONU GERÝ ÇIKAR
        btn.PlayReleaseSound();
        btn.transform.DOKill();
        btn.transform.DOLocalMove(btn.initialPos, stopDuration).SetEase(Ease.Linear);

        // 7. BEKLE
        yield return new WaitForSeconds(stopDuration);

        // 8. KAPAT
        FullStop();

        if (currentCup == null)
        {
            SetButtonsState(true);
        }
    }

    private void StartPouring(int flavorIndex)
    {
        currentState = StreamState.Pouring;
        currentStreamLength = 0f;
        currentStreamStart = 0f;

        if (flavorIndex >= 0 && flavorIndex < flavorColors.Length)
        {
            Color targetColor = flavorColors[flavorIndex];
            if (streamMat.HasProperty("_BaseColor")) streamMat.SetColor("_BaseColor", targetColor);
            else if (streamMat.HasProperty("_Color")) streamMat.SetColor("_Color", targetColor);

            streamLine.startColor = targetColor;
            streamLine.endColor = targetColor;
        }

        streamLine.gameObject.SetActive(true);
    }

    private void BeginStopping()
    {
        currentState = StreamState.Stopping;
    }

    private void FullStop()
    {
        currentState = StreamState.Idle;
        streamLine.gameObject.SetActive(false);
    }

    private void HandleStreamVisuals()
    {
        if (streamLine == null) return;

        if (currentState == StreamState.Pouring)
        {
            currentStreamStart = 0f;
            currentStreamLength = Mathf.MoveTowards(currentStreamLength, targetLength, Time.deltaTime * pourSpeed);
        }
        else if (currentState == StreamState.Stopping)
        {
            float dropSpeed = targetLength / stopDuration;
            currentStreamStart = Mathf.MoveTowards(currentStreamStart, targetLength, Time.deltaTime * dropSpeed);
        }

        streamLine.SetPosition(0, new Vector3(0, -currentStreamStart, 0));
        streamLine.SetPosition(1, new Vector3(0, -currentStreamLength, 0));
    }

    public void HandleCatch(Collider other) // OnTrigger vazifesi görüyor
    {
        // 1. Zaten yuvada bardak varsa alma
        if (currentCup != null) return;

        // --- EKLENECEK KOD ---
        // Eðer makine þu an boþta deðilse (Pouring veya Stopping ise), 
        // bardak yerleþtirmeyi reddet.
        if (currentState != StreamState.Idle) return;
        // ---------------------

        DrinkCup incomingCup = other.GetComponent<DrinkCup>();
        if (incomingCup == null) return;
        
        // Elimizde tutuyorsak yuvaya yapýþmasýn
        if (incomingCup.IsGrabbed) return;
        
        // Zaten doluysa tekrar girmesin
        if (incomingCup.IsFull) return;

        // Kapak kontrolünü de eklediysen:
        if (incomingCup.HasLid) return;

        SnapCupToMachine(incomingCup);
    }

    private void SnapCupToMachine(DrinkCup cup)
    {
        currentCup = cup;
        cup.currentMachine = this;
        Rigidbody rb = cup.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }

        // 1. Önce parent yapýyoruz
        cup.transform.SetParent(cupSnapPoint);

        // 2. Apex noktasýnýn Local pozisyonunu hesapla
        // (Apex transformu null ise güvenlik için dümdüz yukarýda bir nokta seçer)
        Vector3 localApexPos = Vector3.up * 0.2f;
        if (cupPlacementApex != null)
        {
            // "Apex'in dünya pozisyonunu, SnapPoint'in yerel koordinatýna çevir"
            localApexPos = cupSnapPoint.InverseTransformPoint(cupPlacementApex.position);
        }

        // 3. Yol haritasýný oluþtur: [Tepe Noktasý, Hedef Nokta(0,0,0)]
        Vector3[] pathPoints = new Vector3[] { localApexPos, Vector3.zero };

        // 4. Hareketi baþlat (CatmullRom yumuþak bir yay çizer)
        // Süreyi 0.2'den 0.35'e çýkardým ki yay hareketi gözle görülsün.
        cup.transform.DOLocalPath(pathPoints, 0.2f, PathType.CatmullRom)
            .SetEase(Ease.OutSine)
            .OnComplete(() =>
            {
                SoundManager.Instance.PlaySoundFX(cupPlaceSound, cup.transform, cupPlaceVolume, cupPlaceMinPitch, cupPlaceMaxPitch);
                // Garanti olsun diye tam 0 noktasýna oturt
                cup.transform.localPosition = Vector3.zero;
                cup.FinishPuttingOnSodaMachine();
            });

        // Rotasyonu da ayný sürede düzelt
        cup.transform.DOLocalRotate(Vector3.zero, 0.35f).SetEase(Ease.OutBack);
    }

    public void ReleaseCup()
    {
        currentCup = null;

        SetButtonsState(true);
    }

    private void InitializeStack(System.Collections.Generic.List<DrinkCup> stack)
    {
        if (stack == null || stack.Count == 0) return;

        for (int i = 0; i < stack.Count; i++)
        {
            DrinkCup cup = stack[i];
            if (cup == null) continue;

            // Bardaða referans ver
            cup.SodaMachineSC = this;
            cup.IsInCupHolder = true;

            // Eðer listenin baþýndaysa (0) alýnabilir olsun, deðilse kilitli olsun.
            if (i == 0)
            {
                cup.ChangeLayer(grababaleLayer); // En üstteki
            }
            else
            {
                cup.ChangeLayer(ungrabableLayer); // Alttakiler
            }
        }
    }

    // Bardak tarafýndan çaðrýlan fonksiyon
    public void OnCupRemovedFromStack(DrinkCup cup)
    {
        // Hangi listede olduðunu bul ve iþlemi yap
        if (TryRemoveFromStack(smallCups, cup)) return;
        if (TryRemoveFromStack(mediumCups, cup)) return;
        if (TryRemoveFromStack(largeCups, cup)) return;
    }

    private bool TryRemoveFromStack(System.Collections.Generic.List<DrinkCup> stack, DrinkCup cupToRemove)
    {
        if (stack.Contains(cupToRemove))
        {
            // 1. Bardaðý listeden düþ
            stack.Remove(cupToRemove);

            // 2. Eðer listede hala bardak varsa, yeni en üsttekini (0. index) aktif et
            if (stack.Count > 0 && stack[0] != null)
            {
                stack[0].ChangeLayer(grababaleLayer);
            }
            return true; // Bulduk ve sildik
        }
        return false; // Bu listede deðilmiþ
    }

    // --- YENÝ EKLENEN COROUTINE: BOÞ AKITMA EFEKTLERÝ ---
    private IEnumerator HandleEmptyPourFX(float duration, Color liquidColor)
    {
        // 1. Sývýnýn yere düþmesini bekle
        yield return new WaitForSeconds(splashDelay);

        GameObject tempSplashObj = null;

        // 2. Partikül Efektini Baþlat
        if (splashFXPrefab != null && splashPoint != null)
        {
            tempSplashObj = Instantiate(splashFXPrefab, splashPoint.position, Quaternion.identity, splashPoint);

            // Rengi içeceðe göre ayarla
            ParticleSystem ps = tempSplashObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                var main = ps.main;
                main.startColor = liquidColor;
            }
        }

        // 3. Sesi Çal
        // Delay'i zaten yukarýda WaitForSeconds ile yaptýk, o yüzden buradaki delay 0.
        if (SoundManager.Instance != null && splashSound != null && splashPoint != null)
        {
            SoundManager.Instance.PlaySoundFXWithRandomDelay(
                splashSound,
                splashPoint,
                splashVolume,
                splashMinPitch,
                splashMaxPitch,
                0f, 0f // Zaten bekledik, anýnda çalsýn
            );
        }

        // 4. Akýþýn geri kalaný kadar bekle
        // Toplam süre - geçen süre (delay)
        float remainingTime = duration - splashDelay;
        if (remainingTime > 0)
            yield return new WaitForSeconds(remainingTime);

        // 5. Partikülü Durdur ve Yok Et
        if (tempSplashObj != null)
        {
            ParticleSystem ps = tempSplashObj.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(); // Emission durur, mevcutlar bitince biter
            }
            // Garanti temizlik
            Destroy(tempSplashObj, 2.0f);
        }
    }

    // --- YENÝ HELPER: Butonlarýn durumunu toplu deðiþtir ---
    private void SetButtonsState(bool isActive)
    {
        foreach (var b in buttons)
        {
            b.CanInteract = isActive;

            // Eðer aktifse normal layer, pasifse player interact edemesin
            if (isActive)
            {
                b.ChangeLayer(interactableLayer);
            }
            else
            {
                // Buton kilitliyken basýlmasýný engellemek için Interact resetliyoruz
                if (PlayerManager.Instance != null) PlayerManager.Instance.ResetPlayerInteract(b, true);
            }
        }
    }
}