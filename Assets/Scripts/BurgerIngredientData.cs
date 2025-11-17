using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBurgerIngredientData", menuName = "Data/BurgerIngredient")]
public class BurgerIngredientData : ScriptableObject
{

    public bool isUseable = false;
    [Space]
    public IngredientType ingredientType;
    public PlayerManager.HandGrabTypes handGrabType;
    public float yHeight = 0.1f;
    public float timeToPutOnTray = 0.3f;
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
}
