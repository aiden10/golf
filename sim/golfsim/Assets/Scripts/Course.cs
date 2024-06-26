using UnityEngine;

public class Course : MonoBehaviour
{
    // assign these properties to those in the scene
    public Vector3 startLocation;
    public GameObject hole;

    void Start()
    {
        if (hole == null)
        {
            Debug.LogError("Hole object is not assigned");
        }
    }
}
