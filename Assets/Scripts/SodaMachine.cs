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
    [SerializeField] private float pourDuration = 3.0f;
    [SerializeField] private float stopDuration = 0.3f;
    [SerializeField] private float buttonPressDepth = 0.02f;

    [Header("Cup Placement")]
    [SerializeField] private Transform cupSnapPoint;
    [SerializeField] private Transform cupPlacementApex;
    private DrinkCup currentCup;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;

    private enum StreamState { Idle, Pouring, Stopping }
    private StreamState currentState = StreamState.Idle;
    private float currentStreamLength = 0f;
    private float currentStreamStart = 0f;
    private Material streamMat;

    private int interactableLayer;
    private int ungrabableLayer;
    private int grababaleLayer;

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
        foreach (var b in buttons)
        {
            b.CanInteract = false;
            if (PlayerManager.Instance != null) PlayerManager.Instance.ResetPlayerInteract(b, true);
        }

        // --- YENÝ: SÜRE HESAPLAMA ---
        // Varsayýlan olarak makinenin ayarýný al
        float effectivePourDuration = pourDuration;

        // Eðer bardak varsa, bardaðýn datasýndaki süreyi al
        if (currentCup != null)
        {
            effectivePourDuration = currentCup.GetFillDuration; // <--- DEÐÝÞÝKLÝK BURADA

            currentCup.IsGettingFilled = true;
            currentCup.ChangeLayer(ungrabableLayer);
            if (PlayerManager.Instance != null) PlayerManager.Instance.ResetPlayerGrab(currentCup);
        }
        // -----------------------------

        // 2. BUTONU ÝÇERÝ GÖM
        btn.transform.DOKill();
        btn.transform.DOLocalMove(btn.initialPos + (Vector3.up * buttonPressDepth), 0.2f).SetEase(Ease.OutQuad);

        // 3. AKIÞI BAÞLAT
        StartPouring(btn.flavorIndex);

        if (currentCup != null)
        {
            Color selectedColor = Color.white;
            if (btn.flavorIndex >= 0 && btn.flavorIndex < flavorColors.Length)
                selectedColor = flavorColors[btn.flavorIndex];

            // Hesabý "effectivePourDuration" üzerinden yapýyoruz
            float totalFillTime = effectivePourDuration + stopDuration - 0.1f;

            currentCup.StartFilling(selectedColor, totalFillTime);
        }

        // 4. BEKLE (Artýk dinamik süreyi bekliyoruz)
        yield return new WaitForSeconds(effectivePourDuration); // <--- DEÐÝÞÝKLÝK BURADA

        if (currentCup != null)
            currentCup.FinishGettingFilled();

        // 5. DURDURMA EVRESÝ
        BeginStopping();

        // 6. BUTONU GERÝ ÇIKAR
        btn.transform.DOKill();
        btn.transform.DOLocalMove(btn.initialPos, stopDuration).SetEase(Ease.Linear);

        // 7. BEKLE (Stopping Süresi)
        yield return new WaitForSeconds(stopDuration);

        // 8. KAPAT
        FullStop();

        // 9. KÝLÝTLERÝ AÇ
        foreach (var b in buttons)
        {
            b.ChangeLayer(interactableLayer);
            b.CanInteract = true;
        }
    }

    // ... (StartPouring, StopPouring, HandleStreamVisuals, OnTriggerEnter, SnapCupToMachine, ReleaseCup ayný) ...
    // ... Bu kýsýmlar deðiþmediði için tekrar kopyalamýyorum, önceki cevabýn aynýsý ...

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
        if (audioSource) audioSource.Play();
    }

    private void BeginStopping()
    {
        currentState = StreamState.Stopping;
    }

    private void FullStop()
    {
        currentState = StreamState.Idle;
        streamLine.gameObject.SetActive(false);
        if (audioSource) audioSource.Stop();
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
}