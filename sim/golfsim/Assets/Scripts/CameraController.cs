using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // Reference to the ball
    public Vector3 offset = new Vector3(0, 5, -10); // Default offset from the ball's position
    public float smoothSpeed = 0.125f; // Smoothness of camera movement
    public float rotationSpeed = 1.5f; // Speed of camera rotation
    public float zoomSpeed = 10.0f; // Speed of zoom
    public float minZoom = 2.0f; // Minimum zoom distance
    public float maxZoom = 15.0f; // Maximum zoom distance
    private float yaw = 0.0f; // Yaw angle for horizontal rotation
    private float angleChange = 0.0f; // Angle for the forwardDirection
    private float pitch = 20.0f; // Initial pitch angle for vertical rotation
    private float currentZoom = 10.0f; // Initial zoom level
    private bool locked = false;
    private Camera cam;

    void LateUpdate()
    {
        if (target == null) return; // Ensure a target is assigned

        HandleInput();
        HandleCameraPositionAndRotation();
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            locked = !locked;
        }

        if (!locked)
        {
            // Smooth zooming
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);

            // Handle camera rotation based on mouse input
            yaw += Input.GetAxis("Mouse X") * rotationSpeed;
            pitch -= Input.GetAxis("Mouse Y") * rotationSpeed;
            pitch = Mathf.Clamp(pitch, -35, 60); // Clamping the pitch to prevent flipping
        }
        else
        {
            // Adjust the active ball's direction when the camera is locked and the mouse moves
            angleChange += Input.GetAxis("Mouse X") * rotationSpeed;
            target.GetComponent<Ball>().data.forwardDirection = angleChange;
        }
    }

    void HandleCameraPositionAndRotation()
    {
        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPosition = target.position + rotation * new Vector3(0, 0, -currentZoom);
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
        transform.position = smoothedPosition;
        transform.LookAt(target.position + Vector3.up * offset.y * 0.5f);
    }

    // Method to switch the target to a new ball
    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        yaw = 0.0f;
        pitch = 20.0f;
        // Recalculate the offset based on the new target
        currentZoom = offset.magnitude;
    }
}
