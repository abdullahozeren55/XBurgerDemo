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
        PhoneLook
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

    [SerializeField] private Volume postProcessVolume;
    [SerializeField] private NoiseSettings wobbleNoise;
    [SerializeField] private NoiseSettings shakeNoise;
    [Space]

    [Header("Player Throw Charge Effect Settings")]
    [SerializeField] private float maxAmplitudeGain = 0.2f;
    [SerializeField] private float maxFrequencyGain = 0.4f;
    [SerializeField] private float maxFOV = 50f;
    [SerializeField] private float throwMaxChargeTime = 1.5f;
    [SerializeField] private float vignetteIntensity = 0.25f;
    [SerializeField] private Color throwChargeColor;
    [SerializeField] private float releaseSpeedMultiplier = 4f;
    private float normalFOV;

    private CinemachineVirtualCamera firstPersonCam;
    private CinemachineVirtualCamera customerDialogueCam;

    private CinemachineBasicMultiChannelPerlin perlin;
    private Vignette vignette;

    private CameraEntry currentCam;
    private int basePriority = 10;

    private Tween firstPersonCamFOVTween;

    private void Awake()
    {
        if (Instance == null)
        {
            // If not, set this instance as the singleton
            Instance = this;

            // Optionally, mark GameManager as not destroyed between scene loads
            DontDestroyOnLoad(gameObject);
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
        }

        if (postProcessVolume.profile.TryGet(out Vignette v))
            vignette = v;

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

    public void SetCustomerCamLookAt(Transform lookAt)
    {
        customerDialogueCam.LookAt = lookAt;
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

    public void PlayVignette(float targetIntensity, float duration, Color? color = null, Ease ease = Ease.OutSine, string tweenId = "Vignette")
    {
        DOTween.Kill(tweenId);

        if (color.HasValue)
            vignette.color.value = color.Value;

        DOTween.To(() => vignette.intensity.value, x => vignette.intensity.value = x, targetIntensity, duration)
            .SetEase(ease)
            .SetId(tweenId);
    }

    public void PlayFOV(float targetFOV, float duration, Ease ease = Ease.OutSine, string tweenId = "FOV")
    {
        DOTween.Kill(tweenId);

        DOTween.To(() => firstPersonCam.m_Lens.FieldOfView, x => firstPersonCam.m_Lens.FieldOfView = x, targetFOV, duration)
            .SetEase(ease)
            .SetId(tweenId);
    }

    public void PlayThrowEffects(bool isCharging)
    {
        perlin.m_NoiseProfile = wobbleNoise;

        float duration = isCharging ? throwMaxChargeTime : throwMaxChargeTime / releaseSpeedMultiplier;
        Ease ease = isCharging ? Ease.OutSine : Ease.InSine;

        PlayScreenShake(isCharging ? maxAmplitudeGain : 0f,
                        isCharging ? maxFrequencyGain : 0f,
                        duration, ease, "ThrowEffects_Shake");

        PlayVignette(isCharging ? vignetteIntensity : 0f,
                     duration, throwChargeColor, ease, "ThrowEffects_Vignette");

        PlayFOV(isCharging ? maxFOV : normalFOV,
                duration, ease, "ThrowEffects_FOV");
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
        PlayScreenShake(preset.amplitude, preset.frequency, preset.shakeLerpDuration, Ease.OutBack, $"Jumpscare_{type}_Shake");

        // Vignette
        PlayVignette(preset.vignetteIntensity, preset.vignetteLerpDuration, preset.vignetteColor, Ease.OutSine, $"Jumpscare_{type}_Vignette");

        // FOV
        PlayFOV(preset.fov, preset.fovLerpDuration, Ease.OutSine, $"Jumpscare_{type}_FOV");

        StartCoroutine(EndScreenShake(preset.shakeTotalDuration, preset.shakeResetLerpDuration, type));
        StartCoroutine(EndVignette(preset.vignetteTotalDuration, preset.vignetteResetLerpDuration, type));
        StartCoroutine(EndFOV(preset.fovTotalDuration, preset.fovResetLerpDuration, type));
    }

    private IEnumerator EndScreenShake(float delay, float duration, JumpscareType type)
    {
        yield return new WaitForSeconds(delay);

        perlin.m_NoiseProfile = shakeNoise;

        PlayScreenShake(0f, 0f, duration, Ease.OutExpo, $"Jumpscare_{type}_Shake");
    }

    private IEnumerator EndVignette(float delay, float duration, JumpscareType type)
    {
        yield return new WaitForSeconds(delay);

        PlayVignette(0f, duration, null, Ease.InSine, $"Jumpscare_{type}_Vignette");
    }

    private IEnumerator EndFOV(float delay, float duration, JumpscareType type)
    {
        yield return new WaitForSeconds(delay);

        PlayFOV(normalFOV, duration, Ease.InSine, $"Jumpscare_{type}_FOV");
    }
}
