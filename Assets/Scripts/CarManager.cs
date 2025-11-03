using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    public static CarManager Instance;

    [System.Serializable]
    public class CarDestinations
    {
        public Transform spawnPoint;
        public Quaternion spawnQuaternion;
        public Transform[] endPoint;
    }

    [SerializeField] private Material[] car0Materials;
    [SerializeField] private CarDestinations[] carDestinations;
    [SerializeField] private GameObject car0GO;

    private CarDestinations currentDestinations;
    private void Awake()
    {
        if (Instance == null)
        {
            // If not, set this instance as the singleton
            Instance = this;

            // Optionally, mark GameManager as not destroyed between scene loads
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // If an instance already exists, destroy this one to enforce the singleton pattern
            Destroy(gameObject);
        }

        StartCoroutine(SpawnEachCar());
    }

    public Material GetRandomCar0Material() => car0Materials[Random.Range(0, car0Materials.Length)];

    private CarDestinations GetRandomDestination() => carDestinations[Random.Range(0, carDestinations.Length)];

    public CarDestinations GetCurrentDestination() => currentDestinations;

    public void SpawnRandomCar0()
    {
        currentDestinations = GetRandomDestination();

        Instantiate(car0GO, currentDestinations.spawnPoint.position, currentDestinations.spawnQuaternion);
    }

    private IEnumerator SpawnEachCar()
    {
        yield return new WaitForSeconds(3);

        currentDestinations = carDestinations[0];

        Instantiate(car0GO, currentDestinations.spawnPoint.position, currentDestinations.spawnQuaternion);

        yield return new WaitForSeconds(3);

        currentDestinations = carDestinations[1];

        Instantiate(car0GO, currentDestinations.spawnPoint.position, currentDestinations.spawnQuaternion);

        yield return new WaitForSeconds(3);

        currentDestinations = carDestinations[2];

        Instantiate(car0GO, currentDestinations.spawnPoint.position, currentDestinations.spawnQuaternion);

        yield return new WaitForSeconds(3);

        currentDestinations = carDestinations[3];

        Instantiate(car0GO, currentDestinations.spawnPoint.position, currentDestinations.spawnQuaternion);

        yield return new WaitForSeconds(3);

        currentDestinations = carDestinations[4];

        Instantiate(car0GO, currentDestinations.spawnPoint.position, currentDestinations.spawnQuaternion);

        yield return new WaitForSeconds(3);

        currentDestinations = carDestinations[5];

        Instantiate(car0GO, currentDestinations.spawnPoint.position, currentDestinations.spawnQuaternion);
    }
}
