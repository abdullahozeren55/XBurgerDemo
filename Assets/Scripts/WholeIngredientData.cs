using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWholeIngredientData", menuName = "Data/WholeIngredient")]
public class WholeIngredientData : ScriptableObject
{
    public enum WholeIngredientType
    {
        BUN,
        PICKLE,
        TOMATO,
        ONION,
        LETTUCE
    }

    public WholeIngredientType Type;
    [Space]
    public GameObject destroyParticle;
    public GameObject destroyParticleExplode;
    public Quaternion instantiateRotationOffset;
    [Space]
    public string focusTextKey;
    public bool isUseable = false;
    public PlayerManager.HandGrabTypes handGrabType;
    [Space]
    public float minForce = 0.2f;
    public float maxForce = 0.4f;
    public float minForceExplode = 0.4f;
    public float maxForceExplode = 0.8f;
    public int objectAmount = 4;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]
    public ParticleSystem dropParticles;
    public ParticleSystem throwParticles;
    [Space]
    public AudioClip[] audioClips; //0 grab, 1 drop, 2 throw, 3 slice, 4 explode
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
    public float openSoundVolume = 0.8f;
    public float openSoundMinPitch = 0.8f;
    public float openSoundMaxPitch = 1.2f;
    [Space]
    public float explodeSoundVolume = 0.8f;
    public float explodeSoundMinPitch = 0.8f;
    public float explodeSoundMaxPitch = 1.2f;
    [Space]
    public float soundCooldown = 0.1f;
    public float throwThreshold = 6f;
    public float dropThreshold = 2f;
}
