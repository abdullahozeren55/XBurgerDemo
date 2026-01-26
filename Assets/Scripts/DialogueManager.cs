using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Febucci.UI;
using System;
using DG.Tweening;

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

[System.Flags]
public enum RichTextTag
{
    None = 0,
    Shake = 1 << 0,    // <shake>Titreme</shake>
    Wave = 1 << 1,     // <wave>Dalga</wave>
    Wiggle = 1 << 2,   // <wiggle>Kýpýrkýpýr</wiggle>
    RedColor = 1 << 3, // <color=red>Kýrmýzý</color>
    GlitchFont = 1 << 4 // <font="GlitchSDF">Font deðiþimi</font> (Varsa)
}

public enum LineBehavior
{
    Normal,         // Standart satýr. Týkla geç.
    Meltdown,       // Normal baþlar, Glitch 0->2 artar, SONRAKÝ SATIRA OTOMATÝK ATLAR. (Skippable: False)
    HorrorReveal,   // Glitchli (0->0.4) baþlar, sonunda (2.0) patlar, SONRAKÝ SATIRA OTOMATÝK ATLAR. (Skippable: False)
    InstantRecover  // Typewriter yok. Pat diye belirir. Normal input bekler.
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

    [Header("Glitch Audio Settings")]
    [SerializeField] private AudioSource glitchAudioSource; // Child objeyi buraya sürükle
    [Range(0f, 1f)][SerializeField] private float glitchMaxVolume = 0.5f;
    [Range(0.5f, 2f)][SerializeField] private float glitchMinPitch = 0.8f;
    [Range(0.5f, 2f)][SerializeField] private float glitchMaxPitch = 1.2f;

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

