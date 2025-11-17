using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NewFoodPackData", menuName = "Data/FoodPack")]
public class FoodPackData : ScriptableObject
{
    public GameObject destroyParticle;
    [Space]
    public bool isUseable = false;
    public PlayerManager.HandGrabTypes handGrabType;
    [Space]
    public float minForce = 0.4f;
    public float maxForce = 0.8f;
    public bool haveWholeIngredient;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]
    public string focusTextKey;
    [Space]

    public AudioClip[] audioClips;
}
