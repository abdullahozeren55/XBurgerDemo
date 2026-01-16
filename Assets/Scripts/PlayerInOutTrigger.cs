using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using VLB; // VLB Kütüphanesi

public class PlayerInOutTrigger : MonoBehaviour
{
    // --- ENUMLAR ---
    public enum InOutTriggerType
    {
        Null,
        EnterColdRoom,
        ExitColdRoom,
        TurnOnLights,
        TurnOffLights,
    }

    public enum LightSourceType
    {
        Floresan,
        NormalAmpul
    }

    // --- CONFIG CLASSLARI (LightSwitch'ten alýndý) ---
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

    // --- TRIGGER AYARLARI ---
    [Header("Yön Ayarlarý")]
    [Tooltip("Yeþil Tarafa (Forward) geçince devreye girecek Actionlar")]
    public InOutTriggerType[] greenActions;

    [Tooltip("Kýrmýzý Tarafa (Back) geçince devreye girecek Actionlar")]
    public InOutTriggerType[] redActions;

    // --- OTOMATÝK IÞIK GECÝKME AYARLARI ---
    [Header("Automatic Light Delays")]
    [SerializeField] private float minTurnOnDelay = 0.5f;
    [SerializeField] private float maxTurnOnDelay = 1.5f;
    [Space]
    [SerializeField] private float minTurnOffDelay = 2.0f;
    [SerializeField] private float maxTurnOffDelay = 5.0f;

    // --- IÞIK KONFÝGÜRASYONLARI ---
    [Header("Global Type Configurations")]
    [SerializeField] private LightTypeConfig fluorescentConfig;
    [SerializeField] private LightTypeConfig bulbConfig;

    [Header("Light Instances")]
    [SerializeField] private List<LightInstanceSettings> lightsToEffect;

    [Header("Audio Settings")]
    [SerializeField] private AudioSource buzzSoundSource; // Floresan cýzýrtýsý için
    [SerializeField] private AudioClip sensorOnSound;  // Açýlma "Klik" sesi
    [SerializeField] private AudioClip sensorOffSound; // Kapanma "Klik" sesi

    [Header("Flicker Audio Settings")]
    [SerializeField] private AudioClip[] flickClips;
    [SerializeField] private float flickVolume = 1f;
    [SerializeField] private float flickMinPitch = 0.85f;
    [SerializeField] private float flickMaxPitch = 1.15f;

    [Header("Continuous Flicker Settings")]
    [SerializeField] private float minFlickerCheckDelay = 5f;
    [SerializeField] private float maxFlickerCheckDelay = 15f;

    [Header("Flicker Timing")]
    [SerializeField] private float flickerMinDelay = 0.1f;
    [SerializeField] private float flickerMaxDelay = 0.3f;
    [SerializeField] private int minFlickerCount = 3;
    [SerializeField] private int maxFlickerCount = 12;

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    // State Variables
    private bool isOn = false;
    private Coroutine pendingSwitchCoroutine; // O an saymakta olan açma/kapama sayacý
    private float buzzSoundIncreasePerLight = 0.01f; // Buzz artýþ oraný (default)

