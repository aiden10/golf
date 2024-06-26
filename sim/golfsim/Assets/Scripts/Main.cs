using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
/* 
Python Side:
Audio for: 
    When the ball is ready to hit 
    When the ball is hit and starts to be tracked
    When the ball goes from ready to not detected
  
TODO:
    Add starting position and hole to each course
    Fix error with 'new' keyword
    Make remove button remove its prefab 
    Make sure that the balls are being created properly after the course loads
    Switch camera to track the active ball after the course loads
    Display the player names over their ball 
    Display the directional arrow under each ball
    Game over overlay which displays the strokes of each player and has a button to go back to the menu scene
*/
public class Main : MonoBehaviour
{
    public List<Ball> activeBalls; // how is this going to be called and how is the ball list going to be passed in?
    public List<Ball> finishedBalls;
    private Scene course;
    public string MovementURL = "http://localhost:8000";
    public string ResetURL = "http://localhost:8000/reset";
    public float forceScale = 1000f;
    public CameraController cameraController;
    public GameObject ballPrefab;
    private Ball activeBall;
    private Course currentCourse;
    void Start()
    {
        List<BallData> playerBallDataList = GameManager.Instance.GetPlayerBallData();
        if (playerBallDataList == null || playerBallDataList.Count == 0)
        {
            Debug.LogError("No player balls found!");
            return;
        }

        currentCourse = FindObjectOfType<Course>(); // initialize the course by finding the Course object in the scene
        if (currentCourse == null)
        {
            Debug.LogError("No Course object found in the scene!");
            return;
        }

        foreach (BallData ballData in playerBallDataList)
        {
            GameObject ballObject = Instantiate(ballPrefab, currentCourse.startLocation, Quaternion.identity);
            Ball ball = ballObject.GetComponent<Ball>();
            ball.Initialize(ballData);
            activeBalls.Add(ball);
        }
        int randomIndex = Random.Range(0, activeBalls.Count);
        activeBall = activeBalls[randomIndex];
        activeBall.data.isTurn = true;
        activeBall.ballBody.isKinematic = false;
    }

    void updateActiveBall()
        // call this after the ball has been hit and stops moving
    {
        activeBall.data.isTurn = false;
        int index = activeBalls.IndexOf(activeBall);
        if (index != -1)
        {
            activeBall = activeBalls[(index + 1) % activeBalls.Count];
            activeBall.data.isTurn = true;
            cameraController.SetTarget(activeBall.transform); 
        }
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
                if (didMove && activeBall.ballBody.velocity.x == 0 && activeBall.ballBody.velocity.y == 0 && activeBall.ballBody.velocity.z == 0) // wait for ball to stop moving before reseting
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
                    // check if ball is in hole
                    if (isBallInHole())
                    {
                        finishedBalls.Add(activeBall);
                        activeBalls.Remove(activeBall);
                    }
                    if (activeBalls.Count > 0)
                    {
                        updateActiveBall();
                    }
                    else
                    {
                        displayWinners();
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
    bool isBallInHole()
    {
        float distanceToHole = Vector3.Distance(activeBall.transform.position, currentCourse.hole);
        return distanceToHole < 0.5f; // Adjust the threshold based on the size of the hole

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
        force *= speed * forceScale;
        force.y = rise * (forceScale / 2);
        Debug.Log($"Force: {force}");
        Debug.Log($"Ball position before applying force: {activeBall.ballBody.position}");
        // Move the ball
        activeBall.ballBody.constraints = RigidbodyConstraints.None;
        // reset velocity
        // activeBall.ballBody.velocity = Vector3.zero; 
        // activeBall.ballBody.angularVelocity = Vector3.zero;
        activeBall.ballBody.AddForce(force, ForceMode.Impulse);
        activeBall.data.strokes++;
    }
    void displayWinners()
        // call when activeBalls is empty
    {
        finishedBalls = finishedBalls.OrderBy(ball => ball.data.strokes).ToList(); // sort the balls by stroke order
        foreach (var ball in finishedBalls)
        {
            Debug.Log($"{ball.data.playerName} - Strokes: {ball.data.strokes}");
        }
        // draw overlay which displays the players and their stroke counts

    }
}
