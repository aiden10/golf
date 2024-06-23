using UnityEngine;
// clicking should lock/unlock the camera rotation
public class CameraController : MonoBehaviour
{
    public Transform target; // Reference to the ball's transform
    public Vector3 offset; // Offset from the ball's position
    public float smoothSpeed = 0.125f; // Smoothness of camera movement

    public float rotationSpeed = 3.0f; // Speed of camera rotation
    private float yaw = 0.0f; // Yaw angle for horizontal rotation
    private float pitch = 0.0f; // Pitch angle for vertical rotation

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
}
