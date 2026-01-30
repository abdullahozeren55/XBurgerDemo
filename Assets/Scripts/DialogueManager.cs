using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Febucci.UI;
using System;
using DG.Tweening;

public enum TypewriterSoundType
{
    None,
    Soft,   // Pıt pıt (Normal konuşma)
    Medium, // Tak tuk (Sert adam)
    Hard    // Çat Çut (Daktilo veya Robot)
}

[System.Serializable]
public struct TypewriterSoundProfile
{
    public TypewriterSoundType type;
    public AudioClip clipA; // Dıt
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
    Wiggle = 1 << 2,   // <wiggle>Kıpırkıpır</wiggle>
}

public enum LineBehavior
{
    Normal,         // Standart satır. Tıkla geç.
    Meltdown,       // Normal başlar, Glitch 0->2 artar, SONRAKİ SATIRA OTOMATİK ATLAR. (Skippable: False)
    HorrorReveal,   // Glitchli (0->0.4) başlar, sonunda (2.0) patlar, SONRAKİ SATIRA OTOMATİK ATLAR. (Skippable: False)
    InstantRecover  // Typewriter yok. Pat diye belirir. Normal input bekler.
}
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("Typewriter Audio Settings")]
    [SerializeField] private List<TypewriterSoundProfile> soundProfiles;

    // Hızlı erişim için Dictionary
    private Dictionary<TypewriterSoundType, TypewriterSoundProfile> soundProfileMap;

    // Sesin A mı B mi olduğunu takip etmek için
    private bool useClipA = true;

    [Header("UI Pool System")]
    [SerializeField] private List<DialogueAnimator> dialogueAnimatorPool; // Inspector'dan 4 tane balon ata

    [Header("Glitch Audio Settings")]
    [SerializeField] private AudioSource glitchAudioSource; // Child objeyi buraya sürükle
    [Range(0f, 1f)][SerializeField] private float glitchMaxVolume = 0.5f;
    [Range(0.5f, 2f)][SerializeField] private float glitchMinPitch = 0.8f;
    [Range(0.5f, 2f)][SerializeField] private float glitchMaxPitch = 1.2f;

    // --- DEBUG İÇİN GÖRÜNÜR LİSTE ---
    [System.Serializable]
    public struct DebugSpeakerInfo
    {
        public string ID;
        public string ObjectName;
        public bool HasLookAtTarget;
    }
    [Header("DEBUG - DO NOT EDIT")]
    public List<DebugSpeakerInfo> VisibleSpeakers = new List<DebugSpeakerInfo>();
    // ---------------------------------

    // Aktif olarak en son kullanılan (şu an ekranda en taze olan) balon
    private DialogueAnimator currentActiveDialogueAnimator;

    // String yerine Enum Key kullanıyoruz
    private Dictionary<CustomerID, IDialogueSpeaker> speakers = new Dictionary<CustomerID, IDialogueSpeaker>();

    private DialogueData currentData;
    private int currentIndex;
    private bool isDialogueActive;
    private IDialogueSpeaker currentSpeaker;
    private Action onDialogueCompleteCallback;

    // YENİ: Şu anki satırın datasına Update'den erişmek için
    private DialogueData.DialogueLine currentLineData;

    // YENİ: Delayleri durdurabilmek için Coroutine referansları
    private Coroutine textDelayRoutine;
    private Coroutine jumpscareDelayRoutine;
    private Coroutine eventDelayRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Başlangıçta tüm diyalog animatorleri kapat
        foreach (var dialogAnim in dialogueAnimatorPool) dialogAnim.ForceHide();

        // Listeyi Dictionary'e çevir (Performans)
        soundProfileMap = new Dictionary<TypewriterSoundType, TypewriterSoundProfile>();
        foreach (var profile in soundProfiles)
        {
            if (!soundProfileMap.ContainsKey(profile.type))
                soundProfileMap.Add(profile.type, profile);
        }

        // Başlangıçta glitch sesini sustur ve loop'a al
        if (glitchAudioSource != null)
        {
            glitchAudioSource.loop = true;
            glitchAudioSource.volume = 0f;
            glitchAudioSource.Stop(); // Şimdilik dursun
        }
    }

    private void Update()
    {
        if (!isDialogueActive) return;

        // --- YENİ: INPUT ENGELLEYİCİ ---
        // Eğer şu anki satır "Meltdown" veya "HorrorReveal" ise, kontrol tamamen bizdedir.
        // Oyuncu 'E' tuşuna bassa bile algılamamalıyız.
        // Bu satırlar sadece Coroutine süreleri dolunca (AutoAdvance) geçecek.
        if (currentLineData != null)
        {
            if (currentLineData.Behavior == LineBehavior.Meltdown ||
                currentLineData.Behavior == LineBehavior.HorrorReveal)
            {
                return; // Oyuncu inputunu yoksay ve çık
            }
        }
        // -------------------------------

        if (InputManager.Instance != null && InputManager.Instance.PlayerInteract())
        {
            // 1. Durum: Yazı hala yazılıyor
            if (currentActiveDialogueAnimator != null && currentActiveDialogueAnimator.IsTyping())
            {
                // Sadece NORMAL ve INSTANT satırlarda, eğer IsSkippable açıksa geçilebilir
                if (currentLineData != null && currentLineData.IsSkippable)
                {
                    currentActiveDialogueAnimator.SkipTypewriter();
                }
            }
            // 2. Durum: Yazı bitti, sonraki satıra geçmek isteniyor
            else
            {
                AdvanceDialogue();
            }
        }
    }

    // --- SPEAKER KAYIT SİSTEMİ (ENUM) ---
    public void RegisterSpeaker(IDialogueSpeaker speaker)
    {
        // Mevcut Dictionary kodun:
        if (speakers.ContainsKey(speaker.SpeakerID))
            speakers[speaker.SpeakerID] = speaker;
        else
            speakers.Add(speaker.SpeakerID, speaker);

        // --- BURAYI EKLE: Listeyi Güncelle ---
        UpdateDebugList();
    }

    public void UnregisterSpeaker(IDialogueSpeaker speaker)
    {
        // Senin güvenli kodun:
        if (speakers.TryGetValue(speaker.SpeakerID, out IDialogueSpeaker existing))
        {
            if (existing == speaker)
            {
                speakers.Remove(speaker.SpeakerID);
                // --- BURAYI EKLE ---
                UpdateDebugList();
            }
        }
    }

    private void UpdateDebugList()
    {
        VisibleSpeakers.Clear();
        foreach (var kvp in speakers)
        {
            var sp = kvp.Value as MonoBehaviour;
            VisibleSpeakers.Add(new DebugSpeakerInfo
            {
                ID = kvp.Key.ToString(),
                ObjectName = sp != null ? sp.name : "NULL OBJE!",
                HasLookAtTarget = kvp.Value.LookAtPoint != null
            });
        }
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

            // --- YENİ: KONUŞAN KİŞİNİN KAFASINI KAMERAYA ÇEVİR ---
            // İlk satırın konuşmacısını bulalım
            if (data.lines.Count > 0)
            {
                var firstSpeakerID = data.lines[0].SpeakerID;
                if (speakers.ContainsKey(firstSpeakerID))
                {
                    // Eğer konuşan kişi bir CustomerController ise (veya component'e sahipse)
                    var speakerObj = speakers[firstSpeakerID] as MonoBehaviour;
                    if (speakerObj != null)
                    {
                        var customer = speakerObj.GetComponent<CustomerController>();
                        if (customer != null)
                        {
                            // KAMERAYA BAK EMRİ
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
        // Önceki satırdan kalan bekleyen işleri iptal et
        if (textDelayRoutine != null) StopCoroutine(textDelayRoutine);
        if (jumpscareDelayRoutine != null) StopCoroutine(jumpscareDelayRoutine);
        if (eventDelayRoutine != null) StopCoroutine(eventDelayRoutine);

        // --- KRİTİK DÜZELTME BURADA ---
        if (currentActiveDialogueAnimator != null)
        {
            bool shouldHardCut = false;

            // Eğer şu an biten satırın verisi varsa kontrol et
            if (currentLineData != null)
            {
                // Eğer Meltdown veya HorrorReveal ise -> BIÇAK GİBİ KES (ForceHide)
                // Çünkü bu modlarda süre dolunca "PAT" diye kesilip diğerine geçmesi gerekir.
                // Fade-out animasyonu glitch etkisini yumuşatır ve senin yaşadığın bug'a sebep olur.
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
                // Normal satırsa nazikçe silinsin
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
        currentLineData = line; // Update'de erişmek için kaydet

        // 2. HAVUZDAN BALON SEÇ
        DialogueAnimator selectedDialogueAnimator = GetAvailableDialogueAnimator();
        currentActiveDialogueAnimator = selectedDialogueAnimator;

        selectedDialogueAnimator.SetColor(line.TextColor);
        selectedDialogueAnimator.SetSpeed(line.TypewriterSpeedMultiplier);

        // --- YENİ EKLENEN KISIM ---
        // Data'da font seçimi olmadığı için şimdilik manuel olarak "DialogueOutlined" veriyoruz.
        // Bu çağrı, dil Çince/Japonca ise fontu ve boyutu (Scale) otomatik ayarlayacak.
        selectedDialogueAnimator.UpdateFontSettings(FontType.DialogueOutlined);
        // --------------------------

        if (glitchAudioSource != null) glitchAudioSource.volume = 0f;

        useClipA = true;

        string rawText = LocalizationManager.Instance.GetText(line.LocalizationKey);
        string processedText = selectedDialogueAnimator.ApplyRichText(rawText, line.TextEffects);

        // ... (Kodun geri kalanı aynı şekilde devam ediyor: Camera Juice, Events, Behavior vb.) ...

        bool hasJumpscare = (line.Jumpscare != JumpscareType.None);

        if (CameraManager.Instance != null)
        {
            // ... Camera kodları aynı ...
            Transform targetTrans = null;
            CustomerID targetID = (line.FocusTargetID != CustomerID.None) ? line.FocusTargetID : line.SpeakerID;

            if (speakers.ContainsKey(targetID))
            {
                if (speakers[targetID] is DialogueSpeaker ds) targetTrans = ds.LookAtPoint;
                else if (speakers[targetID] is MonoBehaviour mb) targetTrans = mb.transform;
            }

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
                (currentIndex == 0),
                hasJumpscare
            );

            // ... (Kalan tüm switch-case ve delay mantıkları aynen kalıyor)
            float organicTextDelay = GetOrganicDelay(line.TextDelay);
            float organicJumpscareDelay = GetOrganicDelay(line.JumpscareDelay);
            float organicEventDelay = GetOrganicDelay(line.EventDelay);
            float organicAutoAdvanceDuration = GetOrganicDelay(line.AutoAdvanceDuration);

            if (line.Events != DialogueEvent.None)
            {
                eventDelayRoutine = StartCoroutine(ProcessEventsRoutine(organicEventDelay, line.Events));
            }

            if (line.Jumpscare != JumpscareType.None)
            {
                jumpscareDelayRoutine = StartCoroutine(JumpscareRoutine(organicJumpscareDelay, line.Jumpscare, line.JumpscareFadeInDuration, line.JumpscareFadeOutDuration));
            }

            selectedDialogueAnimator.transform.DOKill();

            switch (line.Behavior)
            {
                case LineBehavior.Normal:
                    StopGlitchAudio(0.1f);
                    selectedDialogueAnimator.SetGlitchStrength(0f);
                    textDelayRoutine = StartCoroutine(TextRoutine(organicTextDelay, line, selectedDialogueAnimator, processedText));
                    break;

                case LineBehavior.Meltdown:
                    selectedDialogueAnimator.SetGlitchStrength(0f);
                    textDelayRoutine = StartCoroutine(TextRoutine(organicTextDelay, line, selectedDialogueAnimator, processedText));
                    StartCoroutine(MeltdownRoutine(selectedDialogueAnimator, organicAutoAdvanceDuration));
                    break;

                case LineBehavior.HorrorReveal:
                    selectedDialogueAnimator.SetGlitchStrength(0f);
                    textDelayRoutine = StartCoroutine(TextRoutine(organicTextDelay, line, selectedDialogueAnimator, processedText));
                    StartCoroutine(HorrorRevealRoutine(selectedDialogueAnimator, organicAutoAdvanceDuration));
                    break;

                case LineBehavior.InstantRecover:
                    StopGlitchAudio(0.1f);
                    selectedDialogueAnimator.SetGlitchStrength(0f);
                    textDelayRoutine = StartCoroutine(InstantTextRoutine(organicTextDelay, line, selectedDialogueAnimator, processedText));
                    break;
            }
        }
    }

    // --- YENİ: ORGANİK DELAY HESAPLAYICI ---
    private float GetOrganicDelay(float baseDelay)
    {
        if (baseDelay <= 0.05f) return 0f; // Çok küçükse direkt oynat

        // %10 aşağı veya yukarı sapma
        // Örn: 2.0 saniye -> 1.8 ile 2.2 arasında değişir.
        return baseDelay * UnityEngine.Random.Range(0.9f, 1.1f);
    }

    // --- YENİ: EVENT RUTİNİ ---
    private IEnumerator ProcessEventsRoutine(float delay, DialogueEvent events)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        HandleDialogueEvents(events);
    }

    // --- HAVUZ MANTIĞI ---
    private DialogueAnimator GetAvailableDialogueAnimator()
    {
        // 1. Öncelik: Hiç meşgul olmayan (Animasyonu bitmiş, kapalı) bir animator bul
        foreach (var dialogueAnimator in dialogueAnimatorPool)
        {
            if (!dialogueAnimator.IsBusy) return dialogueAnimator;
        }

        // 2. Durum: Hepsi meşgul (Çok hızlı geçildi). 
        // O zaman Disappear animasyonu bitmeye EN YAKIN olanı (yani listedeki en eski active olanı) bulup resetleyelim.
        // Basit çözüm: Listede sırayla dönüyoruz ya, muhtemelen index 0 en eskidir.
        // Ama biz yine de aktif olanlardan birini kurban seçelim.

        Debug.LogWarning("Tüm diyalog animatorleri meşgul! Birini zorla sıfırlıyorum.");

        // İlkini al, zorla kapat ve onu ver.
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

        // --- YENİ: HERKESİN KAFASINI SERBEST BIRAK ---
        // Sahnedeki tüm kayıtlı konuşmacıları gez ve eğer müşteriyseler bakışlarını sıfırla
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

    // --- YENİ EVENT MANTIĞI ---
    private void HandleDialogueEvents(DialogueEvent events)
    {
        if (events.HasFlag(DialogueEvent.ChromaticAberrationGlitch))
        {
            if (PostProcessManager.Instance != null)
            {
                // Özel ayarlı glitch'i tetikle
                PostProcessManager.Instance.TriggerChromaticGlitch(
                    currentLineData.EventFadeInDuration,
                    currentLineData.EventFadeOutDuration
                );
            }
        }
    }

    // --- YENİ RUTİNLER ---

    private IEnumerator MeltdownRoutine(DialogueAnimator anim, float duration)
    {
        // Sürenin %70'i güvenli
        float safeTime = duration * 0.7f;
        float glitchTime = duration * 0.3f;

        // Başlangıçta ses yok
        if (glitchAudioSource != null) glitchAudioSource.volume = 0f;

        yield return new WaitForSeconds(safeTime);

        // --- GLITCH BAŞLIYOR ---
        StartGlitchAudio(); // Sesi başlat (Volume 0'da)

        // Görsel bozulma
        anim.TweenGlitch(2.0f, glitchTime, Ease.InExpo);

        // İşitsel bozulma (Sesi aç)
        TweenGlitchVolume(glitchMaxVolume, glitchTime);

        yield return new WaitForSeconds(glitchTime);

        // Süre bitti, AdvanceDialogue çağrılacak.
        // AdvanceDialogue içindeki ForceHide sesi aniden kesmemeli, 
        // bir sonraki satır (InstantRecover) sesi susturacak.
        AdvanceDialogue();
    }

    private IEnumerator HorrorRevealRoutine(DialogueAnimator anim, float duration)
    {
        // --- BAŞLANGIÇ ---
        // HorrorReveal başlar başlamaz hafif bir cızırtı olsun
        StartGlitchAudio();

        // Hafif Glitch (Görsel 0.4, Ses %30)
        anim.TweenGlitch(0.4f, duration * 0.8f, Ease.Linear);
        TweenGlitchVolume(glitchMaxVolume * 0.3f, duration * 0.8f); // Hafif ses

        // Süre sonuna kadar bekle
        yield return new WaitForSeconds(duration);

        // --- FİNAL PATLAMASI ---
        // Aniden max glitch ve max ses
        anim.SetGlitchStrength(2.0f);
        if (glitchAudioSource != null) glitchAudioSource.volume = glitchMaxVolume;

        yield return null;

        AdvanceDialogue();
    }

    private IEnumerator InstantTextRoutine(float delay, DialogueData.DialogueLine line, DialogueAnimator anim, string text)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        // Instant = true parametresiyle çağır
        anim.Show(text);
        anim.SkipTypewriter();

        // Ses çal (Genelde recover'da ses olmaz ama data ne derse o)
        if (line.SoundType != TypewriterSoundType.None)
        {
            // Instant olduğu için tek bir 'bip' çalabiliriz veya hiç çalmayız.
            // PlayNextTypewriterSound(); 
        }
    }

    // TextRoutine biraz sadeleşti (String'i parametre olarak alıyor)
    private IEnumerator TextRoutine(float delay, DialogueData.DialogueLine line, DialogueAnimator anim, string text)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        anim.Show(text); // Typewriter aktif

        // Ses başlatma (Eski kodun aynısı)
        if (line.SoundType != TypewriterSoundType.None)
            PlayNextTypewriterSound(); // İlk harf için tetik (veya Febucci eventi halleder)
    }

    private IEnumerator JumpscareRoutine(float delay, JumpscareType type, float fadeIn, float fadeOut)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        if (CameraManager.Instance != null && currentLineData != null)
        {
            // Data'daki Fade In ve Fade Out sürelerini gönderiyoruz
            // CameraManager içinde bunlar randomize edilecek.
            CameraManager.Instance.TriggerJumpscare(type, fadeIn, fadeOut);
        }
    }

    public void PlayNextTypewriterSound()
    {
        // Eğer şu anki satırın ses ayarı 'None' ise veya data yoksa çalma
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

        // Sırayı değiştir
        useClipA = !useClipA;
    }

    // --- SES YARDIMCISI: BAŞLAT ---
    private void StartGlitchAudio()
    {
        if (glitchAudioSource == null) return;

        // 1. Rastgele Pitch
        glitchAudioSource.pitch = UnityEngine.Random.Range(glitchMinPitch, glitchMaxPitch);

        // 2. Rastgele Başlangıç Noktası
        if (glitchAudioSource.clip != null)
        {
            float randomTime = UnityEngine.Random.Range(0f, glitchAudioSource.clip.length);
            glitchAudioSource.time = randomTime;
        }

        // 3. Başlat (Ama ses kısık)
        if (!glitchAudioSource.isPlaying) glitchAudioSource.Play();
    }

    // --- SES YARDIMCISI: SES SEVİYESİNİ LERP ET ---
    private void TweenGlitchVolume(float targetVolume, float duration)
    {
        if (glitchAudioSource == null) return;
        glitchAudioSource.DOFade(targetVolume, duration).SetUpdate(true);
    }

    // --- SES YARDIMCISI: DURDUR ---
    private void StopGlitchAudio(float fadeOutDuration = 0.2f)
    {
        if (glitchAudioSource == null) return;

        // Sesi kıs ve sonra durdur
        glitchAudioSource.DOFade(0f, fadeOutDuration)
            .SetUpdate(true)
            .OnComplete(() => glitchAudioSource.Stop());
    }
}