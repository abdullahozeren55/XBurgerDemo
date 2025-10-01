using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBurgerBoxData", menuName = "Data/BurgerBox")]
public class BurgerBoxData : ScriptableObject
{
    public bool isUseable = false;
    public PlayerManager.HandGrabTypes handGrabType;
    [Space]
    public float timeToPutOnTray = 0.3f;
    [Space]
    public Vector3[] grabPositionOffset; //0 for open, 1 for close
    public Vector3[] grabRotationOffset; //0 for open, 1 for close
    [Space]
    public Vector3[] grabLocalPositionOffset; //0 for open, 1 for close
    public Vector3[] grabLocalRotationOffset; //0 for open, 1 for close
    [Space]
    public Sprite[] focusImages;

    public AudioClip[] audioClips;
}
