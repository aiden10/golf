using UnityEngine;

public class TestMovement : MonoBehaviour
{
    public Rigidbody m_Rigidbody; // Public reference to a Rigidbody component
    public float m_Speed = 5f;

    void Start()
    {
        // Check if the Rigidbody component is attached
        if (m_Rigidbody == null)
        {
            Debug.LogError("Rigidbody component is missing from this GameObject. Please attach a Rigidbody component.");
        }
    }

    void FixedUpdate()
    {
        // Check if the Rigidbody component is attached
        if (m_Rigidbody != null)
        {
            // Store user input as a movement vector
            Vector3 m_Input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            Debug.Log("Input Vector: " + m_Input);

            // Apply the movement vector to the current position, which is
            // multiplied by deltaTime and speed for a smooth MovePosition
            if (m_Input != Vector3.zero)
            {
                m_Rigidbody.MovePosition(m_Rigidbody.position + m_Input * Time.deltaTime * m_Speed);
                Debug.Log("Moving to position: " + (m_Rigidbody.position + m_Input * Time.deltaTime * m_Speed));
            }
        }
    }
}
