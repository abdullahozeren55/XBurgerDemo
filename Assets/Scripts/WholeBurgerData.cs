using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWholeBurgerData", menuName = "Data/WholeBurger")]
public class WholeBurgerData : ScriptableObject
{
    public bool isUseable = false;
    public bool isThrowable = true;
    public float throwMultiplier = 1f;
    [Space]
    public PlayerManager.HandGrabTypes handGrabType;
    public Sprite icon;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    public Vector3 boxPlacementPositionOffset;
    public Vector3 boxPlacementRotationOffset;
    [Space]
    public string[] focusTextKeys;
}
