using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class MenuController : MonoBehaviour
{ 
    // References to the UI elements
    public Transform courseListContent; 
    public GameObject playerConfigPanel; 
    public GameObject playerConfigPrefab;
    public Transform playerConfigContent;
    public Button startGameButton; 
    public Button addPlayerButton; 
    public Button course1Button;
    public Button course2Button;

    private int playerCount = 0; 
    private int sceneIndex = 0;
    private List<BallData> playerBallDataList = new List<BallData>();

    private void Start()
    {
        // Set button events
        course1Button.onClick.AddListener(() => SetSceneIndex(1));
        course2Button.onClick.AddListener(() => SetSceneIndex(2));

        course1Button.onClick.AddListener(() => OnCourseSelected());
        course2Button.onClick.AddListener(() => OnCourseSelected());
        
        startGameButton.onClick.AddListener(StartGame); 
        addPlayerButton.onClick.AddListener(AddPlayerConfig); 
        playerConfigPanel.SetActive(false); // Initially hide player config panel
    }

    private void OnCourseSelected()
    {
        playerConfigPanel.SetActive(true); // Show player config panel
        ClearPlayerConfigs(); // This actually isn't necessary now that I think about it. But I'll leave it anyways
        AddPlayerConfig(); // Add initial player config entry
    }

    private void SetSceneIndex(int index)
    {
        sceneIndex = index;
        Debug.Log("Selected scene index: " + sceneIndex);
    }

    public void AddPlayerConfig() // Adds a player config prefab to the canvas
    {
        GameObject playerConfig = Instantiate(playerConfigPrefab, playerConfigContent);
        Button removeButton = playerConfig.transform.Find("Remove Button").GetComponent<Button>();
        removeButton.onClick.AddListener(() => RemovePlayerConfig(playerConfig));
        playerCount++;
    }

    public void RemovePlayerConfig(GameObject playerConfig)
    {
        Destroy(playerConfig); // Destroys the prefab it's passed
        playerCount--;
    }

    private void ClearPlayerConfigs() // Gets rid of all the player config prefabs. In hindsight, this doesn't actually need to exist
    {
        foreach (Transform child in playerConfigContent)
        {
            Destroy(child.gameObject);
        }
        playerCount = 0;
    }
    
    private Color parseHexColor(string hexCode) // Tries to parse the hex code, if it's not valid it returns white
    { // Idealy I'd use a color wheel but that would be more UI work I don't want to do
        if (ColorUtility.TryParseHtmlString(hexCode, out Color color))
        {
            return color;
        }
        else
        {
            return Color.white; // default
        }
    }

    private void StartGame() 
    {
        playerBallDataList.Clear(); // Clear any preexisting data to be safe
        foreach (Transform child in playerConfigContent) 
        {
            // Get the data from the text fields
            TMP_InputField playerNameInput = child.Find("Name Field").GetComponent<TMP_InputField>();
            TMP_InputField colorPicker = child.Find("Color Field").GetComponent<TMP_InputField>();

            string playerName = playerNameInput.text;
            string hexColor = colorPicker.text;
            Color playerColor = parseHexColor(hexColor);
            
            // Instantiate the Ball prefab and configure its properties
            BallData playerBallData = new BallData // Create the ball data object
            {
                playerName = playerName,
                color = playerColor,
                strokes = 0,
                forwardDirection = 90.0f
            };
            playerBallDataList.Add(playerBallData); 
        }
        GameManager.Instance.SetPlayerBallData(playerBallDataList); // Store it in the singleton class 
        SceneManager.LoadScene(sceneIndex); // Load the course
    }
}
