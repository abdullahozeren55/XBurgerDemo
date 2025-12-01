using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Linq;

public class SauceManager : MonoBehaviour
{
    public static SauceManager Instance;

    public class SauceDropData
    {
        public GameObject drop;
        public float spawnTime;

        public SauceDropData(GameObject drop, float spawnTime)
        {
            this.drop = drop;
            this.spawnTime = spawnTime;
        }
    }


    public GameObject[] sauceDrops = new GameObject[4]; //0 ketchup, 1 mayo, 2 mustard, 3 BBQ
    public float dropLifetime = 10f;

    private List<SauceDropData> activeOutsideDrops = new List<SauceDropData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        float currentTime = Time.time;

        for (int i = activeOutsideDrops.Count - 1; i >= 0; i--)
        {
            SauceDropData data = activeOutsideDrops[i];
            float age = currentTime - data.spawnTime;
            float timeLeft = dropLifetime - age;

            if (age >= dropLifetime)
            {
                Destroy(data.drop);
                activeOutsideDrops.RemoveAt(i);
            }
        }
    }

    public void SpawnDrop(SauceBottle.SauceType sauceType, Vector3 position, Quaternion rotation, Vector3 scale, Transform parent)
    {
        GameObject newDrop = Instantiate(sauceType == SauceBottle.SauceType.Ketchup ? sauceDrops[0] : sauceType == SauceBottle.SauceType.Mayo ? sauceDrops[1] : sauceType == SauceBottle.SauceType.Mustard ? sauceDrops[2] : sauceType == SauceBottle.SauceType.BBQ ? sauceDrops[3] : sauceDrops[4], position, rotation, null);

        newDrop.transform.localScale = scale;

        newDrop.transform.parent = parent;

        activeOutsideDrops.Add(new SauceDropData(newDrop, Time.time));

    }
}
