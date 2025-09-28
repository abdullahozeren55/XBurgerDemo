using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBurgerBoxData", menuName = "Data/BurgerBox")]
public class BurgerBoxData : ScriptableObject
{
    public float trashSpaceValue = 35f;
    public float followingSpeed = 40f;
    public float timeToPutOnTray = 0.3f;
    [Space]
    public Vector3 grabPositionOffsetForOpen;
    public Vector3 grabRotationOffsetForOpen;
    [Space]
    public Vector3 grabPositionOffsetForClose;
    public Vector3 grabRotationOffsetForClose;
    [Space]
    public Sprite[] focusImages;

    public AudioClip[] audioClips;
}
