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
 * Give ball a forward direction which corresponds to the left side of the camera
 * Represent that direction with an arrow and allow the player to change the direction
 * Add a mini golf course with a hole
 * Allow for multiple balls and players with alternating turns
 * Menu to select different courses or go back to main menu
 * Stroke count
 * Don't always move the ball in a straight line
 * 
 * 
 * Python Side:
 * TTS for: 
 * When the ball is ready to hit 
 * When the ball is hit and starts to be tracked
 * When the ball goes from ready to not detected
 * 
 * 
 * How Unity works:
 * There is a main game loop which is essentially hidden from you.
 * You can call certain functions at the intervals of the loop. Update, FixedUpdate, and LateUpdate.
 * Update is called every frame, FixedUpdate is called every 0.02 seconds (by default), and LateUpdate is called after the other update functions have been called.
 *
 *
 * Menu:
 * Menu script for handling menu logic
 * In the menu after course is chosen: add player button, which creates an rgb color wheel, and a text field for the name.
 * List of ball objects gets generated from the menu script and passed to the main game script.
 *
 *
 * Handling multiple players:
 * I believe this would need a "main" script that handles my game specific logic.
 * Spawn the balls at the start of the game
 * Define a ball class which contains the following:
 *  bool isTurn
 *  int strokes
 *  Color ballColor
 *  float direction // angle 0-360 represented by an arrow
 *  RigidBody ballBody
 *  string name
 * Move the BallMovementFetcher logic of fetching the API every 2 seconds into the main script
 * The main script takes in a list of ball objects which represent the players (activeBalls).
 * Main script finds whose turn it is and applies the movements to that player's ball.
 * While locked, the camera script modifies the current ball's angle.
 * After the ball stops:
 *  - Camera script gets recalled with the next ball.
 *  - The current ball has its isTurn set to false, and the next ball in the list has its isTurn set to True (use modulo to wrap around)
 *  - Check if the ball is in the hole
 * If the ball is in the hole store it in a new list (the order of the list would determine the placements) and remove it from activeBalls. 
 * Game ends after the activeBalls list is empty.
 * 
*/
public class BallMovementFetcher : MonoBehaviour
{
    public string MovementURL = "http://localhost:8000";
    public string ResetURL = "http://localhost:8000/reset";
    public Rigidbody ballRigidbody;
    public float forceScale = 1000f; // Adjust this value to increase/decrease the force

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
        ballRigidbody.angularDrag = 0.2f;
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
                Debug.Log($"Ball Velocity: {ballRigidbody.velocity}");
                bool didMove = ProcessMovements(movementRequest.downloadHandler.text);
                if (didMove && ballRigidbody.velocity.x == 0 && ballRigidbody.velocity.y == 0 && ballRigidbody.velocity.z == 0) // wait for ball to stop moving before reseting
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

            yield return new WaitForSeconds(2); 
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
