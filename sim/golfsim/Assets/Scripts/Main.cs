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

    Game over overlay which displays the strokes of each player and has a button to go back to the menu scene
        Create overlay prefab with canvas with a menu button and a text component
        Update the displayWinners method:
            void displayWinners()
            {
                string scoresString;
                finishedBalls = finishedBalls.OrderBy(ball => ball.data.strokes).ToList(); // sort the balls by stroke order
                foreach (var ball in finishedBalls)
                {
                    scoreString += $"{ball.data.playerName}\nStrokes: {ball.data.strokes}\n"; // could also be formatted in a table
                }
                I don't know how to force it to be on the screen directly 
                I don't want it to be a camera pointing at it, I want it to be like a pause screen or something
                GameObject overlay = Instantiate(overlayPrefab); 
                Text text = overlay.GetComponent<TMP_Text>();
                Button menuButton = overlay.transform.Find("Menu Button").GetComponent<Button>();
                text.text = scoreString;
                startGameButton.onClick.AddListener(() => { // No idea if this javascript syntax is valid or not
                    SceneManager.LoadScene(0);
                }); 

            }        
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
    public GameObject scoresPrefab;
    private Ball activeBall;
    private Course currentCourse;
    private Camera cam;
    private bool gameOver = false;

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
        }
    }

    void Start()
    {
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
                        activeBall.GetComponent<Renderer>().enabled = false; // hide the ball
                        activeBall.ballBody.isKinematic = true; // remove its physics
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
        if (speed != 0 && direction != "None") // non default values so ball has actually moved 
        {
            Debug.Log($"Speed: {speed}, Direction: {direction}, Angle: {angle}, Rise: {rise}");
            MoveBall(direction, speed, angle, rise);
            return true;
        }
        return false;
    }
    private bool isBallInHole()
    // Check if ball is close enough to hole
    {
        float distanceToHole = Vector3.Distance(activeBall.transform.position, currentCourse.hole); // Should also check the Z to make sure it's in the hole. Not needed if the course is flat though
        return distanceToHole < 0.5f;

    }
    private void MoveBall(string direction, float speed, float angle, float rise)
    // Applies forces
    {
        // apply force in the ball's forwardDirection
        float radians = activeBall.data.forwardDirection * Mathf.Deg2Rad;
        Vector3 force = new Vector3(Mathf.Sin(radians), 0, Mathf.Cos(radians));

        force = Quaternion.Euler(0, angle, 0) * force;
        force *= speed * forceScale;
        force.y = rise * (forceScale / 2);
        Debug.Log($"Force: {force}");

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
