using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "HorrorGame/Level Scenario")]
public class LevelScenario : ScriptableObject
{
    public int DayIndex; // Örn: 1. Gün
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

// Müþterileri kodda tanýmak için Enum (String yerine Enum daha güvenli)
public enum CustomerID
{
    FamilyFather,
    FamilyMother,
    FamilyKid,
    OldMan,
    OldWoman,
    AloneKid,
    TeenGirl0,
    TeenGirl1,
}