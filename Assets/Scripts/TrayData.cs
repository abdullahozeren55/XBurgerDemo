using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewTrayData", menuName = "Data/Tray")]
public class TrayData : ScriptableObject
{
    public bool isUseable = false;
    public bool isThrowable = true;
    public float throwMultiplier = 1f;
    public PlayerManager.HandGrabTypes handGrabType;
    public PlayerManager.HandRigTypes handRigType;
    public Sprite icon;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public string focusTextKey;
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
    public float soundCooldown = 0.1f;
    public float throwThreshold = 6f;
    public float dropThreshold = 2f;
}
