using System.Collections;
using System.Collections.Generic;
using System.Linq;
using SimpleJSON;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using TMPro;
/* 
Python Side:
Audio for: 
    When the ball is ready to hit 
    When the ball is hit and starts to be tracked
    When the ball goes from ready to not detected
  
TODO:
    Add hole to the course
        Hole detection is already there and I have the red sphere drawn where the hole is on the course.
        I just need to cut a hole out of the course itself and add a flag at that position
        I also could make the whole thing a Hole prefab and add a Start method to the Course class which sets the hole to the prefab's position.
        Adding the hole itself is actually a bit more annoying than I initially thought because there's no easy way to modify models like that within Unity
    Still need to tweak the way the ball feels
    Not sure if the ball should always move in a perfectly straight line or if the angle should be used
*/
public class Main : MonoBehaviour
{
    public List<Ball> activeBalls; // how is this going to be called and how is the ball list going to be passed in?
    public List<Ball> finishedBalls;
    private Scene course;
    public string MovementURL = "http://localhost:8000";
    public string ResetURL = "http://localhost:8000/reset";
    public CameraController cameraController;
    public GameObject ballPrefab;
    public GameObject scoresPrefab;
    private Ball activeBall;
    private Course currentCourse;
    private Camera cam;
    private bool gameOver = false;
    private float forceScale = 15.0f;

    void OnGUI()
    {
        if (!gameOver)
        {
            int spacing = 0;
            GUI.Label(new Rect(10, 10, 100, 20), activeBall.data.playerName + "'s" + " Turn");
            foreach (Ball ball in activeBalls)
            {
                Vector3 ballScreenPos = cam.WorldToScreenPoint(ball.transform.position);
                ballScreenPos.y = Screen.height - ballScreenPos.y;
                GUI.Label(new Rect(ballScreenPos.x, ballScreenPos.y, 100, 20), ball.data.playerName);
                GUI.Label(new Rect(10, 30 + spacing, 100, 50), ball.data.playerName + " strokes: " + ball.data.strokes); // display some info about the active ball
                spacing += 15;
            }
        }
    }

    void Update()
    {
        foreach (Ball ball in activeBalls)
        {
            GameObject arrow = ball.transform.Find("ArrowParent").gameObject;
            arrow.transform.localPosition = Vector3.zero;
            arrow.transform.rotation = Quaternion.Euler(0, ball.data.forwardDirection, 0);
            if (isBallInHole(ball))
            {
                finishedBalls.Add(ball);
                activeBalls.Remove(ball);
                ball.GetComponent<Renderer>().enabled = false; // hide the ball
                ball.ballBody.isKinematic = true; // remove its physics
            }
        }
    }

    void Start()
    {
        // reset the ball's movements
        UnityWebRequest movementRequest = UnityWebRequest.Get(MovementURL);
        movementRequest.SendWebRequest();

        gameOver = false;
        cam = Camera.main; // could also do CameraController.cam and make it public in that class but this works too
        List<BallData> playerBallDataList = GameManager.Instance.GetPlayerBallData();
        if (playerBallDataList == null || playerBallDataList.Count == 0)
        {
            Debug.LogError("No player balls found!");
            SceneManager.LoadScene(0); // return to main menu
        }

        currentCourse = FindObjectOfType<Course>(); // initialize the course by finding the Course object in the scene
        if (currentCourse == null)
        {
            Debug.LogError("No Course object found in the scene!");
            SceneManager.LoadScene(0); // return to main menu
            return;
        }

        foreach (BallData ballData in playerBallDataList)
        {
            GameObject ballObject = Instantiate(ballPrefab, currentCourse.startLocation, Quaternion.identity);
            Ball ball = ballObject.GetComponent<Ball>();
            GameObject arrow = ball.transform.Find("ArrowParent").gameObject;
            arrow.transform.localPosition = Vector3.zero;
            ball.Initialize(ballData);
            ball.ballBody.isKinematic = true; // Ensure it starts kinematic
            ball.ballBody.angularDrag = 0.6f;
            activeBalls.Add(ball);
        }
        int randomIndex = Random.Range(0, activeBalls.Count);
        activeBall = activeBalls[randomIndex];
        activeBall.data.isTurn = true;
        Debug.Log($"Active Ball: {activeBall.data.playerName}");
        activeBall.ballBody.isKinematic = false;
        cameraController.SetTarget(activeBall.transform);
        StartCoroutine(FetchMovements());
    }

