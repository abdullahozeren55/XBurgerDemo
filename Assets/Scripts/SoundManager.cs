using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using static System.TimeZoneInfo;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private AudioMixer audioMixer;

    [Space]

    [SerializeField] private AudioSource soundFXObject;

    [Space]
    [SerializeField] private AudioSource soundFXObjectForUI;

    [Header("Grill Settings")]
    public float volumePerPatty = 0.3f; // Her köfte sesi ne kadar açsýn
    public float volumePerBun = 0.3f; // Her köfte sesi ne kadar açsýn
    public float grillAudioMaxVolume = 1.0f;
    public float grillAudioFadeTime = 3.0f;
    public AudioSource grillAudioSource;
    private int cookingPattyCount = 0;
    private int cookingBunCount = 0;
    private float targetVolume = 0f;
    private Coroutine cookingSoundCoroutine;

    [Header("Ambiance Settings")]
    public AudioSource mainAmbianceSource;
    public float mainAmbianceVolume;
    public float mainAmbiancePitch;

    private Dictionary<string, AudioSource> taggedSounds = new Dictionary<string, AudioSource>();

    private void Awake()
    {
        if (Instance == null)
        {
            // If not, set this instance as the singleton
            Instance = this;

            // Optionally, mark GameManager as not destroyed between scene loads
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If an instance already exists, destroy this one to enforce the singleton pattern
            Destroy(gameObject);
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

    public void AddItemToGrill(BurgerIngredientData.IngredientType type)
    {
        if (type == BurgerIngredientData.IngredientType.PATTY)
            cookingPattyCount++;
        else if (type == BurgerIngredientData.IngredientType.BOTTOMBUN || type == BurgerIngredientData.IngredientType.TOPBUN)
            cookingBunCount++;

        RecalculateVolumeForGrill();
    }

    public void RemoveItemFromGrill(BurgerIngredientData.IngredientType type)
    {
        if (type == BurgerIngredientData.IngredientType.PATTY)
            cookingPattyCount--;
        else if (type == BurgerIngredientData.IngredientType.BOTTOMBUN || type == BurgerIngredientData.IngredientType.TOPBUN)
            cookingBunCount--;

        if (cookingPattyCount < 0) cookingPattyCount = 0;
        if (cookingBunCount < 0) cookingBunCount = 0;

        RecalculateVolumeForGrill();
    }

    private void RecalculateVolumeForGrill()
    {
        if (cookingSoundCoroutine != null)
        {
            StopCoroutine(cookingSoundCoroutine);
            cookingSoundCoroutine = null;
        }

        float target = (cookingPattyCount * volumePerPatty) + (cookingBunCount * volumePerBun);

        targetVolume = Mathf.Clamp(target, 0f, grillAudioMaxVolume);

        cookingSoundCoroutine = StartCoroutine(LerpGrillSoundToTarget());
    }

    public void SetMasterVolume (float value)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20f);
    }

    public void SetSoundFXVolume(float value)
    {
        audioMixer.SetFloat("SoundFXVolume", Mathf.Log10(value) * 20f);
    }

    public void SetMusicVolume(float value)
    {
        audioMixer.SetFloat("MusicVolume", Mathf.Log10(value) * 20f);
    }

    public void SetAmbianceVolume(float value)
    {
        audioMixer.SetFloat("AmbianceVolume", Mathf.Log10(value) * 20f);
    }

    public void SetTypewriterVolume(float value)
    {
        audioMixer.SetFloat("TypewriterVolume", Mathf.Log10(value) * 20f);
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

    private IEnumerator LerpGrillSoundToTarget()
    {
        float startVolume = grillAudioSource.volume;

        float elapsedTime = 0f;

        while (elapsedTime < grillAudioFadeTime)
        {
            grillAudioSource.volume = Mathf.Lerp(startVolume, targetVolume, elapsedTime/grillAudioFadeTime);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        grillAudioSource.volume = targetVolume;
    }
}
