using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class CarManager : MonoBehaviour
{
    public static CarManager Instance;

    [Header("Random Car0 Spawning Settings")]
    public bool CanSpawn;
    public float spawnCooldown = 5f;
    public int maxCarCount = 30;
    private readonly List<GameObject> activeCars = new List<GameObject>();

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

    [Header("Walking NPC Settings")]
    [SerializeField] private NavMeshAgent walking1agent;
    [SerializeField] private Transform destination;

    [Header("Running NPC Settings")]
    public float invoke = 10f;
    public DialogueData talkData;
    [SerializeField] private Animator runAnimator;
    [SerializeField] private NavMeshAgent runningagent;
    [SerializeField] private Transform runDestination;
    private void Awake()
    {
        if (Instance == null)
        {
            // If not, set this instance as the singleton
            Instance = this;
        }
        else
        {
            // If an instance already exists, destroy this one to enforce the singleton pattern
            Destroy(gameObject);
        }

        StartCoroutine(SpawnRandomCar0Repeatedly());

        //walking1agent.SetDestination(destination.position);

        //DialogueManager.Instance.StartSelfDialogue(talkData);

        //Invoke("RunGuy", invoke);

    }

    private void RunGuy()
    {
        CameraManager.Instance.PlayJumpscareEffects(CameraManager.JumpscareType.Small);
        runAnimator.SetBool("run", true);
        runningagent.SetDestination(runDestination.position);
    }

    public Material GetRandomCar0Material() => car0Materials[Random.Range(0, car0Materials.Length)];

    private CarDestinations GetRandomDestination() => carDestinations[Random.Range(0, carDestinations.Length)];

    public void SpawnRandomCar0()
    {
        if (!CanSpawn || activeCars.Count >= maxCarCount) return;

        var chosen = GetRandomDestination();
        GameObject car = Instantiate(car0GO, chosen.spawnPoint.position, chosen.spawnQuaternion);

        activeCars.Add(car);

        var carScript = car.GetComponent<Car>();
        if (carScript != null)
        {
            carScript.DecideDestinations(chosen);
            carScript.OnCarDestroyed += HandleCarDestroyed;
        }
    }

    private void HandleCarDestroyed(GameObject car)
    {
        if (activeCars.Contains(car))
            activeCars.Remove(car);
    }

    private IEnumerator SpawnRandomCar0Repeatedly()
    {
        while (true)
        {
            yield return new WaitForSeconds(Random.Range(spawnCooldown * 0.8f, spawnCooldown * 1.2f));
            SpawnRandomCar0();
        }
    }
}
