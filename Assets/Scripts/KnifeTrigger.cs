using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static Cookable;

public class KnifeTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WholeIngredient"))
            other.GetComponent<WholeIngredient>().Slice();
        else if (other.CompareTag("FoodPack"))
            other.GetComponent<FoodPack>().Open();
    }
}
