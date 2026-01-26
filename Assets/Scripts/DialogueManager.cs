using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Febucci.UI;
using System;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Glitch Settings")]
    [SerializeField] private AudioClip glitchSFX;
    [SerializeField] private AudioSource sfxSource;

    [Header("UI Pool System")]
    [SerializeField] private List<DialogueAnimator> dialogueAnimatorPool; // Inspector'dan 4 tane balon ata

    // Aktif olarak en son kullanýlan (þu an ekranda en taze olan) balon
    private DialogueAnimator currentActiveDialogueAnimator;

    // String yerine Enum Key kullanýyoruz
    private Dictionary<CustomerID, IDialogueSpeaker> speakers = new Dictionary<CustomerID, IDialogueSpeaker>();

    private DialogueData currentData;
    private int currentIndex;
    private bool isDialogueActive;
    private IDialogueSpeaker currentSpeaker;
    private Action onDialogueCompleteCallback;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Baþlangýçta tüm diyalog animatorleri kapat
        foreach (var dialogAnim in dialogueAnimatorPool) dialogAnim.ForceHide();
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        // InputManager entegrasyonu
        if (InputManager.Instance != null && InputManager.Instance.PlayerInteract())
        {
            // Eðer þu an aktif olan balon hala yazýyorsa -> Skip
            if (currentActiveDialogueAnimator != null && currentActiveDialogueAnimator.IsTyping())
            {
                currentActiveDialogueAnimator.SkipTypewriter();
            }
            else
            {
                // Yazma bittiyse -> Sonraki satýra geç (Öncekini Disappear yaparak)
                AdvanceDialogue();
            }
        }
    }

    // --- SPEAKER KAYIT SÝSTEMÝ (ENUM) ---
    public void RegisterSpeaker(IDialogueSpeaker speaker)
    {
        if (!speakers.ContainsKey(speaker.SpeakerID))
            speakers.Add(speaker.SpeakerID, speaker);
    }

    public void UnregisterSpeaker(IDialogueSpeaker speaker)
    {
        if (speakers.ContainsKey(speaker.SpeakerID))
            speakers.Remove(speaker.SpeakerID);
    }

    public void StartDialogue(DialogueData data, Action onComplete = null)
    {
        currentData = data;
        onDialogueCompleteCallback = onComplete;
        currentIndex = -1;
        isDialogueActive = true;

        if (PlayerManager.Instance != null) PlayerManager.Instance.SetPlayerBasicMovements(false);

        if (CameraManager.Instance != null)
        {
            CameraManager.Instance.StartDialogueMode();

            // --- YENÝ: KONUÞAN KÝÞÝNÝN KAFASINI KAMERAYA ÇEVÝR ---
            // Ýlk satýrýn konuþmacýsýný bulalým
            if (data.lines.Count > 0)
            {
                var firstSpeakerID = data.lines[0].SpeakerID;
                if (speakers.ContainsKey(firstSpeakerID))
                {
                    // Eðer konuþan kiþi bir CustomerController ise (veya component'e sahipse)
                    var speakerObj = speakers[firstSpeakerID] as MonoBehaviour;
                    if (speakerObj != null)
                    {
                        var customer = speakerObj.GetComponent<CustomerController>();
                        if (customer != null)
                        {
                            // KAMERAYA BAK EMRÝ
                            customer.SetLookTarget(CameraManager.Instance.GetDialogueCameraTransform());
                        }
                    }
                }
            }
        }

        AdvanceDialogue();
    }

    private void AdvanceDialogue()
    {
        // 1. Önceki balon varsa, ona "Kaybolmaya Baþla" emri ver
        if (currentActiveDialogueAnimator != null)
        {
            currentActiveDialogueAnimator.Hide();
            // Hide() içinde StartDisappearingText çaðrýlýyor.
            // Balon hemen kapanmayacak, animasyonla gidecek.
            // IsBusy deðeri ancak animasyon bitince false olacak.
        }

        currentIndex++;

        if (currentIndex >= currentData.lines.Count)
        {
            EndDialogue();
            return;
        }

        PlayLine(currentData.lines[currentIndex]);
    }

    private void PlayLine(DialogueData.DialogueLine line)
    {
        // 1. Konuþmacýyý Bul (Sadece ses ve kamera için lazým artýk)
        if (speakers.ContainsKey(line.SpeakerID))
        {
            currentSpeaker = speakers[line.SpeakerID];
        }

        // 2. HAVUZDAN BALON SEÇ
        DialogueAnimator selectedDialogueAnimator = GetAvailableDialogueAnimator();
        currentActiveDialogueAnimator = selectedDialogueAnimator; // Artýk yeni patron bu

        selectedDialogueAnimator.SetColor(line.TextColor);

        // 3. Metni Göster
        if (line.IsGlitchLine)
        {
            StartCoroutine(HandleGlitchRoutine(line, selectedDialogueAnimator));
        }
        else
        {
            string finalList = LocalizationManager.Instance.GetText(line.LocalizationKey);
            selectedDialogueAnimator.Show(finalList);
        }

        // 4. Ses Efekti ve Kamera (Aynen Kalýyor)
        if (line.CustomVoiceOrSFX != null) sfxSource.PlayOneShot(line.CustomVoiceOrSFX);

        // --- CAMERA JUICE (GÜNCELLENDÝ) ---
        if (CameraManager.Instance != null)
        {
            Transform targetTrans = null;
            CustomerID targetID = (line.FocusTargetID != CustomerID.None) ? line.FocusTargetID : line.SpeakerID;

            if (speakers.ContainsKey(targetID))
            {
                if (speakers[targetID] is DialogueSpeaker ds) targetTrans = ds.LookAtPoint;
                else if (speakers[targetID] is MonoBehaviour mb) targetTrans = mb.transform;
            }

            // --- ÝLK SATIR KONTROLÜ ---
            // Eðer þu an 0. satýrý oynatýyorsak, kamera IÞINLANSIN (Instant).
            // Sonraki satýrlarda YUMUÞAK (Tween) geçsin.
            bool isFirstLine = (currentIndex == 0);

            CameraManager.Instance.UpdateDialogueShot(
                targetTrans,
                line.CamMoveDuration,
                line.CamMoveEase,
                line.DutchAngle,
                line.TargetFOV,
                line.NoiseType,
                line.NoiseAmplitudeMultiplier,
                line.NoiseFrequencyMultiplier,
                isFirstLine // <--- BURASI KRÝTÝK
            );

            if (line.Events != DialogueEvent.None)
            {
                HandleDialogueEvents(line.Events);
            }
        }
    }

    // --- HAVUZ MANTIÐI ---
    private DialogueAnimator GetAvailableDialogueAnimator()
    {
        // 1. Öncelik: Hiç meþgul olmayan (Animasyonu bitmiþ, kapalý) bir animator bul
        foreach (var dialogueAnimator in dialogueAnimatorPool)
        {
            if (!dialogueAnimator.IsBusy) return dialogueAnimator;
        }

        // 2. Durum: Hepsi meþgul (Çok hýzlý geçildi). 
        // O zaman Disappear animasyonu bitmeye EN YAKIN olaný (yani listedeki en eski active olaný) bulup resetleyelim.
        // Basit çözüm: Listede sýrayla dönüyoruz ya, muhtemelen index 0 en eskidir.
        // Ama biz yine de aktif olanlardan birini kurban seçelim.

        Debug.LogWarning("Tüm diyalog animatorleri meþgul! Birini zorla sýfýrlýyorum.");

        // Ýlkini al, zorla kapat ve onu ver.
        var victim = dialogueAnimatorPool[0];
        victim.ForceHide();
        return victim;
    }

    private IEnumerator HandleGlitchRoutine(DialogueData.DialogueLine line, DialogueAnimator dialogueAnimator)
    {
        // Glitch mantýðý artýk bubble.Show() üzerinden çalýþacak
        // ...
        yield return null;
    }

    private void EndDialogue()
    {
        isDialogueActive = false;
        // Son balon da kaybolsun
        if (currentActiveDialogueAnimator != null)
        {
            currentActiveDialogueAnimator.Hide();
        }

        if (PlayerManager.Instance != null) PlayerManager.Instance.SetPlayerBasicMovements(true);

        if (CameraManager.Instance != null) CameraManager.Instance.EndDialogueMode();

        // --- YENÝ: HERKESÝN KAFASINI SERBEST BIRAK ---
        // Sahnedeki tüm kayýtlý konuþmacýlarý gez ve eðer müþteriyseler bakýþlarýný sýfýrla
        foreach (var kvp in speakers)
        {
            var speakerObj = kvp.Value as MonoBehaviour;
            if (speakerObj != null)
            {
                var customer = speakerObj.GetComponent<CustomerController>();
                if (customer != null)
                {
                    customer.ClearLookTarget(); // Player'a dön
                }
            }
        }

        onDialogueCompleteCallback?.Invoke();
        onDialogueCompleteCallback = null;
    }

    // --- YENÝ EVENT MANTIÐI ---
    private void HandleDialogueEvents(DialogueEvent events)
    {
        // 1. Iþýklar Titreyecek mi?
        if (events.HasFlag(DialogueEvent.LightFlicker))
        {
            // Örn: LightManager.Instance.FlickerLights();
            Debug.Log("EVENT: Iþýklar titriyor...");
        }

        // 2. Iþýklar Sönecek mi?
        if (events.HasFlag(DialogueEvent.LightsOff))
        {
            // Örn: LightManager.Instance.Blackout(true);
            Debug.Log("EVENT: Zifiri karanlýk...");
        }

        // 3. Kapý Çarpacak mý?
        if (events.HasFlag(DialogueEvent.DoorSlam))
        {
            // Örn: WorldManager.Instance.SlamAllDoors();
            Debug.Log("EVENT: Kapýlar çarpýyor!");
        }

        // 4. Jumpscare?
        if (events.HasFlag(DialogueEvent.SpawnJumpscare))
        {
            // Örn: CustomerManager.Instance.SpawnStalker();
            Debug.Log("EVENT: Bir þeyler ters gidiyor...");
        }

        // ... Diðerlerini de buraya eklersin ...
    }
}