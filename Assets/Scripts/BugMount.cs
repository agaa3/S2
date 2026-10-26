using UnityEngine;

public class BugMount : MonoBehaviour
{
    [Header("Mount Settings")]
    public Transform playerRig;          // Drag your OVRCameraRig here
    public Transform saddle;             // Drag the Saddle empty object here
    public Transform dismountPoint;      // Drag the DismountPoint here
    public float mountDistance = 2.5f;   // How close you must be to mount

    [Header("Movement")]
    public float moveSpeed = 4.0f;
    public float turnSpeed = 65.0f;
    public Animator bugAnimator;

    [Header("Invert Controls (jeœli model jest odwrócony)")]
    public bool invertForward = false;
    public bool invertTurn = false;

    [Header("Disable When Riding (Optional)")]
    public Behaviour playerWalkingScript;

    private bool isRiding = false;
    private CharacterController playerCharacterController;

    void Start()
    {
        // SprawdŸ czy gracz ma CharacterController, by wy³¹czyæ go podczas jazdy
        if (playerRig != null)
        {
            playerCharacterController = playerRig.GetComponent<CharacterController>();
        }
    }

    void Update()
    {
        // 1. Check for "A" Button press (Right Controller "A" button)
        if (OVRInput.GetDown(OVRInput.Button.One))
        {
            if (!isRiding)
            {
                float dist = Vector3.Distance(playerRig.position, transform.position);
                if (dist <= mountDistance)
                {
                    MountBug();
                }
            }
            else
            {
                DismountBug();
            }
        }

        // 2. Drive the bug if mounted
        if (isRiding)
        {
            HandleSteering();
        }
    }

    // TO LIKWIDUJE OVERSHOOT:
    // LateUpdate wykonuje siê na samym koñcu klatki (po wszystkich innych skryptach chodzenia)
    // i zamra¿a lokaln¹ pozycjê gracza w siodle na (0,0,0)
    void LateUpdate()
    {
        if (isRiding && playerRig != null)
        {
            playerRig.localPosition = Vector3.zero;
        }
    }

    void MountBug()
    {
        isRiding = true;

        // Wy³¹czamy skrypty chodzenia i fizykê gracza
        if (playerCharacterController != null)
            playerCharacterController.enabled = false;

        if (playerWalkingScript != null)
            playerWalkingScript.enabled = false;

        // Dok³adnie tak jak chcia³eœ: reset do siod³a z Quaternion.identity
        playerRig.SetParent(saddle);
        playerRig.localPosition = Vector3.zero;
        playerRig.localRotation = Quaternion.identity;
    }

    void DismountBug()
    {
        isRiding = false;

        // Unparent player from bug
        playerRig.SetParent(null);
        if (dismountPoint != null)
        {
            playerRig.position = dismountPoint.position;
            playerRig.rotation = dismountPoint.rotation;
        }

        // Przywracamy fizykê i chodzenie na nogach
        if (playerCharacterController != null)
            playerCharacterController.enabled = true;

        if (playerWalkingScript != null)
            playerWalkingScript.enabled = true;

        // Stop walking animation
        if (bugAnimator != null)
            bugAnimator.SetBool("isWalking", false);
    }

    void HandleSteering()
    {
        Vector2 stick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);

        // Ignoruj drobny dryf ga³ki
        bool hasInput = stick.magnitude > 0.15f;

        if (hasInput)
        {
            // 1. Skrêcanie
            float turnMultiplier = invertTurn ? -1f : 1f;
            float turn = stick.x * turnSpeed * turnMultiplier * Time.deltaTime;
            transform.Rotate(0, turn, 0);

            // 2. Ruch w przód/ty³
            float moveMultiplier = invertForward ? -1f : 1f;
            Vector3 moveDirection = saddle.forward * (stick.y * moveSpeed * moveMultiplier * Time.deltaTime);

            moveDirection.y = 0;
            transform.position += moveDirection;
        }

        if (bugAnimator != null)
        {
            bugAnimator.SetBool("isWalking", hasInput);
        }
    }
}