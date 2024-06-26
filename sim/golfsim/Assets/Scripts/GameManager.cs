using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    private List<Ball> playerBalls;

    private void Awake()
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

    public void SetPlayerBalls(List<Ball> balls)
    {
        playerBalls = balls;
    }

    public List<Ball> GetPlayerBalls()
    {
        return playerBalls;
    }
}
