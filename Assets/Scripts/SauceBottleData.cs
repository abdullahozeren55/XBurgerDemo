using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SauceBottle;

[CreateAssetMenu(fileName = "NewSauceBottleData", menuName = "Data/Sauce Bottle")]
public class SauceBottleData : ScriptableObject
{
    public bool isUseable = true;
    public bool isThrowable = true;
    public float throwMultiplier = 1f;
    public PlayerManager.HandGrabTypes handGrabType;
    public PlayerManager.HandRigTypes handRigType;
    public ItemIcon iconData;
    public string focusTextKey;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]
    public Vector3 usePositionOffset;
    public Vector3 useRotationOffset;
    [Space]
    public Vector3 useLocalPositionOffset;
    public Vector3 useLocalRotationOffset;
    [Space]
    public float timeToUse = 0.3f;
    public float usingFOV = 70f;
    [Space]
    public AudioClip[] audioClips; //0 grab, 1 drop, 2 throw
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
