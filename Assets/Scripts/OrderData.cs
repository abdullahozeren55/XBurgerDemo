using System.Collections.Generic;
using UnityEngine;
using static SauceBottle;

[CreateAssetMenu(fileName = "NewOrderData", menuName = "Data/Order")]
public class OrderData : ScriptableObject
{
    public string OrderName;

    [System.Serializable]
    public struct BurgerOrder { public GameManager.BurgerTypes Type; public int Count; public string OrderKey; }

    [System.Serializable]
    public struct DrinkOrder { public GameManager.DrinkTypes Type; public int Count; public string OrderKey; }

    [System.Serializable]
    public struct SideOrder { public Holder.HolderIngredient Type; public int Count; public string OrderKey; }

    [System.Serializable]
    public struct SauceOrder { public SauceType Type; public int Count; public string OrderKey; }

    [System.Serializable]
    public struct ToyOrder { public ToyType Type; public int Count; public string OrderKey; }

    [Header("Requirements List")]
    // Listelerin boþ olmasý, o kategoriden istek olmadýðý anlamýna gelir.
    public List<BurgerOrder> RequiredBurgers = new List<BurgerOrder>();
    public List<DrinkOrder> RequiredDrinks = new List<DrinkOrder>();
    public List<SideOrder> RequiredSides = new List<SideOrder>();
    public List<SauceOrder> RequiredSauces = new List<SauceOrder>();
    public List<ToyOrder> RequiredToys = new List<ToyOrder>();
}