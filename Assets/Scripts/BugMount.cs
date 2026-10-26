using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class BugMount : MonoBehaviour
{
    public static BugMount currentlyRiddenBug = null;

    [Header("Mount Settings")]
    public Transform playerRig;
    public Transform saddle;
    public Transform dismountPoint;
    public float mountDistance = 2.5f;

    [Header("Riding Speeds")]
    public float moveSpeed = 4.0f;
    public float turnSpeed = 65.0f;
    public Animator bugAnimator;

    [Header("Invert Controls")]
    public bool invertForward = false;
    public bool invertTurn = false;

    [Header("NavMesh Wandering")]
    public float wanderSpeed = 2.0f;
    public float wanderRadius = 15.0f;
    public float minWaitTime = 2.0f;
    public float maxWaitTime = 5.0f;

    [Header("Disable When Riding (Optional)")]
    public Behaviour playerWalkingScript;

    private NavMeshAgent agent;
    private CharacterController playerCharacterController;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();


        // Auto-find player rig if left empty
        if (playerRig == null)
        {
            GameObject rig = GameObject.Find("[BuildingBlock] Camera Rig");
            if (rig != null) playerRig = rig.transform;
        }

        if (playerRig != null)
        {
            playerCharacterController = playerRig.GetComponent<CharacterController>();
        }

        // Randomize initial wander point
        waitTimer = Random.Range(0.5f, 2.0f);
    }

    void Update()
    {
        // 1. Mount / Dismount Toggle
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (currentlyRiddenBug == this)
            {
                DismountBug();
            }
            else if (currentlyRiddenBug == null && playerRig != null)
            {
                float dist = Vector3.Distance(playerRig.position, transform.position);
                if (dist <= mountDistance)
                {
                    MountBug();
                }
            }
        }

        // 2. Drive or Wander
        if (currentlyRiddenBug == this)
        {
            HandleSteering();
        }
        else
        {
            HandleNavMeshWandering();
        }

        // 3. Align body pitch/roll to ground slope
        AlignToGroundNormal();
    }

    void LateUpdate()
    {
        // Pin player to saddle
        if (currentlyRiddenBug == this && playerRig != null)
        {
            playerRig.localPosition = Vector3.zero;
        }
    }

    void MountBug()
    {
        currentlyRiddenBug = this;

        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        if (playerWalkingScript != null)
            playerWalkingScript.enabled = false;

        // Stop NavMesh autonomous pathfinding
        agent.ResetPath();
        agent.updateRotation = false; // We steer manually

        playerRig.SetParent(saddle);
        playerRig.localPosition = Vector3.zero;
        playerRig.localRotation = Quaternion.identity;
    }

    void DismountBug()
    {
        currentlyRiddenBug = null;

        playerRig.SetParent(null);
        if (dismountPoint != null)
        {
            playerRig.position = dismountPoint.position;
            playerRig.rotation = dismountPoint.rotation;
        }

        if (playerCharacterController != null)
            playerCharacterController.enabled = true;

        if (playerWalkingScript != null)
            playerWalkingScript.enabled = true;

        // Re-enable autonomous NavMesh rotation
        agent.updateRotation = true;
        isWaiting = true;
        waitTimer = Random.Range(minWaitTime, maxWaitTime);

        if (bugAnimator != null)
            bugAnimator.SetBool("isWalking", false);
    }

    void HandleSteering()
    {
        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        bool hasInput = stick.magnitude > 0.15f;

        if (hasInput)
        {
            // Turn manually
            float turnMultiplier = invertTurn ? -1f : 1f;
            float turn = stick.x * turnSpeed * turnMultiplier * Time.deltaTime;
            transform.Rotate(0, turn, 0);

            // Move through NavMeshAgent (keeps bug glued to geometry and walls)
            float moveMultiplier = invertForward ? -1f : 1f;
            Vector3 forwardDir = saddle != null ? saddle.forward : transform.forward * moveMultiplier;
            Vector3 moveDirection = forwardDir * (stick.y * moveSpeed * moveMultiplier * Time.deltaTime);

            agent.Move(moveDirection);
        }

        if (bugAnimator != null)
        {
            bugAnimator.SetBool("isWalking", hasInput);
        }
    }

    void HandleNavMeshWandering()
    {
        agent.speed = wanderSpeed;
        agent.updateRotation = true;

        // Check if agent reached its destination
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
                if (bugAnimator != null) bugAnimator.SetBool("isWalking", false);
            }

            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                // Find a random valid point on the NavMesh
                Vector3 newTarget = GetRandomNavMeshLocation(wanderRadius);
                if (newTarget != Vector3.zero)
                {
                    agent.SetDestination(newTarget);
                    isWaiting = false;
                    if (bugAnimator != null) bugAnimator.SetBool("isWalking", true);
                }
            }
        }
    }

    Vector3 GetRandomNavMeshLocation(float radius)
    {
        Vector3 randomDirection = Random.insideUnitSphere * radius;
        randomDirection += transform.position;

        if (NavMesh.SamplePosition(randomDirection, out NavMeshHit hit, radius, NavMesh.AllAreas))
        {
            return hit.position;
        }

        return Vector3.zero;
    }

    // Tilts the bug to match the slope of the hill
    void AlignToGroundNormal()
    {
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down, out RaycastHit hit, 2.0f))
        {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 6f);
        }
    }
}