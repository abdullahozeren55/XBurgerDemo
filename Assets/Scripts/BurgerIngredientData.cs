using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBurgerIngredientData", menuName = "Data/BurgerIngredient")]
public class BurgerIngredientData : ScriptableObject
{
    public enum IngredientType
    {
        BOTTOMBUN,
        PATTY,
        CHEESE,
        LETTUCE,
        TOMATO,
        ONION,
        PICKLE,
        TOPBUN
    }

    public bool isUseable = false;
    public bool IsCookable = false; //for audio play only
    [Space]
    public IngredientType ingredientType;
    public PlayerManager.HandGrabTypes handGrabType;
    public float yHeight = 0.1f;
    public float timeToPutOnTray = 0.3f;
    [Space]
    public LayerMask stickableLayers;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]
    public string[] focusTextKeys;
    [Space]
    public ParticleSystem[] dropParticles;
    public ParticleSystem[] throwParticles;
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
