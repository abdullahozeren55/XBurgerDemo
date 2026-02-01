using System;
using UnityEngine;

[Serializable]
public class WeightedScenarioEvent
{
    public ScenarioEventSO ev;
    [Min(1)] public int weight = 1;
}
