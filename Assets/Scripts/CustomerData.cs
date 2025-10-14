using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCustomerData", menuName = "Data/Customer")]
public class CustomerData : ScriptableObject
{
    public float minDistance = 1.2f; //min distance to count as in destination
    public float pushForce = 1f; //for pushing player
    public float rotationDuration = 0.3f;
    public float throwForce = 0.8f;

    [Header("Footstep Parameters")]
    public AudioClip[] woodClips = default;
    public AudioClip[] metalClips = default;
    public AudioClip[] grassClips = default;
    public AudioClip[] stoneClips = default;
    public AudioClip[] tileClips = default;
    public AudioClip[] gravelClips = default;
    public LayerMask groundTypeLayers;
    public float rayDistance = 2f;

    [Header("Pushing Player Parameters")]
    public LayerMask playerLayer;
    public float rayDistanceForPushingPlayer = 1.5f;
}
