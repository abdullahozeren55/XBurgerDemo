using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueData", menuName = "Data/Dialogue")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public class DialogueSegment
    {
        public DialogueManager.FontType fontType;
        public DialogueManager.TalkingPerson talkingPerson;
        [Space]
        public FontStyle fontStyle;

        [TextArea] public string DialogueToPrint;
        public Vector2 DialogueOffset;
        public bool Skippable;

        [Space]
        public AudioClip audioClip;
        [Space]
        public CameraManager.CameraName cam;
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
    public CameraManager.CameraName choiceCam;
    [Space]
    public CutsceneType cutsceneType;
}
