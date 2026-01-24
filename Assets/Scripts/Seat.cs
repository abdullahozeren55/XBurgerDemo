using UnityEngine;
using System.Collections.Generic;

public class Seat : MonoBehaviour
{
    [Header("Settings")]
    // Oturulacak fiziksel noktalar (Transformlar).
    // Tekli sandalye için 1 tane, Koltuk için 2 tane nokta ekle.
    public List<Transform> sitPoints;

    // Þu an bu koltukta kimler oturuyor?
    private Dictionary<Transform, ICustomer> occupants = new Dictionary<Transform, ICustomer>();

    public bool IsFullyOccupied => occupants.Count >= sitPoints.Count;

    // Boþ bir nokta ver
    public Transform GetFreePoint()
    {
        foreach (var point in sitPoints)
        {
            if (!occupants.ContainsKey(point)) return point;
        }
        return null;
    }

    // Müþteriyi oturt
    public bool TryOccupy(ICustomer customer, out Transform sitTransform)
    {
        sitTransform = null;
        Transform freePoint = GetFreePoint();

        if (freePoint != null)
        {
            occupants.Add(freePoint, customer);
            sitTransform = freePoint;
            return true;
        }
        return false;
    }

    // Müþteriyi kaldýr
    public void Release(ICustomer customer)
    {
        // Dictionary'den value ile key bulma (biraz tersten ama güvenli)
        Transform keyToRemove = null;
        foreach (var pair in occupants)
        {
            if (pair.Value == customer)
            {
                keyToRemove = pair.Key;
                break;
            }
        }

        if (keyToRemove != null)
        {
            occupants.Remove(keyToRemove);
        }
    }
}