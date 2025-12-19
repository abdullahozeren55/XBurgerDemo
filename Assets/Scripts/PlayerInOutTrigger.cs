using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using static LightSwitch;
using VLB; // <--- EKLEME: VLB Kütüphanesi

public class PlayerInOutTrigger : MonoBehaviour
{
    public enum InOutTriggerType
    {
        Null,
        EnterColdRoom,
        ExitColdRoom,
        TurnOnLights,
        TurnOffLights
    }
    [Header("Yön Ayarlarý")]
    [Tooltip("Yeþil Tarafa (Forward) geçince devreye girecek Snapshot")]
    public InOutTriggerType[] greenActions;

    [Tooltip("Kýrmýzý Tarafa (Back) geçince devreye girecek Snapshot")]
    public InOutTriggerType[] redActions;

    [Header("Light Settings")]

    [SerializeField] private AudioSource buzzSoundSource;
    [SerializeField] private LightSettings[] lightsToEffect;
    [SerializeField] private Material lightMatToCopy;
    [Space]
    [SerializeField] private float buzzSoundIncreasePerLight = 0.01f;

    [Space]

    [SerializeField] private AudioClip[] flickClips;
    [SerializeField] private float flickVolume = 1f;
    [SerializeField] private float flickMinPitch = 0.85f;
    [SerializeField] private float flickMaxPitch = 1.15f;
    [SerializeField] private float baseIntensity = 20f;
    [SerializeField] private float flickerMinDelay = 0.1f;
    [SerializeField] private float flickerMaxDelay = 0.3f;
    [SerializeField] private int minFlickerCount = 3;
    [SerializeField] private int maxFlickerCount = 12;
    [SerializeField] private float turnOffDelay = 3f;

    [Space]

    [SerializeField] private AudioClip turnOnLightSound;
    [SerializeField] private float turnOnLightVolume = 1f;
    [SerializeField] private float turnOnLightMinPitch = 0.8f;
    [SerializeField] private float turnOnLightMaxPitch = 1.2f;

    [Space]

    [SerializeField] private AudioClip turnOffLightSound;
    [SerializeField] private float turnOffLightVolume = 1f;
    [SerializeField] private float turnOffLightMinPitch = 0.8f;
    [SerializeField] private float turnOffLightMaxPitch = 1.2f;

    private Coroutine currentHandieLightCoroutine;

