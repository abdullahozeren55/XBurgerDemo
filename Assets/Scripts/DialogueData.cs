using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Data/Dialogue")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public class DialogueSegment
    {
        public string PersonName;

        [TextArea]
        public string DialogueToPrint;
        public bool Skippable;

        [Range(1f, 50f)]
        public float LettersPerSecond;
        public AudioClip audioClip;
    }

    public enum DialogueType
    {
        NORMAL,
        ENDSWITHACHOICE,
        ENDSWITHACUTSCENE
    }

    public DialogueSegment[] dialogueSegments;
    [Space]
    public DialogueType type;

    public string question;
    public string optionA;
    public string optionD;
    [Space]
    public CutsceneType cutsceneType;
}
