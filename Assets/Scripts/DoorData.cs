using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDoorData", menuName = "Data/Door")]
public class DoorData : ScriptableObject
{
    public Sprite[] focusImages; //0 kapýyý aç, 1 kapýyý kapat, 2 kapý kilitli
    public GameManager.HandRigTypes handRigType;
    [Space]
    public float timeToRotate = 0.3f;
    public float openYRotation = 90f;
    public float lockShakeStrength = 10f;
    [Space]
    public Vector3 jumpscareMoveAmount;
    public float jumpscareAudioDelay = 0.1f;
    public float timeToJumpscare = 0.4f;
    public float jumpscareDoorRotatePercentValue = 0.4f; //when door gets opened %40, jumpscare starts
    [Space]
    public DialogueData dialogueAfterInteraction;
    public float dialoguePlayDelay;
    [Space]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;
    public AudioClip jumpscareSound;
}
