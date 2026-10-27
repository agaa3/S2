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

    [Header("Disable When Riding")]
    public GameObject locomotionObjectToDisable; // Przeci¹gnij tu obiekt 'Locomotor'
    public Behaviour playerWalkingScript;        // Opcjonalne

    [Header("Ground Alignment")]
    public LayerMask groundLayer = ~0;           // Ustaw na warstwê terenu/ziemi

    private NavMeshAgent agent;
    private CharacterController playerCharacterController;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        if (bugAnimator == null)
        {
            bugAnimator = GetComponentInChildren<Animator>();
        }

        // Auto-szukanie riga gracza jeœli slot jest pusty
        if (playerRig == null)
        {
            GameObject rig = GameObject.Find("[BuildingBlock] Camera Rig");
            if (rig != null) playerRig = rig.transform;
        }

        if (playerRig != null)
        {
            playerCharacterController = playerRig.GetComponent<CharacterController>();
        }

        waitTimer = Random.Range(0.5f, 2.0f);
    }

    void Update()
    {
        // 1. Wsiadanie / Zsiadanie pod przyciskiem 'A'
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

        // 2. Sterowanie lub AI wêdrowania
        if (currentlyRiddenBug == this)
        {
            HandleSteering();
        }
        else
        {
            HandleNavMeshWandering();
        }

        // 3. Dopasowanie k¹ta cia³a do zbocza terenu
        AlignToGroundNormal();
    }

    void LateUpdate()
    {
        // Pasy bezpieczeñstwa: blokada pozycji w siodle na poziomie lokalnym
        if (currentlyRiddenBug == this && playerRig != null)
        {
            playerRig.localPosition = Vector3.zero;
        }
    }

    void MountBug()
    {
        currentlyRiddenBug = this;

        // Wy³¹czenie poruszania siê i obracania praw¹ ga³k¹
        if (locomotionObjectToDisable != null)
            locomotionObjectToDisable.SetActive(false);

        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        if (playerWalkingScript != null)
            playerWalkingScript.enabled = false;

        // Zatrzymanie nawigacji autonomicznej
        agent.ResetPath();
        agent.updateRotation = false; // Rêczne sterowanie rotacj¹

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

        // Przywrócenie chodzenia i prawej ga³ki
        if (locomotionObjectToDisable != null)
            locomotionObjectToDisable.SetActive(true);

        if (playerCharacterController != null)
            playerCharacterController.enabled = true;

        if (playerWalkingScript != null)
            playerWalkingScript.enabled = true;

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
            // 1. Skrêcanie
            float turnMultiplier = invertTurn ? -1f : 1f;
            float turn = stick.x * turnSpeed * turnMultiplier * Time.deltaTime;
            transform.Rotate(0, turn, 0);

            // 2. Ruch w przód/ty³
            float moveMultiplier = invertForward ? -1f : 1f;
            Vector3 forwardDir = (saddle != null) ? saddle.forward : transform.forward;
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

        // SprawdŸ czy agent dotar³ do celu
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.1f)
        {
            if (!isWaiting)
            {
                isWaiting = true;
                waitTimer = Random.Range(minWaitTime, maxWaitTime);
            }

            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                Vector3 newTarget = GetRandomNavMeshLocation(wanderRadius);
                if (newTarget != Vector3.zero)
                {
                    agent.SetDestination(newTarget);
                    isWaiting = false;
                }
            }
        }

        // Animacja oparta o faktyczny ruch agenta
        if (bugAnimator != null)
        {
            bool moving = agent.velocity.magnitude > 0.15f;
            bugAnimator.SetBool("isWalking", moving);
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

    void AlignToGroundNormal()
    {
        // Ignorujemy triggery i strzelamy w dó³ z niewielkiego podwy¿szenia
        if (Physics.Raycast(transform.position + Vector3.up * 0.6f, Vector3.down, out RaycastHit hit, 2.5f, groundLayer, QueryTriggerInteraction.Ignore))
        {
            Quaternion targetRotation = Quaternion.FromToRotation(transform.up, hit.normal) * transform.rotation;
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 6f);
        }
    }
}