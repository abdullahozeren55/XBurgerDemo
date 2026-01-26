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
        [Tooltip("Metne eklenecek Rich Text efektleri")]
        public RichTextTag TextEffects; // YENÝ: Çoklu seçim

        [Header("Flow Control")]
        [Tooltip("Oyuncu bu satýrý hýzlýca geçebilir mi?")]
        public bool IsSkippable = true;

        [Tooltip("Yazý yazma hýzý çarpaný. (1 = Normal, 2 = 2x Hýzlý, 0.5 = Yavaþ)")]
        [Range(0.1f, 5f)] public float TypewriterSpeedMultiplier = 1f;

        [Tooltip("Satýr baþladýktan kaç saniye sonra METÝN yazmaya baþlasýn?")]
        public float TextDelay = 0f;

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

        [Header("Glitch & Flow Logic")]
        public LineBehavior Behavior = LineBehavior.Normal; // YENÝ: Satýrýn kaderi

        [Tooltip("Meltdown veya HorrorReveal seçiliyse, bu süre sonunda sonraki satýra geçer.")]
        public float AutoAdvanceDuration = 2.0f;

        [Header("Horror Elements")]
        public JumpscareType Jumpscare = JumpscareType.None;
        [Tooltip("Satýr baþladýktan kaç saniye sonra JUMPSCARE patlasýn?")]
        public float JumpscareDelay = 0f;

        [Header("Audio")]
        public TypewriterSoundType SoundType = TypewriterSoundType.Soft;

        [Header("Events")]
        [Tooltip("Bu satýr oynarken gerçekleþecek olaylar (Birden fazla seçilebilir)")]
        public DialogueEvent Events;
    }

    public List<DialogueLine> lines;
}