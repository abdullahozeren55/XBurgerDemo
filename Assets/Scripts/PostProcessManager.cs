using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using DG.Tweening; // Tween için

public class PostProcessManager : MonoBehaviour
{
    public static PostProcessManager Instance { get; private set; }

    [Header("References")]
    public Volume PostProcessVolumeOnTopAll;
    public Volume PostProcessVolumeGameView;
    public Volume PostProcessInGameUI;

    [Header("Chromatic Aberration Settings")]
    [SerializeField] private float baseChromaticAmount = 0.5f; // Delilik tavan yapýnca ulaþacaðý max deðer (Event hariç)

    // Efekt Bileþenleri
    private ChromaticAberration chromaticAberration;

    // Deðiþkenler
    private float eventGlitchIntensity = 0f; // Eventlerden gelen geçici bozulma

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Profilden bileþenleri çek
        if (PostProcessVolumeOnTopAll != null && PostProcessVolumeOnTopAll.profile != null)
        {
            PostProcessVolumeOnTopAll.profile.TryGet(out chromaticAberration);
        }
    }

    private void Update()
    {
        if (chromaticAberration == null) return;

        // --- MATEMATÝK ZAMANI ---

        // 1. Loop Etkisi: Loop ilerledikçe artan kalýcý bozulma
        // (Madness 0 ise 0, Madness 1 ise baseChromaticAmount kadar)
        float loopIntensity = 0f;
        if (LoopManager.Instance != null)
        {
            loopIntensity = LoopManager.Instance.GetCurrentMadness() * baseChromaticAmount;
        }

        // 2. Event Etkisi: Anlýk gelen (Diyalog eventleri vb.)
        float eventIntensity = eventGlitchIntensity;

        // 3. Toplam Ham Þiddet
        float totalRawIntensity = loopIntensity + eventIntensity;

        // 4. LÝMÝTÖR (SETTINGS): Oyuncu "Midem bulanýyor" dediyse kýsýyoruz
        // Settings static olduðu için direkt eriþebiliriz
        float finalIntensity = totalRawIntensity * Settings.GlobalDistortionMultiplier;

        // 5. Uygula
        chromaticAberration.intensity.value = finalIntensity;

        // Opsiyonel: Eðer bozulma varsa volume'ü aktif et, yoksa kapat (Performans)
        if (PostProcessVolumeOnTopAll.weight < 1f) PostProcessVolumeOnTopAll.weight = 1f;
    }

    // --- EVENT SÝSTEMÝ ÝÇÝN API ---

    // Diyalog eventlerinden çaðýracaðýmýz fonksiyon
    public void TriggerGlitchEvent(float intensity, float duration)
    {
        // Mevcut event þiddetini artýr (Tween ile)
        // Öncekini kill et ki üst üste binmesin
        DOTween.Kill("GlitchEvent");

        // 0'dan Intensity'e çýk, sonra in
        Sequence seq = DOTween.Sequence().SetId("GlitchEvent");

        // Hýzlýca yüksel
        seq.Append(DOTween.To(() => eventGlitchIntensity, x => eventGlitchIntensity = x, intensity, duration * 0.1f));
        // Biraz dur
        seq.AppendInterval(duration * 0.4f);
        // Yavaþça in
        seq.Append(DOTween.To(() => eventGlitchIntensity, x => eventGlitchIntensity = x, 0f, duration * 0.5f));
    }

    // Kalýcý bir event baþlatmak istersen (örn: Bölüm sonuna kadar ekran bozuk kalsýn)
    public void SetPersistentEventIntensity(float intensity)
    {
        DOTween.Kill("GlitchEvent"); // Tweeni durdur, kontrolü ele al
        eventGlitchIntensity = intensity;
    }
}