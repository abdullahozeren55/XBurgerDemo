using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewCustomerProfile", menuName = "Data/CustomerProfile")]
public class CustomerProfile : ScriptableObject
{
    public string ProfileID; // "OldMan", "Teenager"
    public Material SkinMaterial; // Görünüþü
    public float WalkSpeed = 3.5f;
    public float ArrivalDistance = 0.5f;

    [System.Serializable]
    public struct PotentialOrder
    {
        public OrderData Order;

        [Header("Normal Interaction")]
        public DialogueData NormalDialogue; // Eskiden OrderDialogue idi, adýný netleþtirdik.

        [Header("Horror Interaction")]
        public DialogueData GlitchDialogue; // Korkunçlu versiyon
        [Range(0f, 100f)] public float GlitchChance; // % kaç ihtimalle korkunç olacak?
    }

    // Bu profildeki müþteri neleri sipariþ edebilir?
    public List<PotentialOrder> PossibleOrders;

    // Yanlýþ sipariþ gelirse ne desin?
    public DialogueData WrongOrderDialogue;

    // Doðru sipariþ gelirse ne desin?
    public DialogueData CorrectOrderDialogue;
}