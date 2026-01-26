using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum CameraNoiseType
{
    None,           // Sabit kamera
    IdleBreathing,  // Varsayılan: Hafif el titremesi / Nefes alma
    Tension,        // Gergin anlar: Biraz daha hızlı titreme
    Jumpscare,      // Anlık şok: Sert ve geniş sarsıntı
}
public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    [System.Serializable]
    public class JumpscareSettings
    {
        public JumpscareType type;
        [Header("Screen Shake")]
        public float amplitude = 0.2f;
        public float frequency = 0.4f;
        public float shakeLerpDuration = 0.5f;
        public float shakeTotalDuration = 1f;
        public float shakeResetLerpDuration = 0.5f;

        [Header("Vignette")]
        public float vignetteIntensity = 0.4f;
        public Color vignetteColor = Color.red;
        public float vignetteLerpDuration = 0.5f;
        public float vignetteTotalDuration = 1f;
        public float vignetteResetLerpDuration = 0.5f;

        [Header("FOV Kick")]
        public float fov = 55f;
        public float fovLerpDuration = 0.4f;
        public float fovTotalDuration = 1f;
        public float fovResetLerpDuration = 0.4f;
    }

    public enum JumpscareType
    {
        Small,
        Mid,
        Big
    }

    public enum CameraName
    {
        Null,
        FirstPerson,
        Monitor,
    }

    [System.Serializable]
    public class CameraEntry
    {
        public CameraName camName;
        public CinemachineVirtualCamera vCam;
    }

    [System.Serializable]
    public struct NoisePreset
    {
        public CameraNoiseType type;
        public NoiseSettings settingsAsset; // Cinemachine Asset'i
        public float defaultAmplitude;
        public float defaultFrequency;
    }

    [Header("Gameplay Cameras")]
    [SerializeField] private CameraEntry[] cameras;

    [Header("Main Menu Cameras (Random System)")]
    [SerializeField] private List<CinemachineVirtualCamera> mainMenuCameras;
    [SerializeField] private float menuToGameBlendTime = 2.0f; // Menüden oyuna geçiş süresi
    [SerializeField] private float gameToMenuBlendTime = 1.5f; // Oyundan menüye geçiş süresi

    [Header("Render Setup")]
    [SerializeField] private Camera mainCamera;   // Main Camera'yı ata
    [SerializeField] private Camera overlayCamera; // Overlay Camera'yı ata (UI veya Weapon Cam)

    [Tooltip("Main Camera'nın OYUN İÇİNDE görmesi gereken layerlar (Player ve Grabbed HARİÇ her şey)")]
    [SerializeField] private LayerMask gameplayCullingMask;

    [Tooltip("Main Camera'nın MENÜDE görmesi gereken layerlar (HER ŞEY dahil)")]
    [SerializeField] private LayerMask menuCullingMask;

    [Space]
    [SerializeField] private List<JumpscareSettings> jumpscarePresets;
    [Space]
    [SerializeField] private NoiseSettings wobbleNoise;
    [SerializeField] private NoiseSettings shakeNoise;
    [Space]

    [Header("Player Throw Charge Effect Settings")]
    [SerializeField] private float maxAmplitudeGain = 0.2f;
    [SerializeField] private float maxFrequencyGain = 0.4f;
    [SerializeField] private float maxFOV = 50f;
    [SerializeField] private float throwMaxChargeTime = 1.5f;
    [SerializeField] private float vignetteIntensityForThrowCharge = 0.25f;
    [SerializeField] private Color throwChargeColor;
    [SerializeField] private float releaseSpeedMultiplier = 4f;

    [Header("Player Cold Room Enter Effect Settings")]
    [SerializeField] private float vignetteIntensityForColdRoom = 0.25f;
    [SerializeField] private Color coldRoomVignetteColor;
    [SerializeField] private float vigIncreaseTimeForColdRoom = 0.3f;
    [SerializeField] private float vigDecreaseTimeForColdRoom = 0.2f;
    [Space]
    [SerializeField] private float colorAdjustmentsPostExposureForColdRoom = -0.5f;
    [SerializeField] private Color coldRoomColorAdjustmentsColor;
    [SerializeField] private float colorAdjustmentsIncreaseTimeForColdRoom = 1f;
    [SerializeField] private float colorAdjustmentsDecreaseTimeForColdRoom = 0.5f;

    [Header("Dialogue Camera System")]
    [SerializeField] private CinemachineVirtualCamera dialogueCam;
    [SerializeField] private Transform dialogueLookTarget; // Kameranın LookAt slotundaki boş obje

    [Header("Auto-Framing Settings")]
    [Tooltip("Kamera müşterinin yüzünden ne kadar uzakta dursun?")]
    [SerializeField] private float faceToFaceDistance = 0.8f;
    [Tooltip("Kamera müşterinin göz hizasından ne kadar aşağıda/yukarıda olsun?")]
    [SerializeField] private float heightOffset = 0.1f;
    [Tooltip("Yumuşatma hızı")]
    [SerializeField] private float repositionDuration = 1f;

    [Header("Noise Configuration")]
    [SerializeField] private List<NoisePreset> noisePresets; // Inspector'dan doldur

    // Hızlı erişim için Dictionary (Awake'te dolduracağız)
    private Dictionary<CameraNoiseType, NoisePreset> noiseMap = new Dictionary<CameraNoiseType, NoisePreset>();

    private int defaultPriority = 10;
    private int activePriority = 20;

    private float normalFOV;
    private float normalVignetteValue;
    private Color normalVignetteColor;
    private float normalColorAdjustmentsPostExposureValue;
    private Color normalColorAdjustmentsColor;

    private float normalFOVCurrentCam;

    private CinemachineVirtualCamera firstPersonCam;
    private CinemachineBrain cinemachineBrain; // Geçiş sürelerini yönetmek için

    private CinemachineBasicMultiChannelPerlin perlin;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;

    private CameraEntry currentCam;
    private CinemachineVirtualCamera currentMenuCam; // Şu an aktif olan menü kamerası
    private int basePriority = 10;

    private Tween firstPersonCamFOVTween;

    private Coroutine renderTransitionRoutine; // Render ayarlarını değiştiren zamanlayıcı
    private Coroutine blendResetRoutine;       // Geçiş süresini sıfırlayan zamanlayıcı
    private Coroutine controlUnlockRoutine; // Oyuncuya kontrolü verme zamanlayıcısı

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Listeyi Dictionary'e çevir (Hız için)
        foreach (var preset in noisePresets)
        {
            if (!noiseMap.ContainsKey(preset.type))
                noiseMap.Add(preset.type, preset);
        }

        // Main Camera üzerindeki Brain'i bul
        if (Camera.main != null)
            cinemachineBrain = Camera.main.GetComponent<CinemachineBrain>();

        foreach (CameraEntry entry in cameras)
        {
            if (entry.camName == CameraName.FirstPerson)
            {
                firstPersonCam = entry.vCam;
                perlin = firstPersonCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
                normalFOV = firstPersonCam.m_Lens.FieldOfView;
            }
        }
    }

    private void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.PostProcessVolume != null)
        {
            if (GameManager.Instance.PostProcessVolume.profile.TryGet(out Vignette v))
            {
                vignette = v;
                normalVignetteValue = vignette.intensity.value;
                normalVignetteColor = vignette.color.value;
            }

            if (GameManager.Instance.PostProcessVolume.profile.TryGet(out ColorAdjustments c))
            {
                colorAdjustments = c;
                normalColorAdjustmentsPostExposureValue = colorAdjustments.postExposure.value;
                normalColorAdjustmentsColor = colorAdjustments.colorFilter.value;
            }
        }
    }

    public void StartDialogueMode()
    {
        if (dialogueCam != null)
        {
            dialogueCam.Priority = activePriority;
            // Başlangıçta target'ı resetleyebilirsin veya olduğu yerde bırakabilirsin
        }
    }

    public void EndDialogueMode()
    {
        if (dialogueCam != null)
        {
            dialogueCam.Priority = defaultPriority;
        }
    }

    public void UpdateDialogueShot(Transform targetTransform, float moveDuration, Ease moveEase, float dutchAngle, float fov, CameraNoiseType noiseType, float ampMult, float freqMult, bool isInstant = false) // <--- YENİ PARAMETRE
    {
        if (dialogueCam == null || dialogueLookTarget == null) return;

        // --- HESAPLAMALAR ---
        Vector3 targetLookPos = targetTransform != null ? targetTransform.position : dialogueLookTarget.position;
        Vector3 targetCamPos = dialogueCam.transform.position; // Varsayılan: Olduğu yerde kalsın

        // Eğer bir hedef varsa Auto-Framing pozisyonunu hesapla
        if (targetTransform != null)
        {
            // Hedefin (Root/Gövde) baktığı yönü bul
            Vector3 customerForward = targetTransform.root.forward;
            // İdeal pozisyon: Yüzün önünde + Yükseklik farkı
            targetCamPos = targetTransform.position + (customerForward * faceToFaceDistance);
            targetCamPos.y += heightOffset;
        }

        // --- UYGULAMA (SNAP vs TWEEN) ---

        if (isInstant)
        {
            // 1. ÖNCEKİ TWEENLERİ ÖLDÜR (Çakışma olmasın)
            dialogueLookTarget.DOKill();
            dialogueCam.transform.DOKill();
            dialogueCam.DOKill(); // Lens tweenlerini de kapsar

            // 2. DEĞERLERİ ANINDA ATA (IŞINLA)
            dialogueLookTarget.position = targetLookPos;
            dialogueCam.transform.position = targetCamPos;

            // Lens
            dialogueCam.m_Lens.Dutch = dutchAngle;
            dialogueCam.m_Lens.FieldOfView = fov;

            // Noise (Anında geçiş)
            var perlin = dialogueCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (perlin != null)
            {
                if (noiseType == CameraNoiseType.None)
                {
                    perlin.m_NoiseProfile = null;
                    perlin.m_AmplitudeGain = 0;
                    perlin.m_FrequencyGain = 0;
                }
                else if (noiseMap.ContainsKey(noiseType))
                {
                    NoisePreset preset = noiseMap[noiseType];
                    perlin.m_NoiseProfile = preset.settingsAsset;
                    perlin.m_AmplitudeGain = preset.defaultAmplitude * ampMult;
                    perlin.m_FrequencyGain = preset.defaultFrequency * freqMult;
                }
            }
        }
        else
        {
            // --- NORMAL TWEEN AKIŞI (YUMUŞAK GEÇİŞ) ---

            // 1. Hedefe Bak (LookAt Target)
            dialogueLookTarget.DOMove(targetLookPos, moveDuration).SetEase(moveEase);

            // 2. Kamera Pozisyonu (Auto-Framing)
            if (targetTransform != null)
            {
                dialogueCam.transform.DOMove(targetCamPos, repositionDuration).SetEase(Ease.OutCubic);
            }

            // 3. Lens
            DOTween.To(() => dialogueCam.m_Lens.Dutch, x => dialogueCam.m_Lens.Dutch = x, dutchAngle, moveDuration).SetEase(moveEase);
            DOTween.To(() => dialogueCam.m_Lens.FieldOfView, x => dialogueCam.m_Lens.FieldOfView = x, fov, moveDuration).SetEase(moveEase);

            // 4. Noise
            var perlin = dialogueCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (perlin != null)
            {
                if (noiseType == CameraNoiseType.None)
                {
                    // Noise kapatma tweeni
                    DOTween.To(() => perlin.m_AmplitudeGain, x => perlin.m_AmplitudeGain = x, 0f, moveDuration);
                    DOTween.To(() => perlin.m_FrequencyGain, x => perlin.m_FrequencyGain = x, 0f, moveDuration)
                           .OnComplete(() => perlin.m_NoiseProfile = null);
                }
                else if (noiseMap.ContainsKey(noiseType))
                {
                    NoisePreset preset = noiseMap[noiseType];
                    if (perlin.m_NoiseProfile != preset.settingsAsset) perlin.m_NoiseProfile = preset.settingsAsset;

                    float targetAmp = preset.defaultAmplitude * ampMult;
                    float targetFreq = preset.defaultFrequency * freqMult;

                    DOTween.To(() => perlin.m_AmplitudeGain, x => perlin.m_AmplitudeGain = x, targetAmp, moveDuration).SetEase(moveEase);
                    DOTween.To(() => perlin.m_FrequencyGain, x => perlin.m_FrequencyGain = x, targetFreq, moveDuration).SetEase(moveEase);
                }
            }
        }
    }

    public Transform GetDialogueCameraTransform()
    {
        if (dialogueCam != null) return dialogueCam.transform;
        return transform; // Fallback
    }

    // --- YENİ EKLENEN: MENÜ VE OYUN GEÇİŞ SİSTEMİ ---

    public void SwitchToRandomMainMenuCamera(bool instant = false)
    {
        if (renderTransitionRoutine != null) StopCoroutine(renderTransitionRoutine);
        if (blendResetRoutine != null) StopCoroutine(blendResetRoutine);
        if (controlUnlockRoutine != null) StopCoroutine(controlUnlockRoutine);

        if (PlayerManager.Instance != null) PlayerManager.Instance.SetPlayerCanPlay(false);

        if (mainMenuCameras == null || mainMenuCameras.Count == 0) return;

        // Menüye dönüşte de sabit süreyi koruyoruz (Instant hariç)
        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend.m_Time = instant ? 0f : gameToMenuBlendTime;
        }

        ResetAllPriorities();
        int randomIndex = Random.Range(0, mainMenuCameras.Count);
        currentMenuCam = mainMenuCameras[randomIndex];
        if (currentMenuCam != null) currentMenuCam.Priority = basePriority + 1;
        currentCam = null;

        SetRenderStateForMainMenu();
    }

    public void SwitchToGameplayCamera()
    {
        // 1. Temizlik
        if (renderTransitionRoutine != null) StopCoroutine(renderTransitionRoutine);
        if (blendResetRoutine != null) StopCoroutine(blendResetRoutine);
        if (controlUnlockRoutine != null) StopCoroutine(controlUnlockRoutine);

        // 2. Dinamik Süre Hesabı (SADECE EVENTLER İÇİN)
        // Kameraya "2 saniyede git" diyeceğiz ama yarı yoldaysak 
        // Cinemachine veya bizim hissettiğimiz süre aslında daha kısa olacak.
        // O yüzden eventleri bu kısa süreye göre ayarlıyoruz.
        float multiplier = 1f;

        if (cinemachineBrain != null && cinemachineBrain.IsBlending)
        {
            var activeBlend = cinemachineBrain.ActiveBlend;
            if (activeBlend.Duration > 0)
            {
                // Ne kadar tamamlandıysa, dönüş süresi o oranda kısalır.
                multiplier = activeBlend.TimeInBlend / activeBlend.Duration;
            }
        }

        // Bu "tahmini/hissedilen" gerçek süre
        float effectiveDuration = menuToGameBlendTime * multiplier;

        // Çok aşırı kısa olmasın (glitch önlemi)
        effectiveDuration = Mathf.Max(effectiveDuration, 0.1f);

        // 3. KAMERA AYARI (SABİT SÜRE - DOKUNMUYORUZ)
        // Kullanıcının isteği üzerine buraya "multiplier" uygulamıyoruz.
        // Cinemachine'e "Standart süreni kullan" diyoruz.
        if (cinemachineBrain != null)
            cinemachineBrain.m_DefaultBlend.m_Time = menuToGameBlendTime;

        SwitchToCamera(CameraName.FirstPerson);

        // 4. ZAMANLAYICILAR (DİNAMİK/KISA SÜREYE GÖRE)

        // Render değişimi: Efektif sürenin %90'ında
        renderTransitionRoutine = StartCoroutine(TransitionToGameplayRenderRoutine(effectiveDuration * 0.97f));

        // Kontrol verme: Efektif sürenin %60'ında
        controlUnlockRoutine = StartCoroutine(UnlockPlayerControlRoutine(effectiveDuration * 0.5f));

        // Blend reset: Efektif süre bittiğinde
        blendResetRoutine = StartCoroutine(ResetBlendTimeAfterDelay(effectiveDuration));
    }

    private void ResetAllPriorities()
    {
        // Oyun kameralarını düşür
        foreach (var entry in cameras)
        {
            if (entry.vCam != null) entry.vCam.Priority = basePriority;
        }

        // Menü kameralarını düşür
        foreach (var cam in mainMenuCameras)
        {
            if (cam != null) cam.Priority = basePriority;
        }
    }

    // ------------------------------------------------

    public void SwitchToCamera(CameraName name)
    {
        // Eğer menüden geliyorsak priority sıfırlaması yapalım ki çakışma olmasın
        ResetAllPriorities();

        // Standart kamera geçişi
        foreach (CameraEntry entry in cameras)
        {
            if (entry.camName == name)
            {
                currentCam = entry;
                currentCam.vCam.Priority = basePriority + 1;
                break;
            }
        }
    }

    public void InitializeCamera(CameraName name)
    {
        if (currentCam != null)
            return;

        SwitchToCamera(name);
    }

    public void ChangeFirstPersonCameraFOV(float targetFOV, float duration, float delay)
    {
        firstPersonCamFOVTween?.Kill();

        firstPersonCamFOVTween = DOTween.To(
            () => firstPersonCam.m_Lens.FieldOfView,
            x =>
            {
                LensSettings lens = firstPersonCam.m_Lens;
                lens.FieldOfView = x;
                firstPersonCam.m_Lens = lens;
            },
            targetFOV,
            duration
        )
        .SetEase(Ease.OutBack)
        .SetDelay(delay)
        .SetUpdate(true);
    }

    public void ResetFirstPersonCameraFOV(float duration)
    {
        firstPersonCamFOVTween?.Kill();

        firstPersonCamFOVTween = DOTween.To(
            () => firstPersonCam.m_Lens.FieldOfView,
            x =>
            {
                LensSettings lens = firstPersonCam.m_Lens;
                lens.FieldOfView = x;
                firstPersonCam.m_Lens = lens;
            },
            normalFOV,
            duration
        )
        .SetEase(Ease.OutBack)
        .SetUpdate(true);
    }

    public CinemachineVirtualCamera GetCamera()
    {
        // Eğer menü kamerası aktifse onu döndür, yoksa oyun kamerasını
        if (currentCam == null && currentMenuCam != null)
            return currentMenuCam;

        return currentCam != null ? currentCam.vCam : firstPersonCam;
    }

    public void SwitchToFirstPersonCamera()
    {
        SwitchToCamera(CameraName.FirstPerson);
    }

    public void PlayScreenShake(float targetAmplitude, float targetFrequency, float duration, Ease ease = Ease.OutSine, string tweenId = "ScreenShake")
    {
        DOTween.Kill(tweenId);

        Sequence seq = DOTween.Sequence().SetId(tweenId);
        seq.Join(DOTween.To(() => perlin.m_AmplitudeGain, x => perlin.m_AmplitudeGain = x, targetAmplitude, duration));
        seq.Join(DOTween.To(() => perlin.m_FrequencyGain, x => perlin.m_FrequencyGain = x, targetFrequency, duration));
        seq.SetEase(ease);
    }

    public void PlayVignette(float targetIntensity, float duration, Color targetColor, Ease ease = Ease.OutSine, string tweenId = "Vignette")
    {
        DOTween.Kill(tweenId);
        if (vignette == null) return;

        Color startColor = vignette.color.value;
        float startIntensity = vignette.intensity.value;

        Sequence seq = DOTween.Sequence().SetId(tweenId).SetEase(ease);

        seq.Join(DOTween.To(() => startColor, x => vignette.color.value = x, targetColor, duration));
        seq.Join(DOTween.To(() => startIntensity, x => vignette.intensity.value = x, targetIntensity, duration));

        seq.Play();
    }

    public void PlayerColorAdjustments(float targetPostExposure, float duration, Color targetColor, Ease ease = Ease.OutSine, string tweenId = "ColorAdjustments")
    {
        DOTween.Kill(tweenId);
        if (colorAdjustments == null) return;

        Color startColor = colorAdjustments.colorFilter.value;
        float startPostExposure = colorAdjustments.postExposure.value;

        Sequence seq = DOTween.Sequence().SetId(tweenId).SetEase(ease);

        seq.Join(DOTween.To(() => startColor, x => colorAdjustments.colorFilter.value = x, targetColor, duration));
        seq.Join(DOTween.To(() => startPostExposure, x => colorAdjustments.postExposure.value = x, targetPostExposure, duration));

        seq.Play();
    }

    public void PlayDutchAngle(float targetAngle, float duration, Ease ease = Ease.OutSine, string tweenId = "DutchAngle")
    {
        DOTween.Kill(tweenId);
        DOTween.To(() => firstPersonCam.m_Lens.Dutch, x => firstPersonCam.m_Lens.Dutch = x, targetAngle, duration)
            .SetEase(ease)
            .SetId(tweenId);
    }

    public void PlayCurrentCamFOV(float targetFOV, float duration, Ease ease = Ease.InOutBack, float easeValue = 1.7f, string tweenId = "FOVCurrentCam")
    {
        CinemachineVirtualCamera cam = GetCamera();
        if (cam == null) return;

        normalFOVCurrentCam = cam.m_Lens.FieldOfView;

        DOTween.Kill(tweenId);

        DOTween.To(
            () => cam.m_Lens.FieldOfView,
            x =>
            {
                LensSettings lens = cam.m_Lens;
                lens.FieldOfView = x;
                cam.m_Lens = lens;
            },
            targetFOV, duration)
            .SetEase(ease, easeValue)
            .SetId(tweenId);
    }

    public void EndCurrentCamFOV(float duration = 0.5f, Ease ease = Ease.InOutBack, float easeValue = 1.7f, string tweenId = "FOVCurrentCam")
    {
        PlayCurrentCamFOV(normalFOVCurrentCam, duration, ease, easeValue, tweenId);
    }

    public void PlayFOV(float targetFOV, float duration, Ease ease = Ease.OutSine, float easeValue = 1.7f, string tweenId = "FOV")
    {
        DOTween.Kill(tweenId);

        DOTween.To(
            () => firstPersonCam.m_Lens.FieldOfView,
            x =>
            {
                LensSettings lens = firstPersonCam.m_Lens;
                lens.FieldOfView = x;
                firstPersonCam.m_Lens = lens;
            },
            targetFOV,
            duration
        )
        .SetEase(ease, easeValue)
        .SetId(tweenId);
    }

    public void EndFOV(float delay, float duration)
    {
        StartCoroutine(EndFOVCoroutine(delay, duration));
    }

    public void PlayColdRoomEffects(bool isEntering)
    {
        float durationVig = isEntering ? vigIncreaseTimeForColdRoom : vigDecreaseTimeForColdRoom;
        float durationColorAdjustments = isEntering ? colorAdjustmentsIncreaseTimeForColdRoom : colorAdjustmentsDecreaseTimeForColdRoom;
        Ease ease = isEntering ? Ease.OutSine : Ease.InSine;

        PlayVignette(isEntering ? vignetteIntensityForColdRoom : normalVignetteValue, durationVig, isEntering ? coldRoomVignetteColor : normalVignetteColor, ease);
        PlayerColorAdjustments(isEntering ? colorAdjustmentsPostExposureForColdRoom : normalColorAdjustmentsPostExposureValue, durationColorAdjustments, isEntering ? coldRoomColorAdjustmentsColor : normalColorAdjustmentsColor, ease);
    }

    public void PlayThrowEffects(bool isCharging)
    {
        perlin.m_NoiseProfile = wobbleNoise;

        float duration = isCharging ? throwMaxChargeTime : throwMaxChargeTime / releaseSpeedMultiplier;
        Ease ease = isCharging ? Ease.OutSine : Ease.InSine;

        PlayScreenShake(isCharging ? maxAmplitudeGain : 0f,
                        isCharging ? maxFrequencyGain : 0f,
                        duration, ease);

        PlayVignette(isCharging ? vignetteIntensityForThrowCharge : normalVignetteValue,
                     duration, isCharging ? throwChargeColor : normalVignetteColor, ease);

        PlayFOV(isCharging ? maxFOV : normalFOV,
                duration, Ease.InOutBack, 3f, "ThrowEffectsFOV");
    }

    public void PlayJumpscareEffects(JumpscareType type)
    {
        perlin.m_NoiseProfile = shakeNoise;

        var preset = jumpscarePresets.Find(p => p.type == type);
        if (preset == null)
        {
            Debug.LogWarning($"No preset found for jumpscare type {type}");
            return;
        }

        PlayScreenShake(preset.amplitude, preset.frequency, preset.shakeLerpDuration, Ease.OutBack);
        PlayVignette(preset.vignetteIntensity, preset.vignetteLerpDuration, preset.vignetteColor, Ease.OutSine);
        PlayFOV(preset.fov, preset.fovLerpDuration, Ease.OutSine);

        StartCoroutine(EndScreenShake(preset.shakeTotalDuration, preset.shakeResetLerpDuration));
        StartCoroutine(EndVignette(preset.vignetteTotalDuration, preset.vignetteResetLerpDuration));
        StartCoroutine(EndFOVCoroutine(preset.fovTotalDuration, preset.fovResetLerpDuration));
    }

    public Transform GetFirstPersonCamTransform() => firstPersonCam != null ? firstPersonCam.transform : transform;
    private IEnumerator EndScreenShake(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        perlin.m_NoiseProfile = shakeNoise;
        PlayScreenShake(0f, 0f, duration, Ease.OutExpo);
    }

    private IEnumerator EndVignette(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        PlayVignette(normalVignetteValue, duration, normalVignetteColor, Ease.InSine);
    }

    private IEnumerator EndFOVCoroutine(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        PlayFOV(normalFOV, duration, Ease.InSine);
    }

    // --- RENDER YÖNETİMİ ---

    private void SetRenderStateForMainMenu()
    {
        // 1. Overlay kamerayı kapat
        if (overlayCamera != null) overlayCamera.gameObject.SetActive(false);

        // 2. Main Camera her şeyi görsün
        if (mainCamera != null) mainCamera.cullingMask = menuCullingMask;

        // 3. Kafayı görünür yap
        if (PlayerManager.Instance != null) PlayerManager.Instance.SetHeadVisibility(true);
    }

    private IEnumerator TransitionToGameplayRenderRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Süre doldu, render ayarlarını OYUN moduna geçir
        if (PlayerManager.Instance != null) PlayerManager.Instance.SetHeadVisibility(false);
        if (mainCamera != null) mainCamera.cullingMask = gameplayCullingMask;
        if (overlayCamera != null) overlayCamera.gameObject.SetActive(true);

        renderTransitionRoutine = null; // İş bitti, referansı boşa çıkar
    }

    private IEnumerator ResetBlendTimeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (cinemachineBrain != null)
            cinemachineBrain.m_DefaultBlend.m_Time = 0.5f;

        blendResetRoutine = null; // İş bitti
    }

    private IEnumerator UnlockPlayerControlRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (PlayerManager.Instance != null)
        {
            PlayerManager.Instance.SetPlayerCanPlay(true);
        }

        controlUnlockRoutine = null;
    }
}