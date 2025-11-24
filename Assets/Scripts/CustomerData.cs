using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCustomerData", menuName = "Data/Customer")]
public class CustomerData : ScriptableObject
{
    public float minDistance = 1.2f; //min distance to count as in destination
    public float rotationDuration = 0.3f;
    public float throwForce = 0.8f;

    [Header("Footstep Parameters")]
    public AudioClip[] woodClips = default;
    public AudioClip[] stoneClips = default;
    [Space]
    public float FootstepVolume = 1f;
    public float FootstepMinPitch = 0.85f;
    public float FootstepMaxPitch = 1.15f;
}
