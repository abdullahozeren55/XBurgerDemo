using System.Collections;
using System.Collections.Generic;
using Unity.Burst.CompilerServices;
using UnityEngine;

public class SauceCollision : MonoBehaviour
{
    public SauceBottle.SauceType sauceType;

    private ParticleSystem ps;
    private List<ParticleCollisionEvent> collisionEvents;

    void Start()
    {
        ps = GetComponent<ParticleSystem>();
        collisionEvents = new List<ParticleCollisionEvent>();
    }

    void OnParticleCollision(GameObject other)
    {
        if (sauceType != SauceBottle.SauceType.Water && other.CompareTag("BurgerSauceArea"))
        {
            GameManager.Instance.AddSauceToTray(sauceType);
        }
        else if (sauceType == SauceBottle.SauceType.Water && other.CompareTag("Noodle"))
        {
            NoodleManager.Instance.AddWaterToNoodle();
        }
        else
        {
            int count = ps.GetCollisionEvents(other, collisionEvents);

            for (int i = 0; i < count; i++)
            {
                Vector3 hitPoint = collisionEvents[i].intersection;
                Vector3 normal = collisionEvents[i].normal;

                // Normal yönüne göre rotation hesapla
                Quaternion finalRotation = Quaternion.LookRotation(normal) * Quaternion.Euler(0, 180, 0);

                // Objeyi doðru ebeveyne yerleþtir
                Transform decalParent = other.transform.Find("DecalParent");
                Transform parentToUse = decalParent != null ? decalParent : other.transform;

                SauceManager.Instance.SpawnDrop(sauceType, hitPoint, finalRotation, parentToUse);
            }
        }
    }

}
