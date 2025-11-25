using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewDrinkData", menuName = "Data/Drink")]
public class DrinkData : ScriptableObject
{
    public string focusTextKey;
    public GameManager.DrinkTypes drinkType;
    public PlayerManager.HandGrabTypes handGrabType;
    [Space]
    public bool isUseable = false;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;

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
    public float soundCooldown = 0.1f;
    public float throwThreshold = 6f;
    public float dropThreshold = 2f;
}
