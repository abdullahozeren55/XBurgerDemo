using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static SauceBottle;

public class MopTrigger : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Stain"))
            other.GetComponent<Stain>().Clear();
    }
}