        // Baþlangýçta glitch sesini sustur ve loop'a al
        if (glitchAudioSource != null)
        {
            glitchAudioSource.loop = true;
            glitchAudioSource.volume = 0f;
            glitchAudioSource.Stop(); // Þimdilik dursun
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
        // Önceki satýrdan kalan bekleyen iþleri iptal et
        if (textDelayRoutine != null) StopCoroutine(textDelayRoutine);
        if (jumpscareDelayRoutine != null) StopCoroutine(jumpscareDelayRoutine);

        // --- KRÝTÝK DÜZELTME BURADA ---
        if (currentActiveDialogueAnimator != null)
        {
            bool shouldHardCut = false;

            // Eðer þu an biten satýrýn verisi varsa kontrol et
            if (currentLineData != null)
            {
                // Eðer Meltdown veya HorrorReveal ise -> BIÇAK GÝBÝ KES (ForceHide)
                // Çünkü bu modlarda süre dolunca "PAT" diye kesilip diðerine geçmesi gerekir.
                // Fade-out animasyonu glitch etkisini yumuþatýr ve senin yaþadýðýn bug'a sebep olur.
                if (currentLineData.Behavior == LineBehavior.Meltdown ||
                    currentLineData.Behavior == LineBehavior.HorrorReveal)
                {
                    shouldHardCut = true;
                }
            }

            if (shouldHardCut)
            {
                currentActiveDialogueAnimator.ForceHide();
            }
            else
            {
                // Normal satýrsa nazikçe silinsin
                currentActiveDialogueAnimator.Hide();
            }
        }
        // -----------------------------

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

        if (glitchAudioSource != null) glitchAudioSource.volume = 0f;

        // 2. Ses Sýrasýný Sýfýrla
        useClipA = true;

        string rawText = LocalizationManager.Instance.GetText(line.LocalizationKey);
        string processedText = selectedDialogueAnimator.ApplyRichText(rawText, line.TextEffects);

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

            // 4. BEHAVIOR LOGIC (YENÝ KISIM)

            // Önceki Glitch Tweenlerini temizle (Eðer varsa)
            selectedDialogueAnimator.transform.DOKill();

            switch (line.Behavior)
            {
                case LineBehavior.Normal:
                    StopGlitchAudio(0.1f);
                    // Standart akýþ
                    selectedDialogueAnimator.SetGlitchStrength(0f); // Temizle
                    textDelayRoutine = StartCoroutine(TextRoutine(line.TextDelay, line, selectedDialogueAnimator, processedText));
                    break;

                case LineBehavior.Meltdown:
                    // Normal baþla -> Sonra Glitchle -> Sonra Yok Ol
                    selectedDialogueAnimator.SetGlitchStrength(0f);
                    // Burada Typewriter normal çalýþacak
                    textDelayRoutine = StartCoroutine(TextRoutine(line.TextDelay, line, selectedDialogueAnimator, processedText));

                    // Meltdown Özel Rutini: Yazý bitimine yakýn veya belli sürede glitchi artýr
                    StartCoroutine(MeltdownRoutine(selectedDialogueAnimator, line.AutoAdvanceDuration));
                    break;

                case LineBehavior.HorrorReveal:
                    // Glitchli baþla -> Hafif oyna -> Sonunda Patla
                    selectedDialogueAnimator.SetGlitchStrength(0f);
                    // Horror reveal genelde yavaþ olur, hýz ayarýný datadan yaparsýn.
                    textDelayRoutine = StartCoroutine(TextRoutine(line.TextDelay, line, selectedDialogueAnimator, processedText));

                    // Horror Özel Rutini
                    StartCoroutine(HorrorRevealRoutine(selectedDialogueAnimator, line.AutoAdvanceDuration));
                    break;

                case LineBehavior.InstantRecover:
                    StopGlitchAudio(0.1f);
                    // Typewriter yok, anýnda belirir, Glitch sýfýr.
                    selectedDialogueAnimator.SetGlitchStrength(0f);
                    // Delay varsa bekle, sonra PAT diye göster
                    textDelayRoutine = StartCoroutine(InstantTextRoutine(line.TextDelay, line, selectedDialogueAnimator, processedText));
                    break;
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

    // --- YENÝ RUTÝNLER ---

    private IEnumerator MeltdownRoutine(DialogueAnimator anim, float duration)
    {
        // Sürenin %70'i güvenli
        float safeTime = duration * 0.7f;
        float glitchTime = duration * 0.3f;

        // Baþlangýçta ses yok
        if (glitchAudioSource != null) glitchAudioSource.volume = 0f;

        yield return new WaitForSeconds(safeTime);

        // --- GLITCH BAÞLIYOR ---
        StartGlitchAudio(); // Sesi baþlat (Volume 0'da)

        // Görsel bozulma
        anim.TweenGlitch(2.0f, glitchTime, Ease.InExpo);

        // Ýþitsel bozulma (Sesi aç)
        TweenGlitchVolume(glitchMaxVolume, glitchTime);

        yield return new WaitForSeconds(glitchTime);

        // Süre bitti, AdvanceDialogue çaðrýlacak.
        // AdvanceDialogue içindeki ForceHide sesi aniden kesmemeli, 
        // bir sonraki satýr (InstantRecover) sesi susturacak.
        AdvanceDialogue();
    }

    private IEnumerator HorrorRevealRoutine(DialogueAnimator anim, float duration)
    {
        // --- BAÞLANGIÇ ---
        // HorrorReveal baþlar baþlamaz hafif bir cýzýrtý olsun
        StartGlitchAudio();

        // Hafif Glitch (Görsel 0.4, Ses %30)
        anim.TweenGlitch(0.4f, duration * 0.8f, Ease.Linear);
        TweenGlitchVolume(glitchMaxVolume * 0.3f, duration * 0.8f); // Hafif ses

        // Süre sonuna kadar bekle
        yield return new WaitForSeconds(duration);

        // --- FÝNAL PATLAMASI ---
        // Aniden max glitch ve max ses
        anim.SetGlitchStrength(2.0f);
        if (glitchAudioSource != null) glitchAudioSource.volume = glitchMaxVolume;

        yield return null;

        AdvanceDialogue();
    }

    private IEnumerator InstantTextRoutine(float delay, DialogueData.DialogueLine line, DialogueAnimator anim, string text)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        // Instant = true parametresiyle çaðýr
        anim.Show(text);
        anim.SkipTypewriter();

        // Ses çal (Genelde recover'da ses olmaz ama data ne derse o)
        if (line.SoundType != TypewriterSoundType.None)
        {
            // Instant olduðu için tek bir 'bip' çalabiliriz veya hiç çalmayýz.
            // PlayNextTypewriterSound(); 
        }
    }

    // TextRoutine biraz sadeleþti (String'i parametre olarak alýyor)
    private IEnumerator TextRoutine(float delay, DialogueData.DialogueLine line, DialogueAnimator anim, string text)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        anim.Show(text); // Typewriter aktif

        // Ses baþlatma (Eski kodun aynýsý)
        if (line.SoundType != TypewriterSoundType.None)
            PlayNextTypewriterSound(); // Ýlk harf için tetik (veya Febucci eventi halleder)
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

    // --- SES YARDIMCISI: BAÞLAT ---
    private void StartGlitchAudio()
    {
        if (glitchAudioSource == null) return;

        // 1. Rastgele Pitch
        glitchAudioSource.pitch = UnityEngine.Random.Range(glitchMinPitch, glitchMaxPitch);

        // 2. Rastgele Baþlangýç Noktasý
        if (glitchAudioSource.clip != null)
        {
            float randomTime = UnityEngine.Random.Range(0f, glitchAudioSource.clip.length);
            glitchAudioSource.time = randomTime;
        }

        // 3. Baþlat (Ama ses kýsýk)
        if (!glitchAudioSource.isPlaying) glitchAudioSource.Play();
    }

    // --- SES YARDIMCISI: SES SEVÝYESÝNÝ LERP ET ---
    private void TweenGlitchVolume(float targetVolume, float duration)
    {
        if (glitchAudioSource == null) return;
        glitchAudioSource.DOFade(targetVolume, duration).SetUpdate(true);
    }

    // --- SES YARDIMCISI: DURDUR ---
    private void StopGlitchAudio(float fadeOutDuration = 0.2f)
    {
        if (glitchAudioSource == null) return;

        // Sesi kýs ve sonra durdur
        glitchAudioSource.DOFade(0f, fadeOutDuration)
            .SetUpdate(true)
            .OnComplete(() => glitchAudioSource.Stop());
    }
}