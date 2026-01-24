using UnityEngine;
using System.Collections.Generic;
using System.Linq; // Listede arama yapmak için gerekli

[RequireComponent(typeof(Renderer))]
public class LocalizedMaterial : MonoBehaviour
{
    // 1. DÖNGÜYE ÖZEL MATERYAL TANIMI
    [System.Serializable]
    public struct LoopOverride
    {
        [Tooltip("Hangi Loop sayýsýnda bu materyal görünsün?")]
        public int loopIndex;
        public Material material;
    }

    // 2. DÝL PROFÝLÝ (Hem dilin ana materyali hem de o dile özel loop varyasyonlarý)
    [System.Serializable]
    public struct LanguageProfile
    {
        public LocalizationManager.GameLanguage language;
        [Tooltip("Bu dilin standart materyali")]
        public Material baseMaterial;
        [Tooltip("Bu dilde, belirli günlerde deðiþecekse burayý doldur")]
        public List<LoopOverride> loopOverrides;
    }

    [Header("Global / Default Settings (English)")]
    [Tooltip("Hiçbir þart saðlanmazsa görünecek en temel materyal.")]
    public Material globalBaseMaterial;

    [Tooltip("Varsayýlan dil (Ýngilizce) için Loop'a göre deðiþimler.")]
    public List<LoopOverride> globalLoopOverrides;

    [Header("Language Specific Settings")]
    [Tooltip("Diðer diller ve onlarýn loop varyasyonlarý.")]
    public List<LanguageProfile> languageProfiles;

    private Renderer _renderer;

    private void Awake()
    {
        _renderer = GetComponent<Renderer>();
    }

    private void Start()
    {
        UpdateContent();
    }

    private void OnEnable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged += UpdateContent;

        // LoopManager'da bir event olmadýðý için sahne yüklendiðinde veya enable olduðunda günceller.
        // Eðer oyun içinde sahne deðiþmeden gün atlýyorsa LoopManager'a event eklememiz gerekir.
        UpdateContent();
    }

    private void OnDisable()
    {
        if (LocalizationManager.Instance != null)
            LocalizationManager.Instance.OnLanguageChanged -= UpdateContent;
    }

    // Dýþarýdan manuel tetiklemek istersen (Örn: Gün deðiþince)
    public void UpdateContent()
    {
        if (_renderer == null) return;

        // LoopManager yoksa 0 varsayalým, varsa çekelim
        int currentLoop = LoopManager.Instance != null ? LoopManager.Instance.LoopCount : 0;

        // LocalizationManager yoksa Default varsayalým
        var currentLang = LocalizationManager.Instance != null ? LocalizationManager.Instance.currentLanguage : LocalizationManager.GameLanguage.English;

        Material materialToAssign = null;

        // --- ADIM 1: Seçili dil için profil var mý? ---
        // Struct olduðu için Find yerine döngü veya null checkli FirstOrDefault kullanýyoruz
        int profileIndex = languageProfiles.FindIndex(x => x.language == currentLang);

        if (profileIndex != -1)
        {
            // PROFÝL BULUNDU (Örn: Türkçe)
            var profile = languageProfiles[profileIndex];

            // Bu dilin, ÞU ANKÝ LOOP için özel bir override'ý var mý?
            var loopVar = profile.loopOverrides.FirstOrDefault(x => x.loopIndex == currentLoop);

            // Struct default kontrolü (material null deðilse override var demektir)
            if (loopVar.material != null)
            {
                materialToAssign = loopVar.material; // Türkçe + Loop 2 Materyali
            }
            else
            {
                materialToAssign = profile.baseMaterial; // Türkçe Standart Materyali
            }
        }
        else
        {
            // PROFÝL YOK (Global/Ýngilizce Fallback)

            // Global ayarlar içinde ÞU ANKÝ LOOP için override var mý?
            var globalLoopVar = globalLoopOverrides.FirstOrDefault(x => x.loopIndex == currentLoop);

            if (globalLoopVar.material != null)
            {
                materialToAssign = globalLoopVar.material; // Ýngilizce + Loop 2 Materyali
            }
            else
            {
                materialToAssign = globalBaseMaterial; // En Temel Materyal
            }
        }

        // --- ADIM 2: Materyali Uygula ---
        if (materialToAssign != null && _renderer.sharedMaterial != materialToAssign)
        {
            _renderer.sharedMaterial = materialToAssign;
        }
    }
}