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


    public AudioClip[] audioClips;
    
}
