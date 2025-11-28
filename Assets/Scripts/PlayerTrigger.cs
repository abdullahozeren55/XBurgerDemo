using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTrigger : MonoBehaviour
{
    public enum TriggerType
    {
        CloseTheDoorAndStartNoodlePrepare,
    }

    public TriggerType type;
    public bool shouldTurnOffAfterTrigger = true;

    private Collider col;

    private void Awake()
    {
        col = GetComponent<Collider>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            col.enabled = !shouldTurnOffAfterTrigger;

            if (type == TriggerType.CloseTheDoorAndStartNoodlePrepare)
                NoodleManager.Instance.HandleCloseTheDoorAndStartNoodlePrepare();
        }
    }
}
