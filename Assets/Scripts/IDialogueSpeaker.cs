using Febucci.UI;

public interface IDialogueSpeaker
{
    string SpeakerID { get; }
    TypewriterByCharacter TextPlayer { get; } // Febucci'nin Player'ý
    void OnSpeakStart();
    void OnSpeakEnd();
}