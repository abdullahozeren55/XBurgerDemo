using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Data/Dialogue")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public class DialogueLine
    {
        [Header("Settings")]
        public string SpeakerID; // Örn: "Player", "Customer", "Radio"
        public string LocalizationKey; // Normal metin key'i

        [Header("Horror Elements")]
        public bool IsGlitchLine; // Bu satýrda bozulma var mý?
        public GlitchType glitchType;
        public string GlitchAltKey; // Glitch ise görünecek "Halüsinasyon" metni
        public float GlitchDuration; // Swap efekti için bekleme süresi
        public AudioClip CustomVoiceOrSFX; // Özel ses efekti (Bozulma sesi vs.)

        [Header("Events")]
        // Diyalog bittiðinde veya baþladýðýnda tetiklenecek eventler (Iþýk kapatma vs.)
        public string EventTag;
    }

    public enum GlitchType
    {
        None,
        TextSwap,    // "Ne yaptýðýný bili-" -> "Sipariþ verebilir miyim?"
        Corruption,  // Metnin fontu bozuk, titriyor vs.
        HiddenMessage // Metin içinde gizli mesajlar (kýrmýzý harfler vs.)
    }

    public List<DialogueLine> lines;
}