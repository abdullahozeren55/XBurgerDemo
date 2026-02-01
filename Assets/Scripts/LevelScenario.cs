using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevelScenerio", menuName = "Data/LevelScenerio")]
public class LevelScenario : ScriptableObject
{
    public int LoopIndex; // Örn: 1. Loop
    public List<CustomerGroupData> Waves; // Müþteri dalgalarý
}

[System.Serializable]
public struct CustomerGroupData
{
    public string GroupName;
    public float DelayAfterPreviousGroup;
    public List<CustomerRoleAssignment> Members;

    public List<ScenarioEventSO> EventsBeforeSpawn;
    public List<ScenarioEventSO> EventsAfterCounterEmpty;

    public AmbientEventPoolSO AmbientPool;
}

[System.Serializable]
public struct CustomerRoleAssignment
{
    public CustomerID ID; // Kim bu?
    public CustomerProfile ProfileForToday; // Bugün ne giyecek, ne yiyecek? (Hasta hali, Mutlu hali vs.)
}