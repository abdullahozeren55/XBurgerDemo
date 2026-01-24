using AtmosphericHeightFog;
using Cinemachine;
using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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
        CustomerDialogue,
        PhoneLook,
        ShopSellerDialogue,
        CustomerDialogueAtTheDoor
    }

    [System.Serializable]
    public class CameraEntry
    {
        public CameraName camName;
        public CinemachineVirtualCamera vCam;
    }

    [Space]
    [SerializeField] private CameraEntry[] cameras;
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

    [Header("Car Hit Effect Settings")]
    [SerializeField] private float carHitDuration = 1f;        // Kırmızı ekran ve yamuk kafa süresi
    [SerializeField] private float carHitShakeDuration = 0.25f; // EKLENDİ: Sadece titreme süresi (Kısa olmalı)
    [SerializeField] private float turnNormalDuration = 1f;    // Normale dönüş yumuşaklığı
    [SerializeField] private float carHitShakeAmp = 5f;
    [SerializeField] private float carHitShakeFreq = 20f;
    [SerializeField] private float carHitVignetteIntensity = 0.55f;
    [SerializeField] private Color carHitVignetteColor = Color.red;
    [SerializeField] private float carHitDutchAngle = 20f;
    [SerializeField] private float carHitFOV = 70f;

    private float normalFOV;
    private float normalVignetteValue;
    private Color normalVignetteColor;
    private float normalColorAdjustmentsPostExposureValue;
    private Color normalColorAdjustmentsColor;

    private float normalFOVCurrentCam;

    private CinemachineVirtualCamera firstPersonCam;
    private CinemachineVirtualCamera customerDialogueCam;
    private CinemachineVirtualCamera customerDialogueAtTheDoorCam;

    private CinemachineBasicMultiChannelPerlin perlin;
    private Vignette vignette;
    private ColorAdjustments colorAdjustments;

    private CameraEntry currentCam;
    private int basePriority = 10;

    private Tween firstPersonCamFOVTween;

    private void Awake()
    {
        if (Instance == null)
        {
            // If not, set this instance as the singleton
            Instance = this;
        }
        else
        {
            // If an instance already exists, destroy this one to enforce the singleton pattern
            Destroy(gameObject);
        }

        foreach (CameraEntry entry in cameras)
        {
            if (entry.camName == CameraName.FirstPerson)
            {
                firstPersonCam = entry.vCam;

                perlin = firstPersonCam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

                normalFOV = firstPersonCam.m_Lens.FieldOfView;
            }
            else if (entry.camName == CameraName.CustomerDialogue)
            {
                customerDialogueCam = entry.vCam;
            }
            else if (entry.camName == CameraName.CustomerDialogueAtTheDoor)
            {
                customerDialogueAtTheDoorCam = entry.vCam;
            }
        }

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

    public void SwitchToCamera(CameraName name)
    {
        if (name == currentCam.camName || name == CameraName.Null)
            return;

        // Lower priority of current camera
        foreach (CameraEntry entry in cameras)
        {
            if (entry.camName == name)
            {
                currentCam.vCam.Priority = basePriority;
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

    public void ChangeFirstPersonCameraFOV(float targetFOV, float duration, float delay)
    {
        firstPersonCamFOVTween?.Kill();

        firstPersonCamFOVTween = DOTween.To(
            () => firstPersonCam.m_Lens.FieldOfView,
            x => firstPersonCam.m_Lens.FieldOfView = x,
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
            x => firstPersonCam.m_Lens.FieldOfView = x,
            normalFOV,
            duration
        )
        .SetEase(Ease.OutBack)
        .SetUpdate(true);
    }

    public CinemachineVirtualCamera GetCamera()
    {
        return currentCam.vCam;
    }

    public void SetCustomerCamLookAt(Transform lookAt, CameraName name = CameraName.CustomerDialogue)
    {
        if (name == CameraName.CustomerDialogue)
            customerDialogueCam.LookAt = lookAt;
        else if (name == CameraName.CustomerDialogueAtTheDoor)
            customerDialogueAtTheDoorCam.LookAt = lookAt;
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
        // Mevcut tweeni öldür
        DOTween.Kill(tweenId);

        // Başlangıç değerlerini al
        Color startColor = vignette.color.value;
        float startIntensity = vignette.intensity.value;

        // Sequence oluştur
        Sequence seq = DOTween.Sequence().SetId(tweenId).SetEase(ease);

        // Color tween
        seq.Join(DOTween.To(
            () => startColor,
            x => vignette.color.value = x,
            targetColor,
            duration));

        // Intensity tween
        seq.Join(DOTween.To(
            () => startIntensity,
            x => vignette.intensity.value = x,
            targetIntensity,
            duration));

        seq.Play();
    }

    public void PlayerColorAdjustments(float targetPostExposure, float duration, Color targetColor, Ease ease = Ease.OutSine, string tweenId = "ColorAdjustments")
    {
        // Mevcut tweeni öldür
        DOTween.Kill(tweenId);

        // Başlangıç değerlerini al
        Color startColor = colorAdjustments.colorFilter.value;
        float startPostExposure = colorAdjustments.postExposure.value;

        // Sequence oluştur
        Sequence seq = DOTween.Sequence().SetId(tweenId).SetEase(ease);

        // Color tween
        seq.Join(DOTween.To(
            () => startColor,
            x => colorAdjustments.colorFilter.value = x,
            targetColor,
            duration));

        // Intensity tween
        seq.Join(DOTween.To(
            () => startPostExposure,
            x => colorAdjustments.postExposure.value = x,
            targetPostExposure,
            duration));

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
        CinemachineVirtualCamera currentCam = GetCamera();

        normalFOVCurrentCam = currentCam.m_Lens.FieldOfView;

        DOTween.Kill(tweenId);

        DOTween.To(() => currentCam.m_Lens.FieldOfView, x => currentCam.m_Lens.FieldOfView = x, targetFOV, duration)
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

        DOTween.To(() => firstPersonCam.m_Lens.FieldOfView, x => firstPersonCam.m_Lens.FieldOfView = x, targetFOV, duration)
            .SetEase(ease, easeValue)
            .SetId(tweenId);
    }

    public void EndFOV(float delay, float duration)
    {
        StartCoroutine(EndFOVCoroutine(delay, duration));
    }

    public void PlayCarHitEffects(float hitDirection)
    {
        // 1. Ekran Sallantısı (ANLIK DARBE)
        perlin.m_NoiseProfile = shakeNoise;
        // Süreyi çok kısa (0.05f) veriyoruz ki "KÜT" diye anında en tepeye çıksın, yavaş yavaş artmasın.
        PlayScreenShake(carHitShakeAmp, carHitShakeFreq, 0.05f, Ease.OutQuad);

        // 2. Kırmızı Vignette (Acı hissi - Uzun kalacak)
        PlayVignette(carHitVignetteIntensity, 0.1f, carHitVignetteColor, Ease.OutQuad);

        // 3. FOV Kick (Darbe anında görüntü geri gider - Uzun kalacak)
        PlayFOV(carHitFOV, 0.1f, Ease.OutBack);

        // 4. Dutch Angle (Kafa Yamulması - Uzun kalacak)
        float tiltDirection = Mathf.Sign(hitDirection);
        float finalTilt = carHitDutchAngle * -tiltDirection; // - ile çarptım ki savrulduğu yöne yatsın (Test edip ters gelirse -'yi kaldır)

        PlayDutchAngle(finalTilt, 0.15f, Ease.OutBack);

        // --- AYRIŞTIRMA BURADA ---

        // Shake'i hemen durdur (Darbe bitti)
        StartCoroutine(EndScreenShake(carHitShakeDuration, 0.2f));

        // Diğer efektleri (Kızarıklık, yamukluk) uçuş bitince durdur
        StartCoroutine(ResetCarHitVisuals(carHitDuration));
    }

    public void PlayColdRoomEffects(bool isEntering)
    {
        float durationVig = isEntering ? vigIncreaseTimeForColdRoom : vigDecreaseTimeForColdRoom;
        float durationColorAdjustments = isEntering ? colorAdjustmentsIncreaseTimeForColdRoom : colorAdjustmentsDecreaseTimeForColdRoom;
        Ease ease = isEntering ? Ease.OutSine : Ease.InSine;

        // Diğer efektler aynen kalıyor...
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

        // Screen Shake
        PlayScreenShake(preset.amplitude, preset.frequency, preset.shakeLerpDuration, Ease.OutBack);

        // Vignette
        PlayVignette(preset.vignetteIntensity, preset.vignetteLerpDuration, preset.vignetteColor, Ease.OutSine);

        // FOV
        PlayFOV(preset.fov, preset.fovLerpDuration, Ease.OutSine);

        StartCoroutine(EndScreenShake(preset.shakeTotalDuration, preset.shakeResetLerpDuration));
        StartCoroutine(EndVignette(preset.vignetteTotalDuration, preset.vignetteResetLerpDuration));
        StartCoroutine(EndFOVCoroutine(preset.fovTotalDuration, preset.fovResetLerpDuration));
    }

    public Transform GetFirstPersonCamTransform() => firstPersonCam != null ? firstPersonCam.transform : transform; //Olmazsa kendi transformunu döndürüyo hata vermesin diye

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

    private IEnumerator ResetCarHitVisuals(float delay)
    {
        yield return new WaitForSeconds(delay);

        // Yavaşça (turnNormalDuration) normale dön
        PlayVignette(normalVignetteValue, turnNormalDuration, normalVignetteColor, Ease.InSine);
        PlayFOV(normalFOV, turnNormalDuration, Ease.InSine);
        PlayDutchAngle(0f, turnNormalDuration, Ease.OutBack);
    }
}
