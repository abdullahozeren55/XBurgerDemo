using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Collider))]
public class Car : MonoBehaviour
{
    [Header("AI Settings")]
    public Transform target;
    public Transform castOrigin;
    public float targetReachDistance = 1f;
    public float stopDistance = 5f;
    public Vector3 boxHalfExtents = new Vector3(1f, 0.5f, 1f);
    public LayerMask obstacleMask;

    [Header("Animation Settings")]
    private Animator animator;
    public float turnThreshold = 2f;   // kaç derece fark olunca dönüyor saysýn
    public float turnCooldown = 0.1f;  // anim spam’ini engeller

    [Header("Material Settings")]
    public MeshRenderer[] renderers; // only the color change ones

    private NavMeshAgent agent;
    private Collider selfCollider;
    private bool isStoppedByObstacle;
    private bool obstacleDetected;
    private float lastTurnTime;

    private int currentIndex;

    private CarManager.CarDestinations destinations;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        selfCollider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        ChangeCarColor(CarManager.Instance.GetRandomCar0Material());
        DecideDestinations(CarManager.Instance.GetCurrentDestination());
    }

    private void Update()
    {
        HandleObstacleDetection();
        HandleTurnAnimation();
        HandleReachingTarget();
    }

    private void DecideDestinations(CarManager.CarDestinations dest)
    {
        destinations = dest;

        currentIndex = 0;

        target = destinations.endPoint[currentIndex];

        if (!agent.pathPending)
            agent.SetDestination(target.position);
    }

    private void ChangeCarColor(Material mat)
    {
        foreach (Renderer ren in renderers)
        {
            var mats = ren.materials;

            if (mats.Length > 0)
            {
                mats[0] = mat;
                ren.materials = mats;
            }
        }
    }

    private void HandleReachingTarget()
    {
        if (Vector3.Distance(transform.position, target.position) < targetReachDistance)
        {
            if (currentIndex < destinations.endPoint.Length - 1)
            {
                currentIndex++;

                target = destinations.endPoint[currentIndex];

                if (!agent.pathPending)
                    agent.SetDestination(target.position);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }

    private void HandleObstacleDetection()
    {
        Vector3 origin = castOrigin ? castOrigin.position : transform.position + Vector3.up * 0.5f;
        Quaternion rotation = transform.rotation;
        Vector3 direction = transform.forward;

        bool hitForward = Physics.BoxCast(origin, boxHalfExtents, direction, out RaycastHit hit, rotation, stopDistance, obstacleMask, QueryTriggerInteraction.Ignore);

        Collider[] hits = Physics.OverlapBox(origin + direction * stopDistance * 0.5f, boxHalfExtents, rotation, obstacleMask, QueryTriggerInteraction.Ignore);

        bool hitInside = false;
        foreach (var col in hits)
        {
            if (col != selfCollider)
            {
                hitInside = true;
                break;
            }
        }

        obstacleDetected = hitForward || hitInside;

        if (obstacleDetected && !isStoppedByObstacle)
        {
            agent.isStopped = true;
            isStoppedByObstacle = true;
        }
        else if (!obstacleDetected && isStoppedByObstacle)
        {
            agent.isStopped = false;
            isStoppedByObstacle = false;
        }
    }

    private void HandleTurnAnimation()
    {
        if (animator == null || agent.velocity.sqrMagnitude < 0.1f)
        {
            animator?.SetBool("turnLeft", false);
            animator?.SetBool("turnRight", false);
            return;
        }

        // Arabanýn o an baktýðý yön
        Vector3 forward = transform.forward;
        // NavMesh'in gitmeye çalýþtýðý yön
        Vector3 desired = agent.desiredVelocity.normalized;

        // Yön farkýný hesapla
        float angle = Vector3.SignedAngle(forward, desired, Vector3.up);

        if (Time.time - lastTurnTime > turnCooldown)
        {
            if (angle > turnThreshold)
            {
                animator.SetBool("turnRight", true);
                animator.SetBool("turnLeft", false);
            }
            else if (angle < -turnThreshold)
            {
                animator.SetBool("turnLeft", true);
                animator.SetBool("turnRight", false);
            }
            else
            {
                animator.SetBool("turnLeft", false);
                animator.SetBool("turnRight", false);
            }

            lastTurnTime = Time.time;
        }
    }

    private void OnDrawGizmos()
    {
        if (!Application.isPlaying)
            obstacleDetected = false;

        Gizmos.color = obstacleDetected ? Color.red : Color.green;
        Vector3 origin = castOrigin ? castOrigin.position : transform.position + Vector3.up * 0.5f;
        Quaternion rotation = transform.rotation;
        Matrix4x4 cubeMatrix = Matrix4x4.TRS(origin + transform.forward * stopDistance * 0.5f, rotation, boxHalfExtents * 2f);
        Gizmos.matrix = cubeMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(origin, origin + transform.forward * stopDistance);
    }
}
