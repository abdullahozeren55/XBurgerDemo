using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewBurgerIngredientData", menuName = "Data/BurgerIngredient")]
public class BurgerIngredientData : ScriptableObject
{

    public IngredientType ingredientType;
    public float yHeight = 0.1f;
    public float timeToPutOnTray = 0.3f;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public Sprite[] focusImages;
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
