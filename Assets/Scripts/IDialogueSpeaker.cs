using Febucci.UI;
using UnityEngine;

public interface IDialogueSpeaker
{
    CustomerID SpeakerID { get; } // String yerine Enum

    Transform LookAtPoint { get; }
}