using UnityEngine;
using Febucci.UI;

public class DialogueSpeaker : MonoBehaviour, IDialogueSpeaker
{
    [SerializeField] private string _speakerID;
    [SerializeField] private TypewriterByCharacter _textPlayer; // Inspector'dan ata
    [SerializeField] private GameObject _bubbleRoot; // Konuþma balonunun kendisi (Aç/Kapa için)

    public string SpeakerID => _speakerID;
    public TypewriterByCharacter TextPlayer => _textPlayer;

    private void Awake()
    {
        // Kendimizi Manager'a kaydettiriyoruz
        DialogueManager.Instance.RegisterSpeaker(this);
        if (_bubbleRoot) _bubbleRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.UnregisterSpeaker(this);
    }

    public void OnSpeakStart()
    {
        if (_bubbleRoot) _bubbleRoot.SetActive(true);
        // Ýstersen burada konuþma animasyonu (kafa sallama vs.) tetikle
    }

    public void OnSpeakEnd()
    {
        if (_bubbleRoot) _bubbleRoot.SetActive(false);
    }
}