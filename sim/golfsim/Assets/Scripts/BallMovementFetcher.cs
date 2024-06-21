using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

public class BallMovementFetcher : MonoBehaviour
{
    public string MovementURL = "http://localhost:8000";
    public string ResetURL = "http://localhost:8000/reset";
    public Rigidbody ballRigidbody;
    public float movementSpeed = 1.0f; // Control the speed of the movement

    private Vector3 targetPosition;

    void Start()
    {
        if (ballRigidbody == null)
        {
            Debug.LogError("Rigidbody not assigned.");
            return;
        }
        targetPosition = ballRigidbody.position;
        StartCoroutine(FetchMovements());
    }

    void Update()
    {
        // Gradually move the ball towards the target position
        ballRigidbody.position = Vector3.Lerp(ballRigidbody.position, targetPosition, movementSpeed * Time.deltaTime);
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
                    yield return resetRequest.SendWebRequest();
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

    bool ProcessMovements(string json)
    {
        Debug.Log("Processing movements JSON: " + json);
        MovementData data = JsonUtility.FromJson<MovementData>(json);
        if (data.movements.Count > 0)
        {
            Movement move = data.movements.Last(); // move to position of last movement (for now)
            Debug.Log($"Applying movement: x={move.x}, y={move.y}, radius={move.radius}");
            targetPosition = new Vector3(move.x, 0, move.y); // Assuming 2D movement
            return true;
        }
        return false;
    }

    [System.Serializable]
    public class Movement
    {
        public float x;
        public float y;
        public float radius;
    }

    [System.Serializable]
    public class MovementData
    {
        public bool status;
        public List<Movement> movements;
    }
}
