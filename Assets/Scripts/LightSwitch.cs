using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VLB;

public class LightSwitch : MonoBehaviour, IInteractable
{
    public enum LightSourceType
    {
        Floresan,
        NormalAmpul
    }

    // --- CONFIG: Tür Bazlý Genel Ayarlar ---
    [System.Serializable]
    public class LightTypeConfig
    {
        public string label = "Ayarlar";
        public Material sourceMaterial;

        [Tooltip("Maksimum ýþýk þiddeti")]
        public float maxBaseIntensity = 2f;

        [Tooltip("Volumetric Light Beam kullanýlsýn mý?")]
        public bool useVLB = false;

        [Tooltip("VLB Maksimum Opacity/Intensity")]
        [Range(0f, 5f)]
        public float maxVLBIntensity = 1f;

        [Header("Explosion Settings")]
        [Tooltip("Patlama sesi (Opsiyonel)")]
        public AudioClip explosionSound;

        [Tooltip("Her iki tür için de ortak kývýlcým efekti")]
        public GameObject sparksParticlePrefab;

        [Tooltip("Sadece FLORESAN için: Cam kýrýklarý efekti")]
        public GameObject shardsParticlePrefab;

        [Tooltip("Sadece FLORESAN için: Patlayýnca deðiþecek kýrýk mesh")]
        public Mesh brokenMesh;
    }

    // --- INSTANCE: Sahnedeki her bir ýþýðýn ayarý ---
    [System.Serializable]
    public class LightInstanceSettings
    {
        public string name = "Light Name";
        public GameObject lightGO;
        public LightSourceType lightType;

        [Header("Brightness Settings")]
        [Range(0f, 1f)] public float minGlowMultiplier = 0.5f;
        [Range(0f, 1f)] public float maxGlowMultiplier = 1f;

        [Header("Flicker & Explosion")]
        [Range(0, 100)] public int flickerPossibility;
        [Range(0, 100)] public int explosionPossibility;

        // --- CACHE & STATE ---
        [HideInInspector] public Material runtimeMaterial;
        [HideInInspector] public Color baseEmissionColor;
        [HideInInspector] public Light lightComponent;
        [HideInInspector] public VolumetricLightBeamSD vlbComponent;

        [HideInInspector] public MeshFilter meshFilterComponent;
        [HideInInspector] public Rigidbody rigidBodyComponent;

        [HideInInspector] public bool isBroken = false;
    }

    // --- DEÐÝÞKENLER ---
    public bool CanInteract { get => canInteract; set => canInteract = value; }
    [SerializeField] private bool canInteract = true;
    public PlayerManager.HandRigTypes HandRigType { get => handRigType; set => handRigType = value; }
    [SerializeField] private PlayerManager.HandRigTypes handRigType;
    public bool OutlineShouldBeRed { get => outlineShouldBeRed; set => outlineShouldBeRed = value; }
    [SerializeField] private bool outlineShouldBeRed;
    public string FocusTextKey { get => focusTextKeys[switchStateNum]; set => focusTextKeys[switchStateNum] = value; }
    [SerializeField] private string[] focusTextKeys;
    private int switchStateNum = 0;

    [Header("Global Type Configurations")]
    [SerializeField] private LightTypeConfig fluorescentConfig;
    [SerializeField] private LightTypeConfig bulbConfig;

    [Header("Light Instances")]
    [SerializeField] private List<LightInstanceSettings> lightsToEffect;

    [Header("Switch Settings")]
    [SerializeField] private AudioSource buzzSoundSource;
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
    [SerializeField] private float flickerMinDelay = 0.1f;
    [SerializeField] private float flickerMaxDelay = 0.3f;
    [SerializeField] private int minFlickerCount = 3;
    [SerializeField] private int maxFlickerCount = 12;

    [Header("Audio Settings")]
    public AudioClip lightSwitchSound;
    public float minPitchForOn = 1f;
    public float maxPitchForOn = 1.2f;
    public float minPitchForOff = 0.7f;
    public float maxPitchForOff = 0.9f;

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");
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

