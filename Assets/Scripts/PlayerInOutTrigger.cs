using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using static LightSwitch;
using static Unity.VisualScripting.AnnotationUtility;

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
        // Önceki flicker iþlemlerini durdur. 
        // DÝKKAT: Bu switch üzerindeki TÜM coroutine'leri durdurur (Rotate hariç tutulmalýydý ama
        // Rotate zaten kýsa sürdüðü için sorun olmaz, yine de clean olsun diye rotate'i ayrý tutabilirsin)
        // Ama en temizi flicker'larý bir listede tutmak. 
        // Þimdilik "StopAllCoroutines" kullanýyorum ama RotateCoroutine'i tekrar baþlatmamak için dikkatli ol.
        // Flicker için StopAllCoroutines yapmak yerine, kapatma bloðunda manuel kontrol yapalým.

        lightsOn = shouldOn;

        if (lightsOn)
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
            StopAllCoroutines();

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

    private IEnumerator HandleLightsWithDelay()
    {
        yield return new WaitForSeconds(turnOffDelay);

        HandleLights(false);
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

    // --- GÖRSELLEÞTÝRME (GÝZMOS) ---
    private void OnDrawGizmos()
    {
        // Objenin kendi rotasyonunu ve scale'ini hesaba katarak çizim yap
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(transform.position, transform.rotation, transform.lossyScale);
        Gizmos.matrix = rotationMatrix;

        // Collider boyutunu al (Varsayýlan 1x1x1 küp üzerinden)
        BoxCollider box = GetComponent<BoxCollider>();
        if (box != null)
        {
            // Trigger'ýn kendisini þeffaf sarý çiz (Eþik)
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(box.center, box.size);

            // --- YEÞÝL BÖLGE (Gidilecek Yer) ---
            // Z ekseninde ileriye (Forward) ufak bir küre koy
            Gizmos.color = new Color(0, 1, 0, 0.8f); // Yeþil
            Vector3 greenPos = box.center + new Vector3(0, 0, 0.5f);
            Gizmos.DrawSphere(greenPos, 0.2f);

            // --- KIRMIZI BÖLGE (Gelinen Yer) ---
            // Z ekseninde geriye (Back) ufak bir küre koy
            Gizmos.color = new Color(1, 0, 0, 0.8f); // Kýrmýzý
            Vector3 redPos = box.center - new Vector3(0, 0, 0.5f);
            Gizmos.DrawSphere(redPos, 0.2f);

            // Ok iþareti niyetine çizgi
            Gizmos.color = Color.white;
            Gizmos.DrawLine(redPos, greenPos);
        }
    }
}