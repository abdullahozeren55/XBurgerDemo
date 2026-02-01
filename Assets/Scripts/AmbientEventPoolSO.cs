using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Ambient Event Pool")]
public class AmbientEventPoolSO : ScriptableObject
{
    [Header("Enable")]
    public bool enabled = true;

    [Header("Chance (per customer leave)")]
    [Range(0f, 1f)]
    public float chancePerCustomerLeave = 0.25f;

    [Header("Cooldown (seconds)")]
    public Vector2 cooldownSecondsRange = new Vector2(20f, 45f);

    [Header("Selection")]
    public bool avoidImmediateRepeat = true;

    public List<WeightedScenarioEvent> events = new();
}
