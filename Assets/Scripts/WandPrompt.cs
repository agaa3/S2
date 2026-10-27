using UnityEngine;

public class WandPrompt : MonoBehaviour
{
    [Header("UI Prompt")]
    public GameObject grabPromptUI;      // Floating canvas on wand
    public float maxDetectionDistance = 6.0f; // Max reach distance
    public float coneAngle = 25.0f;           // How directly you must point

    private Transform rightController;
    private Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Auto-find right controller anchor
        GameObject controllerObj = GameObject.Find("RightControllerAnchor");
        if (controllerObj == null)
            controllerObj = GameObject.Find("RightHandAnchor");

        if (controllerObj != null)
            rightController = controllerObj.transform;
    }

    void Update()
    {
        if (grabPromptUI == null || rightController == null) return;

        // In Meta Interaction SDK, an object becomes Kinematic while grabbed
        bool isGrabbed = (rb != null && rb.isKinematic);

        if (isGrabbed)
        {
            grabPromptUI.SetActive(false);
            return;
        }

        // Check if player's controller is pointing towards this wand
        Vector3 toWand = (transform.position - rightController.position);
        float distance = toWand.magnitude;

        if (distance <= maxDetectionDistance)
        {
            float angle = Vector3.Angle(rightController.forward, toWand.normalized);
            // Show prompt only if pointing within the cone angle
            grabPromptUI.SetActive(angle <= coneAngle);
        }
        else
        {
            grabPromptUI.SetActive(false);
        }
    }
}