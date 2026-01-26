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
    public string GroupName; // Debug için
    public float DelayAfterPreviousGroup; // Önceki grup gittikten (veya geldikten) ne kadar sonra gelsinler?

    // Gruptaki kiþiler ve o günkü rolleri
    public List<CustomerRoleAssignment> Members;
}

[System.Serializable]
public struct CustomerRoleAssignment
{
    public CustomerID ID; // Kim bu?
    public CustomerProfile ProfileForToday; // Bugün ne giyecek, ne yiyecek? (Hasta hali, Mutlu hali vs.)
}