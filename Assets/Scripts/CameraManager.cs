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
}

public enum JumpscareType
{
    None,
    Small,
    Mid,
    Big
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

    // Hızlı erişim için Dictionary
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
    private Coroutine controlUnlockRoutine;    // Oyuncuya kontrolü verme zamanlayıcısı

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
        if (PostProcessManager.Instance != null && PostProcessManager.Instance.PostProcessVolumeOnTopAll != null)
        {
            if (PostProcessManager.Instance.PostProcessVolumeOnTopAll.profile.TryGet(out Vignette v))
            {
                vignette = v;
                normalVignetteValue = vignette.intensity.value;
                normalVignetteColor = vignette.color.value;
            }

            if (PostProcessManager.Instance.PostProcessVolumeOnTopAll.profile.TryGet(out ColorAdjustments c))
            {
                colorAdjustments = c;
                normalColorAdjustmentsPostExposureValue = colorAdjustments.postExposure.value;
                normalColorAdjustmentsColor = colorAdjustments.colorFilter.value;
            }
        }
    }

    private void OnEnable()
    {
        Settings.OnDistortionChanged += OnDistortionSettingsChanged;
    }

    private void OnDisable()
    {
        Settings.OnDistortionChanged -= OnDistortionSettingsChanged;
    }

    // --- EVENT HANDLER: AYAR DEĞİŞİNCE ÇALIŞIR ---
    private void OnDistortionSettingsChanged(float multiplier)
    {
        // Şu an aktif olan kamerayı bul ve gürültüsünü güncelle
        CinemachineVirtualCamera activeCam = GetActiveCamera();
        if (activeCam == null) return;

        var perlin = activeCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (perlin == null) return;

        // 1. EĞER MENÜDEYSEK (Menu Cam)
        if (currentMenuCam != null && activeCam == currentMenuCam)
        {
            // Menüde her zaman IdleBreathing var varsayıyoruz
            if (noiseMap.ContainsKey(CameraNoiseType.IdleBreathing))
            {
                var preset = noiseMap[CameraNoiseType.IdleBreathing];
                perlin.m_AmplitudeGain = preset.defaultAmplitude * multiplier;
                // Frekansa dokunmuyoruz, sadece şiddet
            }
        }
        // 2. EĞER OYUNDAYSAK (Throw veya Cold Room efekti varsa)
        // Burası biraz karışık olabilir çünkü o an hangi efektin (Throw/Cold) aktif olduğunu bilmiyoruz.
        // Ama en azından "Sıfırla" dendiğinde (multiplier 0) her şeyi susturabiliriz.
        else if (multiplier <= 0.01f)
        {
            perlin.m_AmplitudeGain = 0f;
        }

        // NOT: Diyalog sırasındaysak zaten UpdateDialogueShot sürekli tweenliyor,
        // ama anlık kesilme istiyorsan buraya diyalog kontrolü de ekleyebilirsin.
        // Şimdilik menü ve genel "OFF" durumu için bu yeterli.
    }

    // --- HELPER: AKTİF KAMERAYI BUL ---
    public CinemachineVirtualCamera GetActiveCamera()
    {
        // 1. Eğer Diyalog Modundaysak (Priority yüksekse)
        if (dialogueCam != null && dialogueCam.Priority >= activePriority)
            return dialogueCam;

        // 2. Eğer Menüdeysek
        if (currentMenuCam != null && currentMenuCam.Priority > defaultPriority)
            return currentMenuCam;

        // 3. Oyun Modundaysak (First Person)
        return firstPersonCam;
    }

    // --- "JOLT" FIX: DİYALOG BAŞLANGICI ---
    public void StartDialogueMode()
    {
        if (dialogueCam != null)
        {
            // YENİ: Kamerayı aktif etmeden ÖNCE gürültüsünü ayarla
            // Böylece Inspector değerleriyle (varsayılan noise) bir kare bile görünmez.
            var perlin = dialogueCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
            if (perlin != null)
            {
                // Başlangıçta gürültüyü Settings çarpanına göre ayarla veya sıfırla
                // Genelde diyalog başında noise type 'None' veya 'Idle' olur.
                // Güvenli yöntem: Sıfırla veya mevcut profili çarp.
                perlin.m_AmplitudeGain *= Settings.GlobalDistortionMultiplier;
            }

            dialogueCam.Priority = activePriority;
        }
    }

    public void EndDialogueMode()
    {
        if (dialogueCam != null)
        {
            dialogueCam.Priority = defaultPriority;
        }
    }

    public void UpdateDialogueShot(Transform targetTransform, float moveDuration, Ease moveEase, float dutchAngle, float fov, CameraNoiseType noiseType, float ampMult, float freqMult, bool isInstant = false, bool skipLensAndNoise = false)
    {
        if (dialogueCam == null || dialogueLookTarget == null) return;

        // --- 1. POZİSYON VE ROTASYON ---
        Vector3 targetLookPos = targetTransform != null ? targetTransform.position : dialogueLookTarget.position;
        Vector3 targetCamPos = dialogueCam.transform.position;

        if (targetTransform != null)
        {
            Vector3 customerForward = targetTransform.root.forward;
            targetCamPos = targetTransform.position + (customerForward * faceToFaceDistance);
            targetCamPos.y += heightOffset;
        }

        if (isInstant)
        {
            dialogueLookTarget.DOKill();
            dialogueCam.transform.DOKill();
            dialogueLookTarget.position = targetLookPos;
            dialogueCam.transform.position = targetCamPos;
        }
        else
        {
            dialogueLookTarget.DOMove(targetLookPos, moveDuration).SetEase(moveEase);
            if (targetTransform != null)
            {
                dialogueCam.transform.DOMove(targetCamPos, repositionDuration).SetEase(Ease.OutCubic);
            }
        }

        // --- 2. LENS VE NOISE ---
        if (skipLensAndNoise) return;

        var perlin = dialogueCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        // --- SETTINGS ÇARPANI ---
        float globalMult = Settings.GlobalDistortionMultiplier;
        // Bu satır kritik. Ayarlardan gelen 0.0, 0.5 veya 1.0 değerini alıyoruz.

        if (isInstant)
        {
            dialogueCam.DOKill();
            if (perlin != null) DOTween.Kill(perlin);

            dialogueCam.m_Lens.Dutch = dutchAngle;
            dialogueCam.m_Lens.FieldOfView = fov;

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

                    // GÜNCELLEME: Çarpanı buraya ekledik
                    perlin.m_AmplitudeGain = preset.defaultAmplitude * ampMult * globalMult;
                    perlin.m_FrequencyGain = preset.defaultFrequency * freqMult;
                }
            }
        }
        else
        {
            DOTween.To(() => dialogueCam.m_Lens.Dutch, x => dialogueCam.m_Lens.Dutch = x, dutchAngle, moveDuration)
            .SetEase(moveEase)
            .SetTarget(dialogueCam);

            DOTween.To(() => dialogueCam.m_Lens.FieldOfView, x => dialogueCam.m_Lens.FieldOfView = x, fov, moveDuration)
                .SetEase(moveEase)
                .SetTarget(dialogueCam);

            if (perlin != null)
            {
                if (noiseType == CameraNoiseType.None)
                {
                    // ... (Sıfırlama Tweenleri AYNI - Dokunma) ...
                }
                else if (noiseMap.ContainsKey(noiseType))
                {
                    NoisePreset preset = noiseMap[noiseType];
                    if (perlin.m_NoiseProfile != preset.settingsAsset) perlin.m_NoiseProfile = preset.settingsAsset;

                    // GÜNCELLEME: Hedef şiddeti çarpanla hesaplıyoruz
                    float targetAmp = preset.defaultAmplitude * ampMult * globalMult;
                    float targetFreq = preset.defaultFrequency * freqMult;

                    DOTween.To(() => perlin.m_AmplitudeGain, x => perlin.m_AmplitudeGain = x, targetAmp, moveDuration)
                           .SetEase(moveEase)
                           .SetTarget(perlin);

                    DOTween.To(() => perlin.m_FrequencyGain, x => perlin.m_FrequencyGain = x, targetFreq, moveDuration)
                           .SetEase(moveEase)
                           .SetTarget(perlin);
                }
            }
        }
    }

    public Transform GetDialogueCameraTransform()
    {
        if (dialogueCam != null) return dialogueCam.transform;
        return transform; // Fallback
    }

    // --- MENÜ VE OYUN GEÇİŞ SİSTEMİ ---

    public void SwitchToRandomMainMenuCamera(bool instant = false)
    {
        if (renderTransitionRoutine != null) StopCoroutine(renderTransitionRoutine);
        if (blendResetRoutine != null) StopCoroutine(blendResetRoutine);
        if (controlUnlockRoutine != null) StopCoroutine(controlUnlockRoutine);

        if (PlayerManager.Instance != null) PlayerManager.Instance.SetPlayerCanPlay(false);

        if (mainMenuCameras == null || mainMenuCameras.Count == 0) return;

        if (cinemachineBrain != null)
        {
            cinemachineBrain.m_DefaultBlend.m_Time = instant ? 0f : gameToMenuBlendTime;
        }

        ResetAllPriorities();
        int randomIndex = Random.Range(0, mainMenuCameras.Count);
        currentMenuCam = mainMenuCameras[randomIndex];

        if (currentMenuCam != null)
        {
            currentMenuCam.Priority = basePriority + 1;

            // --- GÜNCELLEME: NOISE ENTEGRASYONU ---
            // Seçilen kameranın gürültü ayarını yapalım.
            var perlin = currentMenuCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

            // IdleBreathing profili var mı diye bakıyoruz
            if (perlin != null && noiseMap.ContainsKey(CameraNoiseType.IdleBreathing))
            {
                var preset = noiseMap[CameraNoiseType.IdleBreathing];

                // Profili ata
                perlin.m_NoiseProfile = preset.settingsAsset;

                // Şiddeti global çarpanla ayarla
                // Menüde genelde "1x" (Normal) şiddet isteriz, o yüzden ampMult yerine direkt 1f ile çarpıyoruz.
                perlin.m_AmplitudeGain = preset.defaultAmplitude * Settings.GlobalDistortionMultiplier;
                perlin.m_FrequencyGain = preset.defaultFrequency;
            }
        }

        currentCam = null;

        SetRenderStateForMainMenu();
    }

    public void SwitchToGameplayCamera()
    {
        // 1. Temizlik
        if (renderTransitionRoutine != null) StopCoroutine(renderTransitionRoutine);
        if (blendResetRoutine != null) StopCoroutine(blendResetRoutine);
        if (controlUnlockRoutine != null) StopCoroutine(controlUnlockRoutine);

        // 2. Dinamik Süre Hesabı
        float multiplier = 1f;

        if (cinemachineBrain != null && cinemachineBrain.IsBlending)
        {
            var activeBlend = cinemachineBrain.ActiveBlend;
            if (activeBlend.Duration > 0)
            {
                multiplier = activeBlend.TimeInBlend / activeBlend.Duration;
            }
        }

        float effectiveDuration = menuToGameBlendTime * multiplier;
        effectiveDuration = Mathf.Max(effectiveDuration, 0.1f);

        // 3. KAMERA AYARI
        if (cinemachineBrain != null)
            cinemachineBrain.m_DefaultBlend.m_Time = menuToGameBlendTime;

        SwitchToCamera(CameraName.FirstPerson);

        // 4. ZAMANLAYICILAR
        renderTransitionRoutine = StartCoroutine(TransitionToGameplayRenderRoutine(effectiveDuration * 0.97f));
        controlUnlockRoutine = StartCoroutine(UnlockPlayerControlRoutine(effectiveDuration * 0.5f));
        blendResetRoutine = StartCoroutine(ResetBlendTimeAfterDelay(effectiveDuration));
    }

    private void ResetAllPriorities()
    {
        foreach (var entry in cameras)
        {
            if (entry.vCam != null) entry.vCam.Priority = basePriority;
        }

        foreach (var cam in mainMenuCameras)
        {
            if (cam != null) cam.Priority = basePriority;
        }
    }

    // ------------------------------------------------

    public void SwitchToCamera(CameraName name)
    {
        ResetAllPriorities();

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
        if (currentCam != null) return;
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
        if (currentCam == null && currentMenuCam != null)
            return currentMenuCam;

        return currentCam != null ? currentCam.vCam : firstPersonCam;
    }

    public void SwitchToFirstPersonCamera()
    {
        SwitchToCamera(CameraName.FirstPerson);
    }

    // --- GENEL EFEKTLER (SETTINGS ENTEGRASYONLU) ---

    public void PlayScreenShake(float targetAmplitude, float targetFrequency, float duration, Ease ease = Ease.OutSine, string tweenId = "ScreenShake")
    {
        // Settings Entegrasyonu
        float globalMult = Settings.GlobalDistortionMultiplier;
        targetAmplitude *= globalMult;

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

    // --- GAMEPLAY EFEKTLERİ (SETTINGS ENTEGRASYONLU) ---

    public void PlayColdRoomEffects(bool isEntering)
    {
        float globalMult = Settings.GlobalDistortionMultiplier;

        float durationVig = isEntering ? vigIncreaseTimeForColdRoom : vigDecreaseTimeForColdRoom;
        float durationColorAdjustments = isEntering ? colorAdjustmentsIncreaseTimeForColdRoom : colorAdjustmentsDecreaseTimeForColdRoom;
        Ease ease = isEntering ? Ease.OutSine : Ease.InSine;

        // Vignette şiddetini kullanıcı tercihine göre ayarla
        float targetVig = Mathf.Lerp(normalVignetteValue, vignetteIntensityForColdRoom, globalMult);

        PlayVignette(isEntering ? targetVig : normalVignetteValue, durationVig, isEntering ? coldRoomVignetteColor : normalVignetteColor, ease);

        // RENKLER DOKUNULMAZ
        PlayerColorAdjustments(isEntering ? colorAdjustmentsPostExposureForColdRoom : normalColorAdjustmentsPostExposureValue, durationColorAdjustments, isEntering ? coldRoomColorAdjustmentsColor : normalColorAdjustmentsColor, ease);
    }

    public void PlayThrowEffects(bool isCharging)
    {
        perlin.m_NoiseProfile = wobbleNoise;
        float globalMult = Settings.GlobalDistortionMultiplier;

        float duration = isCharging ? throwMaxChargeTime : throwMaxChargeTime / releaseSpeedMultiplier;
        Ease ease = isCharging ? Ease.OutSine : Ease.InSine;

        // Shake
        PlayScreenShake(isCharging ? (maxAmplitudeGain * globalMult) : 0f,
                        isCharging ? maxFrequencyGain : 0f,
                        duration, ease);

        // Vignette
        float targetVig = Mathf.Lerp(normalVignetteValue, vignetteIntensityForThrowCharge, globalMult);
        PlayVignette(isCharging ? targetVig : normalVignetteValue,
                     duration, isCharging ? throwChargeColor : normalVignetteColor, ease);

        // FOV
        float targetFOV = Mathf.Lerp(normalFOV, maxFOV, globalMult);
        PlayFOV(isCharging ? targetFOV : normalFOV,
                duration, Ease.InOutBack, 3f, "ThrowEffectsFOV");
    }

    // --- JUMPSCARE SİSTEMİ (SETTINGS ENTEGRASYONLU & TWEEN TEMİZLİKLİ) ---

    public void TriggerJumpscare(JumpscareType type, System.Action onComplete = null)
    {
        if (type == JumpscareType.None) return;

        var preset = jumpscarePresets.Find(p => p.type == type);
        if (preset == null) return;

        CinemachineVirtualCamera activeCam = GetActiveCamera();
        if (activeCam == null) return;

        var perlin = activeCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

        // --- TWEEN TEMİZLİĞİ ---
        activeCam.DOKill();
        DOTween.Kill(activeCam);
        if (perlin != null) DOTween.Kill(perlin);

        NoiseSettings originalProfile = perlin != null ? perlin.m_NoiseProfile : null;
        float originalAmp = perlin != null ? perlin.m_AmplitudeGain : 0f;
        float originalFreq = perlin != null ? perlin.m_FrequencyGain : 0f;
        float originalFOV = activeCam.m_Lens.FieldOfView;

        if (perlin != null)
        {
            perlin.m_NoiseProfile = shakeNoise;
            perlin.m_AmplitudeGain = 0;
            perlin.m_FrequencyGain = 0;
        }

        Sequence jumpSeq = DOTween.Sequence();

        // --- SETTINGS ENTEGRASYONU ---
        float globalMult = Settings.GlobalDistortionMultiplier;

        // A) CAMERA SHAKE
        if (perlin != null)
        {
            float targetAmp = preset.amplitude * globalMult;

            jumpSeq.Insert(0, DOTween.To(() => perlin.m_AmplitudeGain, x => perlin.m_AmplitudeGain = x, targetAmp, preset.shakeLerpDuration)
                .SetEase(Ease.OutExpo).SetTarget(perlin));

            jumpSeq.Insert(0, DOTween.To(() => perlin.m_FrequencyGain, x => perlin.m_FrequencyGain = x, preset.frequency, preset.shakeLerpDuration)
                .SetEase(Ease.OutExpo).SetTarget(perlin));

            // Decay
            float decayDuration = preset.shakeTotalDuration - preset.shakeLerpDuration;
            if (decayDuration < 0.1f) decayDuration = 0.1f;

            jumpSeq.Insert(preset.shakeLerpDuration, DOTween.To(() => perlin.m_AmplitudeGain, x => perlin.m_AmplitudeGain = x, 0f, decayDuration)
                .SetEase(Ease.InQuad).SetTarget(perlin));

            jumpSeq.Insert(preset.shakeLerpDuration, DOTween.To(() => perlin.m_FrequencyGain, x => perlin.m_FrequencyGain = x, 0f, decayDuration)
                .SetEase(Ease.InQuad).SetTarget(perlin));
        }

        // B) FOV KICK
        float targetFOV = Mathf.Lerp(originalFOV, preset.fov, globalMult);

        jumpSeq.Insert(0, DOTween.To(() => activeCam.m_Lens.FieldOfView, x => activeCam.m_Lens.FieldOfView = x, targetFOV, preset.fovLerpDuration)
            .SetEase(Ease.OutBack).SetTarget(activeCam));

        jumpSeq.Insert(preset.fovTotalDuration, DOTween.To(() => activeCam.m_Lens.FieldOfView, x => activeCam.m_Lens.FieldOfView = x, originalFOV, preset.fovResetLerpDuration)
            .SetEase(Ease.InOutSine).SetTarget(activeCam));

        // C) VIGNETTE
        if (vignette != null)
        {
            Color originalVigColor = vignette.color.value;
            float originalVigInt = vignette.intensity.value;

            float targetVigInt = Mathf.Lerp(normalVignetteValue, preset.vignetteIntensity, globalMult);

            jumpSeq.Insert(0, DOTween.To(() => vignette.color.value, x => vignette.color.value = x, preset.vignetteColor, preset.vignetteLerpDuration));
            jumpSeq.Insert(0, DOTween.To(() => vignette.intensity.value, x => vignette.intensity.value = x, targetVigInt, preset.vignetteLerpDuration));

            jumpSeq.Insert(preset.vignetteTotalDuration, DOTween.To(() => vignette.color.value, x => vignette.color.value = x, originalVigColor, preset.vignetteResetLerpDuration));
            jumpSeq.Insert(preset.vignetteTotalDuration, DOTween.To(() => vignette.intensity.value, x => vignette.intensity.value = x, originalVigInt, preset.vignetteResetLerpDuration));
        }

        jumpSeq.OnComplete(() =>
        {
            if (perlin != null)
            {
                perlin.m_NoiseProfile = originalProfile;
                DOTween.To(() => perlin.m_AmplitudeGain, x => perlin.m_AmplitudeGain = x, originalAmp, 0.5f).SetTarget(perlin);
                DOTween.To(() => perlin.m_FrequencyGain, x => perlin.m_FrequencyGain = x, originalFreq, 0.5f).SetTarget(perlin);
            }
            onComplete?.Invoke();
        });
    }

    public Transform GetFirstPersonCamTransform() => firstPersonCam != null ? firstPersonCam.transform : transform;

    private IEnumerator EndFOVCoroutine(float delay, float duration)
    {
        yield return new WaitForSeconds(delay);
        PlayFOV(normalFOV, duration, Ease.InSine);
    }

    // --- RENDER YÖNETİMİ ---

    private void SetRenderStateForMainMenu()
    {
        if (overlayCamera != null) overlayCamera.gameObject.SetActive(false);
        if (mainCamera != null) mainCamera.cullingMask = menuCullingMask;
        if (PlayerManager.Instance != null) PlayerManager.Instance.SetHeadVisibility(true);
    }

    private IEnumerator TransitionToGameplayRenderRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PlayerManager.Instance != null) PlayerManager.Instance.SetHeadVisibility(false);
        if (mainCamera != null) mainCamera.cullingMask = gameplayCullingMask;
        if (overlayCamera != null) overlayCamera.gameObject.SetActive(true);
        renderTransitionRoutine = null;
    }

    private IEnumerator ResetBlendTimeAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (cinemachineBrain != null)
            cinemachineBrain.m_DefaultBlend.m_Time = 0.5f;
        blendResetRoutine = null;
    }

    private IEnumerator UnlockPlayerControlRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (PlayerManager.Instance != null)
            PlayerManager.Instance.SetPlayerCanPlay(true);
        controlUnlockRoutine = null;
    }
}