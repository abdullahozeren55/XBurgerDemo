using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBurgerBoxData", menuName = "Data/BurgerBox")]
public class BurgerBoxData : ScriptableObject
{
    public bool isUseable = false;
    public bool isThrowable = true;
    public PlayerManager.HandGrabTypes[] handGrabTypes; //0 for open, 1 for close
    [Space]
    public float timeToPutOnTray = 0.3f;
    public float throwMultiplier = 1f;
    public Vector3 trayRotation = new Vector3(0, 0, -90);
    [Space]
    public Vector3[] grabPositionOffset; //0 for open, 1 for close
    public Vector3[] grabRotationOffset; //0 for open, 1 for close
    [Space]
    public Vector3[] grabLocalPositionOffset; //0 for open, 1 for close
    public Vector3[] grabLocalRotationOffset; //0 for open, 1 for close
    [Space]
    public string[] focusTextKeys;

    public AudioClip[] audioClips;
    [Space]
    public float grabSoundVolume = 1f;
    public float grabSoundMinPitch = 0.85f;
    public float grabSoundMaxPitch = 1.15f;
    [Space]
    public float dropSoundVolume = 1f;
    public float dropSoundMinPitch = 0.85f;
    public float dropSoundMaxPitch = 1.15f;
    [Space]
    public float throwSoundVolume = 1f;
    public float throwSoundMinPitch = 0.85f;
    public float throwSoundMaxPitch = 1.15f;
    [Space]
    public float closeSoundVolume = 1f;
    public float closeSoundMinPitch = 0.85f;
    public float closeSoundMaxPitch = 1.15f;
    [Space]
    public float soundCooldown = 0.1f;
    public float throwThreshold = 6f;
    public float dropThreshold = 2f;
}
