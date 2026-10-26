using UnityEngine;

public class BugController : MonoBehaviour
{
    public float forwardSpeed = 3.0f;
    public float turnSpeed = 60.0f;
    public Animator bugAnimator;

    void Start()
    {
        if (bugAnimator != null)
            bugAnimator.Play("walk");
    }

    void Update()
    {
        transform.Translate(Vector3.forward * forwardSpeed * Time.deltaTime);

        Vector2 thumbstick = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
        float turn = thumbstick.x * turnSpeed * Time.deltaTime;
        transform.Rotate(0, turn, 0);
    }
}