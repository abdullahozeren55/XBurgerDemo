using DG.Tweening;
using System.Collections;
using UnityEngine;

public class SodaMachine : MonoBehaviour
{
    // ... (Deðiþkenler ayný) ...
    [Header("Components")]
    [SerializeField] private SodaButton[] buttons;
    [SerializeField] private LineRenderer streamLine;

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

        if (currentCup != null)
        {
            currentCup.IsGettingFilled = true;
            currentCup.ChangeLayer(ungrabableLayer);
            if (PlayerManager.Instance != null) PlayerManager.Instance.ResetPlayerGrab(currentCup);
        }

        // 2. BUTONU ÝÇERÝ GÖM
        btn.transform.DOKill();
        btn.transform.DOLocalMove(btn.initialPos + (Vector3.up * buttonPressDepth), 0.2f).SetEase(Ease.OutQuad);

        // 3. AKIÞI BAÞLAT
        StartPouring(btn.flavorIndex);

        // --- YENÝ: BARDAÐI DOLDURMA EMRÝ ---
        if (currentCup != null)
        {
            // Hangi rengi seçtik?
            Color selectedColor = Color.white;
            if (btn.flavorIndex >= 0 && btn.flavorIndex < flavorColors.Length)
                selectedColor = flavorColors[btn.flavorIndex];

            // Toplam süre = Akýþ Süresi + Durma (Damlama) - 0.1f (for juice) Süresi
            float totalFillTime = pourDuration + stopDuration - 0.1f;

            // Bardaða "Baþla" de
            currentCup.StartFilling(selectedColor, totalFillTime);
        }
        // ------------------------------------

        // 4. BEKLE
        yield return new WaitForSeconds(pourDuration);

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

    private void OnTriggerEnter(Collider other)
    {
        if (currentCup != null) return;
        DrinkCup incomingCup = other.GetComponent<DrinkCup>();
        if (incomingCup == null) return;
        if (incomingCup.IsGrabbed) return;
        if (incomingCup.IsFull) return;

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
        cup.transform.SetParent(cupSnapPoint);
        cup.transform.DOLocalMove(Vector3.zero, 0.2f).SetEase(Ease.OutBack).OnComplete(() =>
        {
            cup.FinishPuttingOnSodaMachine();
        });
        cup.transform.DOLocalRotate(Vector3.zero, 0.2f).SetEase(Ease.OutBack);
    }

    public void ReleaseCup()
    {
        currentCup = null;
    }
}