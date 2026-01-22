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
        CRISPYCHICKEN,
    }

    [System.Serializable]
    public struct ParticleColorSet
    {
        public Color minColor;
        public Color maxColor;
    }

    [Header("Cooking Particle Colors")]
    public ParticleColorSet rawParticleColors;
    public ParticleColorSet cookedParticleColors;
    public ParticleColorSet burntParticleColors;

    [Header("Smoke Particle Colors")]
    public ParticleColorSet rawSmokeColors;
    public ParticleColorSet cookedSmokeColors;
    public ParticleColorSet burntSmokeColors;

    [Space]

    public bool isUseable = false;
    public bool isSauce = false;
    public bool isThrowable = true;
    public bool isCookable = false;
    public int sauceDropAmount = 5;
    public float targetDropScale = 0.8f;
    public float randomMultiplier = 1.3f;
    public float throwMultiplier = 1f;
    public float timeToCook = 10f;
    public float timeToBurn = 10f;
    public float cookingVariance = 0.15f; // <--- YENÝ: Varsayýlan %15 sapma
    [Space]
    public Material rawMat;
    public Material cookedMat;
    public Material burntMat;
    [Space]
    public IngredientType ingredientType;
    public SauceBottle.SauceType sauceType;
    public PlayerManager.HandGrabTypes handGrabType;
    public PlayerManager.HandRigTypes handRigType = PlayerManager.HandRigTypes.SingleHandGrab;
    public ItemIcon iconData;
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
    public Vector3 grillPositionOffset;
    public Vector3 grillRotationOffset;
    [Space]
    public string[] focusTextKeys;
    [Space]
    public ParticleSystem[] dropParticles;
    public ParticleSystem[] throwParticles;
    [Space]
    public AudioClip[] audioClips;
    public AudioClip cookingSound; // O meþhur COSS sesi
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
    public float cookingSoundVolume = 1f;
    public float cookingSoundMinPitch = 0.85f;
    public float cookingSoundMaxPitch = 1.15f;
    [Space]
    public float soundCooldown = 0.1f;
    public float throwThreshold = 5f;
    public float dropThreshold = 1f;
    [Space]
    public float cookedSoundMultiplier = 0.8f;
    public float burntSoundMultiplier = 0.6f;
}
