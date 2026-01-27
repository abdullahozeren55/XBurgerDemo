using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public class PostProcessManager : MonoBehaviour
{
    public static PostProcessManager Instance { get; private set; }

    [Header("References")]
    public Volume PostProcessVolumeOnTopAll;

    [Header("Settings")]
    [Tooltip("Loop son seviyeye geldiðinde (Madness 1.0) ulaþýlacak kalýcý CA miktarý.")]
    [SerializeField] private float loopMaxChromatic = 0.5f;

    [Tooltip("Event tetiklendiðinde eklenecek ekstra CA miktarý.")]
    [SerializeField] private float eventGlitchAmount = 1.0f;

    // Efekt Bileþeni
    private ChromaticAberration chromaticAberration;

    // Deðiþkenler
    private float currentEventIntensity = 0f;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        if (PostProcessVolumeOnTopAll != null && PostProcessVolumeOnTopAll.profile != null)
        {
            PostProcessVolumeOnTopAll.profile.TryGet(out chromaticAberration);
        }
    }

    private void OnEnable()
    {
        Settings.OnDistortionChanged += OnSettingsChanged;
    }

    private void OnDisable()
    {
        Settings.OnDistortionChanged -= OnSettingsChanged;
    }

    private void Start()
    {
        // Baþlangýçta Loop deðerine göre bir kere ayarla
        RefreshVisuals();
    }

    private void OnSettingsChanged(float multiplier)
    {
        RefreshVisuals();
    }

    // --- GÖRSEL GÜNCELLEME MERKEZÝ ---
    // Bunu hem Start'ta, hem ayar deðiþince, HEM DE TWEEN SIRASINDA çaðýracaðýz.
    private void RefreshVisuals()
    {
        if (chromaticAberration == null) return;

        // 1. Loop'un getirdiði kalýcý delilik
        float loopContribution = 0f;
        if (LoopManager.Instance != null)
        {
            loopContribution = LoopManager.Instance.GetCurrentMadness() * loopMaxChromatic;
        }

        // 2. Event'in getirdiði anlýk delilik
        float eventContribution = currentEventIntensity;

        // 3. Toplam Ham Deðer
        float totalRaw = loopContribution + eventContribution;

        // 4. SON FREN: Ayarlar
        float finalIntensity = totalRaw * Settings.GlobalDistortionMultiplier;

        // 5. Uygula
        chromaticAberration.intensity.value = finalIntensity;

        // Volume aðýrlýðý kontrolü (Gereksiz render'ý önlemek için)
        if (finalIntensity <= 0.001f) PostProcessVolumeOnTopAll.weight = 0f;
        else PostProcessVolumeOnTopAll.weight = 1f;
    }

    // --- API: GLITCH TETÝKLEYÝCÝ ---
    public void TriggerChromaticGlitch(float targetRiseTime, float targetDecayTime)
    {
        DOTween.Kill("GlitchTween");

        // --- RANDOM JITTER EKLEME ---
        // Gelen süreleri %10 aþaðý veya yukarý saptýrýyoruz (0.9x ile 1.1x arasý)
        // Böylece 3 tane efekt ayný anda çalýþsa bile milisaniyelik farklar olur.
        float randomizerIn = Random.Range(0.9f, 1.1f);
        float randomizerOut = Random.Range(0.9f, 1.1f);

        float finalRise = targetRiseTime * randomizerIn;
        float finalDecay = targetDecayTime * randomizerOut;

        // Güvenlik: Süre 0 olmasýn (Tween patlar)
        if (finalRise < 0.05f) finalRise = 0.05f;
        if (finalDecay < 0.05f) finalDecay = 0.05f;

        Sequence seq = DOTween.Sequence().SetId("GlitchTween");

        // A) YÜKSELÝÞ
        seq.Append(DOTween.To(() => currentEventIntensity, x =>
        {
            currentEventIntensity = x;
            RefreshVisuals();
        }, eventGlitchAmount, finalRise).SetEase(Ease.OutCubic));

        // B) DÜÞÜÞ
        seq.Append(DOTween.To(() => currentEventIntensity, x =>
        {
            currentEventIntensity = x;
            RefreshVisuals();
        }, 0f, finalDecay).SetEase(Ease.InQuad));
    }
}