    private void updateActiveBall()
    // call this after the ball has been hit and stops moving
    {
        activeBall.data.isTurn = false;
        int index = activeBalls.IndexOf(activeBall);
        if (index != -1)
        {
            activeBall = activeBalls[(index + 1) % activeBalls.Count];
            activeBall.data.isTurn = true;
            Debug.Log($"New Active Ball: {activeBall.data.playerName}");
            cameraController.SetTarget(activeBall.transform);
        }
    }

    IEnumerator FetchMovements()
    {
        while (!gameOver)
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
                if (didMove) // wait for ball to stop moving before reseting
                {
                    yield return new WaitUntil(() => activeBall.ballBody.velocity.magnitude < 0.01f);
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
                    if (activeBalls.Count > 0)
                    {
                        updateActiveBall();
                    }
                    else
                    {
                        gameOver = true;
                        displayWinners();
                    }
                }
            }

            yield return new WaitForSeconds(2);
        }
    }

    private bool ProcessMovements(string jsonString)
    // Parses JSON and extracts ball info
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
        if (speed != 0 && direction != "None" && angle != 0 && rise != 0) // non default values so ball has actually moved and check lastResult to ensure ball only moves once
        {
            Debug.Log($"Speed: {speed}, Direction: {direction}, Angle: {angle}, Rise: {rise}");
            MoveBall(direction, speed, angle, rise);
            return true;
        }
        return false;
    }
    private bool isBallInHole(Ball ball)
    // Check if ball is close enough to hole
    {
        float distanceToHole = Vector3.Distance(ball.transform.position, currentCourse.hole); // Should also check the Z to make sure it's in the hole. Not needed if the course is flat though
        return distanceToHole < 1.5f;

    }
    private void MoveBall(string direction, float speed, float angle, float rise)
    // Applies forces
    {
        Vector3 forward = Quaternion.Euler(0, activeBall.data.forwardDirection, 0) * Vector3.forward;
        Debug.Log($"Initial Forward Vector: {forward}");
        // Rotate the forward direction by the additional angle
        //Vector3 directionForce = Quaternion.Euler(0, angle, 0) * forward;
        //Debug.Log($"Direction Force after Angle: {directionForce}");

        // Scale the direction vector by the speed
        Vector3 force = forward * (speed * forceScale);
        // Add the vertical component
        // force.y = rise * (forceScale / 2);

        // Debug information
        Debug.Log($"Speed: {speed}");
        Debug.Log($"Final Force: {force}");

        activeBall.ballBody.constraints = RigidbodyConstraints.None;
        activeBall.ballBody.AddForce(force, ForceMode.Impulse);
        activeBall.data.strokes++;
    }

    private void displayWinners()
    {
        string scoresString = "";
        finishedBalls = finishedBalls.OrderBy(ball => ball.data.strokes).ToList(); // sort the balls by stroke order
        foreach (var ball in finishedBalls)
        {
            scoresString += $"{ball.data.playerName}: Strokes: {ball.data.strokes}\n"; // could also be formatted in a table
        }
        GameObject overlay = Instantiate(scoresPrefab, Vector3.zero, Quaternion.identity);
        Transform overlayPanel = overlay.transform.Find("Overlay").GetComponent<Transform>();
        TMP_Text text = overlayPanel.Find("Scores Text").GetComponent<TMP_Text>();
        Button menuButton = overlayPanel.Find("Menu Button").GetComponent<Button>();
        text.text = scoresString;
        menuButton.onClick.AddListener(() => 
        {
            Debug.Log("Button Pressed");
            SceneManager.LoadScene(0); // Load the main menu scene
        });
    }
}
