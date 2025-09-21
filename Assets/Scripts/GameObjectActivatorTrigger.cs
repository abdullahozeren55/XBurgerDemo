using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameObjectActivatorTrigger : MonoBehaviour
{
    private enum TriggerDirection
    {
        Front, // transform.forward yönü
        Back   // transform.back yönü
    }

    [System.Serializable]
    private class DirectionActivation
    {
        public TriggerDirection direction; // hangi yönden çýkýnca uygulanacak
        public bool setActive;             // objeyi bu yöne çýkarken aktif/pasif yap
    }

    [System.Serializable]
    private class GameObjectCheck
    {
        public GameObject go;
        public DirectionActivation[] directionActivations;
    }

    [SerializeField] private string tagName = "Player";
    [SerializeField] private GameObjectCheck[] allGameObjects;

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(tagName)) return;

        Vector3 exitDir = (other.transform.position - transform.position).normalized;
        float dot = Vector3.Dot(transform.forward, exitDir);
        bool isFront = dot > 0f;

        foreach (GameObjectCheck goCheck in allGameObjects)
        {
            foreach (DirectionActivation dirAct in goCheck.directionActivations)
            {
                bool matchesDirection = (isFront && dirAct.direction == TriggerDirection.Front) ||
                                        (!isFront && dirAct.direction == TriggerDirection.Back);

                if (matchesDirection)
                {
                    // Çýkýþ yönüne göre aktif/pasif ayarla
                    goCheck.go.SetActive(dirAct.setActive);
                }
            }
        }
    }

}
