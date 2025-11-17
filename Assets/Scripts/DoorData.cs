using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDoorData", menuName = "Data/Door")]
public class DoorData : ScriptableObject
{
    public string[] focusTextKeys; //0 kapýyý aç, 1 kapýyý kapat, 2 kapý kilitli
    public PlayerManager.HandRigTypes handRigType;
    public CameraManager.JumpscareType jumpscareType;
    [Space]
    public float timeToRotate = 0.3f;
    public float openYRotation = 90f;
    public float lockShakeStrength = 10f;
    [Space]
    public Vector3 jumpscareMoveAmount;
    public float timeToJumpscare = 0.4f;
    public float jumpscareEffectPercentValue = 0.2f;
    public float jumpscareSoundEffectPercentValue = 0.3f;
    public float jumpscareDoorRotatePercentValue = 0.4f; //when door gets opened %40, jumpscare starts
    [Space]
    public AudioClip openSound;
    public AudioClip closeSound;
    public AudioClip lockedSound;
    public AudioClip jumpscareSound;
}
