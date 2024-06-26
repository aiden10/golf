using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // Reference to the ball's transform
    public Vector3 offset; // Offset from the ball's position
    public float smoothSpeed = 0.125f; // Smoothness of camera movement

    public float rotationSpeed = 3.0f; // Speed of camera rotation
    private float yaw = 0.0f; // Yaw angle for horizontal rotation
    private float pitch = 0.0f; // Pitch angle for vertical rotation
    private bool locked = false;

    void Start()
    {
        // Initialize the offset if not set
        if (offset == Vector3.zero)
        {
            offset = transform.position - target.position;
        }
    }

    void LateUpdate()
    {
        if (target == null) return; // Ensure a target is assigned
        if (Input.GetMouseButtonDown(0))
        {
            locked = !locked;
        }
        if (!locked)
        {
            // Handle camera rotation based on mouse input
            yaw += Input.GetAxis("Mouse X") * rotationSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
            pitch = Mathf.Clamp(pitch, -35, 60); // Clamping the pitch to prevent flipping

            // Calculate the desired position and rotation
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
            Vector3 desiredPosition = target.position + rotation * offset;

            // Smoothly move the camera to the desired position
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;

            // Make the camera look at the target
            transform.LookAt(target.position);
        }
        else
        {
            // Adjust the active ball's direction when the camera is locked and the mouse moves
            yaw += Input.GetAxis("Mouse X") * rotationSpeed;
            target.GetComponent<Ball>().forwardDirection = yaw;
        }
    }

    // Method to switch the target to a new ball
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        yaw = 0.0f;
        pitch = 0.0f;
        // Recalculate the offset based on the new target
        offset = transform.position - target.position;
    }
}
