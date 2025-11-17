using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class LightSwitch : MonoBehaviour, IInteractable
{
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract;

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
    [SerializeField] private LightSettings[] lightsToEffect;
    [SerializeField] private Material lightMatToCopy;
    [Space]
    [SerializeField] private GameObject switchPart;
    [SerializeField] private float timeToSwitchRotate = 0.2f;
    [SerializeField] private float onSwitchXRotation = 20f;
    private Quaternion offRotation;
    private Quaternion onRotation;
    private Coroutine rotateCoroutine;
    private bool isOn;

    [Header("Flicker Settings")]
    [SerializeField] private float baseIntensity = 20f;
    [SerializeField] private float flickerMinDelay = 0.1f;
    [SerializeField] private float flickerMaxDelay = 0.3f;
    [SerializeField] private int minFlickerCount = 3;
    [SerializeField] private int maxFlickerCount = 12;
    private Color baseEmission; // orijinal emission rengi
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
    private Coroutine flickerLightCoroutine;

    [Header("Audio Settings")]
    public AudioClip lightSwitchSound;
    [Space]
    public float minPitchForOn = 1f;
    public float maxPitchForOn = 1.2f;
    [Space]
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

        baseEmission = lightMatToCopy.GetColor(EmissionColorID);

        foreach (LightSettings settings in lightsToEffect)
        {
            settings.lightGO.GetComponent<MeshRenderer>().material = new Material(lightMatToCopy);
        }
    }

    public void ChangeLayer(int layerIndex)
    {
        gameObject.layer = layerIndex;
        switchPart.layer = layerIndex;
    }

    public void HandleFinishDialogue()
    {
    }

    public void OnFocus()
    {
        if (!CanInteract) return;

        ChangeLayer(OutlineShouldBeRed ? interactableOutlinedRedLayer : interactableOutlinedLayer);
    }

    public void OnInteract()
    {
        if (!CanInteract) return;

        HandleRotation();
    }

    public void OnLoseFocus()
    {
        if (!CanInteract) return;

        ChangeLayer(interactableLayer);
    }

    public void OutlineChangeCheck()
    {
        if (gameObject.layer == interactableOutlinedLayer && OutlineShouldBeRed)
        {
            ChangeLayer(interactableOutlinedRedLayer);
        }
        else if (gameObject.layer == interactableOutlinedRedLayer && !OutlineShouldBeRed)
        {
            ChangeLayer(interactableOutlinedLayer);
        }
    }

    public void HandleRotation()
    {
        isOn = !isOn;

        SoundManager.Instance.PlaySoundFX(lightSwitchSound, transform, 1f, isOn ? minPitchForOn : minPitchForOff, isOn ? maxPitchForOn : maxPitchForOff);

        switchStateNum = isOn ? 1 : 0;

        PlayerManager.Instance.TryChangingFocusText(this, FocusTextKey);

        HandleLights();

        if (rotateCoroutine != null)
        {
            StopCoroutine(rotateCoroutine);
            rotateCoroutine = null;
        }

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
        rotateCoroutine = null;
    }

    private void HandleLights()
    {
        if (flickerLightCoroutine != null)
        {
            StopCoroutine(flickerLightCoroutine);
            flickerLightCoroutine = null;
        }


        if (isOn)
        {
            foreach (var setting in lightsToEffect)
            {
                Light lightComp = setting.lightGO.GetComponentInChildren<Light>();
                Renderer rend = setting.lightGO.GetComponentInChildren<Renderer>();

                if (lightComp == null || rend == null) continue;

                Material matInstance = rend.material; // kendi kopyasýný alýyoruz
                matInstance.EnableKeyword("_EMISSION");

                if (Random.Range(0, 100) < setting.flickerPossibility)
                {
                    int flickerCount = Random.Range(minFlickerCount, maxFlickerCount);
                    flickerLightCoroutine = StartCoroutine(FlickerLight(lightComp, matInstance, baseEmission, flickerCount));
                }

                lightComp.enabled = true;
                lightComp.intensity = baseIntensity;
                matInstance.SetColor("_EmissionColor", baseEmission);
            }
        }
        else
        {
            foreach (var setting in lightsToEffect)
            {
                Light lightComp = setting.lightGO.GetComponentInChildren<Light>();
                Renderer rend = setting.lightGO.GetComponentInChildren<Renderer>();

                if (lightComp != null)
                    lightComp.enabled = false;

                Material matInstance = rend.material; // kendi kopyasýný alýyoruz
                matInstance.DisableKeyword("_EMISSION");
            }
        }
    }

    private IEnumerator FlickerLight(Light light, Material mat, Color baseEmission, int flickerCount)
    {
        light.enabled = true;

        for (int i = 0; i < flickerCount; i++)
        {
            float randomIntensity = Random.Range(0.01f, baseIntensity * 0.3f);
            float emissionMultiplier = randomIntensity / (baseIntensity * 2);
            Color flickerEmission = baseEmission * Mathf.LinearToGammaSpace(emissionMultiplier);

            light.intensity = randomIntensity;
            mat.SetColor("_EmissionColor", flickerEmission);
            yield return new WaitForSeconds(Random.Range(flickerMinDelay, flickerMaxDelay));

            light.intensity = baseIntensity;
            mat.SetColor("_EmissionColor", baseEmission);
            yield return new WaitForSeconds(Random.Range(flickerMinDelay, flickerMaxDelay));
        }
    }
}
