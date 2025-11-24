using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCFootstepChangePoint : MonoBehaviour
{
    [SerializeField] private ICustomer.Footstep footstep1;
    [SerializeField] private ICustomer.Footstep footstep2;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Customer"))
        {
            ICustomer cus = other.GetComponent<ICustomer>();

            if (cus.CurrentFootstep == footstep1)
                cus.CurrentFootstep = footstep2;
            else if (cus.CurrentFootstep == footstep2)
                cus.CurrentFootstep = footstep1;
        }
    }
}
