using AtmosphericHeightFog;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance;

    public bool DayInLoop = false;
    public bool IsFogControlPaused = false;
    public int DayPartToInitialize;

    [HideInInspector] public float CurrentCalculatedFogIntensity;
    [HideInInspector] public float CurrentCalculatedFogHeight;
    [HideInInspector] public Color CurrentCalculatedFogColorStart;
    [HideInInspector] public Color CurrentCalculatedFogColorEnd;

    [System.Serializable]
    public class DayState
    {
        [Header("Time Info")]
        [Range(0, 10)] public int Day;
        [Range(0, 10)] public int Part;

        [Header("Sun Settings")]
        public Vector3 sunRotation;
        public float sunIntensity;
        public Color sunColor;

        [Header("Skybox Settings")]
        public float skyboxExposure;
        public float skyboxRotate;
        public Color skyboxColor;
        [ColorUsage(true, true)] public Color environmentColor;

        // --- 2. ESKÝ FOG AYARLARI GÝTTÝ, YENÝLERÝ GELDÝ ---
        [Header("Atmospheric Fog Settings")]
        [Range(0f, 1f)] public float fogIntensity; // Yeni Intensity
        public float fogHeightEnd;                 // Yeni Yükseklik (Height End)
        public Color fogColorStart;                // Zemin rengi
        public Color fogColorEnd;                  // Ufuk rengi

        [Header("City Lights")]
        public bool shouldLightsUp;
    }

    [Header("Main Config")]
    public int DayCount = 0;
    public int DayPartCount = 0;
    public float transitionDuration = 10f;

    [Header("Scene References")]
    public Light sun;
    [SerializeField] private Material originalSkyboxMat;

    // --- 3. SAHNEDEKÝ FOG SCRIPTI REFERANSI ---
    public HeightFogGlobal HeightFogScript;

    [Header("Street Lights")]
    [SerializeField] private Material[] lightMats; // Ana materyaller
    public List<GameObject> allLights = new List<GameObject>();

    [Header("Data")]
    public DayState[] DayStates;
    public DayState CurrentDayState;

    private int currentIndex = 0;
    private Coroutine transitionRoutine;
    private Material instancedSkybox;

    // --- YENÝ DEÐÝÞKENLER (HAFIZA) ---
    private Dictionary<Light, float> defaultLightIntensities = new Dictionary<Light, float>();
    private Dictionary<Material, Color> defaultEmissions = new Dictionary<Material, Color>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // --- HAFIZAYA ALMA ÝÞLEMÝ ---
        foreach (var mat in lightMats)
        {
            if (mat != null && !defaultEmissions.ContainsKey(mat))
            {
                mat.EnableKeyword("_EMISSION");
                defaultEmissions.Add(mat, mat.GetColor("_EmissionColor"));
            }
        }

        foreach (var obj in allLights)
        {
            RegisterLightInternal(obj);
        }

        // --- MATERIAL FIX ---
        if (originalSkyboxMat != null)
        {
            instancedSkybox = new Material(originalSkyboxMat);
            RenderSettings.skybox = instancedSkybox;
        }

        // Eðer HeightFogScript atanmamýþsa, sahnede bulmaya çalýþ (Güvenlik önlemi)
        if (HeightFogScript == null)
            HeightFogScript = FindObjectOfType<HeightFogGlobal>();

        InitializeDay(DayCount, DayPartToInitialize);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            NextDayState();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (instancedSkybox != null)
        {
            RenderSettings.skybox = instancedSkybox;
        }

        // KRÝTÝK KISIM: YENÝ SAHNEDEKÝ SÝSÝ BULMA
        // 1. Önce eski referansý boþalt (Hafýza temizliði)
        HeightFogScript = null;

        // 2. Sahnede aktif bir HeightFogGlobal var mý ara
        // FindObjectOfType, sahnedeki aktif objeler arasýnda arama yapar.
        HeightFogScript = FindObjectOfType<HeightFogGlobal>();

        // 3. Eðer bulduysan ve o anki gün saatine göre bir ayar varsa uygula
        if (HeightFogScript != null && CurrentDayState != null)
        {
            // Script yeni bulunduðu için Main Camera'yý kendi otomatik bulur (Boxophobic özelliði).
            // Biz sadece renk ve yoðunluk ayarlarýný basacaðýz.
            ApplyStateInstant(CurrentDayState);
        }

        if (CurrentDayState != null)
        {
            ApplyStateInstant(CurrentDayState);
        }

        allLights.RemoveAll(item => item == null);

        var keysToRemove = defaultLightIntensities.Keys.Where(k => k == null).ToList();
        foreach (var key in keysToRemove) defaultLightIntensities.Remove(key);
    }

    public void RegisterLight(GameObject lightObj)
    {
        if (!allLights.Contains(lightObj))
        {
            allLights.Add(lightObj);
            RegisterLightInternal(lightObj);

            float currentLightVal = (CurrentDayState != null && CurrentDayState.shouldLightsUp) ? 1f : 0f;
            UpdateSingleLight(lightObj, currentLightVal);
        }
    }

    private void RegisterLightInternal(GameObject lightObj)
    {
        if (lightObj == null) return;

        Light l = lightObj.GetComponent<Light>();
        if (l == null) l = lightObj.GetComponentInChildren<Light>();

        if (l != null && !defaultLightIntensities.ContainsKey(l))
        {
            defaultLightIntensities.Add(l, l.intensity);
        }
    }

    public void UnregisterLight(GameObject lightObj)
    {
        if (allLights.Contains(lightObj))
            allLights.Remove(lightObj);
    }

    public void NextDayState()
    {
        var todaysStates = DayStates.Where(s => s.Day == DayCount).OrderBy(s => s.Part).ToArray();
        if (todaysStates.Length == 0) return;

        int nextIndex = (currentIndex + 1) % todaysStates.Length;

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionRoutine(todaysStates[currentIndex], todaysStates[nextIndex]));

        currentIndex = nextIndex;
    }

    public void NextDay()
    {
        DayCount++;
        currentIndex = 0;
    }

    public void ResetForGameplay()
    {
        DayInLoop = false;
        InitializeDay(DayCount, DayPartCount);
    }

    public void ResetForMainMenu()
    {
        DayInLoop = true;
        InitializeDay(0, 0);
    }

    public void InitializeDay(int dayIndex, int partNumber = 0)
    {
        DayCount = dayIndex;

        var targetState = DayStates.FirstOrDefault(s => s.Day == DayCount && s.Part == partNumber);
        CurrentDayState = targetState;

        var todaysStates = DayStates.Where(s => s.Day == DayCount).OrderBy(s => s.Part).ToList();
        currentIndex = todaysStates.IndexOf(targetState);

        if (transitionRoutine != null) StopCoroutine(transitionRoutine);

        ApplyStateInstant(targetState);

        if (DayInLoop) NextDayState();
    }

    public Color GetOriginalEmissionColor(Material mat)
    {
        if (mat != null && defaultEmissions.ContainsKey(mat))
        {
            return defaultEmissions[mat];
        }
        return mat != null ? mat.GetColor("_EmissionColor") : Color.black;
    }

    private void UpdateCityLightsLerp(float percent)
    {
        foreach (var kvp in defaultEmissions)
        {
            Material mat = kvp.Key;
            Color originalColor = kvp.Value;

            if (mat != null)
            {
                Color targetEmission = Color.Lerp(Color.black, originalColor, percent);
                mat.SetColor("_EmissionColor", targetEmission);

                if (percent <= 0.01f) mat.DisableKeyword("_EMISSION");
                else mat.EnableKeyword("_EMISSION");
            }
        }

        foreach (var kvp in defaultLightIntensities)
        {
            Light l = kvp.Key;
            float originalIntensity = kvp.Value;

            if (l != null)
            {
                l.intensity = Mathf.Lerp(0f, originalIntensity, percent);

                if (percent <= 0.01f) l.gameObject.SetActive(false);
                else l.gameObject.SetActive(true);
            }
        }
    }

    private void UpdateSingleLight(GameObject obj, float percent)
    {
        Light l = obj.GetComponent<Light>();
        if (l == null) l = obj.GetComponentInChildren<Light>();

        if (l != null && defaultLightIntensities.ContainsKey(l))
        {
            l.intensity = Mathf.Lerp(0f, defaultLightIntensities[l], percent);
            obj.SetActive(percent > 0.01f);
        }
    }

    private void ApplyFogValues(float intensity, float height, Color startColor, Color endColor)
    {
        // A. Önce hesaplanan deðerleri hafýzaya al (Her zaman güncel kalsýn)
        CurrentCalculatedFogIntensity = intensity;
        CurrentCalculatedFogHeight = height;
        CurrentCalculatedFogColorStart = startColor;
        CurrentCalculatedFogColorEnd = endColor;

        // B. Eðer kontrol PAUSE edilmediyse (Soðuk odada deðilsek), scripte uygula
        if (!IsFogControlPaused && HeightFogScript != null)
        {
            HeightFogScript.fogIntensity = intensity;
            HeightFogScript.fogHeightEnd = height;
            HeightFogScript.fogColorStart = startColor;
            HeightFogScript.fogColorEnd = endColor;
        }
    }

    private void ApplyStateInstant(DayState state)
    {
        sun.transform.rotation = Quaternion.Euler(state.sunRotation);
        sun.intensity = state.sunIntensity;
        sun.color = state.sunColor;

        if (instancedSkybox != null)
        {
            instancedSkybox.SetFloat("_Exposure", state.skyboxExposure);
            instancedSkybox.SetFloat("_Rotation", state.skyboxRotate);
            instancedSkybox.SetColor("_Tint", state.skyboxColor);
        }

        // --- 4. ESKÝ RENDER SETTINGS YERÝNE YENÝ FOG AYARLARI ---
        // RenderSettings.fogColor ve fogDensity SÝLÝNDÝ.

        ApplyFogValues(state.fogIntensity, state.fogHeightEnd, state.fogColorStart, state.fogColorEnd);

        RenderSettings.ambientLight = state.environmentColor;

        UpdateCityLightsLerp(state.shouldLightsUp ? 1f : 0f);
    }

    private IEnumerator TransitionRoutine(DayState from, DayState to)
    {
        CurrentDayState = to;
        float t = 0f;

        float startLightVal = from.shouldLightsUp ? 1f : 0f;
        float endLightVal = to.shouldLightsUp ? 1f : 0f;

        float startRot = from.skyboxRotate;
        float endRot = to.skyboxRotate;

        if (endRot > startRot)
        {
            endRot -= 360f;
        }

        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            float lerpT = t;

            // ... (Sun ayný) ...
            sun.transform.rotation = Quaternion.Slerp(Quaternion.Euler(from.sunRotation), Quaternion.Euler(to.sunRotation), lerpT);
            sun.intensity = Mathf.Lerp(from.sunIntensity, to.sunIntensity, lerpT);
            sun.color = Color.Lerp(from.sunColor, to.sunColor, lerpT);

            if (instancedSkybox != null)
            {
                instancedSkybox.SetFloat("_Exposure", Mathf.Lerp(from.skyboxExposure, to.skyboxExposure, lerpT));
                instancedSkybox.SetFloat("_Rotation", Mathf.Lerp(startRot, endRot, lerpT));
                instancedSkybox.SetColor("_Tint", Color.Lerp(from.skyboxColor, to.skyboxColor, lerpT));
            }

            // --- 5. LERP ÝÞLEMÝ DE YENÝ SÝSTEME UYARLANDI ---
            ApplyFogValues
                (
                    Mathf.Lerp(from.fogIntensity, to.fogIntensity, lerpT),
                    Mathf.Lerp(from.fogHeightEnd, to.fogHeightEnd, lerpT),
                    Color.Lerp(from.fogColorStart, to.fogColorStart, lerpT),
                    Color.Lerp(from.fogColorEnd, to.fogColorEnd, lerpT)
                );

            // RenderSettings.fogColor ve Density SÝLÝNDÝ.
            RenderSettings.ambientLight = Color.Lerp(from.environmentColor, to.environmentColor, lerpT);

            float currentLightVal = Mathf.Lerp(startLightVal, endLightVal, lerpT);
            UpdateCityLightsLerp(currentLightVal);

            yield return null;
        }

        ApplyStateInstant(to);

        if (DayInLoop) NextDayState();
    }

    private void OnDestroy()
    {
        foreach (var kvp in defaultEmissions)
        {
            if (kvp.Key != null)
            {
                kvp.Key.SetColor("_EmissionColor", kvp.Value);
                kvp.Key.EnableKeyword("_EMISSION");
            }
        }
    }
}