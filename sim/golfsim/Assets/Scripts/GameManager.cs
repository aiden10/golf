using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    // Singleton instance for transfering data between scenes
    public static GameManager Instance { get; set; }

    private List<BallData> playerBallDataList = new List<BallData>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetPlayerBallData(List<BallData> ballDataList)
    {
        playerBallDataList = ballDataList;
    }

    public List<BallData> GetPlayerBallData()
    {
        return playerBallDataList;
    }
}
