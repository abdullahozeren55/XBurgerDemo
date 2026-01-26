using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Febucci.UI;
using System;

public enum TypewriterSoundType
{
    None,
    Soft,   // Pýt pýt (Normal konuþma)
    Medium, // Tak tuk (Sert adam)
    Hard    // Çat Çut (Daktilo veya Robot)
}

[System.Serializable]
public struct TypewriterSoundProfile
{
    public TypewriterSoundType type;
    public AudioClip clipA; // Dýt
    public AudioClip clipB; // Düt

    [Range(0f, 1f)] public float volume;
    public float minPitch;
    public float maxPitch;
}
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Typewriter Audio Settings")]
    [SerializeField] private List<TypewriterSoundProfile> soundProfiles;

    // Hýzlý eriþim için Dictionary
    private Dictionary<TypewriterSoundType, TypewriterSoundProfile> soundProfileMap;

    // Sesin A mý B mi olduðunu takip etmek için
    private bool useClipA = true;

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

    // YENÝ: Þu anki satýrýn datasýna Update'den eriþmek için
    private DialogueData.DialogueLine currentLineData;

    // YENÝ: Delayleri durdurabilmek için Coroutine referanslarý
    private Coroutine textDelayRoutine;
    private Coroutine jumpscareDelayRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Baþlangýçta tüm diyalog animatorleri kapat
        foreach (var dialogAnim in dialogueAnimatorPool) dialogAnim.ForceHide();

        // Listeyi Dictionary'e çevir (Performans)
        soundProfileMap = new Dictionary<TypewriterSoundType, TypewriterSoundProfile>();
        foreach (var profile in soundProfiles)
        {
            if (!soundProfileMap.ContainsKey(profile.type))
                soundProfileMap.Add(profile.type, profile);
        }
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        if (InputManager.Instance != null && InputManager.Instance.PlayerInteract())
        {
            // 1. Durum: Yazý hala yazýlýyor
            if (currentActiveDialogueAnimator != null && currentActiveDialogueAnimator.IsTyping())
            {
                // YENÝ: Eðer Skippable ise geç, deðilse basma tuþa
                if (currentLineData != null && currentLineData.IsSkippable)
                {
                    currentActiveDialogueAnimator.SkipTypewriter();
                }
            }
            // 2. Durum: Yazý bitti, sonraki satýra geçmek isteniyor
            else
            {
                // Yazý bittiyse her türlü geçebilir (veya buraya da istersen delay koyabilirsin)
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
        // Önceki satýrdan kalan bekleyen iþleri (Delayleri) iptal et
        if (textDelayRoutine != null) StopCoroutine(textDelayRoutine);
        if (jumpscareDelayRoutine != null) StopCoroutine(jumpscareDelayRoutine);

        if (currentActiveDialogueAnimator != null)
        {
            currentActiveDialogueAnimator.Hide();
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
        currentLineData = line; // Update'de eriþmek için kaydet

        // 1. Konuþmacýyý Bul
        IDialogueSpeaker activeSpeaker = null; // Yerel deðiþken yaptýk
        if (speakers.ContainsKey(line.SpeakerID))
        {
            activeSpeaker = speakers[line.SpeakerID];
            // Burada OnSpeakStart'ý çaðýrmýyoruz, yazý baþlayýnca çaðýracaðýz (Opsiyonel)
        }

        // 2. HAVUZDAN BALON SEÇ
        DialogueAnimator selectedDialogueAnimator = GetAvailableDialogueAnimator();
        currentActiveDialogueAnimator = selectedDialogueAnimator; // Artýk yeni patron bu

        selectedDialogueAnimator.SetColor(line.TextColor);

        selectedDialogueAnimator.SetSpeed(line.TypewriterSpeedMultiplier);

        // 2. Ses Sýrasýný Sýfýrla
        useClipA = true;

        bool hasJumpscare = (line.Jumpscare != JumpscareType.None);

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
                (currentIndex == 0), // isInstant
                hasJumpscare         // <--- YENÝ: skipLensAndNoise
            );

            if (line.Events != DialogueEvent.None)
            {
                HandleDialogueEvents(line.Events);
            }

            // --- JUMPSCARE (DELAYLI) ---
            if (line.Jumpscare != JumpscareType.None)
            {
                jumpscareDelayRoutine = StartCoroutine(JumpscareRoutine(line.JumpscareDelay, line.Jumpscare));
            }

            // --- METÝN GÖSTERÝMÝ (DELAYLI) ---
            textDelayRoutine = StartCoroutine(TextRoutine(line.TextDelay, line, selectedDialogueAnimator));
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

    private IEnumerator TextRoutine(float delay, DialogueData.DialogueLine line, DialogueAnimator dialogueAnimator)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        string finalList = LocalizationManager.Instance.GetText(line.LocalizationKey);
        dialogueAnimator.Show(finalList);
    }

    private IEnumerator JumpscareRoutine(float delay, JumpscareType type)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        if (CameraManager.Instance != null)
            CameraManager.Instance.TriggerJumpscare(type);
    }

    public void PlayNextTypewriterSound()
    {
        // Eðer þu anki satýrýn ses ayarý 'None' ise veya data yoksa çalma
        if (currentLineData == null || currentLineData.SoundType == TypewriterSoundType.None) return;

        // Profili bul
        if (!soundProfileMap.ContainsKey(currentLineData.SoundType)) return;
        TypewriterSoundProfile profile = soundProfileMap[currentLineData.SoundType];

        // A/B Seçimi
        AudioClip clipToPlay = useClipA ? profile.clipA : profile.clipB;

        // Çal
        if (SoundManager.Instance != null && clipToPlay != null)
        {
            SoundManager.Instance.PlayTypewriterSoundFX(
                clipToPlay,
                profile.volume,
                profile.minPitch,
                profile.maxPitch
            );
        }

        // Sýrayý deðiþtir
        useClipA = !useClipA;
    }
}