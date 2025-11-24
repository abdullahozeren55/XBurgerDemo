using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrontDoorTrigger : MonoBehaviour
{
    [SerializeField] private Door[] frontDoors;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Customer"))
        {
            for (var i = 0; i < frontDoors.Length; i++)
            {
                if (!frontDoors[i].isOpened)
                    frontDoors[i].HandleRotation();
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Customer"))
        {
            for (var i = 0; i < frontDoors.Length; i++)
            {
                if (frontDoors[i].isOpened)
                    frontDoors[i].HandleRotation();
            }
        }
    }
}
