using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Cookable : MonoBehaviour
{
    public enum CookAmount
    {
        RAW,
        REGULAR,
        BURNT
    }

    [SerializeField] private CookableData cookableData;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject[] cookTexts;

    public CookAmount cookAmount;

    private MeshRenderer meshRenderer;


    private bool isCooking;
    private float currentCookedTime;

    private GameObject currentCookingParticles;

    private BurgerIngredient burgerIngredient;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        burgerIngredient = GetComponent<BurgerIngredient>();

        currentCookedTime = 0;

        //ChangeCookAmount(0);

    }

    private void Update()
    {
            

        if (isCooking && cookAmount != CookAmount.BURNT)
        {
            currentCookedTime += Time.deltaTime;

            if (currentCookedTime > cookableData.cookTime[1])
            {
                ChangeCookAmount(2);

                if (currentCookingParticles != null)
                    DestroyCookingParticles();

                if (audioSource.isPlaying)
                    AudioStopWithFadeOut();

                this.enabled = false;
            }
            else if (currentCookedTime > cookableData.cookTime[0])
            {
                ChangeCookAmount(1);
            }
        }
    }

    private void CreateCookingParticles()
    {
        currentCookingParticles =  Instantiate(cookableData.cookingParticles, transform.position, Quaternion.Euler(-90f, 0f, 0f), transform);
    }

    private void DestroyCookingParticles()
    {
        Destroy(currentCookingParticles);
        currentCookingParticles = null;
    }

    private void ChangeCookAmount(int index)
    {
        if (index == 0)
        {
            cookAmount = CookAmount.RAW;
        }
        else if (index == 1)
        {
            cookAmount = CookAmount.REGULAR;
        }
        else if (index == 2)
        {
            cookAmount = CookAmount.BURNT;
        }

        meshRenderer.material = cookableData.materials[index];
        burgerIngredient.ChangeCookAmount(index);
    }

    private void AudioStopWithFadeOut()
    {
        StartCoroutine(FadeOut(audioSource, cookableData.audioFadeOutDuration));
    }

    public void StopCooking()
    {
        if (isCooking)
        {
            isCooking = false;

            if (currentCookingParticles != null)
                DestroyCookingParticles();

            if (audioSource.isPlaying)
                audioSource.Pause();
        }
    }

    private IEnumerator FadeOut(AudioSource audioSource, float duration)
    {
        float startVolume = audioSource.volume;

        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            audioSource.volume = Mathf.Lerp(startVolume, 0, t / duration);
            yield return null;
        }

        audioSource.Stop(); // Tamamen durdur
        audioSource.volume = startVolume; // Ses seviyesini eski haline getir
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Grill") && isActiveAndEnabled)
        {
            isCooking = true;

            if (currentCookingParticles == null)
                CreateCookingParticles();

            if (!audioSource.isPlaying)
            {
                if (currentCookedTime > 0)
                    audioSource.UnPause();
                else
                    audioSource.PlayOneShot(cookableData.cookingSound);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Grill") && isActiveAndEnabled)
        {
            StopCooking();
        }
    }
}
