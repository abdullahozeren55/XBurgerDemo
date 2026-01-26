// PostProcessManager.cs

using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening;

public class PostProcessManager : MonoBehaviour
{
    public static PostProcessManager Instance { get; private set; }

    [Header("References")]
    public Volume PostProcessVolumeOnTopAll;

    [Header("Chromatic Aberration Settings")]
    [SerializeField] private float baseChromaticAmount = 0.5f;

    private ChromaticAberration chromaticAberration;
    private float eventGlitchIntensity = 0f;

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
        // Evente abone ol
        Settings.OnDistortionChanged += RefreshVisuals;
    }

    private void OnDisable()
    {
        // Abonelikten çýk (Hata almamak için þart)
        Settings.OnDistortionChanged -= RefreshVisuals;
    }

    private void Start()
    {
        // Baþlangýçta bir kere çalýþtýr
        RefreshVisuals(Settings.GlobalDistortionMultiplier);
    }

    // ARTIK UPDATE YOK! Sadece gerektiðinde çalýþýr.
    private void RefreshVisuals(float multiplier)
    {
        if (chromaticAberration == null) return;

        // 1. Loop Etkisi (Static)
        float loopIntensity = 0f;
        if (LoopManager.Instance != null)
        {
            loopIntensity = LoopManager.Instance.GetCurrentMadness() * baseChromaticAmount;
        }

        // 2. Event Etkisi (Dynamic - O an varsa)
        float currentEventIntensity = eventGlitchIntensity;

        // 3. Toplam
        float totalRaw = loopIntensity + currentEventIntensity;

        // 4. Uygula
        float finalVal = totalRaw * multiplier;

        chromaticAberration.intensity.value = finalVal;

        // Volume aðýrlýðýný ayarla
        if (finalVal > 0.01f) PostProcessVolumeOnTopAll.weight = 1f;
        // Tamamen 0 ise kapatabiliriz ama geçiþlerde sorun olmasýn diye 1 tutmak daha güvenli
    }

    public void TriggerGlitchEvent(float intensity, float duration)
    {
        DOTween.Kill("GlitchEvent");

        // Event baþladýðýnda Tween ile deðeri deðiþtiriyoruz.
        // Ama "Update" olmadýðý için, her adýmda (OnUpdate) deðeri volume'e iþlememiz lazým.

        Sequence seq = DOTween.Sequence().SetId("GlitchEvent");

        seq.Append(DOTween.To(() => eventGlitchIntensity, x =>
        {
            eventGlitchIntensity = x;
            // Her karede görseli güncelle (Çarpaný dikkate alarak)
            RefreshVisuals(Settings.GlobalDistortionMultiplier);
        }, intensity, duration * 0.1f));

        seq.AppendInterval(duration * 0.4f);

        seq.Append(DOTween.To(() => eventGlitchIntensity, x =>
        {
            eventGlitchIntensity = x;
            RefreshVisuals(Settings.GlobalDistortionMultiplier);
        }, 0f, duration * 0.5f));
    }
}