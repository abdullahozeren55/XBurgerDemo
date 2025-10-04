using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWholeIngredientData", menuName = "Data/WholeIngredient")]
public class WholeIngredientData : ScriptableObject
{
    public GameObject destroyParticle;
    [Space]
    public Sprite focusImage;
    public bool isUseable = false;
    public PlayerManager.HandGrabTypes handGrabType;
    [Space]
    public float minForce = 0.2f;
    public float maxForce = 0.4f;
    public int objectAmount = 4;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]
    public AudioClip[] audioClips;
}
