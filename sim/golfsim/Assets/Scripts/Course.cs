using UnityEngine;

public class Course : MonoBehaviour
{
    public Vector3 startLocation; // Start location of the course
    public Vector3 hole; // Position of the hole

    private void OnDrawGizmos()
    {
        // Draw a sphere at the start location for visualization
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(startLocation, 0.5f);

        // Draw a sphere at the hole position for visualization
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(hole, 0.5f);
    }
}
