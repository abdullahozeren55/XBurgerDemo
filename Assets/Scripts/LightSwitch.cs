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

    [System.Serializable]
    public class LightTypeConfig
    {
        public string label = "Ayarlar";
        public Material sourceMaterial;
        public float maxBaseIntensity = 2f;
        public bool useVLB = false;
        [Range(0f, 5f)] public float maxVLBIntensity = 1f;

        [Header("Explosion Settings")]
        public AudioClip explosionSound;
        public GameObject sparksParticlePrefab;
        public GameObject shardsParticlePrefab;
        public Mesh brokenMesh;
        [Tooltip("Ampul düþerken uygulanacak itme kuvveti")]
        public float bulbExplosionForce = 0.5f;
    }

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

        [HideInInspector] public Coroutine activeLifecycleCoroutine;
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
    private Coroutine switchRotateCoroutine;
    private bool isOn;

    [Header("Continuous Flicker Settings")]
    [Tooltip("Iþýk yandýktan sonra tekrar titremeyi denemek için bekleyeceði min süre")]
    [SerializeField] private float minFlickerCheckDelay = 5f;
    [Tooltip("Iþýk yandýktan sonra tekrar titremeyi denemek için bekleyeceði max süre")]
    [SerializeField] private float maxFlickerCheckDelay = 15f;

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

        if (switchRotateCoroutine != null) StopCoroutine(switchRotateCoroutine);
        switchRotateCoroutine = StartCoroutine(ToogleRotate(isOn));
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

                if (setting.activeLifecycleCoroutine != null) StopCoroutine(setting.activeLifecycleCoroutine);
                setting.activeLifecycleCoroutine = StartCoroutine(LightLifecycle(setting));
            }
        }
        else
        {
            buzzSoundSource.volume = 0f;

            foreach (var setting in lightsToEffect)
            {
                if (setting.activeLifecycleCoroutine != null)
                {
                    StopCoroutine(setting.activeLifecycleCoroutine);
                    setting.activeLifecycleCoroutine = null;
                }

                if (setting.isBroken) continue;

                if (setting.lightComponent != null) setting.lightComponent.enabled = false;
                if (setting.vlbComponent != null) setting.vlbComponent.enabled = false;
                if (setting.runtimeMaterial != null) setting.runtimeMaterial.DisableKeyword("_EMISSION");
            }
        }
    }

    // --- YARDIMCI METOT: Iþýðý Stabil Hale Getir ---
    // Bu metot, ýþýðý zorla olmasý gereken parlaklýða ve renge sabitler.
    // Flicker bittiðinde veya direkt açýlýþta kullanýlýr.
    private void SetLightStable(LightInstanceSettings setting, float intensity, Color emission, float vlbIntensity, bool useVLB)
    {
        if (!isOn || setting.isBroken) return; // Güvenlik kontrolü

        // 1. Emission'u kesinlikle aç
        setting.runtimeMaterial.EnableKeyword("_EMISSION");

        // 2. Iþýðý aç ve deðerleri ata
        setting.lightComponent.enabled = true;
        setting.lightComponent.intensity = intensity;
        setting.runtimeMaterial.SetColor(EmissionColorID, emission);

        // 3. VLB ayarla
        if (setting.vlbComponent != null && useVLB)
        {
            setting.vlbComponent.enabled = true;
            setting.vlbComponent.intensityGlobal = vlbIntensity;
        }
        else if (setting.vlbComponent != null)
        {
            setting.vlbComponent.enabled = false;
        }
    }

    private IEnumerator LightLifecycle(LightInstanceSettings setting)
    {
        LightTypeConfig config = (setting.lightType == LightSourceType.Floresan) ? fluorescentConfig : bulbConfig;

        float randomMultiplier = Random.Range(setting.minGlowMultiplier, setting.maxGlowMultiplier);
        float targetIntensity = config.maxBaseIntensity * randomMultiplier;
        float targetVLBIntensity = config.maxVLBIntensity * randomMultiplier;
        Color targetEmissionColor = setting.baseEmissionColor * randomMultiplier;

        // --- ÝLK AÇILIÞ ---
        bool initialFlicker = Random.Range(0, 100) < setting.flickerPossibility;

        // Flicker path'ine girse bile Emission keyword'ü aktif olsun!
        setting.runtimeMaterial.EnableKeyword("_EMISSION");
        setting.lightComponent.enabled = true;

        if (initialFlicker)
        {
            int flickerCount = Random.Range(minFlickerCount, maxFlickerCount);
            yield return StartCoroutine(FlickerLightRoutine(setting, targetIntensity, targetEmissionColor, targetVLBIntensity, flickerCount, config));

            // BUG FIX: Flicker bittiðinde ýþýk kýsýk kalmasýn, orijinal gücüne dönsün.
            SetLightStable(setting, targetIntensity, targetEmissionColor, targetVLBIntensity, config.useVLB);
        }
        else
        {
            // Flicker yoksa direkt stabil hale getir
            SetLightStable(setting, targetIntensity, targetEmissionColor, targetVLBIntensity, config.useVLB);
        }

        if (setting.isBroken || !isOn) yield break;

        // Buzz Sesi Ekle (Sadece Floresan)
        if (setting.lightType == LightSourceType.Floresan)
        {
            buzzSoundSource.volume += buzzSoundIncreasePerLight;
        }

        // --- SONSUZ DÖNGÜ (Periyodik Kontrol) ---
        while (isOn && !setting.isBroken)
        {
            float waitTime = Random.Range(minFlickerCheckDelay, maxFlickerCheckDelay);
            yield return new WaitForSeconds(waitTime);

            if (!isOn || setting.isBroken) yield break;

            bool periodicFlicker = Random.Range(0, 100) < setting.flickerPossibility;

            if (periodicFlicker)
            {
                int pFlickerCount = Random.Range(minFlickerCount, maxFlickerCount);
                yield return StartCoroutine(FlickerLightRoutine(setting, targetIntensity, targetEmissionColor, targetVLBIntensity, pFlickerCount, config));

                // BUG FIX: Periyodik flicker bittiðinde de ýþýðý eski gücüne getir
                SetLightStable(setting, targetIntensity, targetEmissionColor, targetVLBIntensity, config.useVLB);
            }
        }
    }

    private IEnumerator FlickerLightRoutine(LightInstanceSettings setting, float maxIntensity, Color maxEmission, float maxVLB, int flickerCount, LightTypeConfig config)
    {
        Light light = setting.lightComponent;
        Material mat = setting.runtimeMaterial;
        VolumetricLightBeamSD vlb = setting.vlbComponent;

        // Garanti olsun diye burada da açýyoruz
        mat.EnableKeyword("_EMISSION");

        bool isDestinedToExplode = Random.Range(0, 100) < setting.explosionPossibility;
        int explosionIndex = -1;

        if (isDestinedToExplode)
        {
            int minIndex = Mathf.Max(1, flickerCount / 2);
            explosionIndex = Random.Range(minIndex, flickerCount);
        }

        for (int i = 0; i < flickerCount; i++)
        {
            if (!isOn) yield break;

            if (isDestinedToExplode && i == explosionIndex)
            {
                ExplodeLight(setting, config);
                yield break;
            }

            // --- SÖNME ---
            float randomDimIntensity = Random.Range(0.01f, maxIntensity * 0.3f);
            float ratio = randomDimIntensity / maxIntensity;
            Color flickerEmission = maxEmission * ratio;

            light.intensity = randomDimIntensity;
            mat.SetColor(EmissionColorID, flickerEmission);

            if (vlb != null && config.useVLB) vlb.enabled = false;

            yield return new WaitForSeconds(Random.Range(flickerMinDelay, flickerMaxDelay));

            // --- YANMA ---
            if (!isOn) yield break;

            light.intensity = maxIntensity;
            mat.SetColor(EmissionColorID, maxEmission);

            if (vlb != null && config.useVLB)
            {
                vlb.enabled = true;
                vlb.intensityGlobal = maxVLB;
            }

            if (setting.lightType == LightSourceType.Floresan)
            {
                SoundManager.Instance.PlayRandomSoundFX(flickClips, light.transform, flickVolume, flickMinPitch, flickMaxPitch);
            }

            yield return new WaitForSeconds(Random.Range(flickerMinDelay, flickerMaxDelay));
        }
    }

    private void ExplodeLight(LightInstanceSettings setting, LightTypeConfig config)
    {
        setting.isBroken = true;

        if (setting.lightComponent != null) setting.lightComponent.enabled = false;
        if (setting.vlbComponent != null) setting.vlbComponent.enabled = false;
        if (setting.runtimeMaterial != null) setting.runtimeMaterial.DisableKeyword("_EMISSION");

        if (setting.lightType == LightSourceType.Floresan && isOn)
        {
            if (buzzSoundSource.volume > 0)
            {
                buzzSoundSource.volume -= buzzSoundIncreasePerLight;
            }
        }

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
            if (setting.meshFilterComponent != null && config.brokenMesh != null)
            {
                setting.meshFilterComponent.mesh = config.brokenMesh;
            }

            if (setting.rigidBodyComponent != null)
            {
                setting.rigidBodyComponent.isKinematic = false;
                setting.rigidBodyComponent.AddTorque(Random.insideUnitSphere * 2f, ForceMode.Impulse);

                Vector3 randomDirection = Random.insideUnitSphere;
                randomDirection.y = -1f;
                float forcePower = config.bulbExplosionForce;
                setting.rigidBodyComponent.AddForce(randomDirection * forcePower, ForceMode.Impulse);
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
    }
}