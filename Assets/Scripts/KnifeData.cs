using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[CreateAssetMenu(fileName = "NewKnifeData", menuName = "Data/Knife")]
public class KnifeData : ScriptableObject
{
    public bool isUseable = true;
    public PlayerManager.HandGrabTypes handGrabType;
    [Space]
    public Vector3 grabPositionOffset;
    public Vector3 grabRotationOffset;
    [Space]
    public Vector3 grabLocalPositionOffset;
    public Vector3 grabLocalRotationOffset;
    [Space]
    public Vector3 usePositionOffset;
    public Vector3 useRotationOffset;
    [Space]
    public Vector3 useLocalPositionOffset;
    public Vector3 useLocalRotationOffset;
    [Space]
    public float usingFOV = 50f;
    public float timeToUse = 0.3f;
    [Space]
    public string focusTextKey;
    public AudioClip[] audioClips;
}
