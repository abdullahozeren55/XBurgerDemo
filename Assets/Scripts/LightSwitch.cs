using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    // --- STANDART DEÐÝÞKENLERÝN AYNEN KALIYOR ---
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract = true;

    public PlayerManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes handRigType;

    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;

    public string FocusTextKey { get => focusTextKeys[switchStateNum]; set => focusTextKeys[switchStateNum] = value; }
    [SerializeField] private string[] focusTextKeys;
    private int switchStateNum = 0;

    [System.Serializable]
    public class LightSettings
    {
        public GameObject lightGO;
        [Range(0, 100)] public int flickerPossibility;
    }

    [Header("On Off Settings")]
    [SerializeField] private AudioSource buzzSoundSource;
    [SerializeField] private LightSettings[] lightsToEffect;
    [SerializeField] private Material lightMatToCopy;
    [Space]
    [SerializeField] private GameObject switchPart;
    [SerializeField] private float timeToSwitchRotate = 0.2f;
    [SerializeField] private float onSwitchXRotation = 20f;
    [SerializeField] private float buzzSoundIncreasePerLight = 0.01f;

    private Quaternion offRotation;
    private Quaternion onRotation;
    private Coroutine rotateCoroutine;
    private bool isOn;

    [Header("Flicker Settings")]
    [SerializeField] private AudioClip[] flickClips;
    [SerializeField] private float flickVolume = 1f;
    [SerializeField] private float flickMinPitch = 0.85f;
    [SerializeField] private float flickMaxPitch = 1.15f;
    [SerializeField] private float baseIntensity = 20f;
    [SerializeField] private float flickerMinDelay = 0.1f;
    [SerializeField] private float flickerMaxDelay = 0.3f;
    [SerializeField] private int minFlickerCount = 3;
    [SerializeField] private int maxFlickerCount = 12;

    private Color baseEmission;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    // BURASI DEÐÝÞTÝ: Tek bir coroutine yetmez, çünkü 5 ýþýk ayný anda flicker yapabilir.
    // Kapatýrken StopAllCoroutines kullanacaðýz veya listeye alacaðýz.
    // Þimdilik kafa karýþmasýn diye basit tuttum.

    [Header("Audio Settings")]
    public AudioClip lightSwitchSound;
    public float minPitchForOn = 1f;
    public float maxPitchForOn = 1.2f;
    public float minPitchForOff = 0.7f;
    public float maxPitchForOff = 0.9f;

    [Header("Layer Settings")]
    private int interactableLayer;
    private int interactableOutlinedLayer;
    private int interactableOutlinedRedLayer;

    private void Awake()
    {
        interactableLayer = LayerMask.NameToLayer("Interactable");
        interactableOutlinedLayer = LayerMask.NameToLayer("InteractableOutlined");
        interactableOutlinedRedLayer = LayerMask.NameToLayer("InteractableOutlinedRed");

        isOn = false;
        offRotation = switchPart.transform.localRotation;
        onRotation = Quaternion.Euler(onSwitchXRotation, offRotation.y, offRotation.z);

        if (lightMatToCopy != null)
            baseEmission = lightMatToCopy.GetColor(EmissionColorID);

        // Materyalleri kopyala
        foreach (LightSettings settings in lightsToEffect)
        {
            Renderer rend = settings.lightGO.GetComponentInChildren<Renderer>();
            if (rend != null)
                rend.material = new Material(lightMatToCopy);
        }
    }

    // --- INTERFACE METOTLARI AYNEN KALIYOR ---
    public void ChangeLayer(int layerIndex) { gameObject.layer = layerIndex; switchPart.layer = layerIndex; }
    public void HandleFinishDialogue() { }
    public void OnFocus() { if (!CanInteract) return; ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer); }
    public void OnInteract() { if (!CanInteract) return; HandleRotation(); }
    public void OnLoseFocus() { if (!CanInteract) return; ChangeLayer(interactableLayer); }
    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed) ChangeLayer(interactableOutlinedRedLayer);
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed) ChangeLayer(interactableOutlinedLayer);
    }

    public void HandleRotation()
    {
        isOn = !isOn;

        SoundManager.Instance.PlaySoundFX(lightSwitchSound, transform, 1f, isOn ? minPitchForOn : minPitchForOff, isOn ? maxPitchForOn : maxPitchForOff);
        switchStateNum = isOn ? 1 : 0;
        PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);

        HandleLights();

        if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
        rotateCoroutine = StartCoroutine(ToogleRotate(isOn));
    }

    private IEnumerator ToogleRotate(bool shouldOn)
    {
        Quaternion targetRotation = shouldOn ? onRotation : offRotation;
        Quaternion startingRotation = switchPart.transform.localRotation;
        float timeElapsed = 0f;

        while (timeElapsed < timeToSwitchRotate)
        {
            switchPart.transform.localRotation = Quaternion.Slerp(startingRotation, targetRotation, timeElapsed / timeToSwitchRotate);
            timeElapsed += Time.deltaTime;
            yield return null;
        }
        switchPart.transform.localRotation = targetRotation;
    }

    // --- ASIL DEÐÝÞÝKLÝK BURADA ---
    private void HandleLights()
    {
        // Önceki flicker iþlemlerini durdur. 
        // DÝKKAT: Bu switch üzerindeki TÜM coroutine'leri durdurur (Rotate hariç tutulmalýydý ama
        // Rotate zaten kýsa sürdüðü için sorun olmaz, yine de clean olsun diye rotate'i ayrý tutabilirsin)
        // Ama en temizi flicker'larý bir listede tutmak. 
        // Þimdilik "StopAllCoroutines" kullanýyorum ama RotateCoroutine'i tekrar baþlatmamak için dikkatli ol.
        // Flicker için StopAllCoroutines yapmak yerine, kapatma bloðunda manuel kontrol yapalým.

        if (isOn)
        {
            // IÞIKLARI AÇIYORUZ
            foreach (var setting in lightsToEffect)
            {
                Light lightComp = setting.lightGO.GetComponentInChildren<Light>();
                Renderer rend = setting.lightGO.GetComponentInChildren<Renderer>();
                if (lightComp == null || rend == null) continue;

                Material matInstance = rend.material;
                matInstance.EnableKeyword("_EMISSION");
                lightComp.enabled = true;

                // Flicker olacak mý?
                bool willFlicker = Random.Range(0, 100) < setting.flickerPossibility;

                if (willFlicker)
                {
                    // FLICKER VARSA: Coroutine baþlat, o bitince BuzzManager'a haber verecek
                    int flickerCount = Random.Range(minFlickerCount, maxFlickerCount);
                    StartCoroutine(FlickerLightRoutine(lightComp, matInstance, baseEmission, flickerCount));
                }
                else
                {
                    // FLICKER YOKSA: Direkt aç ve BuzzManager'a haber ver
                    lightComp.intensity = baseIntensity;
                    matInstance.SetColor("_EmissionColor", baseEmission);

                    buzzSoundSource.volume += buzzSoundIncreasePerLight;
                }
            }
        }
        else
        {
            // IÞIKLARI KAPATIYORUZ
            StopAllCoroutines(); // Flickerlarý anýnda kes
            // Rotate durduysa tekrar baþlatmak gerekebilir ama basit çözüm:
            if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
            rotateCoroutine = StartCoroutine(ToogleRotate(isOn)); // Rotasyon bozulmasýn diye tekrar tetikledim

            foreach (var setting in lightsToEffect)
            {
                Light lightComp = setting.lightGO.GetComponentInChildren<Light>();
                Renderer rend = setting.lightGO.GetComponentInChildren<Renderer>();

                // Eðer ýþýk zaten açýksa, BuzzManager'dan düþmeliyiz
                // (lightComp.enabled true ise yanýyordur veya flicker yapýyordur)
                if (lightComp != null && lightComp.enabled)
                {
                    buzzSoundSource.volume = 0f;

                    lightComp.enabled = false;
                }

                if (rend != null)
                {
                    rend.material.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    private IEnumerator FlickerLightRoutine(Light light, Material mat, Color targetEmission, int flickerCount)
    {
        // Flicker sýrasýnda ýþýk bir yanýp bir sönecek
        for (int i = 0; i < flickerCount; i++)
        {
            // Rastgele kýsýk yan
            float randomIntensity = Random.Range(0.01f, baseIntensity * 0.3f);
            float emissionMultiplier = randomIntensity / (baseIntensity * 2);
            Color flickerEmission = targetEmission * Mathf.LinearToGammaSpace(emissionMultiplier);

            light.intensity = randomIntensity;
            mat.SetColor("_EmissionColor", flickerEmission);

            yield return new WaitForSeconds(Random.Range(flickerMinDelay, flickerMaxDelay));

            // Normale dön (Kýsa süreliðine)
            light.intensity = baseIntensity;
            mat.SetColor("_EmissionColor", targetEmission);

            SoundManager.Instance.PlayRandomSoundFX(flickClips, light.transform, flickVolume, flickMinPitch, flickMaxPitch);

            yield return new WaitForSeconds(Random.Range(flickerMinDelay, flickerMaxDelay));
        }

        // --- FLICKER BÝTTÝ, SON HALÝNÝ VER ---
        light.intensity = baseIntensity;
        mat.SetColor("_EmissionColor", targetEmission);

        buzzSoundSource.volume += buzzSoundIncreasePerLight;
    }
}