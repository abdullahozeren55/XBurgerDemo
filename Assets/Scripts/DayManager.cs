using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DayManager : MonoBehaviour
{
    public static DayManager Instance;

    public bool DayInLoop = false;

    public int DayPartToInitialize;

    [System.Serializable]
    public class DayState
    {
        [Header("Time Info")]
        [Range(0, 10)] public int Day;
        [Range(0, 10)] public int Part; // Sabah, Öðle, Akþam vb. sýralamasý için

        [Header("Sun Settings")]
        public Vector3 sunRotation;
        public float sunIntensity;
        public Color sunColor;

        [Header("Skybox Settings")]
        public float skyboxExposure;
        public float skyboxRotate;
        public Color skyboxColor;
        public Color environmentColor; // Ambient Light

        [Header("Atmosphere")]
        public Color fogColor;
        public float fogDensity;

        [Header("City Lights")]
        public bool shouldLightsUp;
    }

    [Header("Main Config")]
    public int DayCount = 0; // 0. günden mi 1. günden mi baþlýyorsun dikkat et
    public float transitionDuration = 10f;

    [Header("Scene References")]
    public Light sun;
    [SerializeField] private Material originalSkyboxMat; // Referans materyal

    [Header("Street Lights")]
    [SerializeField] private Material[] lightMats; // Emissive materyaller
    [SerializeField] private GameObject[] allLights; // Iþýk objeleri (Point/Spot lights)

    [Header("Data")]
    public DayState[] DayStates;

    private int currentIndex = 0;
    private Coroutine transitionRoutine;
    private Material instancedSkybox; // Runtime'da kullanacaðýmýz kopya materyal

    private bool lightsHandled;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            // Eðer çok sahneli bir oyun yapýyorsan buradaki DontDestroyOnLoad baþýný aðrýtabilir.
            // Iþýk referanslarýný kaybettiðin için. Þimdilik býrakýyorum.
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return; // Önemli: Instance varsa alttaki kodlarý çalýþtýrma
        }

        // --- MATERIAL FIX ---
        // Orijinal materyali bozmamak için kopya oluþturuyoruz.
        if (originalSkyboxMat != null)
        {
            instancedSkybox = new Material(originalSkyboxMat);
            RenderSettings.skybox = instancedSkybox;
        }

        InitializeDay(DayCount, DayPartToInitialize);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
            NextDayState();
    }

    public void NextDayState()
    {
        // 1. O günün statelerini bul
        // 2. Part numarasýna göre sýrala (Inspector hatasýný önler)
        var todaysStates = DayStates
            .Where(s => s.Day == DayCount)
            .OrderBy(s => s.Part)
            .ToArray();

        if (todaysStates.Length == 0)
        {
            Debug.LogWarning($"Day {DayCount} için state bulunamadý! Döngü baþa veya ertesi güne sarabilir.");
            return;
        }

        // Mevcut state ve sonraki state'i belirle
        // Not: Eðer günün son partýndaysan (currentIndex + 1) % length ile baþa (sabaha) döner.
        // Eðer gün bitince "NextDay" çaðrýlacaksa bu mantýk doðru.
        int nextIndex = (currentIndex + 1) % todaysStates.Length;

        // Geçiþi Baþlat
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);
        transitionRoutine = StartCoroutine(TransitionRoutine(todaysStates[currentIndex], todaysStates[nextIndex]));

        currentIndex = nextIndex;
    }

    public void NextDay()
    {
        DayCount++;
        currentIndex = 0;
        // Yeni günün ilk state'ine sert geçiþ mi yoksa yumuþak geçiþ mi istersin?
        // Genelde oyuncu uyur ve uyanýr, o yüzden buraya bir "Fade In/Out" eklenebilir.
        // Þimdilik sadece datayý güncelliyoruz.
        Debug.Log("Gün deðiþti: " + DayCount);
    }

    public void InitializeDay(int dayIndex, int partNumber = 1)
    {
        DayCount = dayIndex;

        lightsHandled = false;

        // 1. Hedef State'i Bul
        // (Hem gün hem de Part numarasý eþleþmeli)
        var targetState = DayStates
            .FirstOrDefault(s => s.Day == DayCount && s.Part == partNumber);

        // 2. Index'i Güncelle
        // NextDayState fonksiyonunun doðru sýradan devam etmesi için currentIndex'i ayarlamalýyýz.
        var todaysStates = DayStates
            .Where(s => s.Day == DayCount)
            .OrderBy(s => s.Part)
            .ToList();

        currentIndex = todaysStates.IndexOf(targetState);

        // 3. Varsa çalýþan geçiþi durdur (Çakýþma olmasýn)
        if (transitionRoutine != null) StopCoroutine(transitionRoutine);

        // 4. Deðerleri ANINDA uygula
        ApplyStateInstant(targetState);

        if (DayInLoop)
            NextDayState();
    }

    // Kod tekrarýný önlemek ve temizlik için atama iþlemini ayýrdým
    private void ApplyStateInstant(DayState state)
    {
        // Sun
        sun.transform.rotation = Quaternion.Euler(state.sunRotation);
        sun.intensity = state.sunIntensity;
        sun.color = state.sunColor;

        // Skybox (Kopya materyal üzerinden)
        if (instancedSkybox != null)
        {
            instancedSkybox.SetFloat("_Exposure", state.skyboxExposure);
            instancedSkybox.SetFloat("_Rotation", state.skyboxRotate);
            instancedSkybox.SetColor("_Tint", state.skyboxColor);
        }

        // Fog & Ambient
        RenderSettings.fogColor = state.fogColor;
        RenderSettings.fogDensity = state.fogDensity;
        RenderSettings.ambientLight = state.environmentColor;

        if (!lightsHandled)
        {
            ToggleCityLights(state.shouldLightsUp);
            lightsHandled = true;
        }
        
    }

    private IEnumerator TransitionRoutine(DayState from, DayState to)
    {
        lightsHandled = false;

        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / transitionDuration;
            // Smooth step daha doðal bir geçiþ saðlar (yavaþ baþla, hýzlan, yavaþ bitir)
            float smoothT = Mathf.SmoothStep(0, 1, t);

            // Sun
            sun.transform.rotation = Quaternion.Slerp(Quaternion.Euler(from.sunRotation), Quaternion.Euler(to.sunRotation), smoothT);
            sun.intensity = Mathf.Lerp(from.sunIntensity, to.sunIntensity, smoothT);
            sun.color = Color.Lerp(from.sunColor, to.sunColor, smoothT);

            // Skybox (Kopya materyal üzerinden)
            if (instancedSkybox != null)
            {
                instancedSkybox.SetFloat("_Exposure", Mathf.Lerp(from.skyboxExposure, to.skyboxExposure, smoothT));
                instancedSkybox.SetFloat("_Rotation", Mathf.Lerp(from.skyboxRotate, to.skyboxRotate, smoothT));
                instancedSkybox.SetColor("_Tint", Color.Lerp(from.skyboxColor, to.skyboxColor, smoothT));
            }

            // Fog & Ambient
            RenderSettings.fogColor = Color.Lerp(from.fogColor, to.fogColor, smoothT);
            RenderSettings.fogDensity = Mathf.Lerp(from.fogDensity, to.fogDensity, smoothT);
            RenderSettings.ambientLight = Color.Lerp(from.environmentColor, to.environmentColor, smoothT);

            float threshold = to.shouldLightsUp ? 0.3f : 0.7f;

            if (t > threshold && !lightsHandled)
            {
                ToggleCityLights(to.shouldLightsUp);
                lightsHandled = true;
            }

            yield return null;
        }

        // Loop bitti, deðerleri kesinleþtir:
        ApplyStateInstant(to);

        if (DayInLoop)
            NextDayState();
    }

    private void ToggleCityLights(bool turnOn)
    {
        // 1. Materyallerin Emission'ý
        foreach (Material mat in lightMats)
        {
            if (mat == null) continue;
            if (turnOn) mat.EnableKeyword("_EMISSION");
            else mat.DisableKeyword("_EMISSION");
        }

        // 2. Gerçek Iþýk Objeleri
        foreach (GameObject lightObj in allLights)
        {
            if (lightObj != null) lightObj.SetActive(turnOn);
        }
    }
}