    private bool lightsOn;
    private Color baseEmission;
    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    private void Awake()
    {
        lightsOn = false;

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


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Vector3 localPos = transform.InverseTransformPoint(other.transform.position);

            if (localPos.z > 0)
            {
                foreach (InOutTriggerType action in greenActions)
                {
                    if (action == InOutTriggerType.EnterColdRoom)
                        PlayerManager.Instance.HandlePlayerEnterExitColdRoom(true);
                    else if (action == InOutTriggerType.ExitColdRoom)
                        PlayerManager.Instance.HandlePlayerEnterExitColdRoom(false);
                    else if (action == InOutTriggerType.TurnOnLights)
                    {
                        if (currentHandieLightCoroutine != null)
                        {
                            StopCoroutine(currentHandieLightCoroutine);
                            currentHandieLightCoroutine = null;
                        }

                        if (!lightsOn)
                            HandleLights(true);
                    }

                    else if (action == InOutTriggerType.TurnOffLights && lightsOn)
                    {
                        if (currentHandieLightCoroutine != null)
                        {
                            StopCoroutine(currentHandieLightCoroutine);
                            currentHandieLightCoroutine = null;
                        }

                        currentHandieLightCoroutine = StartCoroutine(HandleLightsWithDelay());
                    }
                }

            }
            else
            {
                foreach (InOutTriggerType action in redActions)
                {
                    if (action == InOutTriggerType.EnterColdRoom)
                        PlayerManager.Instance.HandlePlayerEnterExitColdRoom(true);
                    else if (action == InOutTriggerType.ExitColdRoom)
                        PlayerManager.Instance.HandlePlayerEnterExitColdRoom(false);
                    else if (action == InOutTriggerType.TurnOnLights)
                    {
                        if (currentHandieLightCoroutine != null)
                        {
                            StopCoroutine(currentHandieLightCoroutine);
                            currentHandieLightCoroutine = null;
                        }

                        if (!lightsOn)
                            HandleLights(true);
                    }

                    else if (action == InOutTriggerType.TurnOffLights && lightsOn)
                    {
                        if (currentHandieLightCoroutine != null)
                        {
                            StopCoroutine(currentHandieLightCoroutine);
                            currentHandieLightCoroutine = null;
                        }

                        currentHandieLightCoroutine = StartCoroutine(HandleLightsWithDelay());
                    }
                }
            }
        }
    }

    private void HandleLights(bool shouldOn)
    {
        lightsOn = shouldOn;

        if (lightsOn)
        {
            SoundManager.Instance.PlaySoundFX(turnOnLightSound, buzzSoundSource.transform, turnOnLightVolume, turnOnLightMinPitch, turnOnLightMaxPitch);
            // IÞIKLARI AÇIYORUZ
            foreach (var setting in lightsToEffect)
            {
                Light lightComp = setting.lightGO.GetComponentInChildren<Light>();
                Renderer rend = setting.lightGO.GetComponentInChildren<Renderer>();

                // --- EKLEME: VLB Componentini al ---
                VolumetricLightBeamSD vlb = setting.lightGO.GetComponentInChildren<VolumetricLightBeamSD>();

                if (lightComp == null || rend == null) continue;

                Material matInstance = rend.material;
                matInstance.EnableKeyword("_EMISSION");
                lightComp.enabled = true;

                // Flicker yoksa hemen açýyoruz, flicker varsa coroutine içinde açacaðýz
                // Ama varsayýlan olarak açýk kalsýn, flicker yönetecek
                if (vlb != null) vlb.enabled = true;

                // Flicker olacak mý?
                bool willFlicker = Random.Range(0, 100) < setting.flickerPossibility;

                if (willFlicker)
                {
                    // FLICKER VARSA: VLB'yi de gönderiyoruz
                    int flickerCount = Random.Range(minFlickerCount, maxFlickerCount);
                    StartCoroutine(FlickerLightRoutine(lightComp, vlb, matInstance, baseEmission, flickerCount));
                }
                else
                {
                    // FLICKER YOKSA: Direkt aç
                    lightComp.intensity = baseIntensity;
                    matInstance.SetColor("_EmissionColor", baseEmission);
                    // VLB zaten yukarýda açýldý

                    buzzSoundSource.volume += buzzSoundIncreasePerLight;
                }
            }
        }
        else
        {
            SoundManager.Instance.PlaySoundFX(turnOffLightSound, buzzSoundSource.transform, turnOffLightVolume, turnOffLightMinPitch, turnOffLightMaxPitch);
            // IÞIKLARI KAPATIYORUZ
            StopAllCoroutines();

            foreach (var setting in lightsToEffect)
            {
                Light lightComp = setting.lightGO.GetComponentInChildren<Light>();
                Renderer rend = setting.lightGO.GetComponentInChildren<Renderer>();

                // --- EKLEME: VLB Componentini al ---
                VolumetricLightBeamSD vlb = setting.lightGO.GetComponentInChildren<VolumetricLightBeamSD>();

                if (lightComp != null && lightComp.enabled)
                {
                    buzzSoundSource.volume = 0f;
                    lightComp.enabled = false;
                }

                // --- EKLEME: VLB'yi kapat ---
                if (vlb != null) vlb.enabled = false;

                if (rend != null)
                {
                    rend.material.DisableKeyword("_EMISSION");
                }
            }
        }
    }

    private IEnumerator HandleLightsWithDelay()
    {
        yield return new WaitForSeconds(turnOffDelay);

        HandleLights(false);
    }

    // --- GÜNCELLEME: VLB parametresi eklendi ---
    private IEnumerator FlickerLightRoutine(Light light, VolumetricLightBeamSD vlb, Material mat, Color targetEmission, int flickerCount)
    {
        // Flicker sýrasýnda ýþýk bir yanýp bir sönecek
        for (int i = 0; i < flickerCount; i++)
        {
            // Rastgele kýsýk yan (SÖNME ANI)
            float randomIntensity = Random.Range(0.01f, baseIntensity * 0.3f);
            float emissionMultiplier = randomIntensity / (baseIntensity * 2);
            Color flickerEmission = targetEmission * Mathf.LinearToGammaSpace(emissionMultiplier);

            light.intensity = randomIntensity;
            mat.SetColor("_EmissionColor", flickerEmission);

            // --- VLB: Iþýk kýsýldýðýnda hüzmeyi kapat ---
            if (vlb != null) vlb.enabled = false;

            yield return new WaitForSeconds(Random.Range(flickerMinDelay, flickerMaxDelay));

            // Normale dön (Kýsa süreliðine - YANMA ANI)
            light.intensity = baseIntensity;
            mat.SetColor("_EmissionColor", targetEmission);

            // --- VLB: Iþýk yandýðýnda hüzmeyi aç ---
            if (vlb != null) vlb.enabled = true;

            SoundManager.Instance.PlayRandomSoundFX(flickClips, light.transform, flickVolume, flickMinPitch, flickMaxPitch);

            yield return new WaitForSeconds(Random.Range(flickerMinDelay, flickerMaxDelay));
        }

        // --- FLICKER BÝTTÝ, SON HALÝNÝ VER ---
        light.intensity = baseIntensity;
        mat.SetColor("_EmissionColor", targetEmission);

        // --- VLB: Son olarak açýk býrak ---
        if (vlb != null) vlb.enabled = true;

        buzzSoundSource.volume += buzzSoundIncreasePerLight;
    }

    // --- GÖRSELLEÞTÝRME (GÝZMOS) ---
    private void OnDrawGizmos()
    {
        // ... (Gizmos kodun ayný kalabilir) ...
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