    private void Awake()
    {
        isOn = false; // Varsayýlan kapalý baþlasýn
        InitializeLights();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 localPos = transform.InverseTransformPoint(other.transform.position);

            // Hangi yöne geçtiðini belirle
            InOutTriggerType[] actionsToExecute = (localPos.z > 0) ? greenActions : redActions;

            foreach (InOutTriggerType action in actionsToExecute)
            {
                switch (action)
                {
                    case InOutTriggerType.EnterColdRoom:
                        PlayerManager.Instance.HandlePlayerEnterExitColdRoom(true);
                        break;
                    case InOutTriggerType.ExitColdRoom:
                        PlayerManager.Instance.HandlePlayerEnterExitColdRoom(false);
                        break;
                    case InOutTriggerType.TurnOnLights:
                        TriggerLights(true); // Açmayý tetikle
                        break;
                    case InOutTriggerType.TurnOffLights:
                        TriggerLights(false); // Kapatmayý tetikle
                        break;
                }
            }
        }
    }

    // --- GECÝKMELÝ TETÝKLEME MANTIÐI ---
    private void TriggerLights(bool shouldTurnOn)
    {
        // 1. Durum: Zaten istediðimiz durumdayýz ve bekleyen bir iþlem yok.
        // Örn: Iþýklar açýk, tekrar "Aç" komutu geldi. Hiçbir þey yapma.
        if (isOn == shouldTurnOn && pendingSwitchCoroutine == null) return;

        // 2. Durum: Bekleyen bir iþlem (Delay) var.
        if (pendingSwitchCoroutine != null)
        {
            // Bekleyen iþlemi (örn: Kapanma sayacýný) iptal et.
            StopCoroutine(pendingSwitchCoroutine);
            pendingSwitchCoroutine = null;

            // KRÝTÝK DÜZELTME BURADA:
            // Ýþlemi iptal ettikten sonra bakýyoruz; þu anki durum (isOn) zaten
            // varmak istediðimiz durum (shouldTurnOn) ile ayný mý?
            // Aynýysa (örn: Iþýk hala açýk, biz de açmak istiyoruz), 
            // yeni bir rutin baþlatmaya gerek yok. Iþýk açýk kalmaya devam etsin.
            if (isOn == shouldTurnOn) return;
        }

        // 3. Durum: Durum farklý, yeni bir geçiþ süreci baþlat.
        pendingSwitchCoroutine = StartCoroutine(DelayedSwitchRoutine(shouldTurnOn));
    }

    private IEnumerator DelayedSwitchRoutine(bool targetState)
    {
        // 1. Gecikme Süresini Hesapla
        float delay = targetState ?
            Random.Range(minTurnOnDelay, maxTurnOnDelay) :
            Random.Range(minTurnOffDelay, maxTurnOffDelay);

        yield return new WaitForSeconds(delay);

        // 2. Durumu Deðiþtir ve Sesi Çal
        isOn = targetState;

        AudioClip clip = targetState ? sensorOnSound : sensorOffSound;
        if (clip != null) SoundManager.Instance.PlaySoundFX(clip, buzzSoundSource.transform);

        // 3. Iþýklarý Yönet
        HandleLights();

        pendingSwitchCoroutine = null;
    }

    // --- IÞIK YÖNETÝMÝ (LightSwitch.cs'den uyarlandý) ---
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
            // --- KAPATMA ---
            if (buzzSoundSource != null) buzzSoundSource.volume = 0f;

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

    // --- IÞIK YAÞAM DÖNGÜSÜ ---
    private IEnumerator LightLifecycle(LightInstanceSettings setting)
    {
        LightTypeConfig config = (setting.lightType == LightSourceType.Floresan) ? fluorescentConfig : bulbConfig;

        float randomMultiplier = Random.Range(setting.minGlowMultiplier, setting.maxGlowMultiplier);
        float targetIntensity = config.maxBaseIntensity * randomMultiplier;
        float targetVLBIntensity = config.maxVLBIntensity * randomMultiplier;
        Color targetEmissionColor = setting.baseEmissionColor * randomMultiplier;

        // Ýlk Açýlýþ Flicker
        bool initialFlicker = Random.Range(0, 100) < setting.flickerPossibility;

        setting.runtimeMaterial.EnableKeyword("_EMISSION");
        setting.lightComponent.enabled = true;

        if (initialFlicker)
        {
            int flickerCount = Random.Range(minFlickerCount, maxFlickerCount);
            yield return StartCoroutine(FlickerLightRoutine(setting, targetIntensity, targetEmissionColor, targetVLBIntensity, flickerCount, config));
            SetLightStable(setting, targetIntensity, targetEmissionColor, targetVLBIntensity, config.useVLB);
        }
        else
        {
            SetLightStable(setting, targetIntensity, targetEmissionColor, targetVLBIntensity, config.useVLB);
        }

        if (setting.isBroken || !isOn) yield break;

        // Buzz Sesini Ekle (Sadece Floresan)
        if (setting.lightType == LightSourceType.Floresan && buzzSoundSource != null)
        {
            buzzSoundSource.volume += buzzSoundIncreasePerLight;
        }

        // Periyodik Kontrol
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
                SetLightStable(setting, targetIntensity, targetEmissionColor, targetVLBIntensity, config.useVLB);
            }
        }
    }

    private void SetLightStable(LightInstanceSettings setting, float intensity, Color emission, float vlbIntensity, bool useVLB)
    {
        if (!isOn || setting.isBroken) return;

        setting.runtimeMaterial.EnableKeyword("_EMISSION");
        setting.lightComponent.enabled = true;
        setting.lightComponent.intensity = intensity;
        setting.runtimeMaterial.SetColor(EmissionColorID, emission);

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

    private IEnumerator FlickerLightRoutine(LightInstanceSettings setting, float maxIntensity, Color maxEmission, float maxVLB, int flickerCount, LightTypeConfig config)
    {
        Light light = setting.lightComponent;
        Material mat = setting.runtimeMaterial;
        VolumetricLightBeamSD vlb = setting.vlbComponent;

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

            // Sönme
            float randomDimIntensity = Random.Range(0.01f, maxIntensity * 0.3f);
            float ratio = randomDimIntensity / maxIntensity;
            Color flickerEmission = maxEmission * ratio;

            light.intensity = randomDimIntensity;
            mat.SetColor(EmissionColorID, flickerEmission);

            if (vlb != null && config.useVLB) vlb.enabled = false;

            yield return new WaitForSeconds(Random.Range(flickerMinDelay, flickerMaxDelay));

            // Yanma
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

        if (setting.lightType == LightSourceType.Floresan && isOn && buzzSoundSource != null)
        {
            if (buzzSoundSource.volume > 0)
                buzzSoundSource.volume -= buzzSoundIncreasePerLight;
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

    // --- GÝZMO ÇÝZÝMÝ ---
    private void OnDrawGizmos()
    {
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;

        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(box.center, box.size);

            Gizmos.color = new Color(0, 1, 0, 0.8f);
            Vector3 greenPos = box.center + new Vector3(0, 0, 0.5f);
            Gizmos.DrawSphere(greenPos, 0.2f);

            Gizmos.color = new Color(1, 0, 0, 0.8f);
            Vector3 redPos = box.center - new Vector3(0, 0, 0.5f);
            Gizmos.DrawSphere(redPos, 0.2f);

            Gizmos.color = Color.white;
            Gizmos.DrawLine(redPos, greenPos);
        }
    }
}