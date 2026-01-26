using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Data/Dialogue")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public class DialogueLine
    {
        [Header("Settings")]
        public CustomerID SpeakerID;
        public string LocalizationKey;

        [Header("Visuals")]
        public Color TextColor = Color.white;

        [Header("Camera Juice")]
        public CustomerID FocusTargetID;
        [Range(0f, 5f)] public float CamMoveDuration = 1f;
        public Ease CamMoveEase = Ease.InOutSine;
        [Range(-45f, 45f)] public float DutchAngle = 0f;
        public float TargetFOV = 60f;

        [Header("Noise Settings")]
        // YENÝ: Enum seçimi
        public CameraNoiseType NoiseType = CameraNoiseType.IdleBreathing;

        // YENÝ: Varsayýlan þiddeti kullanmak istemiyorsan bunlarý deðiþtir.
        // -1 vererek "Varsayýlaný kullan" diyebiliriz veya bir bool koyabiliriz.
        // Pratik yöntem: Varsayýlan 1.0f (Multiplier) kullanmak.
        [Tooltip("Gürültü þiddeti çarpaný (1 = Normal, 2 = 2 Kat Þiddetli)")]
        [Range(0f, 5f)] public float NoiseAmplitudeMultiplier = 1f;
        [Tooltip("Gürültü hýzý çarpaný (1 = Normal, 2 = 2 Kat Hýzlý)")]
        [Range(0f, 5f)] public float NoiseFrequencyMultiplier = 1f;

        [Header("Horror Elements")]
        public bool IsGlitchLine;
        public GlitchType glitchType;
        public string GlitchAltKey;
        public float GlitchDuration;
        public AudioClip CustomVoiceOrSFX;

        [Header("Events")]
        [Tooltip("Bu satýr oynarken gerçekleþecek olaylar (Birden fazla seçilebilir)")]
        public DialogueEvent Events;
    }

    public enum GlitchType
    {
        None, TextSwap, Corruption, HiddenMessage
    }

    public List<DialogueLine> lines;
}