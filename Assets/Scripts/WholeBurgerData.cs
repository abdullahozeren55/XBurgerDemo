using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWholeBurgerData", menuName = "Data/WholeBurger")]
public class WholeBurgerData : ScriptableObject
{
    public bool isUseable = false;
    public bool isThrowable = true;
    public float throwMultiplier = 1f;
    [Space]
    public PlayerManager.HandGrabTypes handGrabType;
    public PlayerManager.HandRigTypes handRigType = PlayerManager.HandRigTypes.SingleHandGrab;
    public ItemIcon iconData;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    public Vector3 boxPlacementPositionOffset;
    public Vector3 boxPlacementRotationOffset;
    [Space]
    public string[] focusTextKeys;
    [Space]
    public ParticleSystem dropParticle;
    public ParticleSystem throwParticle;
    [Space]
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
    public float initSoundVolume = 1f;
    public float initSoundMinPitch = 0.85f;
    public float initSoundMaxPitch = 1.15f;
    [Space]
    public float soundCooldown = 0.1f;
    public float throwThreshold = 5f;
    public float dropThreshold = 1f;
}
