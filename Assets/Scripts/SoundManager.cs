using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [SerializeField] private AudioMixer audioMixer;

    [Space]

    [SerializeField] private AudioSource soundFXObject;

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

    public void PlaySoundFX(
    AudioClip audioClip,
    Transform spawnTransform,
    float volume = 1f,
    float minPitch = 0.85f,
    float maxPitch = 1.15f,
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
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

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

    public void PlayRandomSoundFX(AudioClip[] audioClip, Transform spawnTransform, float volume, float minPitch, float maxPitch)
    {

        int rand = Random.Range(0, audioClip.Length);

        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClip[rand];

        audioSource.volume = volume;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.Play();

        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
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
}
