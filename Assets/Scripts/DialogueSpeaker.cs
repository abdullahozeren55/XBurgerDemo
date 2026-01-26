using UnityEngine;
using Febucci.UI;

public class DialogueSpeaker : MonoBehaviour, IDialogueSpeaker
{
    [Header("Identity")]
    [SerializeField] private CustomerID _speakerID; // Inspector'dan seç

    [Header("Camera Focus")]
    [Tooltip("Kamera bu kiþiye bakarken tam olarak nereyi hedeflesin? (Gözler)")]
    [SerializeField] private Transform _lookAtPoint;

    // Interface Implementation
    public CustomerID SpeakerID => _speakerID;

    public Transform LookAtPoint => _lookAtPoint != null ? _lookAtPoint : transform;

    private void Awake()
    {
        DialogueManager.Instance.RegisterSpeaker(this);
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.UnregisterSpeaker(this);
    }
}