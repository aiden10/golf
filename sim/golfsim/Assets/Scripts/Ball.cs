using UnityEngine;

public class Ball : MonoBehaviour
{
    public BallData data = new BallData();
    public Rigidbody ballBody;
    public GameObject arrow;
    private void Awake()
    {
        // Automatically assign the Rigidbody component of the same GameObject to ballBody
        ballBody = GetComponent<Rigidbody>();

        // Check if the Rigidbody component was found and assign it
        if (ballBody == null)
        {
            Debug.LogError("No Rigidbody component found on this GameObject!");
        }
        if (arrow == null)
        {
            arrow = transform.Find("ArrowParent").gameObject;
            arrow.transform.position = Vector3.zero; 
        }
    }

    public void Initialize(BallData ballData)
    {
        data = ballData;
        // Apply the color to the Renderer material
        GetComponent<Renderer>().material.color = data.color;
        // Reset the Rigidbody properties if necessary
        if (ballBody != null)
        {
            ballBody.isKinematic = true;
        }
    }
}