        InitializeLights();
    }

    private void InitializeLights()
    {
        foreach (var setting in lightsToEffect)
        {
            if (setting.lightGO == null) continue;

            setting.isBroken = false;

            // Component Cache
            setting.lightComponent = setting.lightGO.GetComponentInChildren<Light>();
            setting.vlbComponent = setting.lightGO.GetComponentInChildren<VolumetricLightBeamSD>();
            setting.meshFilterComponent = setting.lightGO.GetComponentInChildren<MeshFilter>();
            setting.rigidBodyComponent = setting.lightGO.GetComponent<Rigidbody>();

            LightTypeConfig activeConfig = (setting.lightType == LightSourceType.Floresan) ? fluorescentConfig : bulbConfig;
            Renderer rend = setting.lightGO.GetComponentInChildren<Renderer>();

            if (rend != null && activeConfig.sourceMaterial != null)
            {
                if (activeConfig.sourceMaterial.HasProperty(EmissionColorID))
                    setting.baseEmissionColor = activeConfig.sourceMaterial.GetColor(EmissionColorID);
                else
                    setting.baseEmissionColor = Color.white;

                setting.runtimeMaterial = new Material(activeConfig.sourceMaterial);
                setting.runtimeMaterial.SetColor(EmissionColorID, Color.black);
                rend.material = setting.runtimeMaterial;
            }

            if (setting.vlbComponent != null) setting.vlbComponent.enabled = false;
            if (setting.lightComponent != null) setting.lightComponent.enabled = false;
        }
    }

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

    private void HandleLights()
    {
        if (isOn)
        {
            foreach (var setting in lightsToEffect)
            {
                if (setting.isBroken) continue;
                if (setting.lightComponent == null || setting.runtimeMaterial == null) continue;

                LightTypeConfig config = (setting.lightType == LightSourceType.Floresan) ? fluorescentConfig : bulbConfig;

                float randomMultiplier = Random.Range(setting.minGlowMultiplier, setting.maxGlowMultiplier);
                float targetIntensity = config.maxBaseIntensity * randomMultiplier;
                float targetVLBIntensity = config.maxVLBIntensity * randomMultiplier;
                Color targetEmissionColor = setting.baseEmissionColor * randomMultiplier;

                setting.runtimeMaterial.EnableKeyword("_EMISSION");
                setting.lightComponent.enabled = true;

                if (setting.vlbComponent != null && config.useVLB)
                {
                    setting.vlbComponent.enabled = true;
                    setting.vlbComponent.intensityGlobal = targetVLBIntensity;
                }
                else if (setting.vlbComponent != null)
                {
                    setting.vlbComponent.enabled = false;
                }

                bool willFlicker = Random.Range(0, 100) < setting.flickerPossibility;

                if (willFlicker)
                {
                    int flickerCount = Random.Range(minFlickerCount, maxFlickerCount);
                    StartCoroutine(FlickerLightRoutine(setting, targetIntensity, targetEmissionColor, targetVLBIntensity, flickerCount, config));
                }
                else
                {
                    setting.lightComponent.intensity = targetIntensity;
                    setting.runtimeMaterial.SetColor(EmissionColorID, targetEmissionColor);
                    buzzSoundSource.volume += buzzSoundIncreasePerLight;
                }
            }
        }
        else
        {
            StopAllCoroutines();
            if (rotateCoroutine != null) StopCoroutine(rotateCoroutine);
            rotateCoroutine = StartCoroutine(ToogleRotate(isOn));

            buzzSoundSource.volume = 0f;

            foreach (var setting in lightsToEffect)
            {
                if (setting.isBroken) continue;

                if (setting.lightComponent != null) setting.lightComponent.enabled = false;
                if (setting.vlbComponent != null) setting.vlbComponent.enabled = false;
                if (setting.runtimeMaterial != null) setting.runtimeMaterial.DisableKeyword("_EMISSION");
            }
        }
    }

    // --- BURASI GÜNCELLENDÝ ---
    private IEnumerator FlickerLightRoutine(LightInstanceSettings setting, float maxIntensity, Color maxEmission, float maxVLB, int flickerCount, LightTypeConfig config)
    {
        Light light = setting.lightComponent;
        Material mat = setting.runtimeMaterial;
        VolumetricLightBeamSD vlb = setting.vlbComponent;

        // 1. Önce bu turda patlayýp patlamayacaðýna karar ver
        bool isDestinedToExplode = Random.Range(0, 100) < setting.explosionPossibility;
        int explosionIndex = -1;

        if (isDestinedToExplode)
        {
            // 2. Patlayacaksa, HANGÝ flicker turunda patlayacaðýný seç.
            //    Mantýk: Toplam flicker sayýsýnýn yarýsý ile tamamý arasýnda bir yerde patlasýn.
            //    Böylece ýþýk en az birkaç kere yanýp söner, oyuncu tam alýþýrken patlar.

            int minIndex = Mathf.Max(1, flickerCount / 2); // En az 1. tur olsun ki hemen gümlemesin
            explosionIndex = Random.Range(minIndex, flickerCount);
        }

        for (int i = 0; i < flickerCount; i++)
        {
            // 3. Sýra geldi mi kontrol et
            if (isDestinedToExplode && i == explosionIndex)
            {
                ExplodeLight(setting, config);
                yield break; // Döngüyü kýr ve çýk
            }

            // --- SÖNME ANI ---
            float randomDimIntensity = Random.Range(0.01f, maxIntensity * 0.3f);
            float ratio = randomDimIntensity / maxIntensity;
            Color flickerEmission = maxEmission * ratio;

            light.intensity = randomDimIntensity;
            mat.SetColor(EmissionColorID, flickerEmission);

            if (vlb != null && config.useVLB) vlb.enabled = false;

            yield return new WaitForSeconds(Random.Range(flickerMinDelay, flickerMaxDelay));

            // --- YANMA ANI ---
            light.intensity = maxIntensity;
            mat.SetColor(EmissionColorID, maxEmission);

            if (vlb != null && config.useVLB)
            {
                vlb.enabled = true;
                vlb.intensityGlobal = maxVLB;
            }

            SoundManager.Instance.PlayRandomSoundFX(flickClips, light.transform, flickVolume, flickMinPitch, flickMaxPitch);

            yield return new WaitForSeconds(Random.Range(flickerMinDelay, flickerMaxDelay));
        }

        // Bitiþ (Eðer patlamadýysa)
        light.intensity = maxIntensity;
        mat.SetColor(EmissionColorID, maxEmission);
        if (vlb != null && config.useVLB)
        {
            vlb.enabled = true;
            vlb.intensityGlobal = maxVLB;
        }

        buzzSoundSource.volume += buzzSoundIncreasePerLight;
    }

    private void ExplodeLight(LightInstanceSettings setting, LightTypeConfig config)
    {
        setting.isBroken = true;

        if (setting.lightComponent != null) setting.lightComponent.enabled = false;
        if (setting.vlbComponent != null) setting.vlbComponent.enabled = false;
        if (setting.runtimeMaterial != null) setting.runtimeMaterial.DisableKeyword("_EMISSION");

        if (buzzSoundSource.volume > 0) buzzSoundSource.volume -= buzzSoundIncreasePerLight;

        if (config.explosionSound != null)
        {
            SoundManager.Instance.PlaySoundFX(config.explosionSound, setting.lightGO.transform, 1f, 0.9f, 1.1f);
        }

        if (setting.lightType == LightSourceType.Floresan)
        {
            if (setting.meshFilterComponent != null && config.brokenMesh != null)
            {
                setting.meshFilterComponent.mesh = config.brokenMesh;
            }
            if (config.shardsParticlePrefab != null)
            {
                Instantiate(config.shardsParticlePrefab, setting.lightGO.transform.position, setting.lightGO.transform.rotation);
            }
            if (config.sparksParticlePrefab != null)
            {
                Instantiate(config.sparksParticlePrefab, setting.lightGO.transform.position, setting.lightGO.transform.rotation);
            }
        }
        else
        {
            if (setting.rigidBodyComponent != null)
            {
                setting.rigidBodyComponent.isKinematic = false;
                setting.rigidBodyComponent.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);
            }
            if (config.sparksParticlePrefab != null)
            {
                Instantiate(config.sparksParticlePrefab, setting.lightGO.transform.position, setting.lightGO.transform.rotation);
            }
        }
    }
}