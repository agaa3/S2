using UnityEngine;

public class Billboard : MonoBehaviour
{
    private Transform vrCamera;

    void Start()
    {
        // Find the VR eye camera
        if (Camera.main != null)
        {
            vrCamera = Camera.main.transform;
        }
        else
        {
            Camera cam = FindObjectOfType<Camera>();
            if (cam != null) vrCamera = cam.transform;
        }
    }

    void LateUpdate()
    {
        if (vrCamera == null) return;

        // Calculate direction from camera to canvas
        Vector3 dir = transform.position - vrCamera.position;

        // LOCK vertical tilt: this stops the text from sliding/floating when you get closer
        dir.y = 0;

        if (dir.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(dir);
        }
    }
}