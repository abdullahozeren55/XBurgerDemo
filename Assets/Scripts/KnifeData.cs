using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NewKnifeData", menuName = "Data/Knife")]
public class KnifeData : ScriptableObject
{
    public bool isUseable = true;
    public bool isThrowable = true;
    public float throwMultiplier = 1f;
    public PlayerManager.HandGrabTypes handGrabType;
    public PlayerManager.HandRigTypes handRigType;
    public ItemIcon iconData;
    [Space]
    public LayerMask stabableLayers;
    [Space]
    public ParticleSystem throwParticles;
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
    public float usingFOV = 50f;
    public float timeToUse = 0.3f;
    [Space]
    public string focusTextKey;
    [Space]
    public AudioClip[] audioClips;
    [Space]
    public float grabSoundVolume = 1f;
    public float grabSoundMinPitch = 0.95f;
    public float grabSoundMaxPitch = 1.05f;
    [Space]
    public float dropSoundVolume = 0.7f;
    public float dropSoundMinPitch = 1f;
    public float dropSoundMaxPitch = 1.3f;
    [Space]
    public float throwSoundVolume = 0.8f;
    public float throwSoundMinPitch = 0.8f;
    public float throwSoundMaxPitch = 1.2f;
    [Space]
    public float soundCooldown = 0.1f;
    public float throwThreshold = 6f;
    public float dropThreshold = 2f;
}
