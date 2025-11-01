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

    public void PlaySoundFX(AudioClip audioClip, Transform spawnTransform, float volume, float minPitch, float maxPitch)
    {
        AudioSource audioSource = Instantiate(soundFXObject, spawnTransform.position, Quaternion.identity);

        audioSource.clip = audioClip;

        audioSource.volume = volume;

        audioSource.pitch = Random.Range(minPitch, maxPitch);

        audioSource.Play();

        float clipLength = audioSource.clip.length;

        Destroy(audioSource.gameObject, clipLength);
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
}
