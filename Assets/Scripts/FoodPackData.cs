using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFoodPackData", menuName = "Data/FoodPack")]
public class FoodPackData : ScriptableObject
{
    public GameObject destroyParticle;
    public float minForce = 0.4f;
    public float maxForce = 0.8f;
    public bool haveWholeIngredient;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;

    public AudioClip[] audioClips;
}
