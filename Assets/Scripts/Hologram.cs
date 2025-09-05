using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Hologram : MonoBehaviour
{
    private enum HologramType
    {
        HouseNoodle,
        HouseSaucePack,
        HouseKettle
    }

    private Collider col;

    [SerializeField] private HologramType hologramType;

    private void Awake()
    {
        col = GetComponent<Collider>();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Noodle") && hologramType == HologramType.HouseNoodle)
        {
            col.enabled = false;

            other.GetComponent<Noodle>().PutOnHologram(transform.position, transform.rotation);
        }
        else if (other.CompareTag("SaucePack") && hologramType == HologramType.HouseSaucePack)
        {
            col.enabled = false;

            other.GetComponent<SaucePack>().PutOnHologram(transform.position, transform.rotation);
        }
        else if (other.CompareTag("Kettle") && hologramType == HologramType.HouseKettle)
        {
            col.enabled = false;

            other.GetComponent<Kettle>().PutOnHologram(transform.position, transform.rotation);
        }
    }
}
