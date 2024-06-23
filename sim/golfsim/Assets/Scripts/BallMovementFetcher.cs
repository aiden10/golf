using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
/*
 * Unity Side:
 * Make ball lose speed
 * Wait for ball to lose speed before sending the reset request
 * Add a mini golf course with a hole
 * Stroke count
 * 
 * Python Side:
 * TTS for: 
 * when the ball is ready to hit 
 * when the ball is hit and starts to be tracked
 * when the ball goes from ready to not detected
*/
public class BallMovementFetcher : MonoBehaviour
{
    public string MovementURL = "http://localhost:8000";
    public string ResetURL = "http://localhost:8000/reset";
    public Rigidbody ballRigidbody;
    public float movementSpeed = 1.0f; // Control the speed of the movement
    public float forceScale = 200f; // Adjust this value to increase/decrease the force

    private Vector3 targetPosition;

    void Start()
    {
        if (ballRigidbody == null)
        {
            Debug.LogError("Rigidbody not assigned.");
            return;
        }
        ballRigidbody.velocity = Vector3.zero;
        ballRigidbody.angularVelocity = Vector3.zero;

        ballRigidbody.constraints = RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ | RigidbodyConstraints.FreezeRotation;
        targetPosition = ballRigidbody.position;
        StartCoroutine(FetchMovements());
    }

    IEnumerator FetchMovements()
    {
        while (true)
        {
            UnityWebRequest movementRequest = UnityWebRequest.Get(MovementURL);
            yield return movementRequest.SendWebRequest();

            if (movementRequest.result == UnityWebRequest.Result.ConnectionError || movementRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(movementRequest.error);
            }
            else
            {
                bool didMove = ProcessMovements(movementRequest.downloadHandler.text);
                if (didMove)
                {
                    // Send reset request
                    UnityWebRequest resetRequest = UnityWebRequest.Get(ResetURL);
                    yield return resetRequest.SendWebRequest(); // should update this to reset when the ball has stopped moving
                    if (resetRequest.result == UnityWebRequest.Result.ConnectionError || resetRequest.result == UnityWebRequest.Result.ProtocolError)
                    {
                        Debug.LogError(resetRequest.error);
                    }
                    else
                    {
                        Debug.Log("Successfully reset. Ball is ready to be tracked");
                        didMove = false;
                    }
                }
            }

            yield return new WaitForSeconds(3); // Wait for 3 seconds before fetching again
        }
    }

    bool ProcessMovements(string jsonString)
    {
        var json = JSON.Parse(jsonString);
        if (json == null)
        {
            Debug.LogError("Failed to parse JSON");
            return false;
        }

        float speed = json["status"]["speed"].AsFloat;
        string direction = json["status"]["direction"][0];
        float angle = json["status"]["angle"].AsFloat;
        float rise = json["status"]["rise"].AsFloat;
        if (speed != 0 && direction != "None" && angle != 0 && rise != 0) // non default values so ball has actually moved 
        {
            Debug.Log($"Speed: {speed}, Direction: {direction}, Angle: {angle}, Rise: {rise}");
            MoveBall(direction, speed, angle, rise);
            return true;
        }
        return false;
    }

    void MoveBall(string direction, float speed, float angle, float rise)
    {
        Vector3 force = Vector3.zero;
        // Convert direction string to vector
        switch (direction.ToLower())
        {
            case "up":
                force = Vector3.forward;
                break;
            case "down":
                force = Vector3.back;
                break;
            case "left":
                force = Vector3.left;
                break;
            case "right":
                force = Vector3.right;
                break;
        }
        force = Quaternion.Euler(0, angle, 0) * force;
        force *= speed  * forceScale;
        force.y = rise * (forceScale / 2);
        Debug.Log($"Force: {force}");
        Debug.Log($"Ball position before applying force: {ballRigidbody.position}");
        // Move the ball
        ballRigidbody.constraints = RigidbodyConstraints.None;
        // reset velocity
        // ballRigidbody.velocity = Vector3.zero; 
        // ballRigidbody.angularVelocity = Vector3.zero;
        ballRigidbody.AddForce(force, ForceMode.Impulse);
        Debug.Log($"Ball position after applying force: {ballRigidbody.position}");
    }
}
