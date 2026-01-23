using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KnifeTrigger : MonoBehaviour
{
    public bool IsJustThrowed = false;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("WholeIngredient"))
            other.GetComponent<WholeIngredient>()?.Slice(IsJustThrowed);
        else if (other.CompareTag("FoodPack"))
            other.GetComponent<FoodPack>()?.Open(IsJustThrowed);
        else if (other.CompareTag("Balloon"))
            other.GetComponent<Balloon>()?.PopBalloon();
    }
}
