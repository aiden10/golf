using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using TMPro;

public class MenuController : MonoBehaviour
{ 
    public Transform courseListContent; // Parent transform for course buttons
    public GameObject playerConfigPanel; // Panel for player configuration
    public GameObject playerConfigPrefab; // Prefab for player configuration entries
    public Transform playerConfigContent; // Parent transform for player configuration entries
    public Button startGameButton; // Button to start the game
    public Button addPlayerButton; // Button to add a player
    public Button course1Button;
    public Button course2Button;
    private int playerCount = 0; // Counter for player entries
    private int sceneIndex = 0;
    private List<BallData> playerBallDataList = new List<BallData>();

    private void Start()
    {

        course1Button.onClick.AddListener(() => SetSceneIndex(1));
        course2Button.onClick.AddListener(() => SetSceneIndex(2));

        course1Button.onClick.AddListener(() => OnCourseSelected());
        course2Button.onClick.AddListener(() => OnCourseSelected());
        
        startGameButton.onClick.AddListener(StartGame); // Add listener to start game button
        addPlayerButton.onClick.AddListener(AddPlayerConfig); // Add listener to add player button
        playerConfigPanel.SetActive(false); // Initially hide player config panel
    }

    private void OnCourseSelected()
    {
        playerConfigPanel.SetActive(true); // Show player config panel
        ClearPlayerConfigs(); // Clear any existing player configs
        AddPlayerConfig(); // Add initial player config entry
    }

    private void SetSceneIndex(int index)
    {
        sceneIndex = index;
        Debug.Log("Selected scene index: " + sceneIndex);
    }

    public void AddPlayerConfig()
    {
        GameObject playerConfig = Instantiate(playerConfigPrefab, playerConfigContent);
        playerCount++;
    }

    public void RemovePlayerConfig(GameObject playerConfig)
    {
        Destroy(playerConfig);
        playerCount--;
    }

    private void ClearPlayerConfigs()
    {
        if (playerConfigContent == null)
        {
            Debug.LogError("playerConfigContent is not assigned!");
            return;
        }

        foreach (Transform child in playerConfigContent)
        {
            Destroy(child.gameObject);
        }
        playerCount = 0;
    }

    private void StartGame()
    {
        if (playerConfigContent == null)
        {
            Debug.LogError("playerConfigContent is not assigned!");
            return;
        }

        if (sceneIndex < 0)
        {
            Debug.LogError("Scene index is not set!");
            return;
        }
        playerBallDataList.Clear();
        foreach (Transform child in playerConfigContent)
        {
            TMP_InputField playerNameInput = child.Find("Name Field").GetComponent<TMP_InputField>();
            TMP_InputField colorPicker = child.Find("Color Field").GetComponent<TMP_InputField>();

            if (playerNameInput != null && colorPicker != null)
            {
                string playerName = playerNameInput.text;
                string hexColor = colorPicker.text;

                if (ColorUtility.TryParseHtmlString(hexColor, out Color playerColor))
                {
                    // Instantiate the Ball prefab and configure its properties
                    BallData playerBallData = new BallData
                    {
                        playerName = playerName,
                        color = playerColor,
                        strokes = 0,
                        forwardDirection = 0.0f
                    };
                    playerBallDataList.Add(playerBallData);
                }
                else
                {
                    Debug.LogError($"Invalid color code: {hexColor}");
                }
            }
        }
        GameManager.Instance.SetPlayerBallData(playerBallDataList);
        SceneManager.LoadScene(sceneIndex);
    }
}
