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
        TOPBUN,
    }

    public bool isUseable = false;
    public bool isSauce = false;
    public int sauceDropAmount = 5;
    public GameObject drop;
    [Space]
    public IngredientType ingredientType;
    public SauceBottle.SauceType sauceType;
    public PlayerManager.HandGrabTypes handGrabType;
    public float yHeight = 0.1f;
    public float timeToPutOnTray = 0.3f;
    public Vector3 localScaleWhenGrabbed;
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
    public float traySoundVolume = 1f;
    public float traySoundMinPitch = 0.85f;
    public float traySoundMaxPitch = 1.15f;
    [Space]
    public float soundCooldown = 0.1f;
    public float throwThreshold = 6f;
    public float dropThreshold = 2f;
    [Space]
    public float cookedSoundMultiplier = 0.8f;
    public float burntSoundMultiplier = 0.6f;
}
