using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using DG.Tweening; // DOTween kütüphanesi eklendi

// 1. Adým: Ambiyans Tiplerini Belirleyen Enum
public enum AmbianceType
{
    None,       // Sessizlik için
    Wind,
    Restaurant,
    Forest,     // Örnek olarak ekledim
    Cave        // Örnek olarak ekledim
}

// 2. Adým: Her bir ambiyansýn ayarlarýný tutan sýnýf
[System.Serializable]
public class AmbianceConfig
{
    public AmbianceType type;
    public AudioSource source;
    [Range(0f, 1f)] public float maxVolume = 0.5f;
}

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private AudioMixer audioMixer;

    [Space]

    [SerializeField] private AudioSource soundFXObject;
    [Space]
    [SerializeField] private AudioSource soundFXObjectForAmbiance;
    [Space]
    [SerializeField] private AudioSource soundFXObjectForUI;
    [Space]
    [SerializeField] private AudioSource soundFXObjectForTypewriter;

    [Header("Ambiance System")]
    // 3. Adým: Artýk tek tek deðiþkenler yerine bir liste kullanýyoruz.
    [SerializeField] private List<AmbianceConfig> ambianceConfigs;

    // Performans için Dictionary cache'i (Update veya Change anýnda liste taramamak için)
    private Dictionary<AmbianceType, AmbianceConfig> ambianceDictionary;
    private AmbianceType currentAmbiance = AmbianceType.None;

    [Header("Bird Chirping Settings")]
    public bool CanPlayBirdChirping = true;
    [Space]
    public int minAmountToPlayAtOnce = 2;
    public int maxAmountToPlayAtOnce = 5;
    [Space]
    public float minCooldownForBirdChirping = 5f;
    public float maxCooldownForBirdChirping = 15f;
    [Space]
    public float minPitchForBirdChirping = 0.8f;
    public float maxPitchForBirdChirping = 1.2f;
    [Space]
    public AudioClip[] birdChirpingClips;
    public Transform[] birdChirpingTransforms;
    [Space]
    private float lastTimeBirdChirpingPlayed = 0f;
    private float currentCooldownForBirdChirping = 0f;
    private int currentAmountToPlayAtOnce;
    private Transform currentTransformForBirdChirping;

    private Dictionary<string, AudioSource> taggedSounds = new Dictionary<string, AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            // If not, set this instance as the singleton
            Instance = this;
            InitializeAmbiances();
        }
        else
        {
            // If an instance already exists, destroy this one to enforce the singleton pattern
            Destroy(gameObject);
        }
    }

    private void Update()
    {
        if (CanPlayBirdChirping)
            HandleBirdChirping();
    }

    // Dictionary'i doldur ve sesleri baþlangýçta kapat
    private void InitializeAmbiances()
    {
        ambianceDictionary = new Dictionary<AmbianceType, AmbianceConfig>();

        foreach (var config in ambianceConfigs)
        {
            if (config.source == null) continue;

            // Dictionary'e ekle
            if (!ambianceDictionary.ContainsKey(config.type))
            {
                ambianceDictionary.Add(config.type, config);
            }

            // Eðer oyun baþlar baþlamaz çalmasýn istiyorsan Stop, ama genelde
            // Play diyip volume 0 tutmak, fade-in yaparken "pýt" sesini engeller.
            if (!config.source.isPlaying)
                config.source.Play();
        }
    }

    // 4. Adým: Ýstenen ChangeAmbiance Fonksiyonu
    /// <summary>
    /// Ambiyansý yumuþak geçiþle deðiþtirir.
    /// </summary>
    /// <param name="targetType">Geçilecek yeni ambiyans tipi</param>
    /// <param name="duration">Geçiþ süresi (Default: 2 saniye)</param>
    public void ChangeAmbiance(AmbianceType targetType, float duration = 2f)
    {
        // Eðer zaten o ambiyanstaysak iþlem yapma (veya zorla resetlemek istersen bu check'i kaldýr)
        if (currentAmbiance == targetType) return;

        currentAmbiance = targetType;

        // Tüm tanýmlý ambiyanslarý gez
        foreach (var config in ambianceConfigs)
        {
            if (config.source == null) continue;

            // Önce bu AudioSource üzerindeki mevcut Volume tween'lerini öldür.
            // Bu çok önemli, yoksa fade-out yapan bir tween ile fade-in yapan çakýþabilir.
            config.source.DOKill();

            // Eðer bu konfigürasyon, hedeflediðimiz tip ise:
            if (config.type == targetType)
            {
                // Hedef sese geçiþ yap (Kendi max volume deðerine)
                if (!config.source.isPlaying) config.source.Play();
                config.source.DOFade(config.maxVolume, duration).SetEase(Ease.Linear);
            }
            else
            {
                // Hedef bu deðilse, sesini 0'a çek (Sustur)
                // Volume 0 olunca tamamen dursun istersen OnComplete ekleyebilirsin ama genelde gerekmez.
                config.source.DOFade(0f, duration).SetEase(Ease.Linear);
            }
        }
    }

    public void SwitchSnapshot(string name, float duration)
    {
        AudioMixerSnapshot snap = audioMixer.FindSnapshot(name);

        if (snap != null)
        {
            snap.TransitionTo(duration);
        }
    }

    public void PlaySoundFX(
    AudioClip audioClip,
    Transform spawnTransform,
    float volume = 1f,
    float minPitch = 0.85f,
    float maxPitch = 1.15f,
    bool shouldBeParentToTransform = true,
    string tag = null)
    {
        // Eðer tag varsa eski sesi durdur
        if (!string.IsNullOrEmpty(tag))
        {
            if (taggedSounds.TryGetValue(tag, out AudioSource existing))
            {
                if (existing != null)
                {
                    existing.Stop();
                    Destroy(existing.gameObject);
                }

                taggedSounds.Remove(tag);
            }
        }

        // Yeni ses objesini oluþtur
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity, shouldBeParentToTransform ? spawnTransform : null);

        audioSource.clip = audioClip;
        audioSource.volume = volume;
        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.Play();

        // Pitch’e göre gerçek süre
        float duration = audioSource.clip.length / audioSource.pitch;

        // Eðer tag varsa bu sesi dictionary’e ekle
        if (!string.IsNullOrEmpty(tag))
        {
            taggedSounds[tag] = audioSource;

            // Süre bitince dictionary’den temizle
            StartCoroutine(RemoveTagAfterDelay(tag, audioSource, duration));
        }

        Destroy(audioSource.gameObject, duration);
    }

    public void PlayUISoundFX(AudioClip audioClip, float volume = 1f, float minPitch = 0.85f, float maxPitch = 1.15f)
    {
        AudioSource audioSource = Instantiate(soundFXObjectForUI, Vector3.zero, Quaternion.identity);

        audioSource.clip = audioClip;

        audioSource.volume = volume;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        float clipLength = audioSource.clip.length / audioSource.pitch;

        audioSource.Play();

        Destroy(audioSource.gameObject, clipLength);
    }

    public AudioSource PlayTypewriterSoundFX(AudioClip audioClip, float volume = 1f, float minPitch = 0.85f, float maxPitch = 1.15f)
    {
        AudioSource audioSource = Instantiate(soundFXObjectForTypewriter, Vector3.zero, Quaternion.identity);

        audioSource.clip = audioClip;

        audioSource.volume = volume;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        float clipLength = audioSource.clip.length / audioSource.pitch;

        audioSource.Play();

        Destroy(audioSource.gameObject, clipLength);

        return audioSource;
    }

    public void PlaySoundFXWithRandomDelay(AudioClip audioClip, Transform spawnTransform, float volume = 1f, float minPitch = 0.85f, float maxPitch = 1.15f, float minDelay = 0.05f, float maxDelay = 0.25f)
    {
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity, spawnTransform);

        audioSource.clip = audioClip;

        audioSource.volume = volume;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        float clipLength = audioSource.clip.length / audioSource.pitch;

        StartCoroutine(PlayWithRandomDelay(audioSource, minDelay, maxDelay, clipLength));
    }

    public void PlayUISoundFXWithDelay(AudioClip audioClip, float volume = 1f, float minPitch = 0.85f, float maxPitch = 1.15f, float delay = 0f)
    {
        AudioSource audioSource = Instantiate(soundFXObjectForUI, Vector3.zero, Quaternion.identity);

        audioSource.clip = audioClip;

        audioSource.volume = volume;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        float clipLength = audioSource.clip.length / audioSource.pitch;

        StartCoroutine(PlayWithDelay(audioSource, delay, clipLength));
    }

    public void PlayRandomSoundFX(AudioClip[] audioClip, Transform spawnTransform, float volume = 1f, float minPitch = 0.85f, float maxPitch = 1.15f)
    {

        int rand = Random.Range(0, audioClip.Length);

        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity, spawnTransform);

        audioSource.clip = audioClip[rand];

        audioSource.volume = volume;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.Play();

        float clipLength = audioSource.clip.length / audioSource.pitch;

        Destroy(audioSource.gameObject, clipLength);
    }

    public void PlayRandomAmbianceSoundFX(AudioClip[] audioClip, Transform spawnTransform, float volume = 1f, float minPitch = 0.85f, float maxPitch = 1.15f)
    {

        int rand = Random.Range(0, audioClip.Length);

        AudioSource audioSource = Instantiate(soundFXObjectForAmbiance, spawnTransform.position, Quaternion.identity, spawnTransform);

        audioSource.clip = audioClip[rand];

        audioSource.volume = volume;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.Play();

        float clipLength = audioSource.clip.length / audioSource.pitch;

        Destroy(audioSource.gameObject, clipLength);
    }

    public void PlayRandomUISoundFX(AudioClip[] audioClip, Transform spawnTransform, float volume = 1f, float minPitch = 0.85f, float maxPitch = 1.15f)
    {

        int rand = Random.Range(0, audioClip.Length);

        AudioSource audioSource = Instantiate(soundFXObjectForUI, spawnTransform.position, Quaternion.identity, spawnTransform);

        audioSource.clip = audioClip[rand];

        audioSource.volume = volume;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.Play();

        float clipLength = audioSource.clip.length / audioSource.pitch;

        Destroy(audioSource.gameObject, clipLength);
    }

    public void SetMasterVolume(float value)
    {
        // 0 gelirse 0.0001 yap ki logaritma patlamasýn
        float safeValue = Mathf.Clamp(value, 0.0001f, 1f);
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(safeValue) * 20f);
    }

    public void SetSoundFXVolume(float value)
    {
        // 0 gelirse 0.0001 yap ki logaritma patlamasýn
        float safeValue = Mathf.Clamp(value, 0.0001f, 1f);
        audioMixer.SetFloat("SoundFXVolume", Mathf.Log10(safeValue) * 20f);
    }

    public void SetMusicVolume(float value)
    {
        // 0 gelirse 0.0001 yap ki logaritma patlamasýn
        float safeValue = Mathf.Clamp(value, 0.0001f, 1f);
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(safeValue) * 20f);
    }

    public void SetAmbianceVolume(float value)
    {
        // 0 gelirse 0.0001 yap ki logaritma patlamasýn
        float safeValue = Mathf.Clamp(value, 0.0001f, 1f);
        audioMixer.SetFloat("AmbianceVolume", Mathf.Log10(safeValue) * 20f);
    }

    public void SetTypewriterVolume(float value)
    {
        // 0 gelirse 0.0001 yap ki logaritma patlamasýn
        float safeValue = Mathf.Clamp(value, 0.0001f, 1f);
        audioMixer.SetFloat("TypewriterVolume", Mathf.Log10(safeValue) * 20f);
    }

    public void SetUIVolume(float value)
    {
        // 0 gelirse 0.0001 yap ki logaritma patlamasýn
        float safeValue = Mathf.Clamp(value, 0.0001f, 1f);
        audioMixer.SetFloat("UIVolume", Mathf.Log10(safeValue) * 20f);
    }

    private void HandleBirdChirping()
    {
        if (Time.time > lastTimeBirdChirpingPlayed + currentCooldownForBirdChirping)
        {
            currentAmountToPlayAtOnce = Random.Range(minAmountToPlayAtOnce, maxAmountToPlayAtOnce);

            for (int i = 0; i < currentAmountToPlayAtOnce; i++)
            {
                currentTransformForBirdChirping = birdChirpingTransforms[Random.Range(0, birdChirpingTransforms.Length)];

                PlayRandomAmbianceSoundFX(birdChirpingClips, currentTransformForBirdChirping, 1f, minPitchForBirdChirping, maxPitchForBirdChirping);

                i++;
            }

            currentCooldownForBirdChirping = Random.Range(minCooldownForBirdChirping, maxCooldownForBirdChirping);

            lastTimeBirdChirpingPlayed = Time.time;
        }
    }

    private IEnumerator RemoveTagAfterDelay(string tag, AudioSource source, float delay)
    {
        yield return new WaitForSeconds(delay);

        // Eðer o tag hala ayný source'u tutuyorsa temizle
        if (taggedSounds.TryGetValue(tag, out var current) && current == source)
        {
            taggedSounds.Remove(tag);
        }
    }

    private IEnumerator PlayWithRandomDelay(AudioSource audioSource, float minDelay, float maxDelay, float clipLength)
    {
        yield return new WaitForSeconds(Random.Range(minDelay, maxDelay));

        audioSource.Play();

        Destroy(audioSource.gameObject, clipLength);
    }

    private IEnumerator PlayWithDelay(AudioSource audioSource, float delay, float clipLength)
    {
        yield return new WaitForSeconds(delay);

        audioSource.Play();

        Destroy(audioSource.gameObject, clipLength);
    }